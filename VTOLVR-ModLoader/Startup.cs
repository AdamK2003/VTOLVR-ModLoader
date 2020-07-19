/* Startup is a static class for handeling actions which need to be ran before the UI shows.
 
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

        private static readonly string[] needFiles = { "SharpMonoInjector.dll", "injector.exe", "Updater.exe", "Gameloop.Vdf.dll","Newtonsoft.Json.dll" };
        private static readonly string[] neededDLLFiles = { @"\Plugins\discord-rpc.dll", @"\Managed\0Harmony.dll", @"\Managed\Newtonsoft.Json.dll" };
        public static void RunStartUp()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            Program.ProgramName = $"{Program.ProgramNameBase} {version.Major}.{version.Minor}.{version.Build} {(debug ? "[Development Mode]" : string.Empty)}";
            HttpHelper.SetHeader();
            Program.SetVariables();
            SearchForProcess();
            CheckBaseFolder();
            CheckFolder();
        }

        private static void SearchForProcess()
        {
            //Stopping their being more than one open (Yes this could close the other one half way through a download)
            Process[] p = Process.GetProcessesByName("VTOLVR-ModLoader");
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id != Process.GetCurrentProcess().Id)
                {
                    // check if the window is hidden / minimized
                    if (p[i].MainWindowHandle == IntPtr.Zero)
                    {
                        // the window is hidden so try to restore it before setting focus.
                        ShowWindow(p[i].Handle, ShowWindowEnum.Restore);
                    }

                    // set user the focus to the window
                    SetForegroundWindow(p[i].MainWindowHandle);
                    MainWindow.Quit();
                }
            }
        }

        private static void CheckBaseFolder()
        {
            //Checking the folder which this is in
            string[] pathSplit = Program.root.Split('\\');
            if (pathSplit[pathSplit.Length - 1] != "VTOLVR_ModLoader")
            {
                if (pathSplit[pathSplit.Length - 1] == "System32")
                {
                    //The user has ran it from a URI so it's path is System32
                    try
                    {
                        FindSteamFolders();
                    }
                    catch (Exception e)
                    {
                        Notification.Show(e.ToString());
                        throw;
                    }
                }
                else
                {
                    Notification.Show("It seems I am not in the folder \"VTOLVR_ModLoader\", place make sure I am in there other wise the in game menu won't load", "Wrong Folder");
                    Program.Quit();
                }
                
            }

            //Now it should be in the correct folder, but just need to check if its in the games folder
            string vtolexe = Program.root.Replace("VTOLVR_ModLoader", "VTOLVR.exe");
            if (!File.Exists(vtolexe))
            {
                Notification.Show("It seems the VTOLVR_ModLoader folder isn't with the other games files\nPlease move me to VTOL VR's game Program.root directory.", "Wrong Folder Location");
                Program.Quit();
            }
        }
        /// <summary>
        /// Checks for files which the Mod Loader needs to work such as .dll files
        /// </summary>
        private static void CheckFolder()
        {
            //Checking if the files we need to run are there
            foreach (string file in needFiles)
            {
                if (!File.Exists(Program.root + @"\" + file))
                {
                    WrongFolder(file);
                    return;
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
                }
            }
        }
        private static void WrongFolder(string file)
        {
            Notification.Show("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.",
                "Missing File",
                callback: delegate { Program.Quit(); });
        }
        private static void MissingManagedFile(string file)
        {
            Notification.Show("I can't seem to find " + file + " in VTOL VR > VTOLVR_Data, please make sure this file is here otherwise the mod loader won't work",
                "Missing File",
                callback: delegate { Program.Quit(); });
        }

        private static void FindSteamFolders()
        {
            //Environment.CurrentDirectory

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
            Environment.CurrentDirectory = folder + @"\steamapps\common\VTOL VR\VTOLVR_ModLoader";
            Program.SetVariables();
        }
    }
}
