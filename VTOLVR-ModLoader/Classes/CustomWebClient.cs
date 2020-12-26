using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;

namespace VTOLVR_ModLoader.Classes
{
    class CustomWebClient : WebClient
    {
        public class RequestData
        {
            public string FilePath;
            public Uri Uri;
            public object[] ExtraData;
            public AsyncCompletedEventArgs EventHandler;
            public int Progress = 0;
        }
        public RequestData Request { get; private set; }
        public Action<RequestData> DownloadComplete;
        public Action<RequestData> DownloadProgress;

        public CustomWebClient() : base()
        {

        }
        public void DownloadFileAsync(Uri uri, string filePath, object[] extraData = null)
        {
            Request = new RequestData { Uri = uri, FilePath = filePath, ExtraData = extraData };
            base.DownloadProgressChanged += DownloadProgressChanged;
            base.DownloadFileCompleted += DownloadFileCompleted;
            base.DownloadFileAsync(uri, filePath);
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Request.Progress = e.ProgressPercentage;
            DownloadProgress?.Invoke(Request);
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Request.EventHandler = e;
            DownloadComplete?.Invoke(Request);
        }
    }
}
