using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Facebook;

namespace WebLogin
{
    public partial class MainPage
    {
        public string AccessToken;
        public DateTime TokenExpiry;

        private async Task Login()
        {
            //Client ID of the Facebook App (retrieved from the Facebook Developers portal)
            var clientId = "your app id";
            //Required permissions
            var scope = "public_profile, email";

            var redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            var fb = new FacebookClient();
            var loginUrl = fb.GetLoginUrl(new
            {
                client_id = clientId,
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
            MyName.Text = user.name;
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            await Login();
        }
    }
}