using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Console = VTOLVR_ModLoader.Views.Console;

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
                using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Read)) 
                {
                    List<ZipArchiveEntry> filesInZip = zip.Entries.ToList();
                    for (int f = 0; f < filesInZip.Count; f++)
                    {
                        if (!filesInZip[f].FullName.EndsWith("\\"))
                        {
                            if (filesInZip[f].Name.Length == 0)
                            {
                                //This is just a folder
                                Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName));
                                continue;
                            }
                            //This is a file
                            Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName.Replace(filesInZip[f].Name, string.Empty)));
                            filesInZip[f].ExtractToFile(Path.Combine(extractPath, filesInZip[f].FullName), File.Exists(Path.Combine(extractPath, filesInZip[f].FullName)));
                        }
                        else if (!Directory.Exists(Path.Combine(extractPath, filesInZip[f].FullName)))
                            Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName));
                    }
                }
            });
            completed?.Invoke(zipPath, extractPath);
            //using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            //{
            //    zip.
            //    await Task.WhenAll(zip.Entries.Select(file => Task.Run(() =>
            //    {
            //        if (!file.FullName.EndsWith("\\"))
            //        {
            //            if (file.Name.Length == 0)
            //            {
            //                //This is just a folder
            //                Directory.CreateDirectory(Path.Combine(extractPath, file.FullName));
            //            }
            //            else
            //            {
            //                //This is a file
            //                Directory.CreateDirectory(Path.Combine(extractPath, file.FullName.Replace(file.Name, string.Empty)));
            //                System.Console.WriteLine(file.Name);
            //                file.ExtractToFile(Path.Combine(extractPath, file.FullName), File.Exists(Path.Combine(extractPath, file.FullName)));
            //            }

            //        }
            //        else if (!Directory.Exists(Path.Combine(extractPath, file.FullName)))
            //            Directory.CreateDirectory(Path.Combine(extractPath, file.FullName));
            //    })));

            //    
            //    //List<ZipArchiveEntry> filesInZip = zip.Entries.ToList();
            //    //for (int f = 0; f < filesInZip.Count; f++)
            //    //{
            //    //    if (!filesInZip[f].FullName.EndsWith("\\"))
            //    //    {
            //    //        if (filesInZip[f].Name.Length == 0)
            //    //        {
            //    //            //This is just a folder
            //    //            Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName));
            //    //            continue;
            //    //        }
            //    //        //This is a file
            //    //        Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName.Replace(filesInZip[f].Name, string.Empty)));
            //    //        filesInZip[f].ExtractToFile(Path.Combine(extractPath, filesInZip[f].FullName), File.Exists(Path.Combine(extractPath, filesInZip[f].FullName)));
            //    //    }
            //    //    else if (!Directory.Exists(Path.Combine(extractPath, filesInZip[f].FullName)))
            //    //        Directory.CreateDirectory(Path.Combine(extractPath, filesInZip[f].FullName));
            //    //}
            //}
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
    }
}