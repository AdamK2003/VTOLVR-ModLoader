/* Start up is a static class for handling actions which need to be ran before the UI shows.
 
The current start up process for the mod loader 
1. Check that there isn't any other mod loaders running
     yes: Close this and send the command to that one
     no: Continue
*/

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Gameloop;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using VTOLVR_ModLoader.Windows;
using System.Reflection;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Views;

namespace VTOLVR_ModLoader
{
    static class Startup
    {
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);
        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        private static readonly string[] needFiles = { "injector.exe", "Updater.exe" };
        private static readonly string[] neededDLLFiles = { @"\Plugins\discord-rpc.dll", @"\Managed\0Harmony.dll" };
        public static bool RunStartUp()
        {
            Helper.SentryLog("Running Start up", Helper.SentryLogCategory.Startup);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            string devText = string.Empty;
            if (version.Revision != 0)
                devText = $"d{version.Revision}";
            Program.ProgramName = $"{Program.ProgramNameBase} {version.Major}.{version.Minor}.{version.Build}{devText} {(debug ? "[Development Mode]" : string.Empty)}";
            Views.Console.Log(Program.ProgramName);
            HttpHelper.SetHeader();
            Program.SetVariables();
            if (!CheckBaseFolder())
                return false;
            if (!CheckFolder())
                return false;
            ClearOldFiles();
            AttachCoreLogger();
            return true;
        }
        /// <summary>
        /// Returns True if another instance of the mod loader is found.
        /// </summary>
        /// <returns></returns>
        public static bool SearchForProcess()
        {
            Helper.SentryLog("Searching for existing process", Helper.SentryLogCategory.Startup);
            Process[] p = Process.GetProcessesByName("VTOLVR-ModLoader");
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id != Process.GetCurrentProcess().Id)
                {
                    Views.Console.Log("Found another instance");
                    // check if the window is hidden / minimized
                    if (p[i].MainWindowHandle == IntPtr.Zero)
                    {
                        // the window is hidden so try to restore it before setting focus.
                        ShowWindow(p[i].Handle, ShowWindowEnum.Restore);
                    }

                    // set user the focus to the window
                    SetForegroundWindow(p[i].MainWindowHandle);

                    return true;
                }
            }
            return false;
        }

        private static bool CheckBaseFolder()
        {
            Helper.SentryLog("Checking base folder", Helper.SentryLogCategory.Startup);
            //Checking the folder which this is in
            string[] pathSplit = Program.root.Split('\\');
            if (pathSplit[pathSplit.Length - 1] != "VTOLVR_ModLoader")
            {
                try
                {
                    FindSteamFolders();
                }
                catch (Exception e)
                {
                    Views.Console.Log("Not in correct folder\n" + e);
                    Notification.Show("It seems I am not in the \"VTOLVR_ModLoader\" folder, please make sure I am in there otherwise the in-game menu won't load",
                        "Wrong Folder",
                        closedCallback: delegate { Program.Quit("Not in correct folder"); });
                    return false;
                }

            }

            //Now it should be in the correct folder, but just need to check if its in the games folder
            string vtolexe = Program.root.Replace("VTOLVR_ModLoader", "VTOLVR.exe");
            if (!File.Exists(vtolexe))
            {
                Notification.Show("It seems the VTOLVR_ModLoader folder isn't with the other games files\nPlease move me to VTOL VR's game Program.root directory.",
                    "Wrong Folder Location",
                    closedCallback: delegate { Program.Quit("VTOLVR_ModLoader isn't in the correct folder"); });
                return false;
            }
            return true;
        }
        /// <summary>
        /// Checks for files which the Mod Loader needs to work such as .dll files
        /// </summary>
        private static bool CheckFolder()
        {
            Helper.SentryLog("Checking folder", Helper.SentryLogCategory.Startup);
            //Checking if the files we need to run are there
            foreach (string file in needFiles)
            {
                if (!File.Exists(Program.root + @"\" + file))
                {
                    WrongFolder(file);
                    return false;
                }
            }

            //Checking if the mods folder is there
            if (!Directory.Exists(Program.root + Program.modsFolder))
            {
                Directory.CreateDirectory(Program.root + Program.modsFolder);
            }

            //Checking the Managed Folder
            foreach (string file in neededDLLFiles)
            {
                if (!File.Exists(Directory.GetParent(Program.root).FullName + @"\VTOLVR_Data" + file))
                {
                    MissingManagedFile(file);
                    return false;
                }
            }
            return true;
        }
        private static void WrongFolder(string file)
        {
            Notification.Show("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.",
                "Missing File",
                closedCallback: delegate { Program.Quit($"Can't find {file} in my folder"); });
            Views.Console.Log("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.");
        }
        private static void MissingManagedFile(string file)
        {
            Notification.Show("I can't seem to find " + file + " in VTOL VR > VTOLVR_Data, please make sure this file is here otherwise the mod loader won't work",
                "Missing File",
                closedCallback: delegate { Program.Quit($"Can't find {file} in VTOL VR > VTOLVR_Data"); });
        }

        private static void FindSteamFolders()
        {
            Helper.SentryLog("Finding Steam Folders", Helper.SentryLogCategory.Startup);
            string regPath = (string)Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Valve\Steam",
                @"SteamPath",
                @"NULL");

            if (CheckForVTOL(regPath))
            {
                SetWorkingDirectory(regPath);
                return;
            }

            if (!File.Exists(regPath + @"\steamapps\libraryfolders.vdf"))
                Notification.Show("libraryfolders.vdf missing from " + regPath + @"\steamapps");

            VProperty libFolders = VdfConvert.Deserialize(File.ReadAllText(regPath + @"\steamapps\libraryfolders.vdf"));

            int i = 1;
            while (true)
            {
                try
                {
                    //Don't know how to check if a value exists without it causing an exception
                    string folder = libFolders.Value.Value<string>(i.ToString());
                    if (CheckForVTOL(folder))
                    {
                        SetWorkingDirectory(folder);
                        return;
                    }
                }
                catch
                {
                    return;
                }
                i++;
            }
        }

        private static bool CheckForVTOL(string folder)
        {
            return Directory.Exists(folder + @"\steamapps\common\VTOL VR\VTOLVR_ModLoader");
        }
        private static void SetWorkingDirectory(string folder)
        {
            Helper.SentryLog("Setting working directory", Helper.SentryLogCategory.Startup);
            Environment.CurrentDirectory = folder + @"\steamapps\common\VTOL VR\VTOLVR_ModLoader";
            Program.SetVariables();
        }
        private static void ClearOldFiles()
        {
            // When Costura got added, these DLLs were merged into 
            // the launcher.exe, so this function deletes them as 
            // they're just a waste of space.
            if (File.Exists(Path.Combine(Program.root, "WpfAnimatedGif.dll")))
                Helper.TryDelete(Path.Combine(Program.root, "WpfAnimatedGif.dll"));

            if (File.Exists(Path.Combine(Program.root, "Valve.Valve.Newtonsoft.Json.dll")))
                Helper.TryDelete(Path.Combine(Program.root, "Valve.Valve.Newtonsoft.Json.dll"));

            if (File.Exists(Path.Combine(Program.root, "SimpleTCP.dll")))
                Helper.TryDelete(Path.Combine(Program.root, "SimpleTCP.dll"));

            if (File.Exists(Path.Combine(Program.root, "Gameloop.Vdf.dll")))
                Helper.TryDelete(Path.Combine(Program.root, "Gameloop.Vdf.dll"));
        }
        private static void AttachCoreLogger()
        {
            Core.Logger.OnMessageLogged += CoreLogger;
        }

        private static void CoreLogger(object arg1, Core.Logger.LogType arg2)
        {
            Views.Console.Log($"(Core: {arg2}) {arg1}");
        }
    }
}
