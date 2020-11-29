using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using Sentry;
using Sentry.Protocol;
using VTOLVR_ModLoader.Views;
using VTOLVR_ModLoader.Windows;
using Console = VTOLVR_ModLoader.Views.Console;
using Settings = VTOLVR_ModLoader.Views.Settings;

namespace VTOLVR_ModLoader.Classes
{
    public static class Helper
    {
        public static string ClearSpaces(string input)
        {
            return Regex.Replace(input, @"\s+", "");
        }
        public static async void ExtractZipToDirectory(string zipPath, string extractPath, Action<string, string, string> completed)
        {
            string result = await Task.Run(() =>
            {
                try
                {
                    // This is mostly just the example code from here:
                    // https://github.com/icsharpcode/SharpZipLib/wiki/Unpack-a-Zip-with-full-control-over-the-operation#c
                    using (ZipFile zip = new ZipFile(zipPath))
                    {
                        foreach (ZipEntry zipEntry in zip)
                        {
                            if (!zipEntry.IsFile)
                                continue;
                            string entryFileName = zipEntry.Name;

                            string fullZipToPath = Path.Combine(extractPath, entryFileName);
                            string directoryName = Path.GetDirectoryName(fullZipToPath);
                            if (directoryName.Length > 0)
                            {
                                Directory.CreateDirectory(directoryName);
                            }

                            // 4K is optimum
                            var buffer = new byte[4096];

                            // Unzip file in buffered chunks. This is just as fast as unpacking
                            // to a buffer the full size of the file, but does not waste memory.
                            // The "using" will close the stream even if an exception occurs.
                            using (var zipStream = zip.GetInputStream(zipEntry))
                            using (Stream fsOutput = File.Create(fullZipToPath))
                            {
                                StreamUtils.Copy(zipStream, fsOutput, buffer);
                            }
                        }
                    }
                    return "Success";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            });
            completed?.Invoke(zipPath, extractPath, result);
        }
        public static string CalculateMD5(string filename)
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
        public static bool TryCopy(string sourceFileName, string destFileName)
        {
            try
            {
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Console.Log($"Failed to move file: {sourceFileName}\n{e}");
                return false;
            }
            return true;
        }
        public static bool TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Console.Log($"Failed to delete: {filePath}\n{e}");
                return false;
            }
            return true;
        }
        public static bool TryMove(string sourceFileName, string destFileName)
        {
            try
            {
                File.Move(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Console.Log($"Failed to move: {sourceFileName} to {destFileName}\n{e}");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Finds the mods which teh user has downloaded from the website
        /// </summary>
        /// <returns></returns>
        public static List<BaseItem> FindDownloadMods()
        {
            List<BaseItem> foundMods = new List<BaseItem>();
            DirectoryInfo downloadedMods = new DirectoryInfo(Program.root + Program.modsFolder);
            DirectoryInfo[] mods = downloadedMods.GetDirectories();

            JObject json;
            for (int i = 0; i < mods.Length; i++)
            {
                if (!File.Exists(Path.Combine(mods[i].FullName, "info.json")))
                {
                    Console.Log($"Mod: {mods[i].Name} doesn't have a info.json file");
                    continue;
                }

                json = JObject.Parse(File.ReadAllText(Path.Combine(mods[i].FullName, "info.json")));
                if (json[ProjectManager.jDll] == null)
                {
                    Console.Log($"Mod: Couldn't find {ProjectManager.jDll} in {Path.Combine(mods[i].FullName, "info.json")}");
                    continue;
                }
                if (json[ProjectManager.jName] == null)
                {
                    Console.Log($"Mod: Couldn't find {ProjectManager.jName} in {Path.Combine(mods[i].FullName, "info.json")}");
                    continue;
                }
                foundMods.Add(new BaseItem(json[ProjectManager.jName].ToString(), mods[i], json));
            }
            return foundMods;
        }
        /// <summary>
        /// Finds the skins which the user has downloaded from the website
        /// </summary>
        /// <returns></returns>
        public static List<BaseItem> FindDownloadedSkins()
        {
            List<BaseItem> foundSkins = new List<BaseItem>();
            DirectoryInfo downloadedSkins = new DirectoryInfo(Program.root + Program.skinsFolder);
            DirectoryInfo[] skins = downloadedSkins.GetDirectories();

            JObject json;
            for (int i = 0; i < skins.Length; i++)
            {
                if (!File.Exists(Path.Combine(skins[i].FullName, "info.json")))
                {
                    Console.Log($"Skin: {skins[i].Name} doesn't have a info.json file");
                    continue;
                }

                json = JObject.Parse(File.ReadAllText(Path.Combine(skins[i].FullName, "info.json")));
                if (json[ProjectManager.jName] == null)
                {
                    Console.Log($"Skin: Couldn't find {ProjectManager.jName} in {Path.Combine(skins[i].FullName, "info.json")}");
                    continue;
                }
                foundSkins.Add(new BaseItem(json[ProjectManager.jName].ToString(), skins[i], json));
            }

            return foundSkins;
        }
        /// <summary>
        /// Finds the mods which the user has created in the project manager
        /// </summary>
        /// <returns></returns>
        public static List<BaseItem> FindUsersMods()
        {
            return null;
        }
        /// <summary>
        /// Finds the skins which the user has created in the project manager
        /// </summary>
        /// <returns></returns>
        public static List<BaseItem> FindUsersSkins()
        {
            return null;
        }
        public enum SentryLogCategory { Console, DevToos, EditProject, Manager, NewProject, News, NewVersion, ProjectManager, Settings, MainWindow, Program, Startup, CommunicationsManager, Helper }
        public static void SentryLog(string message, SentryLogCategory category)
        {
            SentrySdk.AddBreadcrumb(
                message: message,
                category: category.ToString(),
                level: BreadcrumbLevel.Info);
        }

        public static JArray JArrayTryParse(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JArray.Parse(content);
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }
        public static JObject JObjectTryParse(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JObject.Parse(content);
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }
        public static void DeleteDirectory(string path, out Exception exception)
        {
            try
            {
                Directory.Delete(path, true);
                exception = null;
            }
            catch (Exception e)
            {
                exception = e;
            }
        }
        public static void CreateDiagnosticsZip()
        {
            SentryLog("Creating Diagnostics Zip", SentryLogCategory.Helper);

            string datetime = DateTime.Now.ToString().Replace('/', '-').Replace(':', '-');
            Directory.CreateDirectory(Path.Combine(Program.root, datetime));

            Console.Log("Copying Game Log");
            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(PlayerLogPath());
            }
            catch (Exception e)
            {
                Console.Log("Can't read player log because the game is open");
                Notification.Show("Please close the game before creating a diagnostics zip.", "Error");
                Directory.Delete(Path.Combine(Program.root, datetime));
                return;
            }
            string[] shortLines = ShortenPlayerLog(lines);
            File.WriteAllLines(Path.Combine(Program.root, datetime, "Player.log"), shortLines);

            Console.Log("Copying Mod Loader Log");
            File.Copy(
                Path.Combine(Program.root, Program.LogName),
                Path.Combine(Program.root, datetime, Program.LogName));

            Console.Log("Gathering Extra Info");
            StringBuilder infoBuilder = new StringBuilder("# Created: " + DateTime.Now.ToString());
            infoBuilder.AppendLine();
            infoBuilder.AppendLine($"# Version: {Program.ProgramName}");
            GatherExtraInfo(ref infoBuilder);
            File.WriteAllText(Path.Combine(Program.root, datetime, "Info.txt"), infoBuilder.ToString());

            Console.Log("Zipping up content");
            string zipName = $"DiagnosticsZip [{datetime}].zip";
            FastZip zip = new FastZip();
            zip.CreateZip(zipName, Path.Combine(Program.root, datetime), false, null);

            Directory.Delete(Path.Combine(Program.root, datetime), true);
            Process.Start("explorer.exe", string.Format("/select,\"{0}\\{1}\"", Program.root, zipName));
        }
        private static string PlayerLogPath()
        {
            // This is a massive pain because it's stored in LocalLow but there is no special folder
            // for LocalLow

            DirectoryInfo roaming = new DirectoryInfo(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            DirectoryInfo appData = roaming.Parent;
            DirectoryInfo[] folders = appData.GetDirectories();
            DirectoryInfo localLow = null;
            for (int i = 0; i < folders.Length; i++)
            {
                if (folders[i].Name == "LocalLow")
                {
                    localLow = folders[i];
                    break;
                }
            }

            if (localLow == null)
            {
                Console.Log("For some reason the locallow path wasn't found");
                return string.Empty;
            }

            DirectoryInfo vtolvr = new DirectoryInfo(Path.Combine(
                localLow.FullName, "Boundless Dynamics, LLC", "VTOLVR"));
            if (!vtolvr.Exists)
            {
                Console.Log("VTOL VR folder is missing in LocalLow. Have you launched the game up before?");
                return string.Empty;
            }

            FileInfo playerLog = new FileInfo(Path.Combine(vtolvr.FullName, "Player.log"));
            if (!playerLog.Exists)
            {
                Console.Log("Player log is missing from folder");
                return string.Empty;
            }

            return playerLog.FullName;

        }
        public static string[] ShortenPlayerLog(string[] linesArray)
        {
            List<string> lines = linesArray.ToList();
            lines.RemoveAll(line => string.IsNullOrWhiteSpace(line) | line.StartsWith("(Filename:"));
            return lines.ToArray();
        }
        private static void GatherExtraInfo(ref StringBuilder builder)
        {

            builder.AppendLine();
            DirectoryInfo[] modFolders = new DirectoryInfo(Program.root + Program.modsFolder).GetDirectories();
            builder.AppendLine($"## Downloaded Mods ({modFolders.Length})");
            for (int i = 0; i < modFolders.Length; i++)
            {
                builder.AppendLine($"- {modFolders[i].Name}");
            }

            builder.AppendLine();
            DirectoryInfo[] skinFolders = new DirectoryInfo(Program.root + Program.skinsFolder).GetDirectories();
            builder.AppendLine($"## Downloaded Skins ({skinFolders.Length})");
            for (int i = 0; i < skinFolders.Length; i++)
            {
                builder.AppendLine($"- {skinFolders[i].Name}");
            }

            builder.AppendLine();
            builder.AppendLine("## User Settings");
            builder.AppendLine($"Launch SteamVR: {Views.Settings.SteamVR}");
            builder.AppendLine($"Auto Update: {Views.Settings.AutoUpdate}");
            builder.AppendLine($"Projects Folder: {Views.Settings.ProjectsFolder}");
            builder.AppendLine($"Token Valid: {Views.Settings.tokenValid}");

            builder.AppendLine();
            builder.AppendLine("## Mod Loader Folder Files");
            FileInfo[] files = new DirectoryInfo(Program.root).GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                builder.AppendLine($"/{files[i].Name} (MD5: {CalculateMD5(files[i].Name)})");
            }
        }
    }
}