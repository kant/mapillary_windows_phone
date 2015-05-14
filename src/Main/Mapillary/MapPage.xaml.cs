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
using Windows.Devices.Geolocation;
using System.Globalization;

namespace Mapillary
{
    public partial class MapPage : PhoneApplicationPage
    {
        bool firstcall = true;
        string userAgent = "mozilla/5.0 (iphone; cpu iphone os 7_0_2 like mac os x) applewebkit/537.51.1 (khtml, like gecko) version/7.0 mobile/11a501 safari/9537.53";
        public MapPage()
        {
            InitializeComponent();
            this.Loaded += MapPage_Loaded;
        }

        private async void MapPage_Loaded(object sender, RoutedEventArgs e)
        {
            firstcall = true;
            if (App.GeoLocator != null)
            {
                Geoposition pos = await App.GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                string lat = pos.Coordinate.Latitude.ToString(new CultureInfo("en-US"));
                string lng = pos.Coordinate.Longitude.ToString(new CultureInfo("en-US"));
                browser.Navigate(new Uri("http://www.mapillary.com/map/im/16/" + lat + "/" + lng + "?showCenter&client=iphone&client-version=1.8"), null, "User-Agent: " + userAgent);
                // http://www.mapillary.com/map/im/16/%f/%f?show-center&client=iphone&client-version=1.8
            }
        }

        private void browser_Navigating(object sender, NavigatingEventArgs e)
        {
            if (!firstcall)
            {
                e.Cancel = true;
                browser.Navigate(e.Uri, null, "User-Agent: " + userAgent);
            }
            else
            {
                firstcall = false;
            }
        }

        private void browser_Navigated(object sender, NavigationEventArgs e)
        {
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

    }
}