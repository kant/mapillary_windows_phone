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
    public partial class EditPageSequences : PhoneApplicationPage
    {
        private StorageFile m_selectedFile;
        private StorageFile m_selectedThumbFile;
        private PhotosViewModel viewModel;
        private string shareFileName;
        public EditPageSequences()
        {
            InitializeComponent();
            this.Loaded += EditPage_Loaded;
            noPhotos.Visibility = Visibility.Visible;
            titleCount.Text = string.Empty;
            progessText.Text = "Loading...";
            progress.Show();
        }

        private async void EditPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.SaveToCameraRollEnabled && this.ApplicationBar.Buttons.Count > 1)
            {
                this.ApplicationBar.Buttons.RemoveAt(1);
            }

            await LoadSequences();
            progress.Hide();
        }

        private async Task LoadSequences()
        {
            m_selectedFile = null;
            viewModel = new PhotosViewModel();
            await viewModel.GetSequences();

            noPhotos.Visibility = viewModel.Sequences.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            UpdateNumPhotos();
            DataContext = viewModel;
        }

        private void UpdateNumPhotos()
        {
            int count = 0;
            if (viewModel.Sequences != null && viewModel.Sequences.Count>0)
            {
                foreach(var seq in viewModel.Sequences)
                {
                    count += seq.Count;
                }
            }
            titleCount.Text = count + "  photos waiting for upload";
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


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.SelectedSequence = null;
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            App.SelectedSequence = null;
            NavigationService.Navigate(new Uri("/UploadPage.xaml", UriKind.Relative));
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete all the photos? This cannot be undone!", "Delete photos", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                await DeleteAllPhotos();
            }
        }

        private async Task DeleteSequence(Sequence sequenceToDelete)
        {
            progessText.Text = "Deleting...";
            progress.Show();
            await viewModel.GetPhotos();
            foreach (var photo in viewModel.PhotoList)
            {
                try
                {
                    bool seqFilter = photo.Title.EndsWith(sequenceToDelete.SequenceId);
                    if (sequenceToDelete.SequenceId == "noseq")
                        seqFilter = true;
                    if (photo.ThumbFile != null && seqFilter)
                    {
                        await photo.ThumbFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }

                    if (photo.File != null && seqFilter)
                    {
                        await photo.File.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to delete: " + ex.Message + ". Continuing..", "Delete failed", MessageBoxButton.OK);
                }

            }

            viewModel.PhotoList.Clear();
            viewModel.Sequences.Clear();
            sequenceList.SelectedItem = null;
            m_selectedFile = null;
            m_selectedThumbFile = null;
            await LoadSequences();
            UpdateNumPhotos();
            progress.Hide();
        }

        private async Task DeleteAllPhotos()
        {
            progessText.Text = "Deleting...";
            progress.Show();
            await viewModel.GetPhotos();
            foreach (var photo in viewModel.PhotoList)
            {
                try
                {
                    if (photo.ThumbFile != null)
                    {
                        await photo.ThumbFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }

                    if (photo.File != null)
                    {
                        await photo.File.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to delete: " + ex.Message + ". Continuing..", "Delete failed", MessageBoxButton.OK);
                }

            }

            viewModel.PhotoList.Clear();
            viewModel.Sequences.Clear();
            sequenceList.SelectedItem = null;
            m_selectedFile = null;
            m_selectedThumbFile = null;
            UpdateNumPhotos();
            progress.Hide();
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var sequenceToShow = ((FrameworkElement)e.OriginalSource).DataContext as Sequence;
            if (sequenceToShow != null)
            {
                App.SelectedSequence = sequenceToShow;
                NavigationService.Navigate(new Uri("/EditPage.xaml", UriKind.Relative));
            }
        }

        private async void delSeqButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            var sequenceToDelete = ((FrameworkElement)e.OriginalSource).DataContext as Sequence;
            var result = MessageBox.Show("Are you sure you want to delete this sequence? This cannot be undone!", "Delete sequence", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                if (sequenceToDelete != null)
                {
                    await DeleteSequence(sequenceToDelete);
                }
            }

        }

    }
}