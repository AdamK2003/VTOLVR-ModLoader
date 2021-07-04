// ItemHandler is meant to handle installing and removing all items (mods and skins)
// without effecting the main thread. 

using System;
using System.Threading;
using System.Windows;

namespace LauncherCore.Classes
{
    class ItemHandler
    {
        private Thread _thread;
        public event EventHandler<ItemExtractResult> Callback;

        public void ExtractItem(string zipPath, string extractFolder)
        {
            _thread = new Thread(() => Extract(zipPath, extractFolder));
            _thread.Start();
        }

        private void Extract(string zipPath, string extractFolder)
        {
            Helper.ExtractZipToDirectory(zipPath, extractFolder, ExtractCompleted);
        }

        private void ExtractCompleted(string zipPath, string extractPath, string result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Callback?.Invoke(null,
                    new ItemExtractResult()
                    {
                        IsSuccessful = result == "Success",
                        ErrorMessage = result,
                        Path = extractPath,
                        ZipPath = zipPath
                    });
            });
        }

        public struct ItemExtractResult
        {
            public bool IsSuccessful;
            public string ErrorMessage;
            public string Path;
            public string ZipPath;
        }
    }
}