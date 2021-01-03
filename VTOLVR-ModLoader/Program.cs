/* This is the main class which stores and runs the core background things.

*/
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Views;
using VTOLVR_ModLoader.Windows;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader
{
    public static class Program
    {
        public const string modsFolder = @"\mods";
        public const string skinsFolder = @"\skins";
        public const string injector = @"\injector.exe";
        public static string url = @"https://vtolvr-mods.com";
        public static string branch = string.Empty;
        public const string pageFormat = "&page=";
        public const string jsonFormat = "/?format=json";
        public const string apiURL = "/api";
        public const string modsURL = "/mods";
        public const string skinsURL = "/skins";
        public const string releasesURL = "/releases";
        public const string modsChangelogsURL = "/mods-changelogs";
        public const string skinsChangelogsURL = "/skins-changelogs";
        public const string ProgramNameBase = "VTOL VR Mod Loader";
        public const string LogName = "Launcher Log.txt";

        public static string root;
        public static string vtolFolder;
        public static string ProgramName = ProgramNameBase;
        public static bool autoStart { get; private set; }
        public static bool disableInternet = false;
        public static bool isBusy;
        public static List<Release> Releases { get; private set; }

        private static bool uiLoaded = false;
        private static int modsToExtract, skinsToExtract, extractedMods, extractedSkins, movedDep;
        private static Queue<Action> actionQueue = new Queue<Action>();
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
            GetReleases();
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.Title = $"{ProgramName}";
            MainWindow._instance.CheckForEvent();
            MainWindow._instance.ItemManager.UpdateUI(true);
            MainWindow.SetProgress(100, "Ready");
        }

        public static void SetVariables()
        {
            Helper.SentryLog("Setting Variables", Helper.SentryLogCategory.Program);
            root = Directory.GetCurrentDirectory();
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");
        }

        private async static Task WaitForUI()
        {
            new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.ApplicationIdle, UILoaded,
                       Application.Current.Dispatcher);
            while (!uiLoaded)
                await Task.Delay(1);
            return;
        }

        private static void UILoaded(object sender, EventArgs e)
        {
            uiLoaded = true;
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
            Process.Start("steam://run/250820");
            Views.Console.Log("Started SteamVR");
        }

        private static void AutoStart()
        {
            Helper.SentryLog("Checking for auto start", Helper.SentryLogCategory.Program);
            if (CommunicationsManager.CheckArgs("autostart", out string line))
            {
                if (line == "autostart")
                {
                    autoStart = true;
                }
            }
        }
        public static void LaunchGame()
        {
            Helper.SentryLog("Launching game", Helper.SentryLogCategory.Program);
            MainWindow.GifState(MainWindow.gifStates.Play);
            ExtractMods();
        }
        private static void LaunchProcess()
        {
            LowerCaseJsons();

            Helper.SentryLog("Starting process", Helper.SentryLogCategory.Program);
            Console.Log("Launching VTOL VR");
            Process.Start("steam://run/667970");

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(0, "Launching Game");

            WaitForProcess();
        }

        private static async void WaitForProcess()
        {
            Helper.SentryLog("Waiting for process", Helper.SentryLogCategory.Program);
            Console.Log("Waiting for VTOL VR Process");
            int maxTries = 5;
            for (int i = 1; i <= maxTries; i++)
            {
                //Doing 5 tries to search for the process
                MainWindow.SetProgress(10 * i, "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                    break;

                if (i == maxTries)
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
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Console.Log("Injecting the ModLoader.dll");
            Process.Start(root + injector, defaultStart);
        }

        public static void Quit(string reason)
        {
            Helper.SentryLog("Quitting " + reason, Helper.SentryLogCategory.Program);
            Console.Log($"Closing Application\nReason:{reason}");
            Process.GetCurrentProcess().Kill();
        }

        #region Mod/Skin Handling
        public static void ExtractMods()
        {
            Helper.SentryLog("Extracting Mods", Helper.SentryLogCategory.Program);
            MainWindow.SetPlayButton(true);
            MainWindow.SetProgress(0, "Extracting  mods...");

            //If the mods folder is missing
            if (!Directory.Exists(root + modsFolder))
                Directory.CreateDirectory(root + modsFolder);

            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                MainWindow.SetPlayButton(false);
                MainWindow.SetProgress(100, "No new mods were found");
                ExtractSkins();
                return;
            }
            modsToExtract = files.Length;
            string currentFolder;

            MainWindow.SetProgress(0, "Extracting mods...");
            for (int i = 0; i < files.Length; i++)
            {
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                Directory.CreateDirectory(currentFolder);
                Console.Log("Extracting " + files[i].FullName);
                Helper.ExtractZipToDirectory(files[i].FullName, currentFolder, ExtractedMod);
            }
        }

        private static void ExtractedMod(string zipPath, string extractedPath, string result)
        {
            if (!result.Equals("Success"))
            {
                Notification.Show($"Error Extracting {zipPath}\nError:{result}");
                Console.Log($"Error Extracting {zipPath}\nError:{result}");
            }
            extractedMods++;
            Console.Log($"({extractedMods}/{modsToExtract})Finished Extracting {zipPath}");
            MainWindow.SetProgress(extractedMods / modsToExtract * 100, $"({extractedMods}/{modsToExtract})Extracting mods...");
            //Deleting the zip
            Helper.TryDelete(zipPath);

            if (extractedMods == modsToExtract)
            {
                MainWindow.SetPlayButton(false);
                MainWindow.SetProgress(100, extractedMods == 0 ? "No mods were extracted" : "Extracted " + extractedMods +
                    (extractedMods == 1 ? " new mod" : " new mods"));

                ExtractSkins();
            }
        }

        private static void ExtractSkins()
        {
            Helper.SentryLog("Extracting Skins", Helper.SentryLogCategory.Program);
            MainWindow.SetPlayButton(true);
            MainWindow.SetProgress(0, "Extracting skins...");

            //If the skins folder is missing
            if (!Directory.Exists(root + skinsFolder))
                Directory.CreateDirectory(root + skinsFolder);

            DirectoryInfo folder = new DirectoryInfo(Program.root + Program.skinsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                MainWindow.SetPlayButton(false);
                MainWindow.SetProgress(100,
                    (extractedMods == 0 ? "0 New Mods" : (extractedMods == 1 ? "1 New Mod" : extractedMods + " New Mods")) +
                    " and " +
                    (extractedSkins == 0 ? "0 New Skins" : (extractedSkins == 1 ? "1 New Skin" : extractedSkins + " New Skins")) +
                    " extracted" +
                    " and " +
                    (movedDep == 0 ? "0 New Dependencies" : (movedDep == 1 ? "1 New Dependencies" : movedDep + " New Dependencies")) +
                    " moved");
                extractedMods = 0;
                extractedSkins = 0;
                movedDep = 0;
                skinsToExtract = 0;
                modsToExtract = 0;

                LaunchProcess();
                return;
            }
            skinsToExtract = files.Length;
            string currentFolder;
            MainWindow.SetProgress(0, "Extracting skins...");
            for (int i = 0; i < files.Length; i++)
            {
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                Directory.CreateDirectory(currentFolder);
                Helper.ExtractZipToDirectory(files[i].FullName, currentFolder, SkinExtracted);
            }
        }

        private static void SkinExtracted(string zipPath, string extractedPath, string result)
        {
            if (!result.Equals("Success"))
            {
                Notification.Show($"Error Extracting {zipPath}\nError:{result}");
                Console.Log($"Error Extracting {zipPath}\nError:{result}");
            }
            extractedSkins++;
            Console.Log($"({extractedSkins}/{skinsToExtract})Finished Extracting {zipPath}");
            MainWindow.SetProgress(extractedSkins / skinsToExtract * 100, $"({extractedSkins}/{skinsToExtract})Extracting skins...");
            //Deleting the zip
            Helper.TryDelete(zipPath);

            if (extractedSkins == skinsToExtract)
            {
                MainWindow.SetPlayButton(false);
                //This is the final text displayed in the progress text
                MainWindow.SetProgress(100,
                    (extractedMods == 0 ? "0 New Mods" : (extractedMods == 1 ? "1 New Mod" : extractedMods + " New Mods")) +
                    " and " +
                    (extractedSkins == 0 ? "0 New Skins" : (extractedSkins == 1 ? "1 New Skin" : extractedSkins + " New Skins")) +
                    " extracted" +
                    " and " +
                    (movedDep == 0 ? "0 New Dependencies" : (movedDep == 1 ? "1 New Dependencies" : movedDep + " New Dependencies")) +
                    " moved");

                extractedMods = 0;
                extractedSkins = 0;
                movedDep = 0;
                skinsToExtract = 0;
                modsToExtract = 0;

                LaunchProcess();
            }
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
        #endregion

        #region Action Queueing
        /*
         * Queue is a queuing system so that
         * only one thing will use the progress bar
         * and progress text at a time.
         */
        public static void Queue(Action action)
        {
            actionQueue.Enqueue(action);
            if (isBusy)
                return;

            isBusy = true;
            while (actionQueue.Count > 0)
            {
                actionQueue.Dequeue().Invoke();
            }
            isBusy = false;
        }
        #endregion

        public async static void GetReleases()
        {
            if (!await HttpHelper.CheckForInternet())
                return;
            Helper.SentryLog("Getting Releases", Helper.SentryLogCategory.Program);
            Console.Log($"Connecting to API for latest releases");
            HttpHelper.DownloadStringAsync(
                url + apiURL + releasesURL + "/" + (branch == string.Empty ? string.Empty : $"?branch={branch}"),
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
        private static void LowerCaseJsons()
        {
            // We had to make all the keys in the mods info.json lower case
            // So this function just converts the old info.json to lower case

            Helper.SentryLog("Finding Mods", Helper.SentryLogCategory.Program);
            Console.Log("Checking if we need to update any mods info.json");
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);

            DirectoryInfo[] folders = folder.GetDirectories();
            JObject lastJson;
            Console.Log($"We have {folders.Length} to check");
            for (int i = 0; i < folders.Length; i++)
            {
                if (File.Exists(folders[i].FullName + "\\info.json"))
                {
                    lastJson = Helper.JObjectTryParse(
                        File.ReadAllText(folders[i].FullName + "\\info.json"),
                        out Exception exception);
                    if (lastJson != null)
                    {
                        ConvertJson(lastJson, folders[i].FullName + "\\info.json");
                    }
                    else
                    {
                        Console.Log(exception.Message);
                    }
                }
            }

            //Finding users my projects mods
            if (!string.IsNullOrEmpty(Views.Settings.ProjectsFolder))
            {
                Console.Log("Checking the users projects folder");
                DirectoryInfo projectsFolder = new DirectoryInfo(Views.Settings.ProjectsFolder + ProjectManager.modsFolder);
                folders = projectsFolder.GetDirectories();
                Console.Log($"There are {folders.Length} to check");
                for (int i = 0; i < folders.Length; i++)
                {
                    if (!File.Exists(Path.Combine(folders[i].FullName, "Builds", "info.json")))
                    {
                        Console.Log("Missing info.json in " +
                            Path.Combine(folders[i].FullName, "Builds", "info.json"));
                        continue;
                    }
                    JObject json = Helper.JObjectTryParse(
                        File.ReadAllText(Path.Combine(folders[i].FullName, "Builds", "info.json")),
                        out Exception exception);

                    if (json != null)
                    {
                        ConvertJson(json, Path.Combine(folders[i].FullName, "Builds", "info.json"));
                    }
                }
                Console.Log("End of for loop");
            }
            Console.Log("Finished checking JSONs");
        }
        private static void ConvertJson(JObject json, string path)
        {
            Helper.SentryLog("Checking Json", Helper.SentryLogCategory.Program);
            Console.Log("Checking json at: " + path);
            bool hasChanged = false;
            JObject newJson = new JObject();

            ChangeJsonName("Name", ProjectManager.jName, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Description", ProjectManager.jDescription, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Tagline", ProjectManager.jTagline, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Version", ProjectManager.jVersion, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Dll File", ProjectManager.jDll, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Last Edit", ProjectManager.jEdit, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Source", ProjectManager.jSource, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Preview Image", ProjectManager.jPImage, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Web Preview Image", ProjectManager.jWImage, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Dependencies", ProjectManager.jDeps, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Public ID", ProjectManager.jID, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Is Public", ProjectManager.jPublic, ref json, ref newJson, ref hasChanged);
            ChangeJsonName("Unlisted", ProjectManager.jUnlisted, ref json, ref newJson, ref hasChanged);

            if (hasChanged)
            {
                Helper.SentryLog("Saving new json", Helper.SentryLogCategory.Program);
                Console.Log($"{path} has been changed, saving");
                File.WriteAllText(path, newJson.ToString());
                Console.Log("Saved");
            }
            else
            {
                Console.Log("All JSONs are up to date");
            }
        }
        private static void ChangeJsonName(string oldName, string newName, ref JObject oldJson, ref JObject newJson, ref bool hasChanged)
        {
            if (oldJson[oldName] != null)
            {
                newJson[newName] = oldJson[oldName];
                hasChanged = true;
            }
            else if (oldJson[newName] != null)
            {
                newJson[newName] = oldJson[newName];
            }
        }
    }
}
