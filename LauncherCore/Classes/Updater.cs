using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using LauncherCore.Views;
using LauncherCore.Windows;
using Console = LauncherCore.Views.Console;

namespace LauncherCore.Classes
{
    static class Updater
    {
        private const string _oldLauncherName = "VTOLVR-ModLoader.old.exe";
        private static Queue<UpdateFile> _filesToUpdate = new Queue<UpdateFile>();
        private static UpdateFile _currentFile;
        private static string _oldPath;
        private static bool _updatingLauncher;

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
            UpdateFile[] updateFiles = Program.Releases[0].Files;

            if (updateFiles == null)
                return;
            string lastPath;
            for (int i = 0; i < updateFiles.Length; i++)
            {
                lastPath = Program.VTOLFolder + "/" + updateFiles[i].Location;
                if (!File.Exists(lastPath) || !Helper.CalculateMD5(lastPath).Equals(updateFiles[i].Hash))
                {
                    Console.Log($"Need to update {updateFiles[i].Location}");
                    if (updateFiles[i].Name.Equals("VTOLVR-ModLoader"))
                    {
                        if (!MoveLauncher())
                            return;
                    }

                    AddFile(updateFiles[i]);
                }
            }

            if (_filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                Console.Log("All fines are up to date");
                MainWindow.SetPlayButton(false);
            }
        }

        private static void AddFile(UpdateFile file)
        {
            Console.Log("Added " + file.Name);
            _filesToUpdate.Enqueue(file);
        }

        private static void UpdateFiles()
        {
            _currentFile = _filesToUpdate.Dequeue();
            Console.Log($"Updating {_currentFile.Name}");
            HttpHelper.DownloadFile(
                _currentFile.Url,
                $"{Program.VTOLFolder}/{_currentFile.Location}.temp",
                DownloadProgress,
                DownloadDone);
        }

        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            MainWindow.SetProgress(e.ProgressPercentage, $"Downloading {_currentFile.Name}");
        }

        private static void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                try
                {
                    if (File.Exists($"{Program.VTOLFolder}/{_currentFile.Location}"))
                        Helper.TryDelete($"{Program.VTOLFolder}/{_currentFile.Location}");
                }
                catch (Exception error)
                {
                    Console.Log($"Failed to delete {Program.VTOLFolder}/{_currentFile.Location}\n{error.Message}");
                    ClearUp();
                    return;
                }


                Helper.TryMove($"{Program.VTOLFolder}/{_currentFile.Location}.temp",
                    $"{Program.VTOLFolder}/{_currentFile.Location}");

                //Checking if we need to update dependencies in users mods
                string[] split = _currentFile.Location.Split('/');
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
                            Helper.TryDelete(Path.Combine(subFolders[i].FullName, "Dependencies",
                                split[split.Length - 1]));
                            Helper.TryCopy($"{Program.VTOLFolder}/{_currentFile.Location}",
                                Path.Combine(subFolders[i].FullName, "Dependencies", split[split.Length - 1]));
                        }
                    }
                }
            }
            else
            {
                Console.Log($"Failed to download {_currentFile.Name}\n{e.Error}");
                Notification.Show($"Failed to download {_currentFile.Name}\n{e.Error.Message}",
                    "Error downloading update");
            }

            ClearUp();
        }

        private static void ClearUp()
        {
            if (File.Exists($"{Program.VTOLFolder}/{_currentFile.Location}.temp"))
                File.Delete($"{Program.VTOLFolder}/{_currentFile.Location}.temp");

            if (_filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                MainWindow.SetProgress(100, "Ready");
                MainWindow.SetPlayButton(false);
                
                if (_updatingLauncher)
                    ReLaunch();
            }
        }

        private static bool MoveLauncher()
        {
            string oldPath = Path.Combine(Program.Root, _oldLauncherName);
            if (File.Exists(oldPath))
            {
                if (!Helper.TryDelete(oldPath))
                {
                    Console.Log($"Failed to delete the old exe");
                    Notification.Show($"Failed to delete {oldPath}", "Failed to Auto Update");
                    return false;
                }
            }

            _oldPath = Program.ExePath;
            File.Move(Program.ExePath, oldPath);
            Console.Log($"Moved our exe to {oldPath}");
            _updatingLauncher = true;
            return true;
        }

        private static void ReLaunch()
        {
            Process.Start(_oldPath);
            Program.Quit("Updated");
        }
        
        
    }
}