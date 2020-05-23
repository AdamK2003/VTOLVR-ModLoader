using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        private const string userURL = "/get-token";
        private const string savePath = @"\settings.json";
        private string token;
        public static bool tokenValid = false;
        private bool hideResult;
        public Settings()
        {
            InitializeComponent();
            LoadSettings();
            if (CommunicationsManager.CheckArgs("vtolvrml", out string line))
            {
                if (!line.Contains("token"))
                    TestToken(true);
            }
            else
                TestToken(true);

        }
        public void SetUserToken(string token)
        {
            this.token = token;
            tokenBox.Password = token;
            SaveSettings();
            TestToken();
        }

        private void UpdateToken(object sender, RoutedEventArgs e)
        {
            SetUserToken(tokenBox.Password);
        }

        public void TestToken(bool hideResult = false)
        {
            this.hideResult = hideResult;
            if (Program.CheckForInternet())
            {
                updateButton.IsEnabled = false;
                tokenValid = false;
                Console.Log("Testing new token");
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "VTOL VR Mod Loader");
                client.Headers.Add("Authorization", "Token " + token);
                client.DownloadStringCompleted += TestTokenDone;
                client.DownloadStringAsync(new Uri(Program.url + Program.apiURL + userURL + Program.jsonFormat));
            }
            else
            {
                NoInternet();
            }
        }

        private void TestTokenDone(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                if (!hideResult)
                    MessageBox.Show("Token was successful!");
                tokenValid = true;

            }
            else
            {
                tokenValid = false;
                if (!hideResult)
                    MessageBox.Show(e.Error.Message);
                Console.Log("Error:\n" + e.Error.Message);
            }
            updateButton.IsEnabled = true;
            MainWindow._instance.uploadModButton.IsEnabled = tokenValid;
        }

        private void NoInternet()
        {
            updateButton.IsEnabled = false;
        }

        

        private void SaveSettings()
        {
            JObject jObject = new JObject();
            if (!string.IsNullOrEmpty(token))
                jObject.Add("token", token);

            try
            {
                File.WriteAllText(Program.root + savePath, jObject.ToString());
            }
            catch (Exception e)
            {
                Console.Log($"Failed to save {savePath}");
                Console.Log(e.Message);
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(Program.root + savePath))
                return;

            JObject json;
            try
            {
                json = JObject.Parse(File.ReadAllText(Program.root + savePath));
            }
            catch (Exception e)
            {
                Console.Log($"Failed to read {savePath}");
                Console.Log(e.Message);
                return;
            }

            if (json["token"] != null)
            {
                token = json["token"].ToString();
                tokenBox.Password = token;
            }
        }
    }
}
