using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    public partial class News : UserControl
    {
        private const string modLoaderURL = "/releases";
        private MainWindow main;
        private List<Update> updates = new List<Update>();
        public News()
        {
            InitializeComponent();
            main = MainWindow._instance;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public async void LoadNews()
        {
            if (await HttpHelper.CheckForInternet())
            {
                Console.Log($"Connecting to API for latest releases");
                HttpHelper.DownloadStringAsync(
                    Program.url + Program.apiURL + modLoaderURL + Program.jsonFormat,
                    NewsDone);
            }
            else
            {
                NoInternet();
            }
        }
        private async void NewsDone(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                ConvertUpdates(await response.Content.ReadAsStringAsync());
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + response.StatusCode);
                NoInternet();
            }
            MainWindow._instance.settings.TestToken(true);
        }

        private void ConvertUpdates(string jsonString)
        {
            JArray results = JArray.Parse(jsonString);
            Update lastUpdate;
            JArray lastFilesJson;
            List<UpdateFile> files;
            for (int i = 0; i < results.Count; i++)
            {
                lastUpdate = new Update(results[i]["name"].ToString(),
                    results[i]["tag_name"].ToString(),
                    results[i]["body"].ToString());
                Console.Log(lastUpdate.name);
                if (results[i]["files"]  != null)
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
                updates.Add(lastUpdate);                
            }
            updateFeed.ItemsSource = updates.ToArray();
        }

        private void NoInternet()
        {
            updateFeed.ItemsSource = new Update[1] { new Update() };
            Console.Log("Can't connect to internet");
        }
    }
}
