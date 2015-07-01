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
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using System.Windows.Input;

namespace Mapillary
{
    public partial class LoginPage : PhoneApplicationPage
    {
        ProgressIndicator progress;
        public LoginPage()
        {
            InitializeComponent();
            TestUserBtn.Visibility = Visibility.Collapsed;
#if DEBUG
            TestUserBtn.Visibility = Visibility.Visible;
#endif
            App.FeedLastRefreshed = DateTime.MinValue;
            progress = new ProgressIndicator
            {
                IsVisible = false,
                IsIndeterminate = false,
            };

            SystemTray.SetProgressIndicator(this, progress);
        }

        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            if (email.Text.Trim() == string.Empty || password.Password.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter both e-mail and password to login.", "Login failed", MessageBoxButton.OK);
                return;
            }

            progress.IsVisible = true;
            progress.IsIndeterminate = true;
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            await Login(email.Text.Trim(), password.Password);
            SystemTray.SetIsVisible(this, false);
            progress.IsVisible = false;
        }

 
        private async Task Login(string email, string password)
        {
            try
            {
                App.EventListCache = null;
                App.FeedLastRefreshed = DateTime.MinValue;
                App.ProfileLastRefreshed = DateTime.MinValue;
                App.AboutProfileCache = null;
                App.NumConnProfileCache = null;
                App.NumMetersProfileCache = null;
                App.NumPhotosProfileCache = null;
                bool wasLoggedIn = await LoginService.Login(email, password);
                if (wasLoggedIn)
                {
                    NavigationService.Navigate(new Uri("/MainPage.xaml?op=login", UriKind.Relative));
                }
                else
                {
                    SystemTray.SetIsVisible(this, false);
                    progress.IsVisible = false;
                    MessageBox.Show("E-mail or password is wrong. Please try again.", "Login failed", MessageBoxButton.OK);
                }
            }
            catch (Exception)
            {
                SystemTray.SetIsVisible(this, false);
                progress.IsVisible = false;
                MessageBox.Show("An error occurred while trying to login. Please try again.", "Login failed", MessageBoxButton.OK);
            }

        }

        private void createAccountButton_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/RegisterPage.xaml", UriKind.Relative));
        }

        private void passwordResetLink_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri uri = new Uri("http://www.mapillary.com/map/sendpassword");
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = uri;
            task.Show();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ClearBackStack();
        }

        private void ClearBackStack()
        {
            while (NavigationService.RemoveBackEntry() != null)
            {
            }
        }
        
        private async void TestUserBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await LoginService.LoginTestUser();
            if (result == true)
            {
                MessageBox.Show("Logged in");
                NavigationService.Navigate(new Uri("/MainPage.xaml?op=login", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("FAILED");
            }
        }

        private void email_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                password.Focus();
            }
        }

        private void password_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Focus();
            }
        }
    }
}