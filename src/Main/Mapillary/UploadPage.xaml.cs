using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using Windows.Storage;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Mapillary.Models;
using Windows.Networking.Connectivity;
using Microsoft.Phone.Net.NetworkInformation;
using Mapillary.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Text;

namespace Mapillary
{
    public partial class UploadPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private string m_uploadUrl = "http://mapillary.uploads.images.s3.amazonaws.com/";
        private string m_policy = "<policy>";
        private string m_signature = "<signature>";

        private int countUploaded = 0;
        private int countToUpload = 0;
        private bool m_ispaused;
        private bool m_isStopped;
        private bool m_wasLocked;
        private Queue<Photo> m_uploadQueue;
        int m_numFailed;
        private PhotosViewModel m_viewModel;
        public UploadPage()
        {
            InitializeComponent();
            this.Loaded += UploadPage_Loaded;
            noPhotos.Visibility = Visibility.Collapsed;
            pauseButton.IsEnabled = false;
            startButton.IsEnabled = false;
            totalProgressPanel.Visibility = Visibility.Collapsed;
            errorMsg.Visibility = Visibility.Collapsed;
            countUploaded = 0;
            m_uploadQueue = new Queue<Photo>();
            progress.Show();
            PhoneApplicationFrame rootFrame = App.Current.RootVisual as PhoneApplicationFrame;
            if (rootFrame != null)
            {
                rootFrame.Obscured += RootFrame_Obscured;
                rootFrame.Unobscured += rootFrame_Unobscured;
            }
        }

        void rootFrame_Unobscured(object sender, EventArgs e)
        {
            m_wasLocked = true;
            pauseButton.IsEnabled = false;
            startButton.IsEnabled = true;
            m_isStopped = true;
        }

