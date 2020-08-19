/* This is the main class which stores and runs the core background things.

*/
using Valve.Newtonsoft.Json.Linq;
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
        private const string releasesURL = "/releases";
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
            MainWindow._instance.CreatePages();
            CommunicationsManager.CheckNoInternet();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckCustomBranch();
            CommunicationsManager.CheckAutoUpdate();
            Console.Log(Views.Settings.SteamVR.ToString());
            if (CommunicationsManager.CheckSteamVR() && Views.Settings.SteamVR)
                CheckForSteamVR();
            GetReleases();
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.Title = $"{ProgramName}";
            MainWindow.SetProgress(100, "Ready");
        }

        public static void SetVariables()
        {
            root = Directory.GetCurrentDirectory();
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");
        }

        private async static Task WaitForUI()
        {
            new DispatcherTimer(TimeSpan.Zero,DispatcherPriority.ApplicationIdle,UILoaded,
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
            Process[] processes = Process.GetProcessesByName("vrmonitor");
            if (processes.Length > 0)
            {
                Views.Console.Log("Found a steam vr process");
                return;
            }
            Process.Start("steam://run/250820");
            Views.Console.Log("Started SteamVR");
        }

        private static void AutoStart()
        {
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
            ExtractMods();
        }
        private static void LaunchProcess()
        {
            Console.Log("Launching VTOL VR");
            //Process.Start("steam://run/667970");

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(0, "Launching Game");
            MainWindow.GifState(MainWindow.gifStates.Play);

            WaitForProcess();
        }

        private static async void WaitForProcess()
        {
            Console.Log("Waiting for VTOL VR Process");
            int maxTries = 5;
            for (int i = 1; i <= maxTries; i++)
            {
                //Doing 5 tries to search for the process
                MainWindow.SetProgress(10 * i, "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                {
                    break;
                }

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
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Console.Log("Injecting the ModLoader.dll");
            Process.Start(root + injector, defaultStart);
        }

        public static void Quit(string reason)
        {
            Console.Log($"Closing Application\nReason:{reason}");
            Process.GetCurrentProcess().Kill();
        }

        #region Mod/Skin Handeling
        public static void ExtractMods()
        {
            MainWindow.SetPlayButton(true);
            MainWindow.SetProgress(0, "Extracting  mods...");
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                MainWindow.SetPlayButton(false);
                MainWindow.SetProgress(100, "No new mods were found");
                MoveDependencies();
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

        private static void ExtractedMod(string zipPath, string extractedPath)
        {
            extractedMods++;
            Console.Log($"({extractedMods}/{modsToExtract})Finished Extracting {zipPath}");
            MainWindow.SetProgress(extractedMods / modsToExtract * 100, $"({extractedMods}/{modsToExtract})Extracting mods...");
            //Deleting the zip
            File.Delete(zipPath);

            if (extractedMods == modsToExtract)
            {
                MainWindow.SetPlayButton(false);
                MainWindow.SetProgress(100, extractedMods == 0 ? "No mods were extracted" : "Extracted " + extractedMods +
                    (extractedMods == 1 ? " new mod" : " new mods"));
                
                MoveDependencies();
            }
        }

        private static void ExtractSkins()
        {
            MainWindow.SetPlayButton(true);
            MainWindow.SetProgress(0, "Extracting skins...");
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

        private static void SkinExtracted(string zipPath, string extractedPath)
        {
            extractedSkins++;
            Console.Log($"({extractedSkins}/{skinsToExtract})Finished Extracting {zipPath}");
            MainWindow.SetProgress(extractedSkins / skinsToExtract * 100, $"({extractedSkins}/{skinsToExtract})Extracting skins...");
            //Deleting the zip
            File.Delete(zipPath);

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

        private static void MoveDependencies()
        {
            MainWindow.SetPlayButton(true);
            Task.Run(delegate 
            {
                string[] modFolders = Directory.GetDirectories(root + modsFolder);

                string fileName;
                string[] split;
                for (int i = 0; i < modFolders.Length; i++)
                {
                    string[] subFolders = Directory.GetDirectories(modFolders[i]);
                    for (int j = 0; j < subFolders.Length; j++)
                    {
                        if (subFolders[j].ToLower().Contains("dependencies"))
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() => { 
                                Console.Log("Found the folder dependencies"); }));
                            
                            string[] depFiles = Directory.GetFiles(subFolders[j], "*.dll");
                            for (int k = 0; k < depFiles.Length; k++)
                            {
                                split = depFiles[k].Split('\\');
                                fileName = split[split.Length - 1];

                                if (File.Exists(Directory.GetParent(Program.root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName))
                                {
                                    string oldHash = CalculateMD5(Directory.GetParent(Program.root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName);
                                    string newHash = CalculateMD5(depFiles[k]);
                                    if (!oldHash.Equals(newHash))
                                    {
                                        File.Copy(depFiles[k], Directory.GetParent(Program.root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName,
                                            true);
                                        movedDep++;
                                        Application.Current.Dispatcher.Invoke(new Action(() => {
                                            Console.Log($"Updated Dependencie {depFiles[k]}");
                                        }));
                                    }
                                }
                                else
                                {                                   
                                    File.Copy(depFiles[k], Directory.GetParent(Program.root).FullName +
                                                @"\VTOLVR_Data\Managed\" + fileName,
                                                true);
                                    movedDep++;
                                    Application.Current.Dispatcher.Invoke(new Action(() => {
                                        Console.Log($"Moved Dependencie {depFiles[k]}");
                                    }));
                                }
                            }
                            break;
                        }
                    }
                }
            }).ContinueWith(delegate {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    MovedDependencies();
                }));
            });
        }
        private static void MovedDependencies()
        {
            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(100, movedDep == 0 ? "Checked Dependencies" : "Moved " + movedDep
                + (movedDep == 1 ? " dependency" : " dependencies"));

            ExtractSkins();
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

            Console.Log($"Connecting to API for latest releases");
            HttpHelper.DownloadStringAsync(
                url + apiURL + releasesURL + "/" + (branch == string.Empty ? string.Empty : $"?branch={branch}"),
                NewsDone);
        }

        private static async void NewsDone(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                Releases = new List<Release>();
                ConvertUpdates(await response.Content.ReadAsStringAsync());
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + response.StatusCode);
            }
            if (!string.IsNullOrEmpty(Views.Settings.Token))
                MainWindow._instance.settings.TestToken(true);
        }

        private static void ConvertUpdates(string jsonString)
        {
            JArray results = JArray.Parse(jsonString);
            Release lastUpdate;
            JArray lastFilesJson;
            List<UpdateFile> files;
            for (int i = 0; i < results.Count; i++)
            {
                lastUpdate = new Release($"{results[i]["tag_name"]} {results[i]["name"]}",
                    results[i]["tag_name"].ToString(),
                    results[i]["body"].ToString());
                if (results[i]["files"] != null)
                {
                    lastFilesJson = JArray.FromObject(results[i]["files"]);
                    files = new List<UpdateFile>(lastFilesJson.Count);
                    for (int j = 0; j < lastFilesJson.Count; j++)
                    {
                        files.Add(new UpdateFile(
                            lastFilesJson[j]["file_name"].ToString(),
                            lastFilesJson[j]["file_hash"].ToString(),
                            lastFilesJson[j]["file_location"].ToString(),
                            lastFilesJson[j]["file"].ToString()));
                    }
                    lastUpdate.SetFiles(files.ToArray());
                }
                Releases.Add(lastUpdate);
            }

            MainWindow._instance.news.LoadNews();
            Queue(Updater.CheckForUpdates);
        }

        public static void TestCrash()
        {
            List<string> list = new List<string>(5);
            list[100] = "Crash me now";
        }
    }
}
