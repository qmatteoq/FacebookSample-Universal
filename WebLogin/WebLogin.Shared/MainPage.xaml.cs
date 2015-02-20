using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Security.Authentication.Web;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Facebook;
using WebLogin.Entities;

namespace WebLogin
{
    public partial class MainPage
    {
        public string AccessToken;
        public DateTime TokenExpiry;
        public string ClientId = "411817365649267";

        private async Task Login()
        {
            //Client ID of the Facebook App (retrieved from the Facebook Developers portal)
            //Required permissions
            var scope = "public_profile, email";

            var redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            var fb = new FacebookClient();
            var loginUrl = fb.GetLoginUrl(new
            {
                client_id = ClientId,
                redirect_uri = redirectUri,
                response_type = "token",
                scope = scope
            });

            Uri startUri = loginUrl;
            Uri endUri = new Uri(redirectUri, UriKind.Absolute);


#if WINDOWS_PHONE_APP
            WebAuthenticationBroker.AuthenticateAndContinue(startUri, endUri, null, WebAuthenticationOptions.None);
#endif

#if WINDOWS_APP
    WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, startUri, endUri);
    await ParseAuthenticationResult(result);
#endif

        }

#if WINDOWS_PHONE_APP
        public async void ContinueWebAuthentication(WebAuthenticationBrokerContinuationEventArgs args)
        {
            await ParseAuthenticationResult(args.WebAuthenticationResult);
        }
#endif

        public async Task ParseAuthenticationResult(WebAuthenticationResult result)
        {
            switch (result.ResponseStatus)
            {
                //connection error
                case WebAuthenticationStatus.ErrorHttp:
                    Debug.WriteLine("Connection error");
                    break;
                //authentication successfull
                case WebAuthenticationStatus.Success:
                    var pattern = string.Format("{0}#access_token={1}&expires_in={2}",
                        WebAuthenticationBroker.GetCurrentApplicationCallbackUri(), "(?<access_token>.+)",
                        "(?<expires_in>.+)");
                    var match = Regex.Match(result.ResponseData, pattern);

                    var access_token = match.Groups["access_token"];
                    var expires_in = match.Groups["expires_in"];

                    AccessToken = access_token.Value;
                    TokenExpiry = DateTime.Now.AddSeconds(double.Parse(expires_in.Value));

                    await ShowUserInfo();

                    InviteFriends.Visibility = Visibility.Visible;

                    break;
                //operation aborted by the user
                case WebAuthenticationStatus.UserCancel:
                    Debug.WriteLine("Operation aborted");
                    break;
                default:
                    break;
            }

        }

        private async Task ShowUserInfo()
        {
            FacebookClient client = new FacebookClient(AccessToken);
            dynamic user = await client.GetTaskAsync("me");
            MyName.Text = string.Format("I'm {0}", user.name);
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            await Login();
        }

        private void OnInviteFriendsClicked(object sender, RoutedEventArgs e)
        {
            AppContent.Visibility = Visibility.Collapsed;
            FacebookClient client = new FacebookClient(AccessToken);
            dynamic parameters = new ExpandoObject();
            parameters.app_id = ClientId;
            parameters.message = "Invite your friends";
            parameters.title = "Invite friends";
            parameters.redirect_uri = "https://wp.qmatteoq.com/";

            Uri dialogUrl = client.GetDialogUrl("apprequests", parameters);

            RequestView.Visibility = Visibility.Visible;
            RequestView.Navigate(dialogUrl);
        }

private async void RequestView_OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
{
    if (args.Uri.DnsSafeHost == "wp.qmatteoq.com")
    {
        sender.Visibility = Visibility.Collapsed;
        AppContent.Visibility = Visibility.Visible;

        FacebookClient client = new FacebookClient(AccessToken);
        dynamic result = client.ParseDialogCallbackUrl(args.Uri);

        if (result.error_code == null)
        {
            var items = (IDictionary<string, object>) result;

            ObservableCollection<FacebookUser> invitedFriends = new ObservableCollection<FacebookUser>();

            foreach (KeyValuePair<string, object> value in items)
            {
                if (value.Key != "request")
                {
                    string query = string.Format("/{0}", value.Value);
                    dynamic user = await client.GetTaskAsync(query);
                    FacebookUser facebookUser = new FacebookUser();
                    facebookUser.FullName = user.name;
                    invitedFriends.Add(facebookUser);
                }
            }

            Friends.Visibility = Visibility.Visible;
            FriendsHeader.Visibility = Visibility.Visible;
            Friends.ItemsSource = invitedFriends;
        }
        else
        {
            MessageDialog dialog = new MessageDialog("The user has canceled the operation");
            await dialog.ShowAsync();
        }
    }
}
    }
}