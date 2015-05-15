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
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
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
        public static System.Windows.Media.ImageSource ImageProfileCache { get; set; }
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
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
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            GpsIsReady = false;
            App.ProfileLastRefreshed = DateTime.MinValue;
            App.FeedLastRefreshed = DateTime.MinValue;
            App.SequenceListCache = null;
            App.EventListCache = null;
            InitGps();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            GpsIsReady = false;
            App.ProfileLastRefreshed = DateTime.MinValue;
            App.EventListCache = null;
            App.SequenceListCache = null;
            InitGps();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            StopGps();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            StopGps();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
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

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }

        public static DateTime FeedLastRefreshed { get; set; }
    }
}