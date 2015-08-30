using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace Mapillary.Models
{
    public class Sequence : INotifyPropertyChanged
    {
        private DateTime m_timeStamp;
        private StorageFile m_thumbfile;
        private BitmapImage m_thumbBitmapImage;
        private Visibility m_canDeleteVisibility;
        
        private string m_sequenceId;
        private int m_count;

        public StorageFile ThumbFile { get { return m_thumbfile; } set { m_thumbfile = value; OnPropertyChanged("ThumbFile"); } }
        public DateTime TimeStamp {
            get
            {
                return m_timeStamp;
            }
            set {
                m_timeStamp = value;
                OnPropertyChanged("TimeStamp");
                OnPropertyChanged("TimeStampStr");
            }
        }

        public string TimeStampStr
        {
            get { return TimeStamp.ToShortDateString(); }
        }
        public int Count { get { return m_count; } set { m_count = value; OnPropertyChanged("Count"); } }
        public string SequenceId { get { return m_sequenceId; } set { m_sequenceId = value; OnPropertyChanged("SequenceId"); } }

        public BitmapImage ThumbBitmapImage { get { return m_thumbBitmapImage; } set { m_thumbBitmapImage = value; OnPropertyChanged("ThumbBitmapImage"); } }
        public Visibility CanDeleteVisibility { get { return m_canDeleteVisibility; } set { m_canDeleteVisibility = value; OnPropertyChanged("CanDeleteVisibility"); } }

        

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
