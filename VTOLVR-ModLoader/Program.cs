/* This is the main class which stores and runs the core background things.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VTOLVR_ModLoader.Views;

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

        public static string root;
        public static string vtolFolder;
        public static bool autoStart { get; private set; }
        private static bool uiLoaded = false;

        public async static void SetupAfterUI()
        {
            await WaitForUI();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckCustomBranch();
            MainWindow._instance.CreatePages();
            AutoStart();
            CommunicationsManager.CheckURI();
            MainWindow._instance.news.LoadNews(0);
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

        public static bool CheckForInternet()
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
                return false;
            }
        }

        public static void LaunchGame()
        {
            Process.Start("steam://run/667970");

            MainWindow._instance.SetPlayButton(false);
            MainWindow._instance.SetProgress(0, "Launching Game");
            MainWindow._instance.GifState(MainWindow.gifStates.Play);

            WaitForProcess();
        }

        private static async void WaitForProcess()
        {
            int maxTries = 5;
            for (int i = 1; i <= maxTries; i++)
            {
                //Doing 5 tries to search for the process
                MainWindow._instance.SetProgress(10 * i, "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                {
                    break;
                }

                if (i == maxTries)
                {
                    //If we couldn't find it, go back to how it was at the start
                    MainWindow._instance.GifState(MainWindow.gifStates.Paused);
                    MainWindow._instance.SetProgress(100, "Couldn't find VTOLVR process.");
                    MainWindow._instance.SetPlayButton(false);
                    return;
                }
            }

            //A delay just to make sure the game has fully launched,
            MainWindow._instance.SetProgress(50, "Waiting for game...");
            await Task.Delay(10000);

            //Injecting Default Mod
            MainWindow._instance.SetProgress(75, "Injecting Mod Loader...");
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
    }
}
