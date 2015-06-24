using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Devices;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.IO;
using Windows.Phone.Media.Capture;
using Microsoft.Xna.Framework.Media;
using Windows.Devices.Sensors;
using System.Device.Location;
using Windows.Foundation;
using Mapillary.Models;
using System.Globalization;
using Microsoft.Phone.Info;
using System.Reflection;
using Mapillary.Services;
using System.Security.Cryptography;
using System.Text;
using ExifLibrary;
using ExifLibrary.Phone;
using Windows.Storage;

namespace Mapillary
{
    public enum CaptureMode { Panorama = 0, Single = 1, Sequence = 2 };
    public partial class CapturePage : PhoneApplicationPage
    {
        private VideoBrush viewfinderBrush;
        private Compass m_compass;
        private CompassReading m_compassReading;
        private double m_calculatedHeading;
        PhotoCaptureDevice Camera;
        Guid m_sequenceGuid = Guid.NewGuid();
        private CaptureMode m_captureMode = CaptureMode.Single;
        private bool m_lowaccuracy;
        private bool m_canTakePicture;
        private Accelerometer m_accelerometer;
        private double m_angle;
        private bool m_sequenceIsStarted;
        private GeoCoordinate m_currentposition;
        private string m_deviceMake;
        private string m_deviceModel;
        private AccelerometerReading m_accReading;
        public CapturePage()
        {
            InitializeComponent();
            SetupAccelorometer();
            messageBorder.Visibility = Visibility.Collapsed;
            cameraButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); //none
            m_sequenceIsStarted = false;
            SetCaptureMode(CaptureMode.Single);
            m_deviceMake = DeviceStatus.DeviceManufacturer;
            m_deviceModel = DeviceStatus.DeviceName;
            var ver = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            m_appversion = "WP " + ver.Version.ToString();
#if DEBUG
            currentPosText.Visibility = Visibility.Visible;
#endif
        }

        private bool m_slowMessageShown;
        private double m_currentSpeed;

        private bool IsMoving()
        {
            return m_currentSpeed >=2.79; // 10 km/h
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(async () =>
            {
                base.OnNavigatedTo(e);
                App.SetLockMode(true);

                await InitCamera();
                InitCompass();
                await InitGps();
                CheckAvailableMem();
                CheckBattery();
            }));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (!m_canTakePicture)
            {
                e.Cancel = true;
                StopSequence();
                ShowMessage("Sequence was stopped.");
                return;
            }

            base.OnNavigatingFrom(e);