        private void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
            if (m_uploadService != null)
            {
                m_wasLocked = true;
                pauseButton.IsEnabled = false;
                startButton.IsEnabled = true;
                m_isStopped = true;
                m_uploadService.Stop();
            }
        }

        private async void UploadPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.SaveToCameraRollEnabled)
            {
                photoList.ItemTemplate = this.Resources["ItemTemplate2"] as DataTemplate;
            }
            else
            {
                photoList.ItemTemplate = this.Resources["ItemTemplate"] as DataTemplate;
            }

            await LoadImages();
            progress.Hide();
        }

        private async Task LoadImages()
        {
            try
            {
                m_viewModel = new PhotosViewModel();
                await m_viewModel.GetPhotos();
                DataContext = m_viewModel;

                noPhotos.Visibility = m_viewModel.NumPhotos > 0 ? Visibility.Collapsed : Visibility.Visible;
                uploadMsg.Visibility = Visibility.Collapsed;
                errorMsg.Visibility = Visibility.Collapsed;
                double totalbytes = 0;
                if (m_viewModel.NumPhotos > 0)
                {
                    foreach (var photo in m_viewModel.PhotoList.OrderBy(r => r.TimeStamp))
                    {
                        if (App.SaveToCameraRollEnabled)
                        {
                            var upload = new UploadInfo() { PercentageDone = 0, IsUploaded = false, IsUploading = false };
                            upload.Size = (ulong)GetPhotoSize(photo);
                            totalbytes += upload.Size;
                            photo.UploadInfo = upload;
                        }
                        else
                        {
                            var upload = new UploadInfo() { File = photo.File, PercentageDone = 0, IsUploaded = false, IsUploading = false };
                            upload.Size = (await photo.File.GetBasicPropertiesAsync()).Size;
                            totalbytes += upload.Size;
                            upload.Path = "\\shared\\transfers\\" + photo.File.Name;
                            photo.UploadInfo = upload;
                        }
                    }

                    countToUpload = m_viewModel.NumPhotos;
                    UpdateTotalStatus();
                    startButton.IsEnabled = true;
                    uploadMsg.Visibility = Visibility.Visible ;
                    uploadMsg.Text = "Ready to upload " + Math.Round(totalbytes / 1024 / 1024, 1) + " Mb (" + m_viewModel.NumPhotos + " photos). Do not lock your phone while uploading.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private long GetPhotoSize(Photo image)
        {
            using (MediaLibrary library = new MediaLibrary())
            {
                foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                {
                    if (album.Name == "Camera Roll")
                    {
                        var picture = (from r in album.Pictures where r.Name == "mapi_" + image.Title select r).FirstOrDefault();
                        if (picture == null)
                        {
                            return 0;
                        }

                        var stream = picture.GetImage();
                        return stream.Length;

                    }
                }

            }

            return 0;
        }

        private void UpdateTotalStatus()
        {
            uploadStatus.Text = "Uploaded (" + countUploaded + "/" + countToUpload + ")";
            double p = ((double)countUploaded / (double)countToUpload) * 100;
            var greenWidth = 145 * ((double)p / 100);
            var grayWidth = 145 - greenWidth;
            totalProgressGreen.Width = greenWidth;
            totalProgressGray.Width = grayWidth;

        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            m_ispaused = false;
            if (m_viewModel != null && m_viewModel.NumPhotos > 0)
            {
                if (!CheckForNetwork())
                {
                    return;
                }

                pauseButton.IsEnabled = true;
                startButton.IsEnabled = false;
                totalProgressPanel.Visibility = Visibility.Visible;

                await StartUpload();
            }
        }

        private bool CheckForNetwork()
        {
            if (!DeviceNetworkInformation.IsNetworkAvailable)
            {
                MessageBox.Show("There is no network available. Unable to transfer. Please check network settings.", "No network", MessageBoxButton.OK);
                return false;
            }

            if (!IsWlanEnabled() && SettingsHelper.GetValue("AllowCellularData", "false") == "true")
            {
                var result = MessageBox.Show("You are connected to the internet via cellular data. Uploading can be expensive. Are you sure you want to upload now?", "No WiFi", MessageBoxButton.OKCancel);
                return result == MessageBoxResult.OK;
            }

            if (!IsWlanEnabled() && SettingsHelper.GetValue("AllowCellularData", "false") == "false")
            {
                var result = MessageBox.Show("You are connected to the internet via cellular data. Uploading is disabled. You can enable cellular upload in the settings.", "No WiFi", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        private bool IsWlanEnabled()
        {
            ConnectionProfile profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            var interfaceType = profile.NetworkAdapter.IanaInterfaceType;
            return interfaceType == 71 || interfaceType == 6 || interfaceType == 24;
        }

        private async Task StartUpload()
        {
            try
            {
                var wasLoggedIn = await LoginService.RefreshTokens();
                if (!wasLoggedIn)
                {
                    MessageBox.Show("To upload photos, you need an upload token from the Mapillary service. Please retry login.", "Unable to get upload token", MessageBoxButton.OK);
                    return;
                }

                m_uploadQueue = new Queue<Photo>();
                uploadMsg.Visibility = Visibility.Collapsed;
                errorMsg.Text = "";
                errorMsg.Visibility = Visibility.Collapsed;
                m_numFailed = 0;
                if (m_viewModel != null && m_viewModel.NumPhotos > 0)
                {
                    countToUpload = m_viewModel.NumPhotos;
                    countUploaded = 0;
                    UpdateTotalStatus();
                    errorMsg.Text = "";
                    errorMsg.Visibility = Visibility.Visible;
                    App.SetLockMode(true);
                    m_isStopped = false;
                    foreach (var upload in m_viewModel.PhotoList.ToList())
                    {
                        m_uploadQueue.Enqueue(upload);
                    }

                    await ProcessQueue();
                }

            }
            catch (Exception ex)
            {
                errorMsg.Text = "Upload ERROR: " + ex.Message;
                throw;
            }
        }

        private async Task ProcessQueue()
        {
            if (m_uploadQueue.Count > 0 && !m_isStopped)
            {
                var upload = m_uploadQueue.Dequeue();
                Dispatcher.BeginInvoke(() =>
                      {
                          upload.UploadInfo.IsUploading = true;
                          upload.UploadInfo.IsUploaded = false;
                          upload.UploadInfo.Failed = false;
                          upload.UploadInfo.Exception = null;

                      });
                await UploadFileAsync(upload);
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                      {
                          if (m_numFailed == 0)
                          {
                              errorMsg.Text = "DONE!";
                          }
                          else
                          {
                              errorMsg.Text = "DONE. " + m_numFailed + " failed. Please retry.";

                          }

                          if (m_wasLocked)
                          {
                              errorMsg.Text = "Phone was locked. Upload stopped.";
                              startButton.IsEnabled = true;
                              pauseButton.IsEnabled = false;
                              App.SetLockMode(false);
                          }
                          else
                          {
                              CheckCompleted();
                              startButton.IsEnabled = false;
                              App.SetLockMode(false);
                          }
                      });
            }
        }

        private async Task UploadFileAsync(Photo upload)
        {
            try
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("key", upload.Title);
                parameters.Add("AWSAccessKeyId", "AKIAI2X3BJAT2W75HILA");
                parameters.Add("acl", "private");
                parameters.Add("policy", m_policy);
                parameters.Add("signature", m_signature);
                parameters.Add("Content-Type", "image/jpeg");

                m_uploadService = new UploadService();
                m_uploadService.UploadCompleted += uploadService_UploadCompleted;
                m_uploadService.ProgressChanged += uploadService_ProgressChanged;
                await m_uploadService.StartUpload(upload, new Uri(m_uploadUrl), parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload failed. UploadFileAsync failure. Error=" + ex.Message + " " + ex.StackTrace);
            }
        }

        private void uploadService_ProgressChanged(object sender, UploadEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                e.Upload.UploadInfo.PercentageDone = e.Progress;
            });
        }

        private async void uploadService_UploadCompleted(object sender, UploadEventArgs e)
        {
            App.FeedLastRefreshed = DateTime.MinValue;
            var upload = e.Upload;
            if (upload.UploadInfo.Exception != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    upload.UploadInfo.IsUploaded = false;
                    upload.UploadInfo.IsUploading = false;
                    upload.UploadInfo.Failed = true;
                    if (upload.UploadInfo.Exception != null)
                    {
                        errorMsg.Text = upload.UploadInfo.Exception.Message;
                        if (upload.UploadInfo.Exception.Message != "Upload stopped")
                        {
                            m_numFailed++;
                        }
                    }
                });
            }
            else
            {
                if (upload.UploadInfo.StatusCode == HttpStatusCode.NoContent)
                {
                    Dispatcher.BeginInvoke(async () =>
                    {
                        m_wasLocked = false;
                        countUploaded++;
                        UpdateTotalStatus();
                        upload.UploadInfo.PercentageDone = e.Progress;
                        upload.UploadInfo.IsUploaded = true;
                        upload.UploadInfo.IsUploading = false;
                        upload.UploadInfo.Failed = false;
                        await RemoveUpload(upload);

                    });
                }
                else if (upload.UploadInfo.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        m_numFailed++;
                        SetFailed(upload, upload.UploadInfo.StatusCode);
                        errorMsg.Text = m_numFailed + " Login failed";
                        upload.UploadInfo.Failed = true;
                    });
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        m_numFailed++;
                        SetFailed(upload, upload.UploadInfo.StatusCode);
                        errorMsg.Text = m_numFailed + " Upload(s) failed (Code " + (int)upload.UploadInfo.StatusCode + ")";
                        upload.UploadInfo.Failed = true;
                    });
                }
            }

            if (!m_ispaused)
            {
                await ProcessQueue();
            }
        }



        private async Task UploadFile(Photo upload)
        {

            var httpContent = new MultipartFormDataContent();
            httpContent.Add(new StringContent(upload.File.Name), "key");
            httpContent.Add(new StringContent("AKIAI2X3BJAT2W75HILA"), "AWSAccessKeyId");
            httpContent.Add(new StringContent("private"), "acl");
            httpContent.Add(new StringContent(m_policy), "policy");
            httpContent.Add(new StringContent(m_signature), "signature");
            httpContent.Add(new StringContent("image/jpeg"), "Content-Type");
            var fileContentStream = await upload.File.OpenReadAsync();
            var streamContent = new StreamContent(fileContentStream.AsStreamForRead());
            upload.UploadInfo.IsUploading = true;
            upload.UploadInfo.PercentageDone = 0;
            httpContent.Add(streamContent, "file", upload.File.Name);
            HttpResponseMessage response = null;
            HttpStatusCode statusCode;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(240);
                    response = await httpClient.PostAsync(new Uri(m_uploadUrl), httpContent);
                    statusCode = response.StatusCode;

                }
            }
            catch (Exception ex)
            {
                m_numFailed++;
                errorMsg.Text = m_numFailed + " Upload error: " + ex.Message;
                statusCode = HttpStatusCode.InternalServerError;
                upload.UploadInfo.Failed = true;
            }

            countUploaded++;
            UpdateTotalStatus();
            if (statusCode == HttpStatusCode.NoContent)
            {
                upload.UploadInfo.IsUploaded = true;
                upload.UploadInfo.IsUploading = false;
                upload.UploadInfo.PercentageDone = 100;
                await RemoveUpload(upload);
            }
            else if (statusCode == HttpStatusCode.Unauthorized)
            {
                m_numFailed++;
                SetFailed(upload, statusCode);
                errorMsg.Text = m_numFailed + " Login failed";
                upload.UploadInfo.Failed = true;
            }
            else
            {
                m_numFailed++;
                SetFailed(upload, statusCode);
                errorMsg.Text = m_numFailed + " Upload(s) failed (Code " + (int)statusCode + ")";
                upload.UploadInfo.Failed = true;
            }

        }

        private void SetFailed(Photo upload, HttpStatusCode status)
        {
            try
            {
                upload.UploadInfo.Failed = true;
                upload.UploadInfo.ErrorMessage = status.ToString();
                upload.UploadInfo.IsUploaded = false;
                upload.UploadInfo.IsUploading = false;
                upload.UploadInfo.PercentageDone = 0;
            }
            catch (Exception)
            {
            }
        }

        private void CheckCompleted()
        {
            if (m_viewModel.NumPhotos == 0)
            {
                pauseButton.IsEnabled = false;
                startButton.IsEnabled = false;
                noPhotos.Visibility = Visibility.Visible;

            }
            else
            {
                pauseButton.IsEnabled = m_ispaused;
                startButton.IsEnabled = !m_ispaused;
            }
        }

        private async Task RemoveUpload(Photo upload)
        {
            if (!App.SaveToCameraRollEnabled)
            {
                await upload.File.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            m_viewModel.Remove(upload);
        }



        private async void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            pauseButton.IsEnabled = true;
            if (m_ispaused)
            {
                App.SetLockMode(true);
                m_ispaused = false;
                pauseButton.Content = "Pause";
                await StartUpload();
            }
            else
            {
                m_ispaused = true;
                if (m_uploadService != null)
                {
                    m_uploadService.Stop();
                }
                App.SetLockMode(false);

                pauseButton.Content = "Resume";
            }

        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            PhoneApplicationFrame rootFrame = App.Current.RootVisual as PhoneApplicationFrame;
            if (rootFrame != null)
            {
                rootFrame.Obscured -= RootFrame_Obscured;
                rootFrame.Unobscured -= rootFrame_Unobscured;

            }
            if (m_uploadService != null)
            {
                m_uploadService.Stop();
            }

            m_isStopped = true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private UploadService m_uploadService;
        private void OnPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}