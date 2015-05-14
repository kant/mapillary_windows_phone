using Mapillary.Models;
using Microsoft.Phone.BackgroundTransfer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace Mapillary
{
    public class UploadInfo : INotifyPropertyChanged
    {
        private StorageFile m_file;
        private int m_percentageDone;
        private bool m_isUploaded;
        private bool m_isUploading;
        private ulong m_size;
        private string m_path;
        private bool m_failed;
        private string m_errorMessage;


        public UploadInfo()
        {
            Failed = false;
            OnPropertyChanged("StatusImage");
        }

        public HttpStatusCode StatusCode { get; set; }

        public Exception Exception { get; set; }
        public BitmapImage StatusImage
        {
            get 
            {
                if (Failed)
                {
                    return new BitmapImage(new Uri("/Assets/error.png", UriKind.Relative));
                }

                if(IsUploading)
                {
                    return new BitmapImage(new Uri("/Assets/running.png", UriKind.Relative));
                }

                return new BitmapImage(new Uri("/Assets/stopped.png", UriKind.Relative));

            }
        }

        public StorageFile File
        {
            get
            {
                return m_file;
            }
            set
            {
                m_file = value;
                OnPropertyChanged("File");
            }
        }

        public string PercentageString
        {
            get
            {
                return "(" + m_percentageDone + "%)";
            }
        }
        public int PercentageDone         
        {
            get
            {
                return m_percentageDone;
            }
            set
            {
                m_percentageDone = value;
                OnPropertyChanged("PercentageDone");
                OnPropertyChanged("PercentageString");
                OnPropertyChanged("GreenWidth");
                OnPropertyChanged("GrayWidth");
            }
        }

        public bool IsUploaded 
        {
            get
            {
                return m_isUploaded;
            }
            set
            {
                m_isUploaded = value;
                OnPropertyChanged("IsUploaded");
            }
        }
        public bool IsUploading
        {
            get
            {
                return m_isUploading;
            }
            set
            {
                m_isUploading = value;
                OnPropertyChanged("IsUploading");
                OnPropertyChanged("StatusImage");
            }
        }
        public ulong Size
        {
            get
            {
                return m_size;
            }
            set
            {
                m_size = value;
                OnPropertyChanged("Size");
            }
        }
        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value;
                OnPropertyChanged("Path");
            }
        }

        public bool Failed
        {
            get
            {
                return m_failed;
            }
            set
            {
                m_failed = value;
                OnPropertyChanged("Failed");
                OnPropertyChanged("ErrorVisibility");
                OnPropertyChanged("StatusImage");
            }
        }

        public string ErrorMessage 
        {
            get
            {
                return m_errorMessage;
            }
            set
            {
                m_errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }
        
        public Visibility ErrorVisibility
        {
            get
            {
                if (Failed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public double GreenWidth
        {
            get
            {
                return 145 * ((double)PercentageDone / 100);
            }
        }

        public double GrayWidth 
        { 
            get
            {
                return 145 - GreenWidth;
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
