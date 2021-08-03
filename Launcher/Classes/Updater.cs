using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using Launcher.Views;
using Launcher.Windows;
using Console = Launcher.Views.Console;

namespace Launcher.Classes
{
    static class Updater
    {
        private const string _oldLauncherName = "VTOLVR-ModLoader.old.exe";
        private static string _oldPath;
        private static bool _updatingLauncher;
        private static Action _onComplete;
        private static List<UpdateFile> _updateFiles = new ();
        private static UpdateFile[] _files;

        public static void CheckForUpdates(bool skipChecks = false, Action onComplete = null)
        {
            if (!skipChecks && !Views.Settings.AutoUpdate)
                return;
            _onComplete = onComplete;
            Console.Log("Checking for updates");
            if (Program.Releases == null || Program.Releases.Count == 0)
            {
                Console.Log("Couldn't find any releases");
                return;
            }

            MainWindow.SetPlayButton(true);
            _files = Program.Releases[0].Files;

            if (_files == null)
                return;
            int totalCount = 0;
            string lastPath;
            for (int i = 0; i < _files.Length; i++)
            {
                if (_files[i].Name.Equals("VTOLVR-ModLoader") &&
                    !Helper.CalculateMD5(Program.ExePath).Equals(_files[i].Hash))
                {
                    totalCount++;
                    continue;
                }
                lastPath = $"{Program.VTOLFolder}/{_files[i].Location}" ;
                if (!File.Exists(lastPath) || !Helper.CalculateMD5(lastPath).Equals(_files[i].Hash))
                {
                    totalCount++;
                }
            }

            string message = $"There are {totalCount} {(totalCount == 1 ? "file" : "files")} to update.\n" +
                             $"Would you like to update?";
            Notification.Show(message, "Update Available", Notification.Buttons.NoYes,
                yesNoResultCallback: UpdateCallback);
        }

        private static void UpdateCallback(bool result)
        {
            if (!result)
            {
                Console.Log("User Rejected Update");
                MainWindow.SetPlayButton(false);
                _onComplete?.Invoke();
                return;
            }
            
            string lastPath;
            for (int i = 0; i < _files.Length; i++)
            {
                if (_files[i].Name.Equals("VTOLVR-ModLoader"))
                {
                    if (!Helper.CalculateMD5(Program.ExePath).Equals(_files[i].Hash))
                    {
                        if (!MoveLauncher())
                            return;
                        _oldPath = Program.ExePath;
                        AddFile(_files[i]);
                    }
                    continue;
                }
                
                lastPath = Program.VTOLFolder + "/" + _files[i].Location;
                if (!File.Exists(lastPath) || !Helper.CalculateMD5(lastPath).Equals(_files[i].Hash))
                {
                    Console.Log($"Need to update {_files[i].Location}");

                    AddFile(_files[i]);
                }
            }

            if (_updateFiles.Count == 0)
            {
                Console.Log("All fines are up to date");
                MainWindow.SetPlayButton(false);
                _onComplete?.Invoke();
            }
        }

        private static void AddFile(UpdateFile file)
        {
            Console.Log("Updating " + file.Name);
            _updateFiles.Add(file);
            
            Downloads.DownloadFile(
                file.Url,
                $"{Program.VTOLFolder}/{file.Location}.temp",
                null,
                DownloadDone);
        }

        private static void DownloadDone(CustomWebClient.RequestData data)
        {
            if (!data.Cancelled && data.Error == null)
            {
                string oldFile = data.FilePath.Replace(".temp", string.Empty);

                if (File.Exists(oldFile))
                {
                    if (!Helper.TryDelete(oldFile))
                    {
                        Console.Log($"Failed to delete {oldFile}");
                        ClearUp(data);
                        return;
                    }
                }

                if (data.FilePath.Contains("VTOLVR-ModLoader.exe"))
                {
                    Helper.TryMove(data.FilePath,Program.ExePath);
                }
                else
                {
                    Helper.TryMove(data.FilePath,oldFile);
                }
                

                //Checking if we need to update dependencies in users mods
                FileInfo fileInfo = new(oldFile);
                string fileName = fileInfo.Name;
                
                if (!string.IsNullOrEmpty(Views.Settings.ProjectsFolder) &&
                    Directory.Exists(Views.Settings.ProjectsFolder + ProjectManager.modsFolder))
                {
                    DirectoryInfo folder = new (Views.Settings.ProjectsFolder + ProjectManager.modsFolder);
                    DirectoryInfo[] subFolders = folder.GetDirectories();
                    for (int i = 0; i < subFolders.Length; i++)
                    {
                        Console.Log($"Checking project {subFolders[i].Name}");
                        if (!Directory.Exists(Path.Combine(subFolders[i].FullName, "Dependencies")))
                            continue;

                        if (File.Exists(Path.Combine(subFolders[i].FullName, "Dependencies", fileName)))
                        {
                            Console.Log($"Moved {fileName} to {subFolders[i].Name}");
                            Helper.TryDelete(Path.Combine(subFolders[i].FullName, "Dependencies",
                                fileName));
                            Helper.TryCopy(oldFile,
                                Path.Combine(subFolders[i].FullName, "Dependencies", fileName));
                        }
                    }
                }
            }
            else
            {
                Console.Log($"Failed to download {data.FilePath}\n{data.Error}");
                Notification.Show($"Failed to download {data.FilePath}\n{data.Error.Message}",
                    "Error downloading update");
            }

            ClearUp(data);
        }

        private static void ClearUp(CustomWebClient.RequestData data)
        {
            if (File.Exists(data.FilePath))
                File.Delete(data.FilePath);

            for (int i = 0; i < _updateFiles.Count; i++)
            {
                if (data.FilePath.Contains(_updateFiles[i].Location))
                {
                    _updateFiles.RemoveAt(i);
                    Console.Log($"Removing one");
                    break;
                }
            }
            Console.Log($"Count is {_updateFiles.Count}");
            if (_updateFiles.Count == 0)
            {
                MainWindow.SetProgress(100, "Ready");
                MainWindow.SetPlayButton(false);
                _onComplete?.Invoke();
                if (_updatingLauncher)
                    ReLaunch();
            }
        }
        
        private static bool MoveLauncher()
        {
            FileInfo currentPath = new(Program.ExePath);
            DirectoryInfo folder = currentPath.Directory;
            
            string oldPath = Path.Combine(folder.FullName, _oldLauncherName);
            if (File.Exists(oldPath) && !Helper.TryDelete(oldPath))
            {
                Console.Log($"Failed to delete the old exe");
                Notification.Show($"Failed to delete {oldPath}", "Failed to Auto Update");
                return false;
            }
            
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