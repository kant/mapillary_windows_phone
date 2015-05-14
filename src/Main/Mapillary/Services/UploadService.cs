using Mapillary.Models;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mapillary.Services
{
    public class UploadData
    {
        public Stream PostStream { get; set; }
        public Stream FileStream { get; set; }
        public byte[] HeaderBytes {get; set;}
        public byte[] FooterBytes  {get; set;}
        public byte[] Buffer { get; set; }
        public Photo Upload { get; set; }
        public int BytesWritten { get; set; }
    }

    public class UploadEventArgs : EventArgs
    {
        public Photo Upload { get; set; }
        public int Progress { get; set; }
    }

    public class UploadService
    {
        public delegate void CompletedEventHandler(object sender, UploadEventArgs e);
        public event CompletedEventHandler UploadCompleted;
        public delegate void ProgressEventHandler(object sender, UploadEventArgs e);
        public event ProgressEventHandler ProgressChanged;

        private static string contentType = "multipart/form-data; boundary={0}";
        private static string headerString = "Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\nContent-Type: Content-Type: application/octet-stream\r\n\r\n";
        private HttpWebRequest m_request;
        private static string boundarystr;

        private UploadData m_uploadData;
        private bool m_isStopped;

        public async Task StartUpload(Photo upload, Uri uri, Dictionary<string, string> parameters)
        {
            try
            {
                m_isStopped = false;
                Stream fileStream = null;
                if (App.SaveToCameraRollEnabled)
                {
                    fileStream = GetBitmapFromMediaLib(upload);
                }
                else
                {
                    fileStream = (await upload.File.OpenReadAsync()).AsStreamForRead();
                }
                var uploadData = new UploadData();

                boundarystr = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                string para = GetParamsString(parameters);
                string headAndParams = para + String.Format(headerString, HttpUtility.UrlEncode(upload.Title));

                var headerBytes = System.Text.Encoding.UTF8.GetBytes(headAndParams);
                var footerBytes = Encoding.UTF8.GetBytes("\r\n--" + boundarystr + "--\r\n");

                uploadData.Upload = upload;
                uploadData.FileStream = fileStream;
                uploadData.FooterBytes = footerBytes;
                uploadData.HeaderBytes = headerBytes;
                uploadData.BytesWritten = 0;
                m_uploadData = uploadData;
                m_request = (HttpWebRequest)WebRequest.Create(uri);
                m_request.Method = "POST";
                m_request.AllowWriteStreamBuffering = false;
                m_request.ContentType = string.Format(contentType, boundarystr);
                m_request.ContentLength = fileStream.Length + headerBytes.Length + footerBytes.Length;
                var asyncResult = m_request.BeginGetRequestStream((ar) => { GetRequestStreamCallback(ar, uploadData); }, m_request);
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = new Exception("Start upload failed: " + ex.Message);
                var argsStopped = new UploadEventArgs();
                argsStopped.Upload = m_uploadData.Upload;
                m_uploadData.FileStream.Close();
                m_uploadData.PostStream.Close();
                OnUploadComplete(argsStopped);
            }
        }

        private Stream GetBitmapFromMediaLib(Photo image)
        {
            using (MediaLibrary library = new MediaLibrary())
            {
                foreach (PictureAlbum album in library.RootPictureAlbum.Albums)
                {
                    if (album.Name == "Camera Roll")
                    {
                        var picture = (from r in album.Pictures where r.Name == "mapi_" + image.Title select r).FirstOrDefault();
                        if (picture == null)
                        {
                            return null;
                        }

                        var stream = picture.GetImage();
                        return stream;

                    }
                }

            }

            return null;
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult, UploadData uploadData)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                Stream postStream = request.EndGetRequestStream(asynchronousResult);

                postStream.Write(uploadData.HeaderBytes, 0, uploadData.HeaderBytes.Length);

                var args = new UploadEventArgs();
                args.Upload = uploadData.Upload;
                args.Progress = 1;
                OnProgressChanged(args);
                uploadData.PostStream = postStream;

                WriteNextChunck(uploadData);
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = new Exception("Header write failed: " + ex.Message);
                var argsStopped = new UploadEventArgs();
                argsStopped.Upload = m_uploadData.Upload;
                m_uploadData.FileStream.Close();
                m_uploadData.PostStream.Close();
                OnUploadComplete(argsStopped);
            }
        }

        private void WriteNextChunck(UploadData upload)
        {
            try
            {
                Thread.Sleep(20);
                if ((upload.FileStream.Length - upload.BytesWritten) >= 16 * 1024)
                {
                    upload.Buffer = new byte[16 * 1024];
                }
                else
                {
                    // Last part
                    upload.Buffer = new byte[upload.FileStream.Length - upload.BytesWritten];
                }

                upload.FileStream.Read(upload.Buffer, 0, (int)upload.Buffer.Length);
                upload.PostStream.BeginWrite(upload.Buffer, 0, upload.Buffer.Length, BeginWriteCallback, upload);
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = new Exception("Buffer write failed: " + ex.Message);
                var argsStopped = new UploadEventArgs();
                argsStopped.Upload = m_uploadData.Upload;
                upload.FileStream.Close();
                upload.PostStream.Close();
                OnUploadComplete(argsStopped);
            }
        }

        private void BeginWriteCallback(IAsyncResult ar)
        {
            try
            {
                var upload = ar.AsyncState as UploadData;
                upload.PostStream.EndWrite(ar);
                upload.BytesWritten += upload.Buffer.Length;

                var args = new UploadEventArgs();
                args.Upload = upload.Upload;
                args.Progress = (int)(((decimal)upload.BytesWritten / (decimal)upload.FileStream.Length) * 100);
                OnProgressChanged(args);

                if (m_isStopped)
                {
                    upload.FileStream.Close();
                    upload.PostStream.Close();
                    m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                    m_uploadData.Upload.UploadInfo.Exception = new Exception("Upload stopped");
                    var argsStopped = new UploadEventArgs();
                    argsStopped.Upload = m_uploadData.Upload;
                    OnUploadComplete(argsStopped);
                    return;
                }

                // write next chunck
                if (upload.BytesWritten < upload.FileStream.Length)
                {
                    WriteNextChunck(upload);
                }
                if (upload.BytesWritten >= upload.FileStream.Length)
                {
                    WriteFooter(upload);
                }
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = new Exception("Upload write failed: " + ex.Message);
                var argsStopped = new UploadEventArgs();
                argsStopped.Upload = m_uploadData.Upload;
                OnUploadComplete(argsStopped);
            }
        }

        private void WriteFooter(UploadData upload)
        {
            try
            {
                upload.PostStream.Write(upload.FooterBytes, 0, upload.FooterBytes.Length);
                upload.PostStream.Close();
                var asyncResult = m_request.BeginGetResponse(new AsyncCallback(GetResponseCallback), m_request);
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = new Exception("Footer write failed: " + ex.Message);
                var argsStopped = new UploadEventArgs();
                argsStopped.Upload = m_uploadData.Upload;
                OnUploadComplete(argsStopped);
            }
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                string responseString = streamRead.ReadToEnd();
                streamResponse.Close();
                streamRead.Close();
                response.Close();

                m_uploadData.FileStream.Close();
                m_uploadData.Upload.UploadInfo.StatusCode = response.StatusCode;

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    m_uploadData.Upload.UploadInfo.Exception = null;
                }
                else
                {
                    m_uploadData.Upload.UploadInfo.Exception = new Exception(responseString);
                }
                var args = new UploadEventArgs();
                args.Upload = m_uploadData.Upload;
                args.Progress = 100;
                OnUploadComplete(args);
            }
            catch (Exception ex)
            {
                m_uploadData.Upload.UploadInfo.StatusCode = HttpStatusCode.NotFound;
                m_uploadData.Upload.UploadInfo.Exception = ex;
                var args = new UploadEventArgs();
                args.Upload = m_uploadData.Upload;
                OnUploadComplete(args);
            }
        }  

        private string GetParamsString(Dictionary<string, string> parameters)
        {
            bool needsCLRF = false;
            string result = "";
            foreach (var param in parameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    result += "\r\n";

                needsCLRF = true;

                string prm = string.Format("--{0}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Disposition: form-data; name={1}\r\n\r\n{2}",
                        boundarystr,
                        param.Key,
                        param.Value);
                result += prm;

            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundarystr + "\r\n";
            result += footer;

            return result;
        }

        protected virtual void OnUploadComplete(UploadEventArgs e)
        {
            if (UploadCompleted != null)
                UploadCompleted(this, e);
        }

        protected virtual void OnProgressChanged(UploadEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        public void Stop()
        {
            m_isStopped = true;
        }

    }
}
