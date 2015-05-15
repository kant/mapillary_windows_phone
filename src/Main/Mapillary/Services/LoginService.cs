using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mapillary.Services
{
    internal enum SignupResult { Ok, Error }
    public class LoginService
    {
        internal static bool IsLoggedIn { get; set; }
        internal static string SignInToken { get; set; }
        internal static string SignInEmail { get; set; }
        internal static string SignInUserName { get; set; }
        private static string s_password;


        private static string SIGN_IN_URI = "https://a.mapillary.com/v2/ua/login?client_id={0}";
        private static string SIGN_UP_URI = "https://a.mapillary.com/v2/ua/signup?client_id={0}";
        private static string PWD_RESET_URI = "https://a.mapillary.com/v2/ua/sendpasswordform?client_id={0}";
        private static string TOKENS_URI = "https://a.mapillary.com/v2/me?client_id={0}&token={1}";
        

        internal async static Task<bool> LoginTestUser()
        {
#if DEBUG
            return await Login("tommy@ovesen.net", "dire55stMa");
#endif
            return false;
        }

        internal async static Task<bool> RefreshTokens()
        {
            var lastLogin = SettingsHelper.GetObject<DateTime>("SignInTime");
            if (lastLogin == null || IsLoggedIn == false)
            {
                return false;
            }

            string email = SettingsHelper.GetValue("SignInEmail", null);
            string password = SettingsHelper.GetValue("SignInPassword", null);
            if (email == null || password == null)
            {
                return false;
            }

            bool wasLoggedIn = await Login(email, password);
            return wasLoggedIn;
        }

        internal async static Task<bool> Login(string email, string password)
        {
            string[] tokens = await GetAuthTokens(email, password);
            if (tokens != null)
            {
                s_password = password;
                SignInToken = tokens[0];
                UploadToken = tokens[1];
                SignInUserName = tokens[2];
                SignInEmail = tokens[3];
                SaveTokens();
                IsLoggedIn = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static async Task<String[]> GetAuthTokens(string email, string password)
        {
            try
            {
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("email", email),
                        new KeyValuePair<string, string>("password", password)
                    };
                Uri uri = new Uri(string.Format(SIGN_IN_URI, App.WP_CLIENT_ID));
                var httpClient = new HttpClient(new HttpClientHandler());
                var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    httpClient.Dispose();
                    JObject jobj = JObject.Parse(responseString);
                    string authToken = jobj["token"].ToString();
                    string[] tokens = await GetTokensWithJwt(authToken);
                    return tokens;
                }
                else
                {
                    httpClient.Dispose();
                    return null;
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        private static async Task<string[]> GetTokensWithJwt(string authToken)
        {
            Uri uri = new Uri(string.Format(TOKENS_URI, App.WP_CLIENT_ID, authToken));
            var httpClient = new HttpClient(new HttpClientHandler());
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Authorization", "Bearer " + authToken);
            HttpResponseMessage response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                httpClient.Dispose();
                JObject jobj = JObject.Parse(responseString);
                string upload_token = jobj["upload_token"].ToString();
                string username = jobj["username"].ToString();
                string email = jobj["email"].ToString();
                return new string[] { authToken, upload_token, username, email };
            }
            else
            {
                httpClient.Dispose();
                return null;
            }
        }

        internal static void Logout()
        {
            SignInEmail = null;
            SignInToken = null;
            s_password = null;
            SaveTokens();
            IsLoggedIn = false;
        }

        internal static async Task SendPasswordReset(string email)
        {
            try
            {
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("email", email),
                    };

                var httpClient = new HttpClient(new HttpClientHandler());
                Uri uri = new Uri(string.Format(PWD_RESET_URI, App.WP_CLIENT_ID));
                HttpResponseMessage response = await httpClient.PostAsync(uri, new FormUrlEncodedContent(values));
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    httpClient.Dispose();
                    return;
                }
                else
                {
                    httpClient.Dispose();
                    throw new Exception("Error sending mail.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal static async Task<SignUpStatus> Signup(string email, string username, string password)
        {
            Logout();
            var status = new SignUpStatus();
            try
            {
                var values = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("username", username),
                        new KeyValuePair<string, string>("email", email),
                        new KeyValuePair<string, string>("password", password),
                        new KeyValuePair<string, string>("password_confirmation", password)
                    };

                var httpClient = new HttpClient(new HttpClientHandler());
                Uri uri = new Uri(string.Format(SIGN_UP_URI, App.WP_CLIENT_ID));
                HttpResponseMessage response = await httpClient.PostAsync(uri, new FormUrlEncodedContent(values));
                status.StatusCode = response.StatusCode;
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    status.Result = SignupResult.Error;
                    JObject jobj = JObject.Parse(responseString);
                    string message = jobj["message"].ToString();
                    status.Message = message;
                    return status;
                }
                else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    status.Result = SignupResult.Error;
                    status.Message = "Could not sign up. Please retry later. Error code " + response.StatusCode;
                    return status;
                }

                s_password = password;
                JObject jobj2 = JObject.Parse(responseString);
                string token = jobj2["token"].ToString();
                status.Result = SignupResult.Ok;
                status.Message = string.Empty;
                IsLoggedIn = true;
                SignInEmail = email;
                SignInToken = token;
                SaveTokens();
                return status;
            }
            catch (Exception)
            {

                throw;
            }

        }

        private static void SaveTokens()
        {
            SettingsHelper.SetValue("SignInToken", SignInToken);
            SettingsHelper.SetValue("SignInEmail", SignInEmail);
            SettingsHelper.SetValue("UploadToken", UploadToken);
            SettingsHelper.SetObject("SignInTime", DateTime.Now);
            SettingsHelper.SetValue("SignInPassword", s_password);
        }


        public static string UploadToken { get; set; }
    }

    internal class SignUpStatus
    {
        public string Message { get; set; }
        public SignupResult Result { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }
}
