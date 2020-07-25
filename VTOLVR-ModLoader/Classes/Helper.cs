using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
                    filesInZip[f].ExtractToFile(Path.Combine(extractPath, filesInZip[f].FullName), File.Exists(Path.Combine(extractPath, filesInZip[f].FullName)));
                }
            }
        }
    }
}