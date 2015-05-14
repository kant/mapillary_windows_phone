using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapillary.Models
{
    public class MAPIData
    {
        public string MAPLatitude {get; set;}
        public string MAPLongitude { get; set; }
        public string MAPAltitude { get; set; }
        public string MAPGPSAccuracyMeters { get; set; }
        public CompassHeading MAPCompassHeading { get; set; }
        public string MAPVersionString { get; set; }
        public string MAPSettingsEmail { get; set; }
        public string MAPSettingsUploadHash { get; set; }
        public string MAPPhotoUUID { get; set; }
        public string MAPCaptureTime { get; set; }
        public string MAPLocalTimeZone { get; set; }
        public string MAPCameraMode { get; set; }
        public string MAPSettingsProject { get; set; }
        public string MAPSequenceCaptureUsed { get; set; }
        public string MAPSequenceUUID { get; set; }
        public string MAPLightSensor { get; set; }
        public string MAPDeviceModel { get; set; }
        public string MAPDeviceMake { get; set; }
        public AccVector MAPAccelerometerVector { get; set; }
        public string MAPCameraRotation { get; set; }
        public string GetAsJsonString()
        {
            JObject obj = JObject.FromObject(this);
            return obj.ToString();
        }
    }

    public class AccVector
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
    }

    public class CompassHeading
    {
        public string TrueHeading { get; set; }
        public string MagneticHeading { get; set; }
    }
}
