using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Core.Jsons;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Classes.Json;
using VTOLVR_ModLoader.Windows;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : UserControl
    {
        public static FontFamily DefaultFont;
        public static FontFamily BoldFont;
        private ObservableCollection<Item> _mods = new ObservableCollection<Item>();
        private ObservableCollection<Item> _skins = new ObservableCollection<Item>();
        public Manager()
        {
            InitializeComponent();
            Helper.SentryLog("Created Manager", Helper.SentryLogCategory.Manager);

            DefaultFont = new FontFamily(
                new Uri("pack://application:,,,/VTOLVR-ModLoader;component/Resources/"),
                "./#Montserrat Medium");
            BoldFont = new FontFamily(
                new Uri("pack://application:,,,/VTOLVR-ModLoader;component/Resources/"),
                "./#Montserrat ExtraBold");
        }
        public void UpdateUI(bool isMods)
        {
            this.DataContext = this;
            Helper.SentryLog("Updating UI", Helper.SentryLogCategory.Manager);
            Console.Log("Updating UI for Manager");
            if (isMods)
            {
                _mods = new ObservableCollection<Item>();
                _listView.ItemsSource = _mods;
                FindMods(ref _mods);
                _titleText.Text = "Mods";
                _noItemsText.Content = "No Mods Downloaded";
            }
            else
            {
                _skins = new ObservableCollection<Item>();
                _listView.ItemsSource = _skins;
                FindSkins(ref _skins);
                _titleText.Text = "Skins";
                _noItemsText.Content = "No Skins Downloaded";
            }

            RefreshColumns();
            LoadValues();

        }
        private void FindMods(ref ObservableCollection<Item> items)
        {
            Helper.SentryLog("Finding Mods", Helper.SentryLogCategory.Manager);
            if (items == null)
                items = new ObservableCollection<Item>();

            List<BaseItem> downloadedMods = Helper.FindDownloadMods();
            if (downloadedMods.Count > 0)
                _noItemsText.Visibility = Visibility.Hidden;

            for (int i = 0; i < downloadedMods.Count; i++)
            {
                items.Add(new Item(
                    downloadedMods[i].Name,
                    downloadedMods[i].Description,
                    Visibility.Hidden,
                    downloadedMods[i].Version,
                    downloadedMods[i].PublicID == string.Empty ? "N/A" : "Requesting",
                    false,
                    false,
                    downloadedMods[i].Directory.FullName));

                if (downloadedMods[i].PublicID != string.Empty)
                {
                    items[i].PublicID = downloadedMods[i].PublicID;
                    RequestMod(downloadedMods[i].PublicID);
                }
            }
        }
        private void FindSkins(ref ObservableCollection<Item> items)
        {
            Helper.SentryLog("Finding Skins", Helper.SentryLogCategory.Manager);
            if (items == null)
                items = new ObservableCollection<Item>();

            List<BaseItem> downloadSkins = Helper.FindDownloadedSkins();
            if (downloadSkins.Count > 0)
                _noItemsText.Visibility = Visibility.Hidden;
            for (int i = 0; i < downloadSkins.Count; i++)
            {
                items.Add(new Item(
                    downloadSkins[i].Name,
                    downloadSkins[i].Description,
                    Visibility.Hidden,
                    downloadSkins[i].Version,
                    downloadSkins[i].PublicID == string.Empty ? "N/A" : "Requesting",
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
            string pub_id = json["pub_id"].ToString();
            for (int i = 0; i < _mods.Count; i++)
            {
                if (!string.IsNullOrEmpty(_mods[i].PublicID) &&
                    _mods[i].PublicID.Equals(pub_id))
                {
                    Console.Log($"Checking {_mods[i].Name}");
                    _mods[i].SetWebsiteVersion(json["version"].ToString());
                    if (_mods[i].UpdateVisibility == Visibility.Visible)
                    {
                        _mods[i].CurrentVersionColour = new SolidColorBrush(Color.FromRgb(241, 241, 39));
                        _mods[i].Font = BoldFont;

                        _mods[i].PopertyChanged("Font");
                        _mods[i].PopertyChanged("CurrentVersionColour");

                        if (_mods[i].AutoUpdateCheck)
                        {
                            Console.Log($"Auto Updating {_mods[i].Name}");
                            MainWindow.SetPlayButton(true);
                            string fileName = GetFileName(json["user_uploaded_file"].ToString());
                            HttpHelper.DownloadFile(
                                json["user_uploaded_file"].ToString(),
                                $"{Program.root}{Program.modsFolder}\\{fileName}",
                                ModDownloadProgress, ModDownloadComplete, new object[] { _mods[i].PublicID, _mods[i].Name });
                        }
                    }
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
            if (json["user_uploaded_file"] != null && json["pub_id"] != null)
            {
                string fileName = GetFileName(json["user_uploaded_file"].ToString());
                Console.Log("Downloading " + fileName);
                HttpHelper.DownloadFile(
                    json["user_uploaded_file"].ToString(),
                    $"{Program.root}{Program.modsFolder}\\{fileName}",
                    ModDownloadProgress, ModDownloadComplete, new object[] { json["pub_id"].ToString(), json["name"].ToString() });
                return;
            }
            Console.Log($"Couldn't seem to find a file on the website");
            Console.Log($"user_uploaded_file = {json["user_uploaded_file"]} | {"pub_id"} = {json["pub_id"]} | Raw : {json}");
            Notification.Show("There seems to be no file for this mod on the website. Please contact vtolvr-mods.com staff saying which mod it is.", "Strange Error");
        }
        private void ModDownloadProgress(CustomWebClient.RequestData requestData)
        {
            string name = requestData.ExtraData[1] as string;
            Console.Log($"[{requestData.Progress}%] Downloading {name}");
            //The download progress still does get called at 100%, so just added this check
            //so that ModDownloadComplete gets the last call.
            if (requestData.Progress != 100)
            {
                //If the user downloads multiple updates at once, this progress bar is going
                //too look glitchy, jumping back and fourth.
                MainWindow.SetProgress(requestData.Progress, $"Downloading {name}");
                MainWindow.SetBusy(true);
            }

        }
        //The zip from the website has finished downloading and is now in their mods folder
        private void ModDownloadComplete(CustomWebClient.RequestData requestData)
        {
            string publicID = requestData.ExtraData[0] as string;
            string name = requestData.ExtraData[1] as string;
            Helper.SentryLog($"Finished downloading {name}", Helper.SentryLogCategory.Manager);
            MainWindow.SetBusy(false);
            if (!requestData.EventHandler.Cancelled && requestData.EventHandler.Error == null)
            {
                MainWindow.SetProgress(100, $"Downloaded {name}");

                string currentFolder = requestData.FilePath.Split('.')[0];

                Directory.CreateDirectory(currentFolder);
                Console.Log("Extracting " + requestData.FilePath);
                MainWindow.SetBusy(true);
                MainWindow.SetProgress(0, $"Extracting {name}");
                Helper.ExtractZipToDirectory(requestData.FilePath, currentFolder, completedWithArgs: ExtractedMod, extraData: requestData.ExtraData);
            }
            else
            {
                MainWindow.SetProgress(100, $"Ready");
                Notification.Show($"{requestData.EventHandler.Error.Message}", $"Error when downloading {name}");
                Console.Log($"Error when downloading {name}:\n" + requestData.EventHandler.Error.ToString());
                //if (File.Exists(Path.Combine(Program.root, currentDownloadFile)))
                //    File.Delete(Path.Combine(Program.root, currentDownloadFile));
            }
        }
        private void ExtractedMod(string zipPath, string extractedPath, string result, object[] extraData)
        {
            Helper.SentryLog($"Finished Extracting Mod Update", Helper.SentryLogCategory.Manager);
            if (!result.Equals("Success"))
            {
                Notification.Show($"Error Extracting {zipPath}\nError:{result}");
                Console.Log($"Error Extracting {zipPath}\nError:{result}");
                MainWindow.SetProgress(100, $"Error Extracting");
            }
            else
            {
                Console.Log($"Finished Extracting {zipPath}");
                Helper.TryDelete(zipPath);
                MainWindow.SetProgress(100, $"Updated {extraData[1] as string}");
                UpdateModUI(extraData);
            }
            MainWindow.SetBusy(false);
        }
        private void UpdateModUI(object[] data)
        {
            string publicID = data[0] as string;
            for (int i = 0; i < _mods.Count; i++)
            {
                if (_mods[i].PublicID != publicID)
                    continue;

                _mods[i].Updated();
                break;
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
                string folderDir = info[0].ToString();
                for (int i = 0; i < _mods.Count; i++)
                {
                    if (_mods[i].FolderDirectory != folderDir)
                        continue;

                    _mods.Remove(_mods[i]);
                    _listView.ItemsSource = _mods.ToArray();
                    break;
                }
            }
        }
        [JsonObject(MemberSerialization.OptIn)]
        public class Item : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Visibility UpdateVisibility { get; set; }
            public Brush CurrentVersionColour { get; set; }
            public string CurrentVersion { get; set; }
            public string WebsiteVersion { get; set; }
            [JsonProperty("Load On Start Check")]
            public bool LoadOnStartCheck { get; set; }
            [JsonProperty("Auto Update Check")]
            public bool AutoUpdateCheck { get; set; }
            [JsonProperty("Folder Directory")]
            public string FolderDirectory { get; set; }
            public string PublicID { get; set; }
            public FontFamily Font { get; set; }

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
                Font = DefaultFont;
            }

            public event PropertyChangedEventHandler PropertyChanged = delegate { };

            public bool IsUptodate()
            {
                return CurrentVersion.Equals(WebsiteVersion);
            }
            public void Updated()
            {
                CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                CurrentVersion = WebsiteVersion;
                UpdateVisibility = Visibility.Hidden;
                Font = DefaultFont;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentVersion"));
                PropertyChanged(this, new PropertyChangedEventArgs("UpdateVisibility"));
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentVersionColour"));
                PropertyChanged(this, new PropertyChangedEventArgs("Font"));
                Console.Log($"Updated {Name} to {CurrentVersion}");
            }
            public void LoadValue(Item savedValues)
            {
                LoadOnStartCheck = savedValues.LoadOnStartCheck;
                AutoUpdateCheck = savedValues.AutoUpdateCheck;
            }
            public void PopertyChanged(string poperty)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(poperty));
            }
            public void SetWebsiteVersion(string websiteVersion)
            {
                WebsiteVersion = websiteVersion;
                UpdateVisibility = IsUptodate() == true ? Visibility.Hidden : Visibility.Visible;


                PropertyChanged(this, new PropertyChangedEventArgs("WebsiteVersion"));
                PropertyChanged(this, new PropertyChangedEventArgs("UpdateVisibility"));
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
            if (_listView.View is GridView grid)
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
                double descriptionNewWidth = _grid.ActualWidth - totalSize - 35;
                description.Width = descriptionNewWidth > 10 ? descriptionNewWidth : 10;
            }
        }
        private void LoadOnStartChanged(object sender, RoutedEventArgs e)
        {
            SaveValues();
        }
        private void AutoUpdateChanged(object sender, RoutedEventArgs e)
        {
            SaveValues();
        }
        private bool GetItem(bool isMod, string folderDir, out int position)
        {
            if (isMod)
            {
                for (int i = 0; i < _mods.Count; i++)
                {
                    if (_mods[i].FolderDirectory == folderDir)
                    {
                        position = i;
                        return true;
                    }
                }
                position = -1;
                return false;
            }

            for (int i = 0; i < _skins.Count; i++)
            {
                if (_skins[i].FolderDirectory == folderDir)
                {
                    position = i;
                    return true;
                }
            }
            position = -1;
            return false;
        }
        private void SaveValues()
        {
            UserSettings.Settings.Mods = _mods;
            UserSettings.Settings.Skins = _skins;
            UserSettings.SaveSettings(Program.root + Settings.SavePath);
        }
        private void LoadValues()
        {
            ObservableCollection<Item> savedValues = UserSettings.Settings.Mods;
            for (int i = 0; i < _mods.Count; i++)
            {
                for (int j = 0; j < savedValues.Count; j++)
                {
                    if (_mods[i].FolderDirectory == savedValues[j].FolderDirectory)
                    {
                        _mods[i].LoadValue(savedValues[j]);
                        break;
                    }
                }
            }

            savedValues = UserSettings.Settings.Skins;
            for (int i = 0; i < _skins.Count; i++)
            {
                for (int j = 0; j < savedValues.Count; j++)
                {
                    if (_skins[i].FolderDirectory == savedValues[j].FolderDirectory)
                    {
                        _skins[i].LoadValue(savedValues[j]);
                        break;
                    }
                }
            }
        }
    }
}
