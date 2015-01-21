using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Facebook;

namespace FacebookSample
{
    public partial class MainPage
    {
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && !string.IsNullOrEmpty(e.Parameter.ToString()))
            {
                FacebookClient client = new FacebookClient(e.Parameter.ToString());
                dynamic user = await client.GetTaskAsync("me");
                MessageDialog dialog = new MessageDialog(user.name);
                await dialog.ShowAsync();
            }
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            string FacebookAppID = "1531823113737263";
            string AppID = "ea665f7d83be4e608dcf86c652ca12b1";
            string fbLoginUrl = String.Format("fbconnect://authorize?client_id={0}&scope=publish_stream&redirect_uri=msft-{1}://authorize", FacebookAppID, AppID);
            await Launcher.LaunchUriAsync(new Uri(fbLoginUrl));
        }
    }
}
