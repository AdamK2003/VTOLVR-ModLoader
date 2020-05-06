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
        private readonly string githubURL = "https://api.github.com/repos/MarshMello0/VTOLVR-ModLoader/releases";
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
            if (main.CheckForInternet())
            {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)");
                client.DownloadStringCompleted += NewsDone;
                client.DownloadStringAsync(new Uri(githubURL));
            }
            else
            {
                //No internet
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
                    assetsArray = JArray.FromObject(jArray[i]["assets"]);
                    assets = new Asset[assetsArray.Count];
                    for (int j = 0; j < assetsArray.Count; j++)
                    {
                        assets[j] = new Asset(assetsArray[j]["name"].ToString(),assetsArray[j]["browser_download_url"].ToString());
                    }
                    updates[i] = new Updates(jArray[i]["name"].ToString(),
                                            jArray[i]["tag_name"].ToString(),
                                            jArray[i]["body"].ToString(),
                                            assets);
                }

                updateFeed.ItemsSource = updates.ToArray();
            }
            else
            {
                //Failed
                MessageBox.Show(e.Error.ToString());
            }
            
        }
    }
}
