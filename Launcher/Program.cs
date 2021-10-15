/* This is the main class which stores and runs the core background things.

*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Console = Launcher.Views.Console;
using Core.Jsons;
using Launcher.Classes;
using Launcher.Classes.Config;
using Launcher.Classes.Json;
using Launcher.Views;

namespace Launcher
{
    public static class Program
    {
        public const string ModsFolder = @"\mods";
        public const string SkinsFolder = @"\skins";
        public const string Injector = @"\injector.exe";
        public static string URL = @"https://vtolvr-mods.com";
        public static string Branch = string.Empty;
        public const string PageFormat = "&page=";
        public const string JsonFormat = "/?format=json";
        public const string ApiURL = "/api";
        public const string ModsURL = "/mods";
        public const string SkinsURL = "/skins";
        public const string ReleasesURL = "/releases";
        public const string ModsChangelogsURL = "/mods-changelogs";
        public const string SkinsChangelogsURL = "/skins-changelogs";
        public const string ProgramNameBase = "VTOL VR Mod Loader";
        public const string LogName = "Launcher Log.txt";

        public static string Root;
        public static string VTOLFolder;
        public static string ProgramName = ProgramNameBase;
        public static bool IsAutoStarting { get; private set; }
        public static bool DisableInternet = false;
        public static bool IsBusy;
        public static List<Release> Releases { get; private set; }
        public static List<BaseItem> Items;

        private static bool _folderInvalid = false;
        public static string ExePath
        {
            get
            {
                if (_exePath.Equals(String.Empty))
                    _exePath = Process.GetCurrentProcess().MainModule.FileName;
                return _exePath;
            }
        }
        
        private static string _exePath = string.Empty;

        private static bool _uiLoaded = false;
        private static int _itemsToExtract = 0;
        private static int _itemsExtracted = 0;
        private static Queue<Action> _actionQueue = new Queue<Action>();

        public async static void SetupAfterUI()
        {
            await WaitForUI();
            Helper.SentryLog("Setup after UI", Helper.SentryLogCategory.Program);

            GetReleases();
            if (!FolderIsValid())
            {
                if (IsOldUser(out DirectoryInfo currentDirectory))
                {
                    // Pre 5.X user shouldn't need to do the setup
                    ProgramData data = new(){VTOLPath = currentDirectory.Parent.FullName};
                    ProgramData.Save(data);

                    Root = currentDirectory.FullName;
                    VTOLFolder = currentDirectory.Parent.FullName;
                }
                else
                {
                    _folderInvalid = true;
                    MainWindow._instance.RunSetup();
                    return; 
                }
                
            }
            
            MainWindow._instance.CreatePages();
            CommunicationsManager.CheckNoInternet();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckCustomBranch();
            CommunicationsManager.CheckAutoUpdate();
            if (CommunicationsManager.CheckSteamVR() && Views.Settings.SteamVR)
                CheckForSteamVR();

            DisableInternet = !await HttpHelper.CheckForInternet();
            
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.Title = $"{ProgramName}";
            Startup.ClearOldFiles();
            MainWindow._instance.CheckForEvent();
            MainWindow.SetProgress(100, "Ready");
            CheckForItems();
            FindItems();
            MainWindow._instance.ItemManager.UpdateUI(true);
            
            if (!Doorstop.FileExists())
            {
                Doorstop.CreateDefaultFile();
            }
        }

        private static bool FolderIsValid()
        {
            if (Startup.Data == null)
                return false;

            if (Directory.Exists(VTOLFolder))
            {
                if (!Directory.Exists(Root))
                    Directory.CreateDirectory(Root);
                
                return true;
            }

            return false;
        }

        private static bool IsOldUser(out DirectoryInfo currentDirectory)
        {
            currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            return currentDirectory.Name.Equals("VTOLVR_ModLoader");
        }

        private async static Task WaitForUI()
        {
            new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.ApplicationIdle, UILoaded,
                Application.Current.Dispatcher);
            while (!_uiLoaded)
                await Task.Delay(1);
            return;
        }

        private static void UILoaded(object sender, EventArgs e)
        {
            _uiLoaded = true;
            //This stops the timer from running as it would just continue
            DispatcherTimer timer = sender as DispatcherTimer;
            timer.Stop();
        }

        private static void CheckForSteamVR()
        {
            Helper.SentryLog("Checking for Steam VR", Helper.SentryLogCategory.Program);
            Process[] processes = Process.GetProcessesByName("vrmonitor");
            if (processes.Length > 0)
            {
                Views.Console.Log("Found a Steam VR process");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = @"steam://run/250820",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);
            Views.Console.Log("Started SteamVR");
        }

        private static void AutoStart()
        {
            Helper.SentryLog("Checking for auto start", Helper.SentryLogCategory.Program);
            if (CommunicationsManager.CheckArgs("autostart", out string line))
            {
                if (line == "autostart")
                {
                    IsAutoStarting = true;
                }
            }
        }

        public static void FindItems()
        {
            Helper.SentryLog("Finding Items", Helper.SentryLogCategory.Program);
            Views.Console.Log("Finding items");
            Items = Helper.FindDownloadMods();
            Items.AddRange(Helper.FindDownloadedSkins());
            Items.AddRange(Helper.FindMyMods());
            Items.AddRange(Helper.FindMySkins());
        }

        public static void LaunchGame()
        {
            Helper.SentryLog("Launching game", Helper.SentryLogCategory.Program);
            MainWindow.GifState(MainWindow.gifStates.Play);
            CheckForItems();
            LaunchProcess();
        }

        private static void LaunchProcess()
        {
            Helper.SentryLog("Starting process", Helper.SentryLogCategory.Program);
            Views.Console.Log("Launching VTOL VR");

            var psi = new ProcessStartInfo
            {
                FileName = @"steam://run/667970",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(100, "Launching Game");
        }

        public static void Quit(string reason)
        {
            Helper.SentryLog("Quitting " + reason, Helper.SentryLogCategory.Program);
            Views.Console.Log($"Closing Application\nReason:{reason}");
            Process.GetCurrentProcess().Kill();
        }

        #region Item Extracting

        private static void CheckForItems()
        {
            Helper.SentryLog("Checking for items", Helper.SentryLogCategory.Program);
            if (!Directory.Exists(Root + ModsFolder))
                Directory.CreateDirectory(Root + ModsFolder);
            else
                ExtractItems(Root + ModsFolder);

            if (!Directory.Exists(Root + SkinsFolder))
                Directory.CreateDirectory(Root + SkinsFolder);
            else
                ExtractItems(Root + SkinsFolder);
        }

        private static void ExtractItems(string folderPath)
        {
            Helper.SentryLog("Extracting Items", Helper.SentryLogCategory.Program);
            Views.Console.Log("Extracting Items in " + folderPath);

            DirectoryInfo folder = new DirectoryInfo(folderPath);
            FileInfo[] files = folder.GetFiles("*.zip");

            if (files.Length == 0)
            {
                Views.Console.Log("No zips to extract in " + folderPath);
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                _itemsToExtract++;
                ExtractItem(files[i].FullName);
            }
        }

        public static void ExtractItem(string zipPath, bool overideItemsToExtract = false, int overideAmount = -1)
        {
            Helper.SentryLog("Extracting Item", Helper.SentryLogCategory.Program);
            Views.Console.Log("Extracting " + zipPath);

            if (overideItemsToExtract)
                _itemsToExtract = overideAmount;


            // Removing the .zip at the end of this path to create the folder
            string currentFolder = zipPath.Remove(zipPath.Length - 4);
            Directory.CreateDirectory(currentFolder);

            ItemHandler handler = new ItemHandler();
            handler.Callback += ExtractItemCallback;
            handler.ExtractItem(zipPath, currentFolder);
        }

        private static void ExtractItemCallback(object sender, ItemHandler.ItemExtractResult e)
        {
            if (e.IsSuccessful)
            {
                Views.Console.Log("Extracted " + e.ZipPath);
                Helper.TryDelete(e.ZipPath);
            }
            else
            {
                Views.Console.Log($"Failed to extract {e.ZipPath}\nError:{e.ErrorMessage}");
            }

            _itemsExtracted++;

            if (_itemsExtracted == _itemsToExtract)
            {
                Views.Console.Log("Finished extracting all items");
                MainWindow._instance.ItemManager.PopulateList();
                _itemsExtracted = 0;
                _itemsToExtract = -1;
            }
        }

        #endregion

        #region Action Queueing

        /*
         * Queue is a queuing system so that
         * only one thing will use the progress bar
         * and progress text at a time.
         */
        public static void Queue(Action action)
        {
            _actionQueue.Enqueue(action);
            if (IsBusy)
                return;

            IsBusy = true;
            while (_actionQueue.Count > 0)
            {
                _actionQueue.Dequeue().Invoke();
            }

            IsBusy = false;
        }

        #endregion

        public async static void GetReleases()
        {
            if (!await HttpHelper.CheckForInternet())
                return;
            Helper.SentryLog("Getting Releases", Helper.SentryLogCategory.Program);
            Views.Console.Log($"Connecting to API for latest releases");
            HttpHelper.DownloadStringAsync(
                URL + ApiURL + ReleasesURL + "/" + (Branch == string.Empty ? string.Empty : $"?branch={Branch}"),
                NewsDone);
        }

        private static async void NewsDone(HttpResponseMessage response)
        {
            Helper.SentryLog("Got releases", Helper.SentryLogCategory.Program);
            if (response.IsSuccessStatusCode)
            {
                Releases = JsonConvert.DeserializeObject<List<Release>>(await response.Content.ReadAsStringAsync());
                MainWindow._instance.news.LoadNews();
                Views.Console.Log($"Checking for updates###");
                if (!_folderInvalid)
                    Queue(delegate { Updater.CheckForUpdates(); });
            }
            else
            {
                //Failed
                Views.Console.Log("Error:\n" + response.StatusCode);
            }

            if (!string.IsNullOrEmpty(Views.Settings.Token))
                MainWindow._instance.settings.TestToken(true);
        }
    }
}