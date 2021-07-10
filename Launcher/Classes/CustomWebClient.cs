using System;
using System.Net;
using System.ComponentModel;
using System.IO;

namespace Launcher.Classes
{
    public class CustomWebClient : WebClient
    {
        public class RequestData
        {
            public string FilePath;
            public Uri Uri;
            public object[] ExtraData;
            public AsyncCompletedEventArgs EventHandler;
            public int Progress = 0;
            public long BytesReceived;
            public long TotalBytesToReceived;
            public bool Cancelled => EventHandler.Cancelled;

            public Exception? Error => EventHandler.Error;
        }

        public RequestData Request { get; private set; }
        public Action<RequestData> DownloadComplete;
        public Action<RequestData> DownloadProgress;

        public CustomWebClient() : base()
        {
        }

        public void DownloadFileAsync(Uri uri, string filePath, object[] extraData = null)
        {
            Request = new RequestData {Uri = uri, FilePath = filePath, ExtraData = extraData};
            base.DownloadProgressChanged += DownloadProgressChanged;
            base.DownloadFileCompleted += DownloadFileCompleted;
            base.DownloadFileAsync(uri, filePath);
        }

        private new void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Request.Progress = e.ProgressPercentage;
            Request.BytesReceived = e.BytesReceived;
            Request.TotalBytesToReceived = e.TotalBytesToReceive;
            DownloadProgress?.Invoke(Request);
        }

        private new void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Request.EventHandler = e;
            DownloadComplete?.Invoke(Request);
        }
    }
}