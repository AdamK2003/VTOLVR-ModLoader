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
using AutoUpdater.Classes;

namespace AutoUpdater
{
    static class Updater
    {
        private static Queue<UpdateFile> filesToUpdate = new Queue<UpdateFile>();
        private static UpdateFile currentFile;
        public static void CheckForUpdates()
        {
            Console.Log("Checking for updates");
            if (Program.Releases == null || Program.Releases.Count == 0)
            {
                Console.Log("Couldn't find any releases");
                if (Program.Releases == null)
                    Console.Log("Releases == null");
                else
                    Console.Log("Releases.Count == 0");
                return;
            }
            UpdateFile[] updateFiles = Program.Releases[0].files;

            if (updateFiles == null)
                return;
            string lastPath;
            for (int i = 0; i < updateFiles.Length; i++)
            {
                if (updateFiles[i].Name.Equals("Updater"))
                    continue;
                lastPath = Program.VtolFolder + "/" + updateFiles[i].Location;
                if (!File.Exists(lastPath) || !Program.CalculateMD5(lastPath).Equals(updateFiles[i].Hash))
                {
                    Console.Log($"Need to update {updateFiles[i].Location}");
                    AddFile(updateFiles[i]);
                }
            }
            if (filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                Console.Log("All files are up to date");
                Process.Start(Program.Root + "/VTOLVR-ModLoader.exe");
                Program.Quit();
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
            HttpHelper.DownloadFile(
                currentFile.Url,
                $"{Program.VtolFolder}/{currentFile.Location}.temp",
                DownloadProgress,
                DownloadDone);
        }
        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Program.SetProgress(e.ProgressPercentage, $"Downloading {currentFile.Name}");
        }
        private static void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                if (File.Exists($"{Program.VtolFolder}/{currentFile.Location}"))
                    File.Delete($"{Program.VtolFolder}/{currentFile.Location}");

                File.Move($"{Program.VtolFolder}/{currentFile.Location}.temp",
                    $"{Program.VtolFolder}/{currentFile.Location}");
            }
            else
            {
                Console.Log($"Failed to download {currentFile.Name}\n{e.Error}");
            }

            if (File.Exists($"{Program.VtolFolder}/{currentFile.Location}.temp"))
                File.Delete($"{Program.VtolFolder}/{currentFile.Location}.temp");

            if (filesToUpdate.Count > 0)
                UpdateFiles();
            else
            {
                Console.Log("Finished Updating!");
                Process.Start(Program.Root + "/VTOLVR-ModLoader.exe");
                Program.Quit();
            }

        }
    }
}