            StopSequence();
            App.SetLockMode(false);
            ShutDownCompass();
            ShutDownCamera();
            CameraButtons.ShutterKeyHalfPressed -= OnCameraButtonHalfPress;
            CameraButtons.ShutterKeyPressed -= OnCameraButtonFullPress;
            if (App.GeoWatcher != null)
            {
                App.GeoWatcher.Stop();
                App.GeoWatcher.StatusChanged -= GeoWatcher_StatusChanged;
                App.GeoWatcher.PositionChanged -= GeoWatcher_PositionChanged;
            }
        }

        private void ShutDownCompass()
        {
            if (m_compass != null)
            {
                m_compass.ReportInterval = 0;
                m_compass.ReadingChanged -= CompassReadingChanged;
                m_compass = null;
            }
        }

        private void SetCaptureMode(CaptureMode captureMode)
        {
            cameraButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); //none
            m_captureMode = captureMode;
            if (m_captureMode == CaptureMode.Single)
            {
                modeButton.ImageSource = new BitmapImage(new Uri(@"/Assets/appbar.man.walk.png", UriKind.Relative));
            }

            if (m_captureMode == CaptureMode.Sequence)
            {
                modeButton.ImageSource = new BitmapImage(new Uri(@"/Assets/appbar.transit.car.png", UriKind.Relative));
            }


            if (m_captureMode == CaptureMode.Panorama)
            {
                modeButton.ImageSource = new BitmapImage(new Uri(@"/Assets/appbar.image.landscape.png", UriKind.Relative));
            }

        }

        private void SetupAccelorometer()
        {
            m_accelerometer = Accelerometer.GetDefault();
            if (m_accelerometer != null)
            {
                m_accelerometer.ReadingChanged += (s, e) => Dispatcher.BeginInvoke(() =>
                                {
                                    var reading = m_accelerometer.GetCurrentReading();
                                    m_accReading = reading;
                                    m_angle = Math.Atan2(-reading.AccelerationX, reading.AccelerationY) * 180.0 / Math.PI;
                                    Microsoft.Phone.Controls.PageOrientation orientation = (App.Current.RootVisual as PhoneApplicationFrame).Orientation;
                                    if (!OrientationIsLandscape())
                                    {
                                        landscapeMessageBorder.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        landscapeMessageBorder.Visibility = Visibility.Collapsed;
                                    }

                                });
            }
        }

        private void InitCompass()
        {
            m_compass = Compass.GetDefault();
            if (m_compass != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = m_compass.MinimumReportInterval;
                uint reportInterval = minReportInterval > 16 ? minReportInterval : 16;
                m_compass.ReportInterval = reportInterval;
                m_compass.ReadingChanged += CompassReadingChanged;
            }
        }

        private void CompassReadingChanged(object sender, CompassReadingChangedEventArgs e)
        {
            m_compassReading = e.Reading;
            string val = "(none)";
            if (m_compassReading != null)
            {
                val = Math.Round(Adjust90(m_compassReading.HeadingMagneticNorth), 0).ToString() + "°";
            }

            Dispatcher.BeginInvoke(() => { compassText.Text = "Compass: " + val; });

        }

        private void CheckBattery()
        {
            if (Windows.Phone.Devices.Power.Battery.GetDefault().RemainingChargePercent <= 10)
            {
                ShowMessage("Less than 10 % remaining battery");
            }
        }

        private void CheckAvailableMem()
        {
            var sp = IsolatedStorageFile.GetUserStoreForApplication().AvailableFreeSpace;
            if (sp < 30000000)
            {
                MessageBox.Show("Your phone is running low on available storage space. Less than 30 Mb free.", "Warning", MessageBoxButton.OK);
            }
        }

        private void ShutDownCamera()
        {
            if (Camera != null)
            {
                Camera.Dispose();
                Camera = null;
            }
        }


        private async Task InitGps()
        {
            messageBorder.Visibility = Visibility.Collapsed;

            lock (typeof(App))
            {
                if (App.GeoWatcher == null || !App.GpsIsReady)
                {
                    App.GeoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                    App.GeoWatcher.MovementThreshold = 0;
                }

                App.GeoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(GeoWatcher_StatusChanged);
                App.GeoWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoWatcher_PositionChanged);
                App.GeoWatcher.Start();
                m_currentposition = App.GeoWatcher.Position.Location;
            }

            if (App.GeoLocator != null && App.GeoLocator.LocationStatus == Windows.Devices.Geolocation.PositionStatus.Ready)
            {
                var pos = await App.GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                var accuracy = pos.Coordinate.Accuracy;
                CheckAccuracy(accuracy);
            }
            else
            {
                ShowMessage("No GPS detected. Please wait.");
                accuracyText.Text = "NO GPS";
                cameraButton.IsEnabled = false;
                m_lowaccuracy = true;
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0x1E, 0x61)); //red
                await Task.Delay(1000);
                int retrys = 10;
                for (int i = 0; i < retrys; i++)
                {
                    if (App.GeoLocator != null && App.GeoLocator.LocationStatus == Windows.Devices.Geolocation.PositionStatus.Ready)
                    {
                        var pos = await App.GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                        var accuracy = pos.Coordinate.Accuracy;
                        CheckAccuracy(accuracy);
                        break;
                    }

                    await Task.Delay(2000);
                }
            }


        }

        private void GeoWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            m_currentposition = e.Position.Location;
            m_calculatedHeading = e.Position.Location.Course;
            m_currentSpeed = e.Position.Location.Speed;
