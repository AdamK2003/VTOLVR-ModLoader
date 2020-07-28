/* This is the main class which stores and runs the core background things.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
        public const string modsChangelogsURL = "/mods-changelogs";
        public const string skinsChangelogsURL = "/skins-changelogs";
        public const string ProgramNameBase = "VTOL VR Mod Loader";
        public const string LogName = "Launcher Log.txt";

        public static string root;
        public static string vtolFolder;
        public static string ProgramName;
        public static bool autoStart { get; private set; }
        public static bool disableInternet = false;
        public static bool isBusy;

        private static bool uiLoaded = false;
        private static int extractedMods, extractedSkins, movedDep;
        private static Queue<Action> actionQueue = new Queue<Action>();
        public async static void SetupAfterUI()
        {
            await WaitForUI();
            MainWindow._instance.CreatePages();
            CommunicationsManager.CheckNoInternet();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckCustomBranch();
            if (CommunicationsManager.CheckSteamVR())
                CheckForSteamVR();
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.news.LoadNews();
            MainWindow._instance.Title = $"{ProgramName}";
            Queue(ExtractMods);
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
            Process.Start("steam://run/667970");

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(0, "Launching Game");
            MainWindow.GifState(MainWindow.gifStates.Play);

            WaitForProcess();
        }

        private static async void WaitForProcess()
        {
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
                    return;
                }
            }

            //A delay just to make sure the game has fully launched,
            MainWindow.SetProgress(50, "Waiting for game...");
            await Task.Delay(10000);

            //Injecting Default Mod
            MainWindow.SetProgress(75, "Injecting Mod Loader...");
            InjectDefaultMod();


            //Starting a new thread for the console
            MainWindow._instance.console.StartTCPListener();
        }
        private static void InjectDefaultMod()
        {
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Process.Start(root + injector, defaultStart);
        }

        public static void Quit()
        {
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
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                MainWindow.SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting mods... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                Directory.CreateDirectory(currentFolder);
                Helper.ExtractZipToDirectory(files[i].FullName, currentFolder);
                extractedMods++;

                //Deleting the zip
                File.Delete(files[i].FullName);
            }

            MainWindow.SetPlayButton(false);
            MainWindow.SetProgress(100, extractedMods == 0 ? "No mods were extracted" : "Extracted " + extractedMods +
                (extractedMods == 1 ? " new mod" : " new mods"));
            MoveDependencies();

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

                return;
            }
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                MainWindow.SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting skins... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                Directory.CreateDirectory(currentFolder);
                Helper.ExtractZipToDirectory(files[i].FullName, currentFolder);
                extractedSkins++;

                //Deleting the zip
                File.Delete(files[i].FullName);
            }

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
            FinishedQueue();
        }

        private static void MoveDependencies()
        {
            MainWindow.SetPlayButton(true);
            string[] modFolders = Directory.GetDirectories(Program.root + Program.modsFolder);

            string fileName;
            string[] split;
            for (int i = 0; i < modFolders.Length; i++)
            {
                string[] subFolders = Directory.GetDirectories(modFolders[i]);
                for (int j = 0; j < subFolders.Length; j++)
                {
                    Console.Log("Checking " + subFolders[j].ToLower());
                    if (subFolders[j].ToLower().Contains("dependencies"))
                    {
                        Console.Log("Found the folder dependencies");
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
                                }
                            }
                            else
                            {
                                Console.Log("Moved file \n" + Directory.GetParent(Program.root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                File.Copy(depFiles[k], Directory.GetParent(Program.root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName,
                                            true);
                                movedDep++;
                            }
                        }
                        break;
                    }
                }
            }

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

        /*
         * FinishedQueue and Queue is a queuing system so that
         * only one thing will use the progress bar
         * and progress text at a time.
         */

        public static void FinishedQueue()
        {
            if (actionQueue.Count == 0)
            {
                isBusy = false;
                return;
            }
            actionQueue.Dequeue().Invoke();
        }
        public static void Queue(Action action)
        {
            if (!isBusy)
            {
                action.Invoke();
                isBusy = true;
            }
            else
            {
                actionQueue.Enqueue(action);
            }
        }
    }
}
