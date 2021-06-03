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
using LauncherCore.Classes;
using Console = LauncherCore.Views.Console;
using CoreCore.Jsons;

namespace LauncherCore
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

        private static bool _uiLoaded = false;
        private static int _itemsToExtract = 0;
        private static int _itemsExtracted = 0;
        private static Queue<Action> _actionQueue = new Queue<Action>();

        public async static void SetupAfterUI()
        {
            await WaitForUI();
            Helper.SentryLog("Setup after UI", Helper.SentryLogCategory.Program);
            MainWindow._instance.CreatePages();
            CommunicationsManager.CheckNoInternet();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckCustomBranch();
            CommunicationsManager.CheckAutoUpdate();
            if (CommunicationsManager.CheckSteamVR() && Views.Settings.SteamVR)
                CheckForSteamVR();

            DisableInternet = !await HttpHelper.CheckForInternet();

            GetReleases();
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.Title = $"{ProgramName}";
            MainWindow._instance.CheckForEvent();
            MainWindow.SetProgress(100, "Ready");
            CheckForItems();
            FindItems();
            MainWindow._instance.ItemManager.UpdateUI(true);
        }

        public static void SetVariables()
        {
            Helper.SentryLog("Setting Variables", Helper.SentryLogCategory.Program);
            Root = Directory.GetCurrentDirectory();
            VTOLFolder = Root.Replace("VTOLVR_ModLoader", "");
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
            Console.Log("Finding items");
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
            Console.Log("Launching VTOL VR");

            var psi = new ProcessStartInfo
            {
                FileName = @"steam://run/667970",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(0, "Launching Game");

            WaitForProcess();
        }

        private static async void WaitForProcess()
        {
            Helper.SentryLog("Waiting for process", Helper.SentryLogCategory.Program);
            Console.Log("Waiting for VTOL VR Process");
            for (int i = 1; i <= Views.Settings.USettings.MaxProcessAttempts; i++)
            {
                MainWindow.SetProgress((50 / Views.Settings.USettings.MaxProcessAttempts) * i,
                    "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                    break;

                if (i == Views.Settings.USettings.MaxProcessAttempts)
                {
                    //If we couldn't find it, go back to how it was at the start
                    MainWindow.GifState(MainWindow.gifStates.Paused);
                    MainWindow.SetProgress(100, "Couldn't find VTOLVR process.");
                    MainWindow.SetPlayButton(false);
                    Console.Log("Failed to find VTOL VR process");
                    return;
                }
            }

            //A delay just to make sure the game has fully launched
            Console.Log("Found process, waiting a bit");
            MainWindow.SetProgress(50, "Waiting for game...");
            await Task.Delay(10000);

            //Injecting Default Mod
            MainWindow.SetProgress(75, "Injecting Mod Loader...");
            InjectDefaultMod();
        }

        private static void InjectDefaultMod()
        {
            Helper.SentryLog("Injecting Mod", Helper.SentryLogCategory.Program);
            Console.Log("Injecting the ModLoader.dll");

            SharpMonoInjector.Injector injector = new SharpMonoInjector.Injector("vtolvr");
            byte[] assembly = File.ReadAllBytes(Path.Combine(Root, "ModLoader.dll"));

            using (injector)
            {
                IntPtr remoteAssembly = IntPtr.Zero;
                try
                {
                    remoteAssembly = injector.Inject(
                        assembly,
                        nameof(ModLoader),
                        nameof(ModLoader.Load),
                        nameof(ModLoader.Load.Init));
                }
                catch (Exception e)
                {
                    Console.Log($"Failed to inject. Reason: {e.Message}");
                }

                if (remoteAssembly == IntPtr.Zero)
                    return;
            }

            Console.Log($"Injection Finished Okay");
        }

        public static void Quit(string reason)
        {
            Helper.SentryLog("Quitting " + reason, Helper.SentryLogCategory.Program);
            Console.Log($"Closing Application\nReason:{reason}");
            Process.GetCurrentProcess().Kill();
        }

        #region Item Extracting

        private static void CheckForItems()
        {
            Helper.SentryLog("Checking for items", Helper.SentryLogCategory.Program);
            bool hasUpdated = false;
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
            Console.Log("Extracting Items in " + folderPath);

            DirectoryInfo folder = new DirectoryInfo(folderPath);
            FileInfo[] files = folder.GetFiles("*.zip");

            if (files.Length == 0)
            {
                Console.Log("No zips to extract in " + folderPath);
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
            Console.Log("Extracting " + zipPath);

            if (overideItemsToExtract)
                _itemsToExtract = overideAmount;

            string currentFolder = zipPath.Split('.')[0];
            Directory.CreateDirectory(currentFolder);

            ItemHandler handler = new ItemHandler();
            handler.Callback += ExtractItemCallback;
            handler.ExtractItem(zipPath, currentFolder);
        }

        private static void ExtractItemCallback(object sender, ItemHandler.ItemExtractResult e)
        {
            if (e.IsSuccessful)
            {
                Console.Log("Extracted " + e.ZipPath);
                Helper.TryDelete(e.ZipPath);
            }
            else
            {
                Console.Log($"Failed to extract {e.ZipPath}\nError:{e.ErrorMessage}");
            }

            _itemsExtracted++;

            if (_itemsExtracted == _itemsToExtract)
            {
                Console.Log("Finished extracting all items");
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
            Console.Log($"Connecting to API for latest releases");
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
                Queue(Updater.CheckForUpdates);
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + response.StatusCode);
            }

            if (!string.IsNullOrEmpty(Views.Settings.Token))
                MainWindow._instance.settings.TestToken(true);
        }
    }
}