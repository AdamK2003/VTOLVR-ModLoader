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

            modsList.ItemsSource = _mods.ToArray();
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
                    Visibility.Visible,
                    downloadedMods[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadedMods[i].Json[ProjectManager.jVersion].ToString(),
                    "a",
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
                    Visibility.Hidden,
                    downloadSkins[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadSkins[i].Json[ProjectManager.jVersion].ToString(),
                    "a",
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
                Notification.Show(response.StatusCode.ToString(), "Error");
                return;
            }

            JObject json = Helper.JObjectTryParse(await response.Content.ReadAsStringAsync(), out Exception exception);
            if (exception != null)
            {
                MessageBox.Show("There was an error");
                return;
            }

            for (int i = 0; i < _mods.Count; i++)
            {
                if (!string.IsNullOrEmpty(_mods[i].PublicID) &&
                    _mods[i].PublicID.Equals(json["pub_id"].ToString()))
                {
                    _mods[i].WebsiteVersion = json["version"].ToString();
                    _mods[i].UpdateVisibility = _mods[i].IsUptodate() == true ? Visibility.Hidden : Visibility.Visible;
                    modsList.ItemsSource = _mods.ToArray();
                    return;
                }
            }
        }

        //User has pressed the update button for one of their mods
        private void UpdateMod(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Helper.SentryLog($"Update button pressed for {button.Tag}", Helper.SentryLogCategory.Manager);
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
                Notification.Show(response.StatusCode.ToString(), "Error");
                return;
            }

            JObject json = Helper.JObjectTryParse(await response.Content.ReadAsStringAsync(), out Exception exception);
            if (exception != null)
            {
                MessageBox.Show("There was an error");
                return;
            }

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
                //If the user downloads multiple updates at once, this progrress bar is going
                //too look glitchy, jumping back and fouth.
                MainWindow.SetProgress(e.ProgressPercentage, "Downloading Mod Updates");
                MainWindow.SetBusy(true);
            }

        }
        //The zip from the website has finished downloading and is now in their mods folder
        private void ModDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            Helper.SentryLog("Fininshed downloading mod update", Helper.SentryLogCategory.Manager);
            MainWindow.SetBusy(false);

            MessageBox.Show(sender.GetType().Name);
            if (!e.Cancelled && e.Error == null)
            {
                MainWindow.SetProgress(100, $"Ready");
                Console.Log("Finished downloading mod update");
            }
            else
            {
                //MainWindow.SetProgress(100, $"Ready");
                //Notification.Show($"{e.Error.Message}", "Error when downloading file");
                //Console.Log("Error:\n" + e.Error.ToString());
                //if (File.Exists(Path.Combine(Program.root, currentDownloadFile)))
                //    File.Delete(Path.Combine(Program.root, currentDownloadFile));
            }
        }

        private void DeleteMod(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Helper.SentryLog($"Delete button pressed for {button.Tag}", Helper.SentryLogCategory.Manager);
            Console.Log($"Deleting {button.Tag}");
            Helper.DeleteDirectory(button.Tag.ToString(), out Exception exception);

            if (exception != null)
            {
                Notification.Show($"{exception.Message}", "Error when deleteing mod");
                return;
            }
            _mods.Remove(_mods.Find(x => x.FolderDirectory == button.Tag.ToString()));
            modsList.ItemsSource = _mods.ToArray();
        }

        public class Item
        {
            public string Name { get; set; }
            public Visibility UpdateVisibility { get; set; }
            public string CurrentVersion { get; set; }
            public string WebsiteVersion { get; set; }
            public bool LoadOnStartCheck { get; set; }
            public bool AutoUpdateCheck { get; set; }
            public string FolderDirectory { get; set; }
            public string PublicID { get; set; }

            public Item(string name, Visibility updateVisibility, string currentVersion, string websiteVersion, bool loadOnStartCheck, bool autoUpdateCheck, string folderDirectory)
            {
                Name = name;
                UpdateVisibility = updateVisibility;
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
    }
}