#if DEBUG
            currentPosText.Text = "Speed " + m_currentSpeed.ToString();
#endif
            double accuracy = m_currentposition.HorizontalAccuracy;
            if (m_compass == null)
            {
                string val = "(none)";
                val = Math.Round(m_calculatedHeading, 0).ToString() + "°";
                compassText.Text = "Compass: " + val;
                Debug.WriteLine("DIR:" + val);
            }
            CheckAccuracy(accuracy);
        }

        private void CheckAccuracy(double accuracy)
        {
            m_lowaccuracy = false;
            if (accuracy < 15)
            {
                cameraButton.IsEnabled = true;
                accuracyText.Text = "Accuracy " + Math.Round(accuracy, 0).ToString() + " m";
                if (accuracy < 8)
                {
                    SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0x81, 0xC4, 0x00)); //Green
                }
                else
                {
                    SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0xC8, 0x00)); // Yellow
                }
            }
            else
            {
                m_lowaccuracy = true;
                ShowMessage("Too low GPS accuracy. Please wait.");
                cameraButton.IsEnabled = false;
                accuracyText.Text = "Accuracy " + " >15 m";
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0x1E, 0x61)); //red
            }
        }

        private async void GeoWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled)
            {
                cameraButton.IsEnabled = false;
                ShowMessage("GPS is disabled");
                accuracyText.Text = "NO GPS";
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0x1E, 0x61)); //red
                m_lowaccuracy = true;
            }

            if (e.Status == GeoPositionStatus.Initializing)
            {
                cameraButton.IsEnabled = false;
                ShowMessage("GPS is initializing");
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0xC8, 0x00)); //yellow
                m_lowaccuracy = true;
            }

            if (e.Status == GeoPositionStatus.NoData)
            {
                cameraButton.IsEnabled = false;
                ShowMessage("No GPS signal");
                accuracyText.Text = "NO GPS";
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0x1E, 0x61)); //red
                m_lowaccuracy = true;
            }

            if (e.Status == GeoPositionStatus.Ready)
            {
                cameraButton.IsEnabled = true;
                ShowMessage("Ready");
                accuracyText.Text = string.Empty;
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0x81, 0xC4, 0x00)); //Green

            }

            if (App.GeoLocator != null && App.GeoLocator.LocationStatus == Windows.Devices.Geolocation.PositionStatus.Ready)
            {
                var pos = await App.GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                var accuracy = pos.Coordinate.Accuracy;
                CheckAccuracy(accuracy);
            }
            else
            {
                cameraButton.IsEnabled = false;
                m_lowaccuracy = true;
                SetGpsIndicatiorColor(Color.FromArgb(0xFF, 0xFF, 0x1E, 0x61)); //red
                ShowMessage("No GPS detected");
                accuracyText.Text = "NO GPS";
            }

        }

        private void ShowMessage(string message)
        {
            if (OrientationIsLandscape())
            {
                messageBorder.Margin = new Thickness(0, 0, 90, 400);
                messageAngle.Angle = 0;
                messageBorder.Width = 540;
            }
            else
            {
                messageBorder.Margin = new Thickness(0, 0, -310, -25);
                messageAngle.Angle = -90;
                messageBorder.Width = 440;
            }
            messageText.Visibility = Visibility.Visible;
            messageBorder.Visibility = Visibility.Visible;
            messageText.Text = message;
            messageFaderSb.Begin();
        }

        private void SetGpsIndicatiorColor(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            foreach (var path in gpsIndicator.Children)
            {
                if (path is System.Windows.Shapes.Path)
                {
                    ((System.Windows.Shapes.Path)path).Stroke = brush;
                }
            }
        }

        private async Task InitCamera()
        {
            if (!PhotoCaptureDevice.AvailableSensorLocations.Contains(CameraSensorLocation.Back))
            {
                MessageBox.Show("No primary camera found. Unable to capture photos.", "Error", MessageBoxButton.OK);
                cameraButton.IsEnabled = false;
                return;
            }

            System.Collections.Generic.IReadOnlyList<Windows.Foundation.Size> supportedResolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);

            Windows.Foundation.Size res = GetResolution(supportedResolutions);
            Camera = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Back, res);
            Camera.SetProperty(KnownCameraPhotoProperties.FlashMode, FlashState.Off);
            Camera.SetProperty(KnownCameraGeneralProperties.PlayShutterSoundOnCapture, App.ShutterSoundEnabled);
            Camera.SetProperty(KnownCameraGeneralProperties.AutoFocusRange, AutoFocusRange.Normal);
            CameraButtons.ShutterKeyHalfPressed += OnCameraButtonHalfPress;
            CameraButtons.ShutterKeyPressed += OnCameraButtonFullPress;

            var viewFinderTransform = new CompositeTransform();
            viewFinderTransform.CenterX = 0.5;
            viewFinderTransform.CenterY = 0.5;
            viewfinderBrush = new VideoBrush();
            viewfinderBrush.RelativeTransform = viewFinderTransform;
            viewfinderBrush.Stretch = Stretch.UniformToFill;
            camCanvas.Background = viewfinderBrush;
            viewfinderBrush.SetSource(Camera);
            m_canTakePicture = true;
        }

        private Windows.Foundation.Size GetResolution(IReadOnlyList<Windows.Foundation.Size> supportedResolutions)
        {
            double aspect = 4f / 3f;
            var res43list = from r in supportedResolutions where (r.Width / r.Height) == aspect select r;
            if (res43list.Count() > 0)
            {
                return res43list.OrderByDescending(r => r.Width).First();
            }
            else
            {
                return supportedResolutions.First();
            }

        }

        private async Task CapturePhoto()
        {
            try
            {
                if (Camera == null) return;
                m_canTakePicture = false;
                int encodedOrientation = 0;
                int sensorOrientation = (Int32)Camera.SensorRotationInDegrees;

                switch (this.Orientation)
                {
                    // Camera hardware shutter button up.
                    case PageOrientation.LandscapeLeft:
                        encodedOrientation = -90 + sensorOrientation;
                        break;
                    // Camera hardware shutter button down.
                    case PageOrientation.LandscapeRight:
                        encodedOrientation = 90 + sensorOrientation;
                        break;
                    // Camera hardware shutter button right.
                    case PageOrientation.PortraitUp:
                        encodedOrientation = 0 + sensorOrientation;
                        break;
                    // Camera hardware shutter button left.
                    case PageOrientation.PortraitDown:
                        encodedOrientation = 180 + sensorOrientation;
                        break;
                }
                // Apply orientation to image encoding.
                Camera.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation, encodedOrientation);
                MemoryStream stream = new MemoryStream();
                MemoryStream thumbStream = new MemoryStream();
                var sequence = Camera.CreateCaptureSequence(1);
                sequence.Frames[0].CaptureStream = stream.AsOutputStream();
                sequence.Frames[0].ThumbnailStream = thumbStream.AsOutputStream();

                try
                {
                    await Camera.PrepareCaptureSequenceAsync(sequence);
                    await sequence.StartCaptureAsync();
                }
                catch (OperationCanceledException)
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                    if (thumbStream != null)
                    {
                        thumbStream.Close();
                        thumbStream.Dispose();
                    }

                    m_canTakePicture = true;

                    return;
                }

                catch (InvalidOperationException)
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                    if (thumbStream != null)
                    {
                        thumbStream.Close();
                        thumbStream.Dispose();
                    }

                    m_canTakePicture = true;
                    return;
                }

                stream.Seek(0, SeekOrigin.Begin);
                thumbStream.Seek(0, SeekOrigin.Begin);

                try
                {
                    if (App.SaveToCameraRollEnabled)
                    {
                        SaveImageToCameraRoll(stream, thumbStream);
                    }
                    else
                    {
                        SaveImageToIsoStore(stream, thumbStream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot save captured photo. Error=" + ex.Message);
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                    if (thumbStream != null)
                    {
                        thumbStream.Close();
                        thumbStream.Dispose();
                    }
                }

                m_canTakePicture = true;
            }
            catch (Exception exp)
            {
                MessageBox.Show("Capturing photo failed. Please retry. Error=" + exp.Message);
                m_canTakePicture = true;
            }
        }


        private void SaveImageToCameraRoll(Stream stream, MemoryStream thumbStream)
        {
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            ExifFile exif = ExifFile.ReadStream(stream);
            string metadata = GetMetadata(fileName);
            byte[] data = Encoding.UTF8.GetBytes(metadata);
            uint length = (uint)data.Length;
            ushort type = 2;
            var imgDescProp = ExifPropertyFactory.Get(0x010e, type, length, data, BitConverterEx.ByteOrder.BigEndian, IFD.Zeroth);
            exif.Properties.Add(ExifTag.ImageDescription, imgDescProp);

            using (var ml = new MediaLibrary())
            {
                exif.SaveToCameraRoll("mapi_" + fileName + ".jpg", ml);
                using (var memstream = new MemoryStream())
                {
                    WriteableBitmap bmp = new WriteableBitmap(100, 75);
                    bmp = bmp.FromStream(thumbStream);
                    WriteableBitmap tBmp = bmp.Resize(100, 75, WriteableBitmapExtensions.Interpolation.NearestNeighbor);
                    tBmp.SaveJpeg(memstream, 100, 75, 0, 90);
                    memstream.Seek(0, SeekOrigin.Begin);
                    ml.SavePictureToCameraRoll("mapi_thumb_" + fileName + ".jpg", memstream);
                }
            }

            stream.Close();
            thumbStream.Close();
            stream.Dispose();
            thumbStream.Dispose();
        }

        private void SaveImageToIsoStore(Stream stream, MemoryStream thumbStream)
        {
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            ExifFile exif = ExifFile.ReadStream(stream);
            string metadata = GetMetadata(fileName);
            byte[] data = Encoding.UTF8.GetBytes(metadata);
            uint length = (uint)data.Length;
            ushort type = 2;
            var imgDescProp = ExifPropertyFactory.Get(0x010e, type, length, data, BitConverterEx.ByteOrder.BigEndian, IFD.Zeroth);
            exif.Properties.Add(ExifTag.ImageDescription, imgDescProp);
            using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isStore.DirectoryExists("shared"))
                {
                    isStore.CreateDirectory("shared");
                }

                if (!isStore.DirectoryExists("shared\\transfers"))
                {
                    isStore.CreateDirectory("shared\\transfers");
                }
                using (IsolatedStorageFileStream targetStream = isStore.OpenFile(@"shared\\transfers\" + fileName + ".jpg", FileMode.Create, FileAccess.Write))
                {
                    exif.SaveStream(targetStream);
                }


                using (IsolatedStorageFileStream thumbtargetStream = isStore.OpenFile(@"shared\\transfers\thumb_" + fileName + ".jpg", FileMode.Create, FileAccess.Write))
                {
                    WriteableBitmap bmp = new WriteableBitmap(100, 75);
                    bmp = bmp.FromStream(thumbStream);
                    WriteableBitmap tBmp = bmp.Resize(100, 75, WriteableBitmapExtensions.Interpolation.NearestNeighbor);
                    tBmp.SaveJpeg(thumbtargetStream, 100, 75, 0, 90);
                }

                stream.Close();
                thumbStream.Close();
                stream.Dispose();
                thumbStream.Dispose();
            }
        }

        private string GetMetadata(string fileName)
        {
            var mapiData = new MAPIData();
            var enUs = new CultureInfo("en-US");
            mapiData.MAPLatitude = m_currentposition.Latitude.ToString(enUs);
            mapiData.MAPLongitude = m_currentposition.Longitude.ToString(enUs);
            mapiData.MAPAltitude = m_currentposition.Altitude.ToString(enUs);
            if (m_accReading != null)
            {
                mapiData.MAPAccelerometerVector = new AccVector()
                {
                    x = m_accReading.AccelerationX.ToString(enUs),
                    y = m_accReading.AccelerationY.ToString(enUs),
                    z = m_accReading.AccelerationZ.ToString(enUs)
                };
            }
            mapiData.MAPCameraMode = ((int)m_captureMode).ToString();
            mapiData.MAPCameraRotation = GetCameraRotation();
            mapiData.MAPCaptureTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            mapiData.MAPCompassHeading = new CompassHeading();
            if (m_compassReading != null)
            {
                mapiData.MAPCompassHeading.MagneticHeading = Adjust90(m_compassReading.HeadingMagneticNorth).ToString(enUs);
                mapiData.MAPCompassHeading.TrueHeading = m_compassReading.HeadingTrueNorth.HasValue ? Adjust90(m_compassReading.HeadingTrueNorth.Value).ToString(enUs) : mapiData.MAPCompassHeading.MagneticHeading;
            }
            else
            {
                mapiData.MAPCompassHeading.MagneticHeading = m_calculatedHeading.ToString(enUs);
                mapiData.MAPCompassHeading.TrueHeading = m_calculatedHeading.ToString(enUs);
            }

            mapiData.MAPDeviceMake = m_deviceMake;
            mapiData.MAPDeviceModel = m_deviceModel;
            mapiData.MAPGPSAccuracyMeters = m_currentposition.HorizontalAccuracy.ToString(enUs);
            mapiData.MAPLightSensor = "";
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            string tz = String.Format("{0}{1:00}{2:00}", (localZone.BaseUtcOffset >= TimeSpan.Zero) ? "+" : "-", Math.Abs(localZone.BaseUtcOffset.Hours), Math.Abs(localZone.BaseUtcOffset.Minutes));
            mapiData.MAPLocalTimeZone = tz;
            mapiData.MAPPhotoUUID = Guid.NewGuid().ToString();
            mapiData.MAPSequenceCaptureUsed = m_sequenceIsStarted ? "1" : "0";
            mapiData.MAPSequenceUUID = m_sequenceGuid.ToString();
            mapiData.MAPSettingsEmail = LoginService.SignInEmail;
            mapiData.MAPSettingsProject = "";
            string hashString = LoginService.UploadToken + LoginService.SignInEmail + fileName;
            mapiData.MAPSettingsUploadHash = GetUploadHash(hashString);
            mapiData.MAPVersionString = m_appversion;
            return mapiData.GetAsJsonString();
        }

        private double Adjust90(double val)
        {
            double v = val + 90;
            if (v >= 360) v = v - 360;
            return v;
        }

        private string GetUploadHash(string hashString)
        {

            SHA256Managed crypt = new SHA256Managed();
            byte[] digest = crypt.ComputeHash(Encoding.UTF8.GetBytes(hashString), 0, Encoding.UTF8.GetByteCount(hashString));

            string hash = ByteArrayToString(digest);
            return hash;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        private string GetCameraRotation()
        {
            if (OrientationIsLandscape())
            {
                return "0";
            }
            else
            {
                return "90";
            }
        }

        private void OnCameraButtonFullPress(object sender, EventArgs e)
        {
            cameraButton_Click(null, null);
        }


        private async void OnCameraButtonHalfPress(object sender, EventArgs e)
        {
            try
            {
                if (m_canTakePicture)
                {
                    await Camera.FocusAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        private async void cameraButton_Click(object sender, RoutedEventArgs e)
        {
            m_slowMessageShown = false;
            if (!m_canTakePicture)
            {
                return;
            }

            if (m_captureMode == CaptureMode.Sequence && (m_sequenceIsStarted || m_lowaccuracy))
            {
                StopSequence();
                return;
            }

            if (m_captureMode == CaptureMode.Sequence && !m_sequenceIsStarted)
            {
                StartSequence();
            }

            if (m_captureMode == CaptureMode.Sequence)
            {
                while (m_sequenceIsStarted)
                {
                    await Task.Delay(App.CaptureInterval);
                    CheckAvailableMem();
                    if (IsMoving())
                    {
                        m_slowMessageShown = false;
                        flashBorder.Color = Colors.White;
                        await CapturePhoto();
                        flashBorder.Color = Color.FromArgb(0, 0, 0, 0);
                    }
                    else
                    {
                        if (!m_slowMessageShown)
                        {
                            ShowMessage("Move faster to take pictures");
                            m_slowMessageShown = true;
                        }
                    }
                }
            }
            else
            {
                await StartCapture();
            }
        }

        private async Task StartCapture()
        {
            m_slowMessageShown = false;

            CheckAvailableMem();
            CheckBattery();

            if (!m_canTakePicture || m_lowaccuracy) return;


            flashBorder.Color = Colors.White;
            cameraButton.IsEnabled = false;
            await CapturePhoto();
            cameraButton.IsEnabled = true;
            flashBorder.Color = Color.FromArgb(0, 0, 0, 0);
        }

        private void StopSequence()
        {
            m_sequenceIsStarted = false;
            cameraButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); //none
        }

        private void StartSequence()
        {
            m_sequenceIsStarted = true;
            m_currentSpeed = 0;
            cameraButton.Background = new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0, 0)); //red
        }


        private void singleModeButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            modePopup.IsOpen = false;
            if (m_captureMode == CaptureMode.Single) return;
            StopSequence();
            SetCaptureMode(CaptureMode.Single);
            ShowMessage("Changing mode to walking");
            NewSequence();
        }

        private void sequenceModeButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            modePopup.IsOpen = false;
            if (m_captureMode == CaptureMode.Sequence) return;
            ShowMessage("Changing mode to riding");
            SetCaptureMode(CaptureMode.Sequence);
            NewSequence();
        }

        private void panoramaModeButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            modePopup.IsOpen = false;
            if (m_captureMode == CaptureMode.Panorama) return;
            StopSequence();
            SetCaptureMode(CaptureMode.Panorama);
            ShowMessage("Changing mode to panorama");
            NewSequence();
        }

        private void modeButton_Click(object sender, RoutedEventArgs e)
        {
            if (modePopup.IsOpen)
            {
                modePopup.IsOpen = false;
                return;
            }
            if (OrientationIsLandscape())
            {
                modePopupTransform.Angle = 0;
                modePopupTransform.CenterX = 0;
                modePopupTransform.CenterY = 0;
            }
            else
            {
                modePopupTransform.Angle = -90;
                modePopupTransform.CenterX = -165;
                modePopupTransform.CenterY = -185;
            }

            modePopup.IsOpen = true;
        }

        private bool OrientationIsLandscape()
        {
            if (m_angle > 135 && m_angle < 180 || m_angle < -135 && m_angle > -180 || m_angle < 45 && m_angle > -45)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool OrientationIsLandscape(double angle)
        {
            if (angle > 135 && angle < 180 || angle < -135 && angle > -180 || angle < 45 && angle > -45)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void newSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("New sequence started");
            NewSequence();
        }

        private void NewSequence()
        {
            m_sequenceGuid = Guid.NewGuid();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (Camera != null)
            {
                // LandscapeRight rotation when camera is on back of phone.
                int landscapeRightRotation = 180;

                // Rotate video brush from camera.
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    // Rotate for LandscapeRight orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = landscapeRightRotation };
                }
                else
                {
                    // Rotate for standard landscape orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
                }
            }

            base.OnOrientationChanged(e);
        }
        public string m_appversion { get; set; }
    }
}