using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Search;

namespace Mapillary.Models
{
    public class PhotosViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Photo> m_photos;
        public int NumPhotos
        {
            get
            {
                if (m_photos == null) return 0;
                return m_photos.Count;
            }
        }

        public ObservableCollection<Photo> PhotoList
        {
            get
            {
                return m_photos;
            }

            set
            {
                m_photos = value;
                OnPropertyChanged("PhotoList");
            }
        }

        public ObservableCollection<Sequence> Sequences { get; internal set; }

        public void Remove(Photo photo)
        {
            m_photos.Remove(photo);
        }

        public async Task GetPhotos()
        {
            try
            {
                List<Photo> imageList = new List<Photo>();
                if (App.SaveToCameraRollEnabled)
                {
                    using (MediaLibrary library = new MediaLibrary())
                    {
                        foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                        {
                            if (album.Name == "Camera Roll")
                            {
                                var images = from r in album.Pictures where r.Name.StartsWith("mapi_thumb_")  select r;
                                foreach(var item in images)
                                {
                                    Photo imageData = new Photo();
                                    imageData.TimeStamp = item.Date;
                                    imageData.Title = item.Name.Replace("mapi_thumb_", "");
                                    BitmapImage bi = new BitmapImage();
                                    using (var stream = item.GetImage())
                                    {
                                        bi.SetSource(stream);
                                    }
                                    imageData.ThumbBitmapImage = bi;
                                    imageData.File = null;
                                    imageData.ThumbFile = null;
                                    imageData.ImageSource = null;
                                    imageList.Add(imageData);
                                }
                            }
                        }
                    }

                }
                else
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!storage.DirectoryExists("shared\\transfers"))
                        {
                            m_photos = new ObservableCollection<Photo>();
                        }

                        var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
                        var images = await folder.GetFilesAsync();
                        var imagesNoThumb = from r in images where !r.Name.StartsWith("thumb_") select r;
                        foreach (var item in imagesNoThumb)
                        {
                            Photo imageData = new Photo();
                            imageData.TimeStamp = item.DateCreated.DateTime;
                            imageData.Title = item.Name;
                            imageData.File = item;
                            imageData.ThumbFile = await folder.GetFileAsync("thumb_" + item.Name);
                            imageData.ImageSource = new BitmapImage(new Uri(imageData.ThumbFile.Path)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                            imageList.Add(imageData);
                        }

                    }
                }

                PhotoList = new ObservableCollection<Photo>(imageList);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                m_photos = null;
            }
        }

        public async Task GetPhotos(string sequenceId)
        {
            try
            {
                List<Photo> imageList = new List<Photo>();
                if (App.SaveToCameraRollEnabled)
                {
                    using (MediaLibrary library = new MediaLibrary())
                    {
                        foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                        {
                            if (album.Name == "Camera Roll")
                            {
                                IEnumerable<Picture> images = null;
                                if (sequenceId == "noseq")
                                {
                                    images = from r in album.Pictures where r.Name.StartsWith("mapi_thumb_")  select r;
                                }
                                else
                                {
                                    images = from r in album.Pictures where r.Name.StartsWith("mapi_thumb_") && r.Name.EndsWith(sequenceId) select r;
                                }

                                foreach (var item in images)
                                {
                                    Photo imageData = new Photo();
                                    imageData.TimeStamp = item.Date;
                                    imageData.Title = item.Name.Replace("mapi_thumb_", "");
                                    BitmapImage bi = new BitmapImage();
                                    using (var stream = item.GetImage())
                                    {
                                        bi.SetSource(stream);
                                    }
                                    imageData.ThumbBitmapImage = bi;
                                    imageData.File = null;
                                    imageData.ThumbFile = null;
                                    imageData.ImageSource = null;
                                    imageList.Add(imageData);
                                }
                            }
                        }
                    }

                }
                else
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!storage.DirectoryExists("shared\\transfers"))
                        {
                            m_photos = new ObservableCollection<Photo>();
                        }

                        var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
                        var images = await folder.GetFilesAsync();
                        IEnumerable<StorageFile> imagesNoThumb = null;
                        if (sequenceId == "noseq")
                        {
                            imagesNoThumb = from r in images where !r.Name.StartsWith("thumb_") select r;
                        }
                        else
                        {
                            imagesNoThumb = from r in images where !r.Name.StartsWith("thumb_") && r.Name.EndsWith(sequenceId) select r;
                        }
                        foreach (var item in imagesNoThumb)
                        {
                            Photo imageData = new Photo();
                            imageData.TimeStamp = item.DateCreated.DateTime;
                            imageData.Title = item.Name;
                            imageData.File = item;
                            imageData.ThumbFile = await folder.GetFileAsync("thumb_" + item.Name);
                            imageData.ImageSource = new BitmapImage(new Uri(imageData.ThumbFile.Path)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                            imageList.Add(imageData);
                        }

                    }
                }

                PhotoList = new ObservableCollection<Photo>(imageList);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                m_photos = null;
            }
        }

        public async Task GetSequences()
        {
            try
            {
                var list = new List<Sequence>();
                if (App.SaveToCameraRollEnabled)
                {
                    using (MediaLibrary library = new MediaLibrary())
                    {
                        foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                        {
                            if (album.Name == "Camera Roll")
                            {
                                var images = from r in album.Pictures where r.Name.StartsWith("mapi_thumb_") select r;
                                foreach (var item in images)
                                {
                                    string sequenceId = "noseq";
                                    BitmapImage bi = new BitmapImage();
                                    using (var stream = item.GetImage())
                                    {
                                        bi.SetSource(stream);
                                    }
                                    
                                    var split = item.Name.Split(new char[] { '#' }, StringSplitOptions.None);
                                    if (split.Length == 2)
                                    {
                                        sequenceId = split[1];
                                    }
                                    if (list.Count == 0)
                                    {
                                        list.Add(new Sequence()
                                        {
                                            CanDeleteVisibility = App.SaveToCameraRollEnabled ? Visibility.Collapsed : Visibility.Visible,
                                            SequenceId = sequenceId,
                                            ThumbBitmapImage = bi,
                                            TimeStamp = item.Date,
                                            Count = 1
                                        });
                                    }
                                    else
                                    {
                                        var seq = list.Find(r => r.SequenceId == sequenceId);
                                        if (seq != null)
                                        {
                                            seq.Count++;
                                        }
                                        else
                                        {
                                            list.Add(new Sequence()
                                            {
                                                CanDeleteVisibility = App.SaveToCameraRollEnabled ? Visibility.Collapsed : Visibility.Visible,
                                                SequenceId = sequenceId,
                                                ThumbBitmapImage = bi,
                                                TimeStamp = item.Date,
                                                Count = 1
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!storage.DirectoryExists("shared\\transfers"))
                        {
                            m_photos = new ObservableCollection<Photo>();
                        }

                        var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared\\transfers");
                        var images = await folder.GetFilesAsync();
                        var imagesNoThumb = from r in images where !r.Name.StartsWith("thumb_") select r;
                        foreach (var item in imagesNoThumb)
                        {
                            string sequenceId = "noseq";
                            var split = item.Name.Split(new char[] { '#' }, StringSplitOptions.None);
                            if (split.Length == 2)
                            {
                                sequenceId = split[1];
                            }

                            var thumbFile = await folder.GetFileAsync("thumb_" + item.Name);
                            if (list.Count == 0)
                            {
                                list.Add(new Sequence() {
                                    SequenceId = sequenceId,
                                    ThumbFile = thumbFile,
                                    ThumbBitmapImage = new BitmapImage(new Uri(thumbFile.Path)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                                    TimeStamp = item.DateCreated.DateTime,
                                    Count = 1
                                });
                            }
                            else
                            {
                                var seq = list.Find(r => r.SequenceId == sequenceId);
                                if (seq != null)
                                {
                                    seq.Count++;
                                }
                                else
                                {
                                    list.Add(new Sequence()
                                    {
                                        SequenceId = sequenceId,
                                        ThumbFile = thumbFile,
                                        ThumbBitmapImage = new BitmapImage(new Uri(thumbFile.Path)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                                        TimeStamp = item.DateCreated.DateTime,
                                        Count = 1
                                    });
                                }
                            }
                        }

                    }
                }

                Sequences = new ObservableCollection<Sequence>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                m_photos = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
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
