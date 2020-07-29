using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Updater.Classes;
using Valve.Newtonsoft.Json.Linq;

namespace Updater
{
    static class Program
    {
        public const string ProgramNameBase = "VTOL VR Mod Loader Updater";
        public const string LogPath = @"\updater_log.txt";
        public const string apiURL = "/api";
        private const string releasesURL = "/releases";

        public static string Root;
        public static string VtolFolder;
        public static string ProgramName;
        public static bool disableInternet;
        public static string url = @"https://vtolvr-mods.com";
        public static string branch = string.Empty;
        public static List<Release> Releases { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
        public static void Start()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            ProgramName = $"{ProgramNameBase} {version.Major}.{version.Minor}.{version.Build} {(debug ? "[Development Mode]" : string.Empty)}";
            MainWindow.Instance.Title = ProgramName;
            Root = Directory.GetCurrentDirectory();
            VtolFolder = Root.Replace("VTOLVR_ModLoader", "");
            Console.Log($"Root = {Root}\nVtolFolder = {VtolFolder}");

            if (File.Exists(Root + LogPath))
                File.Delete(Root + LogPath);

            CommunicationsManager.CheckCustomBranch();
            CommunicationsManager.CheckCustomURL();
            CommunicationsManager.CheckNoInternet();

            GetReleases();
        }
        private async static void GetReleases()
        {
            Console.Log("Getting Releases");
            if (!await HttpHelper.CheckForInternet())
                return;

            Console.Log($"Connecting to API for latest releases");
            HttpHelper.DownloadStringAsync(
                url + apiURL + releasesURL + "/" + (branch == string.Empty ? string.Empty : $"?branch={branch}"),
                NewsDone);
        }
        private static async void NewsDone(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                Releases = new List<Release>();
                ConvertUpdates(await response.Content.ReadAsStringAsync());
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + response.StatusCode);
            }
        }
        private static void ConvertUpdates(string jsonString)
        {
            JArray results = JArray.Parse(jsonString);
            Release lastUpdate;
            JArray lastFilesJson;
            List<UpdateFile> files;
            for (int i = 0; i < results.Count; i++)
            {
                lastUpdate = new Release(results[i]["name"].ToString(),
                    results[i]["tag_name"].ToString(),
                    results[i]["body"].ToString());
                if (results[i]["files"] != null)
                {
                    lastFilesJson = JArray.FromObject(results[i]["files"]);
                    files = new List<UpdateFile>(lastFilesJson.Count);
                    for (int j = 0; j < lastFilesJson.Count; j++)
                    {
                        files.Add(new UpdateFile(
                            lastFilesJson[j]["file_name"].ToString(),
                            lastFilesJson[j]["file_hash"].ToString(),
                            lastFilesJson[j]["file_location"].ToString(),
                            lastFilesJson[j]["file"].ToString()));
                    }
                    lastUpdate.SetFiles(files.ToArray());
                }
                Releases.Add(lastUpdate);
            }
            Updater.CheckForUpdates();
        }

        public static void SetProgress(int progress, string text)
        {
            Console.Log($"{text} {progress}%");
            MainWindow.Instance.progress.Value = progress;
            MainWindow.Instance.progressText.Text = text;
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
        public static void Quit()
        {
            Application.Current.Shutdown();
        }
    }

    static class Console
    {
        private static DateTime Now;
        public static void Log(string message)
        {
            Now = DateTime.Now;
            File.AppendAllText(Program.Root + Program.LogPath,
                $"[{Now.Day}/{Now.Month}/{Now.Year} {Now.Hour}:{Now.Minute}:{Now.Second}] {message}\n");
        }
    }
}
