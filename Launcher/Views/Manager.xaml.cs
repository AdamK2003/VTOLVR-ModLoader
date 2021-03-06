using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Core.Jsons;
using Launcher.Classes;
using Launcher.Classes.Json;
using Launcher.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ContentType = Core.Enums.ContentType;

namespace Launcher.Views
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : UserControl
    {
        public static FontFamily DefaultFont;
        public static FontFamily BoldFont;

        private ObservableCollection<Item> _items = new ObservableCollection<Item>();
        private ScrollViewer _scrollViewer;

        private int _outdatedItems = 0;
        private readonly Brush _darkBackground = new SolidColorBrush(Color.FromRgb(61, 61, 61));
        private readonly Brush _lightBackground = new SolidColorBrush(Color.FromRgb(75, 75, 75));

        public Manager()
        {
            Loaded += UILoaded;
            InitializeComponent();
            Helper.SentryLog("Created Manager", Helper.SentryLogCategory.Manager);

            DefaultFont = new FontFamily(
                new Uri("pack://application:,,,/Launcher;component/Resources/"),
                "./#Montserrat Medium");
            BoldFont = new FontFamily(
                new Uri("pack://application:,,,/Launcher;component/Resources/"),
                "./#Montserrat ExtraBold");
        }

        private void UILoaded(object sender, RoutedEventArgs e)
        {
            // This is a really bad work around for the grid actualwidth
            // being 0. I've tried Measure and Arrange, Loaded and a dispacher
            // None of them worked. So just have this half a second delay. To call RefreshColumns
            
            TimeSpan delay = TimeSpan.FromSeconds(0.5f);
            var timer = new DispatcherTimer
            {
                Interval = delay
            };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                RefreshColumns();
                timer.Stop();
            };
            
            GetScrollViewer();
            _openSiteButton.Content = "Open " + Program.URL;
        }

        private void GetScrollViewer()
        {
            Decorator border = VisualTreeHelper.GetChild(_listView, 0) as Decorator;
            _scrollViewer = border.Child as ScrollViewer;
        }

        public void UpdateUI(bool isMods)
        {
            this.DataContext = this;
            Helper.SentryLog("Updating UI", Helper.SentryLogCategory.Manager);
            Console.Log("Updating UI for Manager");

            if (_items.Count == 0)
                PopulateList();

            if (isMods)
                ScrollToMods();
            else
                ScrollToSkins();

            if (_outdatedItems > 0)
            {
                _warningText.Content =
                    $"You have {_outdatedItems} outdated {(_outdatedItems == 1 ? "item" : "items")}. Please manually redownload these items marked in red.";
                _warningText.Visibility = Visibility.Visible;
            }
            else
            {
                _warningText.Visibility = Visibility.Hidden;
            }
        }

        public void PopulateList()
        {
            Helper.SentryLog("Populating List", Helper.SentryLogCategory.Manager);
            Console.Log("Populating List");

            _items = new ObservableCollection<Item>();
            Program.FindItems();
            ConvertItems();
            SetBackgroundColours();
            _listView.ItemsSource = _items;

            CreateListSections();

            LoadValues();

            if (_items.Count == 0)
            {
                _listView.Visibility = Visibility.Hidden;
                _grid.RowDefinitions[0].Height = new GridLength(0);
            }
            else
            {
                _listView.Visibility = Visibility.Visible;
                _grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void CreateListSections()
        {
            CollectionView view = (CollectionView) CollectionViewSource.GetDefaultView(_listView.ItemsSource);
            view.GroupDescriptions.Add(new PropertyGroupDescription("ItemType"));
        }

        private void OnRename(object sender, RenamedEventArgs e)
        {
            if (e.Name.EndsWith(".zip"))
                return;
            Dispatcher.Invoke(() =>
            {
                Console.Log($"File Renamed: {e.OldFullPath} renamed to {e.FullPath}");
                PopulateList();
            });
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.Name.EndsWith(".zip"))
                return;
            Dispatcher.Invoke(() =>
            {
                Console.Log($"File Changed: {e.FullPath} has been {e.ChangeType}");
                PopulateList();
            });
        }

        private void ScrollToMods()
        {
            if (_scrollViewer == null)
                return;
            _scrollViewer.ScrollToTop();
        }

        private void ScrollToSkins()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ItemType == ContentType.Skins)
                {
                    _scrollViewer.ScrollToBottom();
                    Dispatcher.Invoke(new Action(() => { _listView.ScrollIntoView(_items[i]); }),
                        DispatcherPriority.ContextIdle,
                        null);
                    return;
                }
            }
        }

        private void ConvertItems()
        {
            List<BaseItem> items = Program.Items;
            Item lastItem;
            for (int i = 0; i < items.Count; i++)
            {
                Console.Log($"On {items[i].Name}");
                lastItem = new Item(
                    items[i].ContentType,
                    items[i].Name,
                    items[i].Tagline,
                    Visibility.Hidden,
                    items[i].Version,
                    items[i].PublicID == string.Empty || items[i].ContentType == ContentType.MyMods ||
                    items[i].ContentType == ContentType.MySkins
                        ? "N/A"
                        : "Requesting",
                    false,
                    false,
                    items[i].Directory.FullName);

                if (!items[i].HasDll())
                    lastItem.LoadOnStartVisibility = Visibility.Hidden;

                if (items[i].ContentType != ContentType.MyMods && items[i].ContentType != ContentType.MySkins)
                {
                    if (items[i].PublicID != string.Empty)
                    {
                        lastItem.PublicID = items[i].PublicID;

                        if (!Program.DisableInternet)
                        {
                            RequestItem(items[i].PublicID,
                                items[i].ContentType == ContentType.Mods ? true : false);
                        }
                        else
                        {
                            lastItem.WebsiteVersion = "No Internet";
                        }
                    }
                    else
                    {
                        _outdatedItems++;
                        lastItem.CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 56, 56));
                        lastItem.Font = BoldFont;

                        lastItem.PopertyChanged("Font");
                        lastItem.PopertyChanged("CurrentVersionColour");
                    }
                }
                else
                {
                    lastItem.AutoUpdateVisibility = Visibility.Hidden;
                }

                _items.Add(lastItem);
            }
        }

        private void RequestItem(string publicID, bool isMod)
        {
            HttpHelper.DownloadStringAsync(
                $"{Program.URL}{Program.ApiURL}{(isMod ? Program.ModsURL : Program.SkinsURL)}/{publicID}",
                RequestItemCallback, extraData: new object[] {isMod, publicID});
        }

        private async void RequestItemCallback(HttpResponseMessage response, object[] extraData)
        {
            Helper.SentryLog("Request Item Callback", Helper.SentryLogCategory.Manager);
            if (!response.IsSuccessStatusCode)
            {
                Console.Log(
                    $"Failed to received info on item\nError Code from website:{response.StatusCode}");
                ReceivedError((string)extraData[1]);
                return;
            }

            string jsonText = await response.Content.ReadAsStringAsync();
            JObject json = Helper.JObjectTryParse(jsonText, out Exception exception);
            if (exception != null)
            {
                Console.Log(
                    $"Failed to read json response from website ({exception.Message}). Raw response is:\n{jsonText}");
                return;
            }

            if ((bool)extraData[0] == true)
            {
                ReceivedModUpdateInfo(json);
            }
            else
            {
                ReceivedSkinUpdateInfo(json);
            }
        }

        private void ReceivedModUpdateInfo(JObject json)
        {
            string pub_id = json["pub_id"].ToString();
            for (int i = 0; i < _items.Count; i++)
            {
                if (!string.IsNullOrEmpty(_items[i].PublicID) &&
                    _items[i].PublicID.Equals(pub_id))
                {
                    Console.Log($"Checking {_items[i].Name}");
                    _items[i].SetWebsiteVersion(json["version"].ToString());
                    if (_items[i].UpdateVisibility == Visibility.Visible)
                    {
                        _items[i].CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 250, 101));
                        _items[i].Font = BoldFont;

                        _items[i].PopertyChanged("Font");
                        _items[i].PopertyChanged("CurrentVersionColour");

                        if (_items[i].AutoUpdateCheck)
                        {
                            Console.Log($"Auto Updating {_items[i].Name}");
                            MainWindow.SetPlayButton(true);
                            string fileName = GetFileName(json["user_uploaded_file"].ToString());
                            Downloads.DownloadFile(
                                json["user_uploaded_file"].ToString(),
                                $"{Program.Root}{Program.ModsFolder}\\{fileName}",
                                null, DownloadComplete, new object[] {_items[i].PublicID, _items[i].Name});
                        }
                    }

                    return;
                }
            }
        }

        private void ReceivedSkinUpdateInfo(JObject json)
        {
            string pub_id = json["pub_id"].ToString();
            for (int i = 0; i < _items.Count; i++)
            {
                if (!string.IsNullOrEmpty(_items[i].PublicID) &&
                    _items[i].PublicID.Equals(pub_id))
                {
                    Console.Log($"Checking {_items[i].Name}");
                    _items[i].SetWebsiteVersion(json["version"].ToString());
                    if (_items[i].UpdateVisibility == Visibility.Visible)
                    {
                        _items[i].CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 250, 101));
                        _items[i].Font = BoldFont;

                        _items[i].PopertyChanged("Font");
                        _items[i].PopertyChanged("CurrentVersionColour");

                        if (_items[i].AutoUpdateCheck)
                        {
                            Console.Log($"Auto Updating {_items[i].Name}");
                            MainWindow.SetPlayButton(true);
                            string fileName = GetFileName(json["user_uploaded_file"].ToString());
                            Downloads.DownloadFile(
                                json["user_uploaded_file"].ToString(),
                                $"{Program.Root}{Program.ModsFolder}\\{fileName}",
                                null, DownloadComplete, new object[] {_items[i].PublicID, _items[i].Name});
                        }
                    }

                    return;
                }
            }
        }

        private void ReceivedError(string publicID)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.IsNullOrEmpty(_items[i].PublicID) ||
                    !_items[i].PublicID.Equals(publicID))
                    continue;

                _items[i].WebsiteVersion = "Error";
                _items[i].PopertyChanged("WebsiteVersion");
                return;
            }
        }

        private void UpdateItem(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Helper.SentryLog($"Update button pressed for {button.Tag}", Helper.SentryLogCategory.Manager);
            Console.Log($"Update button pressed for {button.Tag}");

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].PublicID == (string)button.Tag)
                {
                    if (_items[i].ItemType == ContentType.Mods)
                    {
                        HttpHelper.DownloadStringAsync(
                            $"{Program.URL}{Program.ApiURL}{Program.ModsURL}/{button.Tag}",
                            UpdateModReceivedInfo);
                        return;
                    }

                    HttpHelper.DownloadStringAsync(
                        $"{Program.URL}{Program.ApiURL}{Program.SkinsURL}/{button.Tag}",
                        UpdateSkinReceivedInfo);
                    return;
                }
            }
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
                Notification.Show(
                    $"Error reading response from website, if this continues, please report to vtolvr-mods.com staff",
                    "Error");
                Console.Log($"Error reading response from website ({exception.Message}). Raw response:\n{jsonText}");
                return;
            }

            Console.Log("Received Mod Info from server");
            if (json["user_uploaded_file"] != null && json["pub_id"] != null)
            {
                string fileName = GetFileName(json["user_uploaded_file"].ToString());
                Console.Log("Downloading " + fileName);
                Downloads.DownloadFile(
                    json["user_uploaded_file"].ToString(),
                    $"{Program.Root}{Program.ModsFolder}\\{fileName}",
                    null, DownloadComplete,
                    new object[] {json["pub_id"].ToString(), json["name"].ToString()});
                return;
            }

            Console.Log($"Couldn't seem to find a file on the website");
            Console.Log(
                $"user_uploaded_file = {json["user_uploaded_file"]} | {"pub_id"} = {json["pub_id"]} | Raw : {json}");
            Notification.Show(
                "There seems to be no file for this mod on the website. Please contact vtolvr-mods.com staff saying which mod it is.",
                "Strange Error");
        }

        private async void UpdateSkinReceivedInfo(HttpResponseMessage response)
        {
            Helper.SentryLog("Update Skin Received Info", Helper.SentryLogCategory.Manager);
            if (!response.IsSuccessStatusCode)
            {
                Notification.Show($"Error from website, please try again later\n{response.StatusCode}", "Error");
                return;
            }

            string jsonText = await response.Content.ReadAsStringAsync();
            JObject json = Helper.JObjectTryParse(jsonText, out Exception exception);
            if (exception != null)
            {
                Notification.Show(
                    $"Error reading response from website, if this continues, please report to vtolvr-mods.com staff",
                    "Error");
                Console.Log($"Error reading response from website ({exception.Message}). Raw response:\n{jsonText}");
                return;
            }

            Console.Log("Received Skin Info from server");
            if (json["user_uploaded_file"] != null && json["pub_id"] != null)
            {
                string fileName = GetFileName(json["user_uploaded_file"].ToString());
                Console.Log("Downloading " + fileName);
                Downloads.DownloadFile(
                    json["user_uploaded_file"].ToString(),
                    $"{Program.Root}{Program.SkinsFolder}\\{fileName}",
                    null, DownloadComplete,
                    new object[] {json["pub_id"].ToString(), json["name"].ToString()});
                return;
            }

            Console.Log($"Couldn't seem to find a file on the website");
            Console.Log(
                $"user_uploaded_file = {json["user_uploaded_file"]} | {"pub_id"} = {json["pub_id"]} | Raw : {json}");
            Notification.Show(
                "There seems to be no file for this skin on the website. Please contact vtolvr-mods.com staff saying which mod it is.",
                "Strange Error");
        }


        //The zip from the website has finished downloading and is now in their mods folder
        private void DownloadComplete(CustomWebClient.RequestData requestData)
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
                Console.Log("Extracting " + requestData.FilePath + " to " + currentFolder);
                MainWindow.SetBusy(true);
                MainWindow.SetProgress(0, $"Extracting {name}");
                Helper.ExtractZipToDirectory(requestData.FilePath, currentFolder, completedWithArgs: ExtractedMod,
                    extraData: requestData.ExtraData);
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
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].PublicID != publicID)
                    continue;

                _items[i].Updated();
                break;
            }
        }

        private void DeleteMod(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            Notification.Show("Are you sure you want to delete this mod?", $"Deleting {button.Tag}",
                Notification.Buttons.NoYes,
                callbackData: new string[] {button.Tag.ToString()},
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
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].FolderDirectory != folderDir)
                        continue;

                    _items.Remove(_items[i]);
                    _listView.ItemsSource = _items.ToArray();
                    break;
                }
                
                CreateListSections();
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Item : INotifyPropertyChanged
        {
            public ContentType ItemType { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public Visibility UpdateVisibility { get; set; }
            public Visibility LoadOnStartVisibility { get; set; } = Visibility.Visible;
            public Visibility AutoUpdateVisibility { get; set; } = Visibility.Visible;
            public Brush CurrentVersionColour { get; set; }
            public string CurrentVersion { get; set; }
            public string WebsiteVersion { get; set; }
            [JsonProperty("Load On Start Check")] public bool LoadOnStartCheck { get; set; }
            [JsonProperty("Auto Update Check")] public bool AutoUpdateCheck { get; set; }
            [JsonProperty("Folder Directory")] public string FolderDirectory { get; set; }
            public string PublicID { get; set; }
            public bool HasPublicID { get { return PublicID == string.Empty; } }
            public FontFamily Font { get; set; }
            public Brush BackgroundColour { get; set; }

            public Item(ContentType contentType, string name, string description, Visibility updateVisibility,
                string currentVersion, string websiteVersion, bool loadOnStartCheck, bool autoUpdateCheck,
                string folderDirectory)
            {
                ItemType = contentType;
                Name = name;
                if (description != null && description.Length > 75)
                    Description = description.Remove(75) + "...";
                else
                    Description = description;
                UpdateVisibility = updateVisibility;
                CurrentVersionColour = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                CurrentVersion = currentVersion;
                WebsiteVersion = websiteVersion;
                LoadOnStartCheck = loadOnStartCheck;
                AutoUpdateCheck = autoUpdateCheck;
                FolderDirectory = folderDirectory;
                Font = DefaultFont;

                if (ItemType == ContentType.Skins || ItemType == ContentType.MySkins)
                    LoadOnStartVisibility = Visibility.Hidden;
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
        /// Gets the file name by reading the header from the web response.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetFileName(string url)
        {
            string filename = string.Empty;
            
            using (WebClient client = new WebClient())
            {
                // This is just for the server end in case we do need to see what
                // is sending requests to us.
                client.Headers.Add("user-agent", Program.ProgramName.RemoveSpecialCharacters());
                
                client.OpenRead(url);
                string header_contentDisposition = client.ResponseHeaders["content-disposition"];
                filename = new ContentDisposition(header_contentDisposition).FileName;
            }

            return filename;
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshColumns();
        }

        private void RefreshColumns()
        {
            // Thank you Assistant for this snippet
            double totalSize = 0;
            GridViewColumn description = null;
            if (_listView.View is GridView grid)
            {
                foreach (var column in grid.Columns)
                {
                    if (column.Header?.ToString() == "Description")
                        description = column;
                    else
                        totalSize += column.ActualWidth;

                    if (double.IsNaN(column.Width))
                    {
                        column.Width = column.ActualWidth;
                        column.Width = double.NaN;
                    }
                }

                double descriptionNewWidth = _grid.ActualWidth - totalSize - 35;
                description.Width = descriptionNewWidth > 0 ? descriptionNewWidth : 0;
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
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].FolderDirectory == folderDir)
                    {
                        position = i;
                        return true;
                    }
                }

                position = -1;
                return false;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].FolderDirectory == folderDir)
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
            UserSettings.Settings.Items = _items;
            UserSettings.SaveSettings(Program.Root + Settings.SavePath);
        }

        private void LoadValues()
        {
            ObservableCollection<Item> savedValues = UserSettings.Settings.Items;
            for (int i = 0; i < _items.Count; i++)
            {
                for (int j = 0; j < savedValues.Count; j++)
                {
                    if (_items[i].FolderDirectory == savedValues[j].FolderDirectory)
                    {
                        _items[i].LoadValue(savedValues[j]);
                        break;
                    }
                }
            }
        }

        private void SetBackgroundColours()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (i % 2 == 0)
                {
                    _items[i].BackgroundColour = _darkBackground;
                }
                else
                {
                    _items[i].BackgroundColour = _lightBackground;
                }
            }
        }

        private void OpenSite(object sender, RoutedEventArgs e)
        {
            var url = Program.URL;
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void ListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridView gridView = listView.View as GridView;
            
            double workingWidth = e.NewSize.Width - SystemParameters.VerticalScrollBarWidth;
            GridViewColumn description = null;
            
            foreach (var column in gridView.Columns)
            {
                if (column.Header?.ToString() == "Description")
                {
                    description = column;
                }
                else
                {
                    workingWidth -= column.ActualWidth;
                }
            }
            description.Width = workingWidth > 0 ? workingWidth : 0;
        }
    }
}