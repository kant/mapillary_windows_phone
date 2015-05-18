using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Mapillary.Resources;
using Mapillary.Services;
using System.Device.Location;
using System.Collections.Generic;

namespace Mapillary
{
    /* Mapillary App for Windows Phone. Developed By Tommy Ovesen, ***REMOVED***, http://www.facebook.com/ovesen.net */
    public partial class App : Application
    {
        public static DateTime ProfileLastRefreshed { get; set; }
        public static PhoneApplicationFrame RootFrame { get; private set; }
        public static int CaptureInterval { get; set; }
        public static bool ShutterSoundEnabled { get; set; }
        public static bool SaveToCameraRollEnabled { get; set; }
        public static string WP_CLIENT_ID = "MkJKbDA0bnZuZlcxeTJHTmFqN3g1dzo0ZmYxN2MzMTRlYzM1M2E2";
        public static Windows.Devices.Geolocation.Geolocator GeoLocator { get; set; }
        public static List<ImgSequence> SequenceListCache { get; set; }
        public static List<FeedItem> EventListCache { get; set; }
        public static string NumConnProfileCache { get; set; }
        public static string NumMetersProfileCache { get; set; }
        public static string NumPhotosProfileCache { get; set; }
        public static string AboutProfileCache { get; set; }
        public static DateTime FeedLastRefreshed { get; set; }
        public static System.Windows.Media.ImageSource ImageProfileCache { get; set; }
        public App()
        {
            UnhandledException += Application_UnhandledException;
            InitializeComponent();
            InitializePhoneApplication();
            InitializeLanguage();
            Application.Current.Host.Settings.EnableFrameRateCounter = false;

            if (Debugger.IsAttached)
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            var captureIntervalString = SettingsHelper.GetValue("CaptureInterval", "2000");
            CaptureInterval = int.Parse(captureIntervalString);
            ShutterSoundEnabled = bool.Parse(SettingsHelper.GetValue("ShutterSoundEnabled", Boolean.FalseString));
            SaveToCameraRollEnabled = bool.Parse(SettingsHelper.GetValue("SaveToCameraRollEnabled", Boolean.FalseString));
            ValidateLogin();
            GpsIsReady = false;
            InitGps();
        }

        private void ValidateLogin()
        {
            string signinToken = SettingsHelper.GetValue("SignInToken", null);
            string uploadToken = SettingsHelper.GetValue("UploadToken", null);
            string signinEmail = SettingsHelper.GetValue("SignInEmail", null);
            LoginService.SignInToken = signinToken;
            LoginService.SignInEmail = signinEmail;
            LoginService.IsLoggedIn = signinToken != null && uploadToken != null;
        }

        internal static void InitGps()
        {
            lock (typeof(App))
            {
                if (m_geoWatcher == null)
                {
                    m_geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                    m_geoWatcher.MovementThreshold = 0;
                    m_geoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(GeoWatcher_StatusChanged);
                    m_geoWatcher.Start();
                }
            }

        }

        private void StopGps()
        {
            if (m_geoWatcher != null)
            {
                m_geoWatcher.Stop();
                m_geoWatcher.StatusChanged -= GeoWatcher_StatusChanged;
                m_geoWatcher.Dispose();
                m_geoWatcher = null;
                GpsIsReady = false;
            }
        }


        private static void GeoWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                GpsIsReady = true;
            }
        }

        public static void SetLockMode(bool disableLock)
        {
            if (disableLock)
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            else
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            }
        }

        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            GpsIsReady = false;
            App.ProfileLastRefreshed = DateTime.MinValue;
            App.FeedLastRefreshed = DateTime.MinValue;
            App.SequenceListCache = null;
            App.EventListCache = null;
            InitGps();
        }

        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            GpsIsReady = false;
            App.ProfileLastRefreshed = DateTime.MinValue;
            App.EventListCache = null;
            App.SequenceListCache = null;
            InitGps();
        }

        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            StopGps();
        }

        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            StopGps();
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            else
            {
                string err = "?";
                if (e != null && e.ExceptionObject != null && e.ExceptionObject.Message != null)
                    err = e.ExceptionObject.Message;
                MessageBox.Show("An unexpected error occurred: " + err, "Error", MessageBoxButton.OK);
                e.Handled = true;
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;
        private static GeoCoordinateWatcher m_geoWatcher;

        internal static bool GpsIsReady { get; set; }
        internal static GeoCoordinateWatcher GeoWatcher
        {
            get
            {
                return m_geoWatcher;
            }

            set
            {
                m_geoWatcher = value;
            }
        }

        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;
            RootFrame.Navigated += CheckForResetNavigation;
            phoneApplicationInitialized = true;
        }

        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            if (RootVisual != RootFrame)
            {
                RootVisual = RootFrame;
            }

            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            RootFrame.Navigated -= ClearBackStackAfterReset;

            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion


        private void InitializeLanguage()
        {
            try
            {

                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}