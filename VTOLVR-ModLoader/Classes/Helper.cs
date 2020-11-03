using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using Sentry;
using Sentry.Protocol;
using VTOLVR_ModLoader.Views;
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
        public static async void ExtractZipToDirectory(string zipPath, string extractPath, Action<string, string> completed)
        {
            await Task.Run(() =>
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
            });
            completed?.Invoke(zipPath, extractPath);
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
        public enum SentryLogCategory { Console, DevToos, EditProject, Manager, NewProject, News, NewVersion, ProjectManager, Settings, MainWindow, Program, Startup }
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
    }
}