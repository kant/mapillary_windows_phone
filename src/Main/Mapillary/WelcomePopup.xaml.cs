using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Mapillary
{
    public partial class WelcomePopup : UserControl
    {

        List<Uri> m_images;
        public WelcomePopup()
        {
            InitializeComponent();
            m_images = new List<Uri>();
            m_images.Add(new Uri("/Assets/screen_1_welcome.png", UriKind.Relative));
            m_images.Add(new Uri("/Assets/screen_2_takephotos.png", UriKind.Relative));
            m_images.Add(new Uri("/Assets/screen_3_camera.png", UriKind.Relative));
            m_images.Add(new Uri("/Assets/screen_4_map.png", UriKind.Relative));
            m_images.Add(new Uri("/Assets/screen_5_upload.png", UriKind.Relative));
            m_images.Add(new Uri("/Assets/screen_6_done.png", UriKind.Relative));
            pivot.ItemsSource = m_images;
            SetRect(rect1);
        }

        private void SetRect(System.Windows.Shapes.Rectangle rect)
        {
            rect1.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));
            rect2.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));
            rect3.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));
            rect4.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));
            rect5.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));
            rect6.Fill = new SolidColorBrush(Color.FromArgb(0, 0x34,0xAD,0x6B));

            rect.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x34,0xAD,0x6B));
        }

        private void HyperlinkButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Grid grid = this.Parent as Grid;
            ((Popup)grid.Parent).IsOpen = false;
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Uri)e.AddedItems[0]).ToString().Contains("_1_")) SetRect(rect1);
            if (((Uri)e.AddedItems[0]).ToString().Contains("_2_")) SetRect(rect2);
            if (((Uri)e.AddedItems[0]).ToString().Contains("_3_")) SetRect(rect3);
            if (((Uri)e.AddedItems[0]).ToString().Contains("_4_")) SetRect(rect4);
            if (((Uri)e.AddedItems[0]).ToString().Contains("_5_")) SetRect(rect5);
            if (((Uri)e.AddedItems[0]).ToString().Contains("_6_")) SetRect(rect6);
        }
    }
}
