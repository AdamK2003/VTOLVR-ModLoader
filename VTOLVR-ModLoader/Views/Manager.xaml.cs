using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Windows;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : UserControl
    {
        private List<Item> _mods = new List<Item>();
        private List<Item> _skins = new List<Item>();
        public Manager()
        {
            InitializeComponent();
            Helper.SentryLog("Created Manager", Helper.SentryLogCategory.Manager);
        }
        public void UpdateUI()
        {
            Helper.SentryLog("Updating UI", Helper.SentryLogCategory.Manager);
            _mods = new List<Item>();
            _skins = new List<Item>();
            FindMods(ref _mods);
            FindSkins(ref _skins);

            _modsList.ItemsSource = _mods;
            RefreshColumns();
            this.DataContext = this;
        }
        private void FindMods(ref List<Item> items)
        {
            Helper.SentryLog("Finding Mods", Helper.SentryLogCategory.Manager);
            if (items == null)
                items = new List<Item>();

            List<BaseItem> downloadedMods = Helper.FindDownloadMods();
            if (downloadedMods.Count > 0)
                NoModsText.Visibility = Visibility.Hidden;

            for (int i = 0; i < downloadedMods.Count; i++)
            {
                items.Add(new Item(
                    downloadedMods[i].Name,
                    downloadedMods[i].Description,
                    Visibility.Hidden,
                    downloadedMods[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadedMods[i].Json[ProjectManager.jVersion].ToString(),
                    downloadedMods[i].Json[ProjectManager.jID] == null ? "N/A" : "Requesting",
                    false,
                    false,
                    downloadedMods[i].Directory.FullName));

                if (downloadedMods[i].Json[ProjectManager.jID] != null)
                {
                    items[i].PublicID = downloadedMods[i].Json[ProjectManager.jID].ToString();
                    RequestMod(downloadedMods[i].Json[ProjectManager.jID].ToString());
                }
            }
        }
        private void FindSkins(ref List<Item> items)
        {
            Helper.SentryLog("Finding Skins", Helper.SentryLogCategory.Manager);
            if (items == null)
                items = new List<Item>();

            List<BaseItem> downloadSkins = Helper.FindDownloadedSkins();
            for (int i = 0; i < downloadSkins.Count; i++)
            {
                items.Add(new Item(
                    downloadSkins[i].Name,
                    downloadSkins[i].Description,
                    Visibility.Hidden,
                    downloadSkins[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadSkins[i].Json[ProjectManager.jVersion].ToString(),
                    downloadSkins[i].Json[ProjectManager.jID] == null ? "N/A" : "Requesting",
                    false,
                    false,
                    downloadSkins[i].Directory.FullName));
            }

        }

        private void RequestMod(string publicID)
        {
            HttpHelper.DownloadStringAsync(
                $"{Program.url}{Program.apiURL}{Program.modsURL}/{publicID}",
                RequestModsCallback);
        }

        private async void RequestModsCallback(HttpResponseMessage response)
        {
            Helper.SentryLog("Request Mods Callback", Helper.SentryLogCategory.Manager);
            if (!response.IsSuccessStatusCode)
            {
                Console.Log("Failed to received info on mod\nError Code from website:" + response.StatusCode.ToString());
                return;
            }
            string jsonText = await response.Content.ReadAsStringAsync();
            JObject json = Helper.JObjectTryParse(jsonText, out Exception exception);
            if (exception != null)
            {
                Console.Log($"Failed to read json response from website ({exception.Message}). Raw response is:\n{jsonText}");
                return;
            }

            for (int i = 0; i < _mods.Count; i++)
            {
                if (!string.IsNullOrEmpty(_mods[i].PublicID) &&
                    _mods[i].PublicID.Equals(json["pub_id"].ToString()))
                {
                    _mods[i].WebsiteVersion = json["version"].ToString();
                    _mods[i].UpdateVisibility = _mods[i].IsUptodate() == true ? Visibility.Hidden : Visibility.Visible;
                    if (_mods[i].UpdateVisibility == Visibility.Visible)
                    {
                        _mods[i].CurrentVersionColour = new SolidColorBrush(Color.FromRgb(150, 0, 0));
                    }
                    _modsList.ItemsSource = _mods.ToArray();
                    return;
                }
            }
        }

        //User has pressed the update button for one of their mods
        private void UpdateMod(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Helper.SentryLog($"Update button pressed for {button.Tag}", Helper.SentryLogCategory.Manager);
            Console.Log($"Update button pressed for {button.Tag}");
            HttpHelper.DownloadStringAsync(
                $"{Program.url}{Program.apiURL}{Program.modsURL}/{button.Tag}",
                UpdateModReceivedInfo);
        }
        // Getting the json string to find the download link of the mod which the user
        // wants to update
        private async void UpdateModReceivedInfo(HttpResponseMessage response)
        {
            Helper.SentryLog("Update Mod Received Info", Helper.SentryLogCategory.Manager);
            if (!response.IsSuccessStatusCode)
            {
                Notification.Show($"Error from website, please try again later\n{response.StatusCode}", "Error");
                return;
            }
            string jsonText = await response.Content.ReadAsStringAsync();
            JObject json = Helper.JObjectTryParse(jsonText, out Exception exception);
            if (exception != null)
            {
                Notification.Show($"Error reading response from website, if this continues, please report to vtolvr-mods.com staff", "Error");
                Console.Log($"Error reading response from website ({exception.Message}). Raw response:\n{jsonText}");
                return;
            }

            Console.Log("Received Mod Info from server");
            if (json["user_uploaded_file"] != null)
            {
                string fileName = GetFileName(json["user_uploaded_file"].ToString());
                Console.Log("Downloading " + fileName);
                HttpHelper.DownloadFile(
                    json["user_uploaded_file"].ToString(),
                    $"{Program.root}{Program.modsFolder}\\{fileName}",
                    ModDownloadProgress, ModDownloadComplete); ;
                return;
            }
            Notification.Show("There seems to be no file for this mod on the website. Please contact vtolvr-mods.com staff saying which mod it is.", "Strange Error");
        }

        private void ModDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Log($"[{e.ProgressPercentage}%] Downloading Mod Update");
            //The download progress still does get called at 100%, so just added this check
            //so that ModDownloadComplete gets the last call.
            if (e.ProgressPercentage != 100)
            {
                //If the user downloads multiple updates at once, this progress bar is going
                //too look glitchy, jumping back and fourth.
                MainWindow.SetProgress(e.ProgressPercentage, "Downloading Mod Updates");
                MainWindow.SetBusy(true);
            }

        }
        //The zip from the website has finished downloading and is now in their mods folder
        private void ModDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            /*
             * The problem with web client is that it doesn't store what it just did
             * So I have no clue how to display what mod just finished updating so I can
             * update it on the list
             */
            Helper.SentryLog("Finished downloading mod update", Helper.SentryLogCategory.Manager);
            MainWindow.SetBusy(false);
            if (!e.Cancelled && e.Error == null)
            {
                MainWindow.SetProgress(100, $"Ready");
                Console.Log("Finished downloading mod update");
            }
            else
            {
                MainWindow.SetProgress(100, $"Ready");
                Notification.Show($"{e.Error.Message}", "Error when downloading file");
                Console.Log("Error when downloading mod update:\n" + e.Error.ToString());
                //if (File.Exists(Path.Combine(Program.root, currentDownloadFile)))
                //    File.Delete(Path.Combine(Program.root, currentDownloadFile));
            }
        }
        private void DeleteMod(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            Notification.Show("Are you sure you want to delete this mod?", $"Deleting {button.Tag}",
                Notification.Buttons.NoYes,
                callbackData: new string[] { button.Tag.ToString() },
                yesNoResultCallbackWithData: DeleteConfirmation);
        }
        private void DeleteConfirmation(bool yesNoResult, object[] data)
        {
            if (yesNoResult)
            {
                string[] info = data as string[];
                Helper.SentryLog($"Delete button pressed for {info[0]}", Helper.SentryLogCategory.Manager);
                Console.Log($"Deleting {info[0]}");
                Helper.DeleteDirectory(info[0], out Exception exception);

                if (exception != null)
                {
                    Notification.Show($"{exception.Message}", "Error when deleting mod");
                    return;
                }
                _mods.Remove(_mods.Find(x => x.FolderDirectory == info[0].ToString()));
                _modsList.ItemsSource = _mods.ToArray();
            }
        }

        public class Item
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Visibility UpdateVisibility { get; set; }
            public Brush CurrentVersionColour { get; set; }
            public string CurrentVersion { get; set; }
            public string WebsiteVersion { get; set; }
            public bool LoadOnStartCheck { get; set; }
            public bool AutoUpdateCheck { get; set; }
            public string FolderDirectory { get; set; }
            public string PublicID { get; set; }

            public Item(string name, string description, Visibility updateVisibility, string currentVersion, string websiteVersion, bool loadOnStartCheck, bool autoUpdateCheck, string folderDirectory)
            {
                Name = name;
                Description = description;
                UpdateVisibility = updateVisibility;
                CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                CurrentVersion = currentVersion;
                WebsiteVersion = websiteVersion;
                LoadOnStartCheck = loadOnStartCheck;
                AutoUpdateCheck = autoUpdateCheck;
                FolderDirectory = folderDirectory;
            }

            public bool IsUptodate()
            {
                return CurrentVersion.Equals(WebsiteVersion);
            }
        }

        /// <summary>
        /// Gets a file name from the end of the URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetFileName(string url)
        {
            string[] split = url.Split('/');
            return split[split.Length - 1];
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshColumns();
        }
        public void RefreshColumns()
        {
            // Thank you Assistant for this snippet
            double totalSize = 0;
            GridViewColumn description = null;
            if (_modsList.View is GridView grid)
            {
                foreach (var column in grid.Columns)
                {
                    if (column.Header?.ToString() == "Description")
                    {
                        description = column;
                    }
                    else
                    {
                        totalSize += column.ActualWidth;
                    }
                    if (double.IsNaN(column.Width))
                    {
                        column.Width = column.ActualWidth;
                        column.Width = double.NaN;
                    }
                }
                double descriptionNewWidth = _grid.ActualWidth - totalSize - 10;
                description.Width = descriptionNewWidth > 10 ? descriptionNewWidth : 10;
            }
        }
    }
}
