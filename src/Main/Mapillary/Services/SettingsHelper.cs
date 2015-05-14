using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mapillary.Services
{
    public class SettingsHelper
    {
        public static void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                applicationData[key] = value;
            }
            else
            {
                applicationData.Add(key, value);
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            return applicationData.Contains(key);
        }

        public static void SetObject(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            XmlSerializer serializer = new XmlSerializer(value.GetType());
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, value);
            string xml = writer.ToString();
            writer.Dispose();
            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                applicationData[key] = xml;
            }
            else
            {
                applicationData.Add(key, xml);
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void SetObject(string key, object value, Type[] types)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            XmlSerializer serializer = new XmlSerializer(value.GetType(), types);
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, value);
            string xml = writer.ToString();
            writer.Dispose();
            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                applicationData[key] = xml;
            }
            else
            {
                applicationData.Add(key, xml);
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void DeleteValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            applicationData[key] = null;

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static string GetValue(string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                return applicationData[key] as string;
            }
            else
            {
                return defaultValue;
            }
        }

        public static T GetObject<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                string xml = applicationData[key] as string;
                if (string.IsNullOrEmpty(xml))
                {
                    return default(T);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(T));
                TextReader reader = new StringReader(xml);
                T obj = (T)serializer.Deserialize(reader);
                reader.Dispose();
                return obj;
            }
            else
            {
                return default(T);
            }
        }

        public static T GetObject<T>(string key, Type[] types)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key must have a value");
            }

            var applicationData = IsolatedStorageSettings.ApplicationSettings;
            if (applicationData.Contains(key))
            {
                string xml = applicationData[key] as string;
                if (string.IsNullOrEmpty(xml))
                {
                    return default(T);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(T), types);
                TextReader reader = new StringReader(xml);
                T obj = (T)serializer.Deserialize(reader);
                reader.Dispose();
                return obj;
            }
            else
            {
                return default(T);
            }
        }
    }
}
