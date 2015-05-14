using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Mapillary.Services;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Tasks;
using System.Reflection;

namespace Mapillary
{
    public partial class InfoPage : PhoneApplicationPage
    {
        private WelcomePopup welcomeCtrl;
        private Popup popup;
        private Grid grid;

        public InfoPage()
        {
            InitializeComponent();
            var ver = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            version.Text = "Version " + ver.Version;
            SetSignInOutText();
        }

        private void SetSignInOutText()
        {
            signOut.Content = LoginService.IsLoggedIn ? "Sign out (" + LoginService.SignInEmail + ")" : "Sign in";
        }

        private void signOut_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (LoginService.IsLoggedIn)
            {
                LoginService.Logout();
                MessageBox.Show("You are now successfully signed out.", "Signed out", MessageBoxButton.OK);
                SetSignInOutText();
            }
            else
            {
                NavigationService.Navigate(new Uri("/LoginPage.xaml?op=signout", UriKind.Relative));
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetSignInOutText();
        }

        private void ShowTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DisplayHelpPopUp();
        }

        public void DisplayHelpPopUp()
        {
            if (welcomeCtrl == null)
            {
                welcomeCtrl = new WelcomePopup();
                popup = new Popup();
                grid = new Grid();
            }

            double height = Application.Current.Host.Content.ActualHeight;
            double width = Application.Current.Host.Content.ActualWidth;


            grid.Height = height;
            grid.Width = width;
            grid.Background = new SolidColorBrush(Colors.White);
            
            grid.Children.Add(welcomeCtrl);
            popup.Child = grid;
            this.LayoutRoot.Children.Add(popup);
            popup.IsOpen = true;

        }

        private void About_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            OpenUrl(new Uri("http://mapillary.com/about.html"));
        }

        private void Terms_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            OpenUrl(new Uri("http://mapillary.com/terms.html"));
        }

        private void OpenUrl(Uri uri)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = uri;
            task.Show();
        }

        private void HyperlinkButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            string uri = "http://www.ovesen.net/phototxt/";
            if (((HyperlinkButton)sender).Tag != null)
            {
                string tag = ((HyperlinkButton)sender).Tag.ToString();

                if (tag == "PhotoTxt")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/phototxt/f4c08db1-75f5-4b2f-96f9-d3e009409a70";
                }

                if (tag == "PhotoTxtW")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/phototxt-weather/821ae293-4497-41bd-9774-80fc8f548074";
                }

                if (tag == "PhotoTxtC")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/phototxt-christmas/d258939c-d24e-463b-9c2f-f6666a7d16e2";
                }

                if (tag == "PhotoTxtM")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/my-big-day-photos/79390863-7180-408e-89c4-9189062a5403";
                }

                if (tag == "Nutrition")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/nutrition/53d16f3b-1f9d-4b16-9bdd-811e37a02344";
                }

                if (tag == "HealthC")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/health-calculators/c80cc9fd-9c6d-493c-8831-717e185b41a0";
                }

                if (tag == "Drive")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/drive/1ebc032b-b40c-452a-87eb-7c3291c11a1b";
                }

                if (tag == "SpeedCam")
                {
                    uri = "http://www.windowsphone.com/en-us/store/app/speedcam-detector/97a6e2dd-e1b5-4bf2-bead-7395b71c4f27";
                }
            }

            task.Uri = new Uri(uri);
            task.Show();
        }
    }
}