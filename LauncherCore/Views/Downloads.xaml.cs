// unset

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using ByteSizeLib;
using LauncherCore.Classes;

namespace LauncherCore.Views
{
    public partial class Downloads : UserControl
    {
        private static Downloads _instance;
        private ObservableCollection<Item> _downloads;
        public Downloads()
        {
            InitializeComponent();
            _instance = this;
            _downloads = new ObservableCollection<Item>();
            _listBox.ItemsSource = _downloads;
        }

        public static void DownloadFile(string url, string path,
            Action<CustomWebClient.RequestData> downloadProgress,
            Action<CustomWebClient.RequestData> downloadComplete,
            object[] extraData = null) =>
            _instance._DownloadFile(url, path, downloadProgress, downloadComplete, extraData);

        private void _DownloadFile(string url, string path, 
            Action<CustomWebClient.RequestData> downloadProgress, 
            Action<CustomWebClient.RequestData> downloadComplete,
            object[] extraData = null)
        {
            Item newDownload = new(path, url, downloadProgress, downloadComplete, extraData);
            _downloads.Add(newDownload);
            newDownload.Download();
        }

        private static void UpdateProgressBar() => _instance._UpdateProgressBar();

        private void _UpdateProgressBar()
        {
            int totalProgress = 0;
            for (int i = 0; i < _downloads.Count; i++)
            {
                totalProgress += _downloads[i].Progress;
            }

            string text = $"{_downloads.Count} {(_downloads.Count == 1 ? "download" : "downloads")} left";
            MainWindow.SetProgress(totalProgress, text);
        }

        private static void RemoveItem(Item item)
        {
            _instance._downloads.Remove(item);
            if (_instance._downloads.Count == 0)
            {
                MainWindow.SetPlayButton(true);
                MainWindow.SetProgress(100, "Downloads Complete");
            }
            else
            {
                UpdateProgressBar();
            }
        }

        private class Item : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            
            public string FilePath { get; set; }
            public string BytesText { get; set; }
            public string StartText { get; set; }
            public int Progress { get; set; }
            public string PercentText { get; set; }
            
            private string _path;
            private string _url;
            private Action<CustomWebClient.RequestData> _downloadProgress;
            private Action<CustomWebClient.RequestData> _downloadComplete;
            private object[] _extraData;
            private bool _saidHalfWay;

            public Item(string path, string url, Action<CustomWebClient.RequestData> downloadProgress, Action<CustomWebClient.RequestData> downloadComplete, object[] extraData = null)
            {
                _path = path;
                FilePath = _path.Replace(Program.Root, String.Empty);
                StartText = $"Started: {DateTime.Now}";
                UpdateProperty("StartText");
                UpdateProperty("FilePath");
                _url = url;
                _downloadProgress = downloadProgress;
                _downloadComplete = downloadComplete;
                _extraData = extraData;
            }

            public void Download()
            {
                Console.Log($"Started Downloading {_path}");
                MainWindow.SetPlayButton(true);
                HttpHelper.DownloadFile(_url, _path, DownloadProgress, DownloadComplete, _extraData);
            }

            private void DownloadComplete(CustomWebClient.RequestData data)
            {
                Console.Log($"Finished Downloading {_path}");
                _downloadComplete?.Invoke(data);
                RemoveItem(this);
            }

            private void DownloadProgress(CustomWebClient.RequestData data)
            {
                PercentText = $"Percent: {data.Progress}%";
                BytesText = $"{ByteSize.FromBytes(data.BytesReceived).ToString()}/" +
                            $"{ByteSize.FromBytes(data.TotalBytesToReceived).ToString()}";
                Progress = data.Progress;
                UpdateProperty("PercentText");
                UpdateProperty("BytesText");
                UpdateProperty("Progress");
                _downloadProgress?.Invoke(data);

                if (data.Progress == 50 && !_saidHalfWay)
                {
                    Console.Log($"50% downloaded for {_path}");
                    _saidHalfWay = true;
                }
                    
                UpdateProgressBar();
            }

            private void UpdateProperty(string property) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}