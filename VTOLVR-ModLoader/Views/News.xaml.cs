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
        private const string modLoaderURL = "/modloader";
        private MainWindow main;
        private List<Updates> updates = new List<Updates>();
        public News()
        {
            InitializeComponent();
            main = MainWindow._instance;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public async void LoadNews(int page)
        {
            if (await HttpHelper.CheckForInternet())
            {
                Console.Log($"Connecting to API for latest releases (Page {page})");
                await HttpHelper.DownloadStringAsync(
                    Program.url + Program.apiURL + modLoaderURL + Program.jsonFormat + (page == 0 ? "" : Program.pageFormat + page),
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
        private void NewsDone(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                ConvertUpdates(e.Result);
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + e.Error.ToString());
                NoInternet();
            } 
        }

        private void ConvertUpdates(string jsonString)
        {
            JObject json = JObject.Parse(jsonString);
            JArray results = JArray.FromObject(json["results"]);
            for (int i = 0; i < results.Count; i++)
            {
                updates.Add(new Updates(results[i]["name"].ToString(),
                    results[i]["tag_name"].ToString(),
                    results[i]["body"].ToString(),
                    results[i]["installer_file"].ToString(),
                    results[i]["zip_file"].ToString(),
                    int.Parse(results[i]["download_count"].ToString())));
            }

            if (json["next"].ToString() != "")
            {
                string url = json["next"].ToString();
                string pageNum = url.Replace(Program.url + Program.apiURL + modLoaderURL + Program.jsonFormat + Program.pageFormat, "");
                Console.Log($"Getting next page of releases ({pageNum})");
                LoadNews(int.Parse(pageNum));
            }
            else
            {
                Console.Log("Collected all pages of releases");
                updateFeed.ItemsSource = updates.ToArray();
            }
        }

        private void NoInternet()
        {
            Updates[] updates = new Updates[1];
            updates[0] = new Updates("No Internet Connection",
                                    string.Empty,
                                    "Please connect to the internet to see the latest releases",
                                    string.Empty,
                                    string.Empty,
                                    0);
            updateFeed.ItemsSource = updates.ToArray();
            Console.Log("Can't connect to internet");
        }
    }
}
