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
using System.Windows.Input;

namespace Mapillary
{
    public partial class RegisterPage : PhoneApplicationPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void signupButton_Click(object sender, RoutedEventArgs e)
        {
            if (email.Text.Trim() == string.Empty || username.Text.Trim() == string.Empty || password.Password.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter e-mail, username and password to sign up.", "Signup failed", MessageBoxButton.OK);
                return;
            }

            await Signup(email.Text.Trim(), username.Text.Trim(), password.Password);

        }

        private async Task Signup(string email, string username, string password)
        {
            try
            {
                SignUpStatus signupResult = await LoginService.Signup(email, username, password);

                switch(signupResult.Result)
                {
                    case SignupResult.Ok:
                        MessageBox.Show("Account was successfully created", "Account created", MessageBoxButton.OK);
                        NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                        return;
                    case SignupResult.Error:
                        MessageBox.Show("Error creating account: " + signupResult.Message, "Account not created", MessageBoxButton.OK);
                        return;
                }

            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while trying to sign up. Please try again.", "Signup failed", MessageBoxButton.OK);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void username_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                email.Focus();
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