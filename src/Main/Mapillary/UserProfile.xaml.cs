using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading.Tasks;
using System.Net.Http;
using Mapillary.Services;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Tasks;

namespace Mapillary
{
    public partial class UserProfile : PhoneApplicationPage
    {
        private static string PROFILE_URI = "https://a.mapillary.com/v2/u/{0}?client_id={1}";
        private static string SEQ_URI = "https://a.mapillary.com/v2/search/s/ul?user={0}&limit=50&client_id={1}";
        ProgressIndicator progress;
        public UserProfile()
        {
            InitializeComponent();
            emailAddress.Text = string.Empty;
            aboutTxt.Text = string.Empty;
            progress = new ProgressIndicator
            {
                IsVisible = true,
                IsIndeterminate = true,
            };

            SystemTray.SetProgressIndicator(this, progress);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if ((DateTime.Now - App.ProfileLastRefreshed > TimeSpan.FromMinutes(5)) || App.SequenceListCache == null)
            {
                await LoadProfile();
                await LoadSequensesList();
                
                App.ProfileLastRefreshed = DateTime.Now;
            }
            else
            {
                LoadFromCache();
            }
        }

        private void LoadFromCache()
        {
            sequenceList.ItemsSource = App.SequenceListCache;
            numConnections.Text = App.NumConnProfileCache;
            numMeters.Text = App.NumMetersProfileCache;
            numPhotos.Text = App.NumPhotosProfileCache;
            aboutTxt.Text = App.AboutProfileCache;
            emailAddress.Text = LoginService.SignInEmail;
            profileImg.Source = App.ImageProfileCache;
        }

        private async Task LoadSequensesList()
        {
            try
            {
                SystemTray.SetIsVisible(this, true);
                SystemTray.SetOpacity(this, 0);
                progress.IsVisible = true;
                string url = string.Format(SEQ_URI, LoginService.SignInUserName, App.WP_CLIENT_ID);
                var httpClient = new HttpClient(new HttpClientHandler());
                HttpResponseMessage response = await httpClient.GetAsync(new Uri(url));
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    httpClient.Dispose();
                    JObject jobj = JObject.Parse(responseString);
                    bool hasMore = jobj["more"] != null ? (bool)jobj["more"] : false;
                    JArray jlist = (JArray)jobj["ss"];
                    var seqList = new List<ImgSequence>();
                    foreach(JObject obj in jlist)
                    {
                        var sq = new ImgSequence();
                        string mkey = obj["mkey"].ToString();
                        sq.MKey = mkey;
                        sq.Image = string.Format("https://d1cuyjsrcm0gby.cloudfront.net/{0}/thumb-320.jpg", mkey);
                        string capturedAt = obj["captured_at"].ToString();
                        sq.Place = obj["location"].ToString();
                        if (string.IsNullOrEmpty(sq.Place))
                        {
                            sq.Place = "Unknown location";
                        }

                        try
                        {
                            var dt = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(Convert.ToDouble(capturedAt));
                            sq.Date = dt.ToShortDateString();
                        }
                        catch (Exception) { }
                        seqList.Add(sq);
                    }

                    sequenceList.ItemsSource = seqList;
                    App.SequenceListCache = seqList;
                }
                else
                {
                    await ShowErrorResponse(response, "An error occurred while getting your sequenses.");
                }


            }
            catch (Exception ex)
            {
                SystemTray.SetIsVisible(this, false);
                progress.IsVisible = false;
                MessageBox.Show("An error occurred while getting your sequenses: " + ex.Message, "Error", MessageBoxButton.OK);
            }

            SystemTray.SetIsVisible(this, false);
            progress.IsVisible = false;
        }

        private async Task ShowErrorResponse(HttpResponseMessage response, string message)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseString))
            {
                MessageBox.Show(message + " Status=" + response.StatusCode, "Error", MessageBoxButton.OK);
            }
            else
            {
                JObject jobj = JObject.Parse(responseString);
                string error = jobj["message"] != null ? jobj["message"].ToString() : "?";
                string code = jobj["code"] != null ? jobj["code"].ToString() : "?";
                MessageBox.Show("Unable to load your profile. Status=" + response.StatusCode + ", Error=" + code + "/" + error, "Error", MessageBoxButton.OK);
            }
        }

        private async Task LoadProfile()
        {
            try
            {
                SystemTray.SetIsVisible(this, true);
                SystemTray.SetOpacity(this, 0);
                progress.IsVisible = true;
                string url = string.Format(PROFILE_URI, LoginService.SignInUserName, App.WP_CLIENT_ID);
                var httpClient = new HttpClient(new HttpClientHandler());
                HttpResponseMessage response = await httpClient.GetAsync(new Uri(url));
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    httpClient.Dispose();
                    JObject jobj = JObject.Parse(responseString);
                    string about = jobj["about"] != null ? jobj["about"].ToString() : "";
                    string img = jobj["avatar"] != null ? jobj["avatar"].ToString() : "";
                    string user = jobj["user"] != null ? jobj["user"].ToString() : "";
                    LoginService.SignInUserName = user;
                    numConnections.Text = jobj["connections_all"] != null ? jobj["connections_all"].ToString() : "0";
                    numMeters.Text = jobj["connections_all"] != null ? jobj["distance_all"].ToString() : "0";
                    numPhotos.Text = jobj["connections_all"] != null ? jobj["images_all"].ToString() : "0";
                    aboutTxt.Text = about;
                    emailAddress.Text = user;
                    if (string.IsNullOrEmpty(numMeters.Text)) numMeters.Text = "0";
                    BitmapImage bitmap = null;
                    if (!string.IsNullOrEmpty(img))
                    {
                        bitmap = new BitmapImage(new Uri(img)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                        profileImg.Source = bitmap;
                    }

                    App.NumConnProfileCache = numConnections.Text;
                    App.NumMetersProfileCache = numMeters.Text;
                    App.NumPhotosProfileCache = numPhotos.Text;
                    App.AboutProfileCache = about;
                    App.ImageProfileCache = bitmap;
                }
                else
                {
                    await ShowErrorResponse(response, "An error occurred while getting your profile.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while getting your profile: " + ex.Message, "Error", MessageBoxButton.OK);
            }

            SystemTray.SetIsVisible(this, false);
            progress.IsVisible = false;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/InfoPage.xaml", UriKind.Relative));
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid != null)
            {
                var item = grid.DataContext as ImgSequence;
                string url = string.Format("http://www.mapillary.com/map/im/{0}/compact?client_id={1}&client=wp", item.MKey, App.WP_CLIENT_ID);
                ShowMap(url);
            }
        }

        private async void ShowMap(string url)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(url);
            task.Show();
        }
    }

    public class ImgSequence
    {
        public string Place { get; set; }
        public string Date { get; set; }
        public string Image { get; set; }
        public string MKey { get; set; }
    }
}