using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Mapillary.Resources;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO.IsolatedStorage;
using Mapillary.Services;
using System.Reflection;
using System.IO;
using ExifLib;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Devices;
using ExifLibrary;
using System.Diagnostics;
using Microsoft.Xna.Framework.Media;
using Windows.Networking.Connectivity;
using System.Globalization;
using Microsoft.Phone.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Mapillary
{
    public partial class MainPage : PhoneApplicationPage
    {
        ProgressIndicator progress;
        private static string FEED_URI = "https://a.mapillary.com/v2/me/feed?limit=10&client_id={0}";
        private Geolocator m_geolocator = null;
        private Geoposition m_currentPosition = null;
        private bool m_locationConsent = false;
        public MainPage()
        {
            InitializeComponent();
            m_locationConsent = SettingsHelper.GetValue("LocationConsent", "false") == "true";
            this.Loaded += MainPage_Loaded;
            CheckFirstTimeLoad();
            progress = new ProgressIndicator
            {
                IsVisible = true,
                IsIndeterminate = true,
            };

            SystemTray.SetProgressIndicator(this, progress);
            //    for (int i = 0; i <= 10;i++ )
            //        for (int x = 0; x <= 60; x++)
            //        CopyTestImage(i,x);
            //
        }

        private async void DoLoginOrLoadFeed()
        {
            if (!LoginService.IsLoggedIn)
            {
                NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
            }
            else
            {
                if ((DateTime.Now - App.FeedLastRefreshed > TimeSpan.FromMinutes(5)) || App.EventListCache == null)
                {
                    await LoadFeed();
                }
                else
                {
                    LoadFeedFromCache();
                }
            }
        }

        private void LoadFeedFromCache()
        {
            eventList.ItemsSource = App.EventListCache;
            ShowHideList(App.EventListCache != null && App.EventListCache.Count > 0);
        }

        private void ShowHideList(bool showList)
        {
            if (showList)
            {
                eventList.Visibility = Visibility.Visible;
                infoPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                infoPanel.Visibility = Visibility.Visible;
                eventList.Visibility = Visibility.Collapsed;
            }
        }

        private void CopyTestImage(int i, int x)
        {
            var uri = new Uri("Assets\\2014_05_05_12_12_12_123.jpg", UriKind.Relative);
            var sri = Application.GetResourceStream(uri);
            var data = sri.Stream;
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (IsolatedStorageFileStream stream = storage.CreateFile("shared\\transfers\\2014_05_06_12_12_" + i + "_" + x + ".jpg"))
            {
                data.CopyTo(stream);
            }

            var uri2 = new Uri("Assets\\thumb_2014_05_05_12_12_12_123.jpg", UriKind.Relative);
            var sri2 = Application.GetResourceStream(uri2);
            var data2 = sri2.Stream;
            IsolatedStorageFile storage2 = IsolatedStorageFile.GetUserStoreForApplication();

            using (IsolatedStorageFileStream stream = storage.CreateFile("shared\\transfers\\thumb_2014_05_06_12_12_" + i + "_" + x + ".jpg"))
            {
                data2.CopyTo(stream);
            }

            Debug.WriteLine("Copying " + i + "/" + x);
        }

        private void OnCameraButtonFullPress(object sender, EventArgs e)
        {
            GoToCamera();
        }

        private void OnCameraButtonHalfPress(object sender, EventArgs e)
        {
            GoToCamera();
        }

        private void GoToCamera()
        {
            if (!CheckLocationConsent()) return;
            NavigationService.Navigate(new Uri("/CapturePage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetLocationConsent(SettingsHelper.GetValue("LocationConsent", "false") == "true");
            CameraButtons.ShutterKeyHalfPressed += OnCameraButtonHalfPress;
            CameraButtons.ShutterKeyPressed += OnCameraButtonFullPress;
            DoLoginOrLoadFeed();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            CameraButtons.ShutterKeyHalfPressed -= OnCameraButtonHalfPress;
            CameraButtons.ShutterKeyPressed -= OnCameraButtonFullPress;
        }

        public void UpdateLiveTile(int count)
        {
            try
            {
                FlipTileData primaryTileData = new FlipTileData();
                primaryTileData.Count = 0;
                if (count > 0)
                {
                    primaryTileData.WideBackContent = count + " pending uploads";
                    primaryTileData.BackContent = count + " pending";
                }
                else
                {
                    primaryTileData.WideBackContent = string.Empty;
                    primaryTileData.BackContent = string.Empty;
                }

                ShellTile primaryTile = ShellTile.ActiveTiles.First();
                primaryTile.Update(primaryTileData);
            }
            catch (Exception) { }
        }

        private async Task ExifTest()
        {
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
                var images = await folder.GetFilesAsync();
                var first = images.FirstOrDefault();
                if (first == null) return;
                using (IsolatedStorageFileStream stream = storage.OpenFile(@"shared\\transfers\" + first.Name, FileMode.Open))
                {
                    ExifFile exif = ExifFile.ReadStream(stream);
                }
            }
        }

        private void CheckFirstTimeLoad()
        {
            if (SettingsHelper.GetValue("FirstTimeLoaded", "false") == "false")
            {
                DisplayHelpPopUp();
                var result = MessageBox.Show("Mapillary must use your current location to work. Is that OK?", "Confirm", MessageBoxButton.OKCancel);
                SettingsHelper.SetValue("FirstTimeLoaded", "true");
                if (result == MessageBoxResult.Cancel)
                {
                    SettingsHelper.SetValue("LocationConsent", "false");
                    m_locationConsent = false;
                }
                else
                {
                    SettingsHelper.SetValue("LocationConsent", "true");
                    m_locationConsent = true;
                }
            }
        }

        public void DisplayHelpPopUp()
        {
            double height = Application.Current.Host.Content.ActualHeight;
            double width = Application.Current.Host.Content.ActualWidth;

            Grid grid = new Grid();
            grid.Height = height;
            grid.Width = width;
            grid.Background = new SolidColorBrush(Colors.White);
            WelcomePopup welcomeCtrl = new WelcomePopup();
            grid.Children.Add(welcomeCtrl);
            Popup popup = new Popup();
            popup.Child = grid;
            this.LayoutRoot.Children.Add(popup);
            popup.IsOpen = true;

        }
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //await ExifTest();
            //await GenerateTestImages();

            int count = await UpdateCounter();
            UpdateLiveTile(count);
            var success = await LoginService.RefreshTokens();
        }

        private async Task LoadFeed()
        {
            try
            {
                SystemTray.SetIsVisible(this, true);
                SystemTray.SetOpacity(this, 0);
                progress.IsVisible = true;
                string url = string.Format(FEED_URI, App.WP_CLIENT_ID);
                var httpClient = new HttpClient(new HttpClientHandler());
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                request.Headers.Add("Authorization", "Bearer " + LoginService.SignInToken);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    httpClient.Dispose();
                    JObject jobj = JObject.Parse(responseString);
                    JArray jlist = (JArray)jobj["feed"];
                    var feedList = new List<FeedItem>();
                    foreach (JObject obj in jlist)
                    {
                        var item = new FeedItem();
                        string action = obj["action"] != null ? obj["action"].ToString() : "";
                        string type = obj["type"] != null ? obj["type"].ToString() : "";
                        string object_type = obj["object_type"] != null ? obj["object_type"].ToString() : "";
                        string object_key = obj["object_key"] != null ? obj["object_key"].ToString() : "";
                        string object_value = obj["object_value"] != null ? obj["object_value"].ToString() : "";
                        string image_url = obj["image_url"] != null ? obj["image_url"].ToString() : "";
                        string image_key = obj["image_key"] != null ? obj["image_key"].ToString() : "";
                        string main_description = obj["main_description"] != null ? obj["main_description"].ToString() : "";
                        string detail_description = obj["detail_description"] != null ? obj["detail_description"].ToString() : "";
                        string created_at = obj["created_at"] != null ? obj["created_at"].ToString() : null;
                        string started_at = obj["started_at"] != null ? obj["started_at"].ToString() : null;
                        string updated_at = obj["updated_at"] != null ? obj["updated_at"].ToString() : null;
                        string id = obj["id"] != null ? obj["id"].ToString() : "";
                        int nbr_objects = obj["nbr_objects"] != null ? (int)obj["nbr_objects"] : 0;

                        string user = obj["user"] != null ? obj["user"].ToString() : "";
                        bool open = obj["open"] != null ? (bool)obj["open"] : false;

                        var dtcreated_at = created_at != null ? new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(Convert.ToDouble(created_at)) : DateTime.MinValue;
                        var dtstarted_at = started_at != null ? new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(Convert.ToDouble(started_at)) : DateTime.MinValue;
                        var dtupdated_at = updated_at != null ? new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(Convert.ToDouble(updated_at)) : DateTime.MinValue;

                        item.Action = action;
                        item.Type = type;
                        item.ObjectType = object_type;
                        item.ObjectKey = object_key;
                        item.ObjectValue = object_value;
                        item.ImageUrl = image_url;
                        item.ImageKey = image_key;
                        item.MainDescription = main_description;
                        item.DetailDescription = detail_description;
                        item.CreatedAt = dtcreated_at;
                        item.StartedAt = started_at;
                        item.UpdatedAt = updated_at;
                        item.Id = id;
                        item.NbrObjects = nbr_objects;
                        item.User = user == LoginService.SignInEmail ? "You" : user;
                        item.Open = open;
                        item.FirstLine = item.User + " " + item.MainDescription;
                        if (dtstarted_at.DayOfYear == DateTime.Now.DayOfYear && dtstarted_at.Year == DateTime.Now.Year)
                        {
                            item.SecondLine = "Today";
                        }
                        else
                        {
                            item.SecondLine = dtstarted_at.ToShortDateString();
                        }

                        if (dtstarted_at == DateTime.MinValue)
                        {
                            item.SecondLine = "";
                        }

                        feedList.Add(item);
                    }

                    eventList.ItemsSource = feedList;
                    App.EventListCache = feedList;
                    ShowHideList(feedList != null && feedList.Count > 0);
                    App.FeedLastRefreshed = DateTime.Now;
                }
                else
                {
                    //MessageBox.Show("An error occurred while getting your feed. Please try again.", "Error", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("An error occurred while getting your feed: " + ex.Message, "Error", MessageBoxButton.OK);
            }

            SystemTray.SetIsVisible(this, false);
            progress.IsVisible = false;
        }

        private async Task GenerateTestImages()
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
            var images = await folder.GetFilesAsync();
            for (int i = 0; i < 100; i++)
            {
                foreach (var file in images)
                {
                    string name = "_" + i + ".jpg";
                    await file.CopyAsync(folder, file.Name.Replace(".jpg", name));
                }
            }
        }

        private async Task<int> UpdateCounter()
        {
            if (App.SaveToCameraRollEnabled)
            {
                using (MediaLibrary library = new MediaLibrary())
                {
                    foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                    {
                        if (album.Name == "Camera Roll")
                        {
                            var images = from r in album.Pictures where r.Name.StartsWith("mapi_thumb_") select r;
                            var count = images.Count();
                            return count;
                        }
                    }
                }

                return 0;
            }
            else
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!storage.DirectoryExists("shared\\transfers"))
                    {
                        return 0;
                    }

                    var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
                    var images = await folder.GetFilesAsync();
                    var imagesNoThumb = from r in images where !r.Name.StartsWith("thumb_") select r;
                    int count = imagesNoThumb.Count();


                    return count;
                }
            }
        }

        private void InitializeGeoLocator()
        {
            if (m_geolocator == null && m_locationConsent)
            {
                var geolocator = new Geolocator();
                App.GeoLocator = geolocator;
                geolocator.DesiredAccuracy = PositionAccuracy.High;
                geolocator.DesiredAccuracyInMeters = 1;
                geolocator.MovementThreshold = 5;
                geolocator.ReportInterval = 500;
                geolocator.PositionChanged += Geolocator_PositionChanged;
                geolocator.StatusChanged += Geolocator_StatusChanged;
                m_geolocator = geolocator;
            }
        }

        public void SetLocationConsent(bool consentIsOn)
        {
            if (consentIsOn)
            {
                m_locationConsent = true;
                InitializeGeoLocator();
            }
            else
            {
                m_locationConsent = false;
                App.GeoLocator = null;
                m_geolocator = null;
            }
        }

        public Geolocator GeoLocator
        {
            get
            {
                return m_geolocator;
            }
        }

        private void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            if (args.Status == PositionStatus.Ready)
            {
                m_geolocator = sender;
                App.GeoLocator = sender;
                Deployment.Current.Dispatcher.BeginInvoke(async () =>
                {
                    Geoposition pos = await m_geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                    m_currentPosition = pos;
                });

            }
        }

        private void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
        }

        private async void ShowMap()
        {
            if (!CheckLocationConsent()) return;
            if (App.GeoLocator != null)
            {
                Geoposition pos = await App.GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                string lat = pos.Coordinate.Latitude.ToString(new CultureInfo("en-US"));
                string lng = pos.Coordinate.Longitude.ToString(new CultureInfo("en-US"));
                var ver = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                var uri = new Uri("http://www.mapillary.com/map/im/16/" + lat + "/" + lng + "/compact?showCenter&client=wp&client-version=" + ver.Version);
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = uri;
                task.Show();
            }
            else
            {
                MessageBox.Show("No GPS position found. Please wait for the GPS to be initialized.");
            }
        }


        private void uploadButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/EditPage.xaml", UriKind.Relative));
        }

        private bool CheckLocationConsent()
        {
            if (m_locationConsent == false)
            {
                var result = MessageBox.Show("Mapillary needs your location before you can use the camera or map. Would you like to turn location on?", "No location", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    SettingsHelper.SetValue("LocationConsent", "true");
                    m_locationConsent = true;
                }

            }

            return m_locationConsent;
        }


        private void settingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void profileButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/UserProfile.xaml", UriKind.Relative));

        }

        private void captureBtn_Click(object sender, EventArgs e)
        {
            GoToCamera();
        }

        private void mapBtn_Click(object sender, EventArgs e)
        {
            ShowMap();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadFeed();
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid != null)
            {
                var item = grid.DataContext as FeedItem;
                if (item.ObjectType == "s" || item.ObjectType == "im" || item.ObjectType == "co" || item.ObjectType == "cm")
                {
                    string url = String.Format("http://www.mapillary.com/map/{0}/{1}/compact?client_id={2}&client=wp", item.ObjectType, item.ObjectKey, App.WP_CLIENT_ID);
                    OpenBrowser(url);
                }
            }
        }

        private async void OpenBrowser(string url)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(url);
            task.Show();
        }
    }
}