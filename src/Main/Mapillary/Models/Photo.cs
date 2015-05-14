using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace Mapillary.Models
{
    public class Photo : INotifyPropertyChanged
    {
        private string m_title;
        private Uri m_imageSource;
        private DateTime m_timeStamp;
        private StorageFile m_file;
        private StorageFile m_thumbfile;
        private UploadInfo m_uploadInfo;


        public string Title { get { return m_title; } set { m_title = value; OnPropertyChanged("Title"); } }
        public Uri ImageSource { get { return m_imageSource; } set { m_imageSource = value; OnPropertyChanged("ImageSource"); } }
        public DateTime TimeStamp { get { return m_timeStamp; } set { m_timeStamp = value; OnPropertyChanged("TimeStamp"); } }
        public StorageFile File { get { return m_file; } set { m_file = value; OnPropertyChanged("File"); } }
        public StorageFile ThumbFile { get { return m_thumbfile; } set { m_thumbfile = value; OnPropertyChanged("ThumbFile"); } }
        public UploadInfo UploadInfo { get { return m_uploadInfo; } set { m_uploadInfo = value; OnPropertyChanged("UploadInfo"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public BitmapImage ThumbBitmapImage { get; set; }
    } 
}
