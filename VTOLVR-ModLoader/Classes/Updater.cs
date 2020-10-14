using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VTOLVR_ModLoader.Views;
using VTOLVR_ModLoader.Windows;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader.Classes
{
    static class Updater
    {
        private static Queue<UpdateFile> filesToUpdate = new Queue<UpdateFile>();
        private static UpdateFile currentFile;
        private static bool _updateLauncher;
        public static void CheckForUpdates()
        {
            if (!Views.Settings.AutoUpdate)
                return;
            Console.Log("Checking for updates");
            if (Program.Releases == null || Program.Releases.Count == 0)
            {
                Console.Log("Couldn't find any releases");
                return;
            }
            MainWindow.SetPlayButton(true);
            UpdateFile[] updateFiles = Program.Releases[0].files;
            
            if (updateFiles == null)
                return;
            string lastPath;
            for (int i = 0; i < updateFiles.Length; i++)
            {
                
                lastPath = Program.vtolFolder + "/" + updateFiles[i].Location;
                if (!File.Exists(lastPath) || !Helper.CalculateMD5(lastPath).Equals(updateFiles[i].Hash))
                {
                    Console.Log($"Need to update {updateFiles[i].Location}");
                    if (updateFiles[i].Name.Equals("VTOLVR-ModLoader"))
                    {
                        _updateLauncher = true;
                        continue;
                    }
                    AddFile(updateFiles[i]);
                }
            }
            if (filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                if (_updateLauncher)
                    Notification.Show("The launcher needs to be updated.\nWould you like to do that now?", "Launcher Update", Notification.Buttons.NoYes, yesNoResultCallback: UpdateLauncherCallback);
                else
                    Console.Log("All fines are upto date");
                MainWindow.SetPlayButton(false);
            }
        }

        private static void AddFile(UpdateFile file)
        {
            Console.Log("Added " + file.Name);
            filesToUpdate.Enqueue(file);
        }
        private static void UpdateFiles()
        {
            currentFile = filesToUpdate.Dequeue();
            Console.Log($"Updating {currentFile.Name}");
            HttpHelper.DownloadFile(
                currentFile.Url,
                $"{Program.vtolFolder}/{currentFile.Location}.temp",
                DownloadProgress,
                DownloadDone);
        }
        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            MainWindow.SetProgress(e.ProgressPercentage, $"Downloading {currentFile.Name}");
        }
        private static void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                try
                {
                    if (File.Exists($"{Program.vtolFolder}/{currentFile.Location}"))
                        Helper.TryDelete($"{Program.vtolFolder}/{currentFile.Location}");
                }
                catch (Exception error)
                {
                    Console.Log($"Failed to delete {Program.vtolFolder}/{currentFile.Location}\n{error.Message}");
                    ClearUp();
                    return;
                }
                

                Helper.TryMove($"{Program.vtolFolder}/{currentFile.Location}.temp",
                    $"{Program.vtolFolder}/{currentFile.Location}");

                //Checking if we need to update dependiences in users mods
                string[] split = currentFile.Location.Split('/');
                if (!string.IsNullOrEmpty(Views.Settings.ProjectsFolder) &&
                    Directory.Exists(Views.Settings.ProjectsFolder + ProjectManager.modsFolder))
                {
                    DirectoryInfo folder = new DirectoryInfo(Views.Settings.ProjectsFolder + ProjectManager.modsFolder);
                    DirectoryInfo[] subFolders = folder.GetDirectories();
                    for (int i = 0; i < subFolders.Length; i++)
                    {
                        Console.Log($"Checking project {subFolders[i].Name}");
                        if (!Directory.Exists(Path.Combine(subFolders[i].FullName, "Dependencies")))
                            continue;

                        if (File.Exists(Path.Combine(subFolders[i].FullName, "Dependencies", split[split.Length - 1])))
                        {
                            Console.Log($"Moved {split[split.Length - 1]} to {subFolders[i].Name}");
                            Helper.TryDelete(Path.Combine(subFolders[i].FullName, "Dependencies", split[split.Length - 1]));
                            Helper.TryCopy($"{Program.vtolFolder}/{currentFile.Location}",
                                Path.Combine(subFolders[i].FullName, "Dependencies", split[split.Length - 1]));
                        }
                    }
                }
            }
            else
            {
                Console.Log($"Failed to download {currentFile.Name}\n{e.Error}");
                Notification.Show($"Failed to download {currentFile.Name}\n{e.Error.Message}", "Error downloading update");
            }
            ClearUp();
        }

        private static void ClearUp()
        {
            if (File.Exists($"{Program.vtolFolder}/{currentFile.Location}.temp"))
                File.Delete($"{Program.vtolFolder}/{currentFile.Location}.temp");

            if (filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                MainWindow.SetProgress(100, "Ready");
                MainWindow.SetPlayButton(false);
                if (_updateLauncher)
                    Notification.Show("The launcher needs to be updated.\nWould you like to do that now?", "Launcher Update", Notification.Buttons.NoYes, yesNoResultCallback: UpdateLauncherCallback);
            }
        }

        private static void UpdateLauncherCallback(bool result)
        {
            if (!result)
                return;

            if (!File.Exists(Path.Combine(Program.root, "Updater.exe")))
            {
                Notification.Show("Couldn't find the Updater.exe.", "Failed to Auto Update");
                return;
            }

            Process.Start(Path.Combine(Program.root, "Updater.exe"), Program.branch == string.Empty ? string.Empty : $"?branch={Program.branch}");
            Program.Quit("Updating Launcher.exe");
        }
    }
}
