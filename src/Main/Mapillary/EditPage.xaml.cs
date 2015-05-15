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
using Mapillary.Models;
using System.Windows.Media.Imaging;
using System.IO;
using ExifLibrary;
using System.Text;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
namespace Mapillary
{
    public partial class EditPage : PhoneApplicationPage
    {
        private StorageFile m_selectedFile;
        private StorageFile m_selectedThumbFile;
        private PhotosViewModel viewModel;
        private string shareFileName;
        public EditPage()
        {
            InitializeComponent();
            this.Loaded += EditPage_Loaded;
            noPhotos.Visibility = Visibility.Visible;
            titleCount.Text = string.Empty;
            progress.Show();
        }

        private async void EditPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.SaveToCameraRollEnabled)
            {
                photoList.ItemTemplate = this.Resources["ItemTemplate1"] as DataTemplate;
                saveBtn.Visibility = Visibility.Collapsed;
                deleteBtn.Visibility = Visibility.Collapsed;
                shareBtn.Visibility = Visibility.Collapsed;
                rotateBtn.Visibility = Visibility.Collapsed;
                noteTxt.Text = "Note: When using the media library for storing photos you cannot edit or delete photos here.";
            }
            else
            {
                saveBtn.Visibility = Visibility.Visible;
                deleteBtn.Visibility = Visibility.Visible;
                shareBtn.Visibility = Visibility.Visible;
                rotateBtn.Visibility = Visibility.Visible;
                photoList.ItemTemplate = this.Resources["ItemTemplate2"] as DataTemplate;
            }
            await LoadImages();
            progress.Hide();
        }

        private async Task LoadImages()
        {
            m_selectedFile = null;
            viewModel = new PhotosViewModel();
            await viewModel.GetPhotos();

            noPhotos.Visibility = viewModel.NumPhotos > 0 ? Visibility.Collapsed : Visibility.Visible;
            UpdateNumPhotos();
            DataContext = viewModel;
        }

        private void UpdateNumPhotos()
        {
            titleCount.Text = viewModel.NumPhotos + "  photos waiting for upload";
        }

        private void photoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                LongListSelector selector = sender as LongListSelector;
                if (selector == null || selector.SelectedItem == null)
                {
                    return;
                }

                this.ApplicationBar.IsVisible = false;
                Photo image = selector.SelectedItem as Photo;
                OpenEditor(image);
 
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
            }
        }

        private int m_selectionIndex;
        private void OpenEditor(Photo image)
        {
            StorageFile file = image.File;
            m_selectedFile = file;
            m_selectedThumbFile = image.ThumbFile;
            m_selectionIndex = viewModel.PhotoList.IndexOf(image) + 1;
            imgNumText.Text = "Photo " + m_selectionIndex + " of " + viewModel.NumPhotos;
            if (App.SaveToCameraRollEnabled)
            {
                BitmapImage bitmap = GetBitmapFromMediaLib(image);
                previewImage.Source = bitmap;
            }
            else
            {
                try
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        WriteableBitmap bmp = new WriteableBitmap(0, 0);
                        using (var fileStream = storage.OpenFile(file.Path, FileMode.Open))
                        {
                            bmp = bmp.FromStream(fileStream);
                            previewImage.Source = bmp;
                            fileStream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading photo: ", "Error :-(", MessageBoxButton.OK);
                }

            }

            editPopup.IsOpen = true;
        }

        private BitmapImage GetBitmapFromMediaLib(Photo image)
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
                            return null;
                        }

                        BitmapImage bi = new BitmapImage();
                        using (var stream = picture.GetImage())
                        {
                            bi.SetSource(stream);
                        }

                        return bi;

                    }
                }

            }

            return null;
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            previewImage.Source = null;
            if (m_selectedFile != null)
            {
                var res = MessageBox.Show("Delete photo?", "Confirm delete", MessageBoxButton.OKCancel);
                if (res == MessageBoxResult.OK)
                {
                    if (m_selectedThumbFile != null)
                    {
                        await m_selectedThumbFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

                    }

                    await m_selectedFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    viewModel.Remove(photoList.SelectedItem as Photo);
                    m_selectedFile = null;
                    UpdateNumPhotos();
                }

                photoList.SelectedItem = null;
                editPopup.IsOpen = false;

            }

        }

        private readonly object readLock = new object();
        private void rotateBtn_Click(object sender, RoutedEventArgs e)
        {
            previewImage.Source = null;
            if (m_selectedFile != null)
            {
                Dictionary<ExifTag, ExifProperty> exifProperties = new Dictionary<ExifTag, ExifProperty>();
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    WriteableBitmap bmp = new WriteableBitmap(0, 0);
                    string exifData = null;
                    using (var fileStream = storage.OpenFile(m_selectedFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        ExifFile exif = ExifFile.ReadStream(fileStream);
                        var data = exif.Properties[ExifTag.ImageDescription];
                        exifData = (string)data.Value;
                        fileStream.Close();
                    }

                    using (var fileStream = storage.OpenFile(m_selectedFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bmp = bmp.FromStream(fileStream);
                        bmp = bmp.Rotate(90);
                        fileStream.Close();
                    }

                    lock (readLock)
                    {
                        using (IsolatedStorageFileStream stream = storage.CreateFile(@"shared\\transfers\" + m_selectedFile.Name))
                        {
                            bmp.SaveJpeg(stream, bmp.PixelWidth, bmp.PixelHeight, 0, 98);
                            stream.Close();
                        }
                    }

 
                    ExifFile exifRotated;
                    using (var fileStream = storage.OpenFile(m_selectedFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        exifRotated = ExifFile.ReadStream(fileStream);
                        fileStream.Close();
                        byte[] data = Encoding.UTF8.GetBytes(exifData);
                        uint length = (uint)data.Length;
                        ushort type = 2;
                        var imgDescProp = ExifPropertyFactory.Get(0x010e, type, length, data, BitConverterEx.ByteOrder.BigEndian, IFD.Zeroth);
                        exifRotated.Properties.Add(ExifTag.ImageDescription, imgDescProp);
                    }

                    lock (readLock)
                    {
                        using (IsolatedStorageFileStream targetStream = storage.OpenFile(m_selectedFile.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            exifRotated.SaveStream(targetStream);
                            targetStream.Close();
                        }
                    }

                    previewImage.Source = bmp;
                }
            }

            if (m_selectedThumbFile != null)
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var bmp = new WriteableBitmap(0, 0);
                    bool exist = storage.FileExists(m_selectedThumbFile.Path);
                    if (exist)
                    {
                        using (var fileStream = storage.OpenFile(m_selectedThumbFile.Path, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            bmp = bmp.FromStream(fileStream);
                            bmp = bmp.Rotate(90);
                            fileStream.Close();
                        }

                        lock (readLock)
                        {
                            using (IsolatedStorageFileStream stream = storage.CreateFile(@"shared\\transfers\" + m_selectedThumbFile.Name))
                            {
                                bmp.SaveJpeg(stream, bmp.PixelWidth, bmp.PixelHeight, 0, 98);
                                stream.Close();
                            }
                        }

                        viewModel.PhotoList[m_selectionIndex - 1].ImageSource = new BitmapImage(new Uri(m_selectedThumbFile.Path)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                    }
                }
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            previewImage.Source = null;
            editPopup.IsOpen = false;
            this.ApplicationBar.IsVisible = true;
            photoList.SelectedItem = null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (editPopup.IsOpen)
            {
                editPopup.IsOpen = false;
                e.Cancel = true;
                photoList.SelectedItem = null;
            }
        }

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectedFile != null)
            {
                var res = MessageBox.Show("Save photo to the Camera roll?", "Confirm save", MessageBoxButton.OKCancel);
                if (res == MessageBoxResult.OK)
                {
                    var ml = new MediaLibrary();
                    using (var stream = await m_selectedFile.OpenStreamForReadAsync())
                    {
                        ml.SavePictureToCameraRoll(m_selectedFile.Name, stream);
                        stream.Close();
                    }
                }

            }

        }

        private async Task Share(StorageFile file)
        {
            var ml = new MediaLibrary();
            Picture pic = null;
            using (var stream = await m_selectedFile.OpenStreamForReadAsync())
            {
                pic = ml.SavePicture("Shared_" + m_selectedFile.Name, stream);
                stream.Close();
            }

            ShareMediaTask shareMediaTask = new ShareMediaTask();
            shareMediaTask.FilePath = pic.GetPath();
            shareFileName = shareMediaTask.FilePath;
            shareMediaTask.Show();
        }

        private async void shareBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectedFile != null)
            {
                await Share(m_selectedFile);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/UploadPage.xaml", UriKind.Relative));
        }

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectionIndex > 0 && m_selectionIndex < viewModel.PhotoList.Count)
            {
                m_selectionIndex++;
                Photo image = viewModel.PhotoList[m_selectionIndex-1];
                OpenEditor(image);
            }
        }
    }
}