using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    public partial class News : UserControl
    {
        private readonly string githubURL = "https://gitlab.com/api/v4/projects/17323170/releases";
        private MainWindow main;
        public News()
        {
            InitializeComponent();
            main = MainWindow._instance;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public void LoadNews()
        {
            if (false) //Blocked till the new API is set up (main.CheckForInternet())
            {
                Console.Log("Connecting to API for latest releases");
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)");
                client.DownloadStringCompleted += NewsDone;
                client.DownloadStringAsync(new Uri(githubURL));
            }
            else
            {
                NoInternet();
            }
        }
        private void NewsDone(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                JArray jArray = JArray.Parse(e.Result);
                JArray assetsArray;
                Asset[] assets;
                Updates[] updates = new Updates[jArray.Count];

                for (int i = 0; i < jArray.Count; i++)
                {
                    assetsArray = JArray.FromObject(jArray[i]["assets"]["sources"]);
                    assets = new Asset[assetsArray.Count];
                    for (int j = 0; j < assetsArray.Count; j++)
                    {
                        assets[j] = new Asset(assetsArray[j]["name"].ToString(),assetsArray[j]["url"].ToString());
                    }
                    updates[i] = new Updates(jArray[i]["name"].ToString(),
                                            jArray[i]["tag_name"].ToString(),
                                            jArray[i]["description"].ToString(),
                                            assets);
                }

                updateFeed.ItemsSource = updates.ToArray();
                Console.Log("Got latest releases");
            }
            else
            {
                //Failed
                MessageBox.Show(e.Error.ToString());
                NoInternet();
            }
            
        }

        private void NoInternet()
        {
            Updates[] updates = new Updates[1];
            updates[0] = new Updates("No Internet Connection",
                                    "",
                                    "Please connect to the internet to see the latest releases",
                                            null);
            updateFeed.ItemsSource = updates.ToArray();
            Console.Log("Can't connect to internet");
        }
    }
}
