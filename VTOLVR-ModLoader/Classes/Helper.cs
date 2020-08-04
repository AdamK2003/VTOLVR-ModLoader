using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace VTOLVR_ModLoader.Classes
{
    public static class Helper
    {
        public static string ClearSpaces(string input)
        {
            return Regex.Replace(input, @"\s+", "");
        }
        public static void ExtractZipToDirectory(string zipPath, string extractPath)
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
    }
}