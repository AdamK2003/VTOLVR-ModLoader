using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Net;
using System.ComponentModel;
using System.Xml;
using System.Security.Cryptography;
using System.Windows.Input;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string LogPath = @"\updater_log.txt";
        private readonly string url = "https://vtolvr-mods.com";
        private readonly string updatesURl = "/files/updates.xml";
        private string path;
        private string vtolFolder;
        private UpdateData updateData;
        private WebClient client;
        private Queue<Item> items;
        private Item currentDownload;

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();

        public MainWindow()
        {
            path = Directory.GetCurrentDirectory();
            vtolFolder = path.Replace(@"\VTOLVR_ModLoader", "/");

            if (File.Exists(path + LogPath))
                File.Delete(path + LogPath);

            Log("Updater has been launched");

            
            Log($"Path = {path}\nvtolFolder = {vtolFolder}", false);

            InitializeComponent();

            FetchUpdatesData();
        }

        private void FetchUpdatesData()
        {
            if (CheckForInternet())
            {
                SetText("Fetching Update Data");
                client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateDataProgress);
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(UpdateDataDone);
                client.DownloadStringAsync(new Uri(url + updatesURl));
            }
        }

        private void UpdateDataProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage);
            Log("UpdateData Progress = " + e.ProgressPercentage);
        }

        private void UpdateDataDone(object sender, DownloadStringCompletedEventArgs e)
        {
            Log("Done Downloading Update Data");
            if (e.Error != null)
            {
                Log("Erroed \n" + e.Error.Message);
            }
            else if (e.Cancelled)
            {
                Log("User Cancelled the download");
            }
            else
            {
                Log("Download Complete");
                updateData = (UpdateData)new XmlSerializer(typeof(UpdateData))
                    .Deserialize(new XmlTextReader(new StringReader(e.Result)));
                AddFiles();
            }
        }

        private void AddFiles()
        {
            Log("Updating Files");
            SetText("Updating Files");

            //Creating the Async Queue
            //We are always going to get the latest one in the list

            client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FileProgress);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
            items = new Queue<Item>(updateData.Updates[0].Files.Length);
            for (int i = 0; i < updateData.Updates[0].Files.Length; i++)
            {
                items.Enqueue(updateData.Updates[0].Files[i]);
            }
            DownloadFiles();
        }

        private void DownloadFiles()
        {
            if (items.Count <= 0)
            {
                SetText("Finished Downloading Update");
                Log("Finished Downloading Update");
                try
                {
                    Process.Start(path + @"\VTOLVR-ModLoader.exe");
                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception e)
                {
                    Log("Error when finished\n" + e.ToString());
                }
                return;
            }

            currentDownload = items.Dequeue();
            string currentHash = "";
            if (File.Exists(vtolFolder + currentDownload.FileLocation))
            {
                currentHash = CalculateMD5(vtolFolder + currentDownload.FileLocation);
            }
            if (currentHash != currentDownload.FileHash.ToLower())
            {
                Log($"Starting download for {currentDownload.FileName} from {url + currentDownload.URLDownload}");
                client.DownloadFileAsync(new Uri(url + currentDownload.URLDownload), vtolFolder + currentDownload.FileLocation + "_TEMP");
            }
            else
            {
                Log($"{currentDownload.FileName} was already upto date");
                DownloadFiles();
                return;
            }


            SetText($"Downloading {currentDownload.FileName}");
        }

        private void FileDone(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Log("Error\n" + e.Error.ToString());
            }
            else if (e.Cancelled)
            {
                Log("User Cancelled File");
            }
            else
            {
                Log($"{currentDownload.FileName} has been downloaded");

                if (File.Exists(vtolFolder + currentDownload.FileLocation))
                    File.Delete(vtolFolder + currentDownload.FileLocation);

                File.Move(vtolFolder + currentDownload.FileLocation + "_TEMP", vtolFolder + currentDownload.FileLocation);
                Log("We have replaced the old file");
            }
            DownloadFiles();
        }

        private void FileProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage);
            Log($"{e.ProgressPercentage}% of {currentDownload.FileName} downloaded");
        }

        private bool CheckForInternet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return true;
                    }
                }

            }
            catch
            {
                //There is a second try incase they have blocked googles domain
                try
                {
                    using (var client = new WebClient())
                    {
                        using (client.OpenRead(url))
                        {
                            return true;
                        }
                    }
                }
                catch 
                {
                    return false;
                }   
            }
        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            Log("Opening Log");
            if (File.Exists(path + LogPath))
                Process.Start(path + LogPath);
        }

        private void SetText(string text)
        {
            Log("progressText = " + text);
            progressText.Text = text;
        }

        private void SetProgress(float amount)
        {
            progress.Value = amount;
        }

        private void Log(string message, bool includeDate = true)
        {
            DateTime Now = DateTime.Now;
            File.AppendAllText(path + LogPath,
                (includeDate ? $"[{Now.Day}/{Now.Month}/{Now.Year} {Now.Hour}:{Now.Minute}:{Now.Second}] " : "") + 
                message + "\n");
        }

        private static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Quit();
        }
        private void Quit()
        {
            if (MessageBox.Show("Are you sure you want to quit?\nThis will stop the update where it currently is", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Process.GetCurrentProcess().Kill();
            }
        }
        #region Moving Window
        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(Application.Current.MainWindow);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(Application.Current.MainWindow).X - lm.X;
                this.Top += Mouse.GetPosition(Application.Current.MainWindow).Y - lm.Y;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {

        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }

        #endregion
    }
}
