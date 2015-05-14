using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Mapillary.Services;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Tasks;
using System.Reflection;

namespace Mapillary
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private bool m_locationConcentOn;
        public SettingsPage()
        {
            InitializeComponent();
            var ver = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            version.Text = "Version " + ver.Version;
            var captureIntervalString = SettingsHelper.GetValue("CaptureInterval", "2000");
            intervalSlider.Value = int.Parse(captureIntervalString);
            shutterSoundChk.IsChecked = bool.Parse(SettingsHelper.GetValue("ShutterSoundEnabled", Boolean.FalseString));
            storePhotosInCameraRollChk.IsChecked = bool.Parse(SettingsHelper.GetValue("SaveToCameraRollEnabled", Boolean.FalseString));
            intervalValue.Text = App.CaptureInterval.ToString();
            m_locationConcentOn = SettingsHelper.GetValue("LocationConsent", "false") == "true";
            if (m_locationConcentOn)
            {
                locationChk.IsChecked = true;
                locationChk.Content = "Location consent is ON";
            }
            else
            {
                locationChk.IsChecked = false;
                locationChk.Content = "Location consent is OFF";
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SettingsHelper.SetValue("CaptureInterval", App.CaptureInterval.ToString());
            SettingsHelper.SetValue("ShutterSoundEnabled", App.ShutterSoundEnabled ? Boolean.TrueString : Boolean.FalseString);
            SettingsHelper.SetValue("SaveToCameraRollEnabled", App.SaveToCameraRollEnabled ? Boolean.TrueString : Boolean.FalseString);
        }

        private void intervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (intervalSlider != null)
            {
                App.CaptureInterval = (int)intervalSlider.Value;
                intervalValue.Text = App.CaptureInterval.ToString();
            }
        }

        private void shutterSoundChk_Checked(object sender, RoutedEventArgs e)
        {
            if (shutterSoundChk != null)
            {
                App.ShutterSoundEnabled = shutterSoundChk.IsChecked.Value;
            }
        }

        private void storePhotosInCameraRollChk_Checked(object sender, RoutedEventArgs e)
        {
            if (storePhotosInCameraRollChk != null)
            {
                App.SaveToCameraRollEnabled = storePhotosInCameraRollChk.IsChecked.Value;
            }
        }

        private void locationChk_Checked(object sender, RoutedEventArgs e)
        {
            if (locationChk.IsChecked.Value)
            {
                SettingsHelper.SetValue("LocationConsent", "true");
                locationChk.Content = "Location consent is ON";
            }
            else
            {
                SettingsHelper.SetValue("LocationConsent", "false");
                locationChk.Content = "Location consent is OFF";
            }
        }

    }
}