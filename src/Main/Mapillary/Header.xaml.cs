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

namespace Mapillary
{
    public partial class Header : UserControl
    {
        public Header()
        {
            InitializeComponent();
        }











































































































        int tapCount;
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                tapCount++;
                if (tapCount == 5)
                {
                    tapCount = 0;
                    var img = new BitmapImage(new Uri(@"/Assets/ronny.jpg", UriKind.Relative));
                    Image image = new Image();
                    image.Tap += image_Tap;
                    image.Source = img;
                    image.Width = 480;
                    image.Height = 853;
                    ((Grid)this.Parent).Children.Add(image);
                }
            }
            catch (Exception)
            {
            }
  
        }

        void image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ((Image)sender).Visibility = Visibility.Collapsed;
            ((Grid)this.Parent).Children.Remove(((Image)sender));
            sender = null;
        }
    }
}
