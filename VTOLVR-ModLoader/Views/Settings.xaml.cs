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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using UserControl = System.Windows.Controls.UserControl;
using VTOLVR_ModLoader.Windows;
using VTOLVR_ModLoader.Classes;
using System.Net.Http;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        private const string userURL = "/get-token";
        private const string savePath = @"\settings.json";
        public static bool tokenValid = false;
        private bool hideResult;
        private Action<bool, string> callBack;

        //Settings
        public static string Token { get; private set; }
        public static string projectsFolder { get; private set; }
        public Settings()
        {
            callBack += SetProjectsFolder;
            InitializeComponent();
            LoadSettings();
            if (CommunicationsManager.CheckArgs("vtolvrml", out string line))
            {
                if (!line.Contains("token"))
                    TestToken(true);
            }

        }
        public void SetUserToken(string token)
        {
            Token = token;
            tokenBox.Password = token;
            SaveSettings();
            TestToken();
        }

        private void UpdateToken(object sender, RoutedEventArgs e)
        {
            SetUserToken(tokenBox.Password);
        }

        public async void TestToken(bool hideResult = false)
        {
            this.hideResult = hideResult;
            if (await HttpHelper.CheckForInternet())
            {
                updateButton.IsEnabled = false;
                tokenValid = false;
                Console.Log("Testing new token");
                HttpHelper.DownloadStringAsync(
                    Program.url + Program.apiURL + userURL + Program.jsonFormat,
                    TestTokenDone,
                    Token);         
            }
            else
            {
                NoInternet();
            }
        }
        private void TestTokenDone(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (!hideResult)
                    Notification.Show("Token was successful!");
                tokenValid = true;
                Console.Log("Token is valid");
            }
            else
            {
                tokenValid = false;
                if (!hideResult)
                    Notification.Show(response.StatusCode.ToString(), "Token Failed");
                Console.Log("Token Failed:\n" + response.StatusCode.ToString());
            }
            updateButton.IsEnabled = true;
        }
        private void NoInternet()
        {
            updateButton.IsEnabled = false;
        }
        private void SaveSettings()
        {
            JObject jObject = new JObject();
            if (!string.IsNullOrEmpty(Token))
                jObject.Add("token", Token);

            if (!string.IsNullOrEmpty(projectsFolder))
                jObject.Add("projectsFolder", projectsFolder);

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
                Token = json["token"].ToString();
                tokenBox.Password = Token;
            }

            if (json["projectsFolder"] != null)
            {
                string path = json["projectsFolder"].ToString();
                if (Directory.Exists(path))
                    SetProjectsFolder(path);
                else
                    Notification.Show($"Projects Folder in settings.json is not valid\n({path})", "Invalid Folder");
            }
            else
            {
                projectsText.Text = "My Projects folder not set.";
                projectsButton.Content = "Set";
            }
        }

        private void SetMyProjectsFolder(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(projectsFolder))
                FolderDialog.Dialog(projectsFolder, callBack);
            else
                FolderDialog.Dialog(Program.root, callBack);
        }
        public void SetProjectsFolder(bool set, string path)
        {
            if (set)
                SetProjectsFolder(path);
        }

        private void SetProjectsFolder(string folder)
        {
            projectsFolder = folder;
            projectsText.Text = "My Projects folder:\n" + projectsFolder;
            projectsButton.Content = "Change";
            MainWindow._instance.uploadModButton.IsEnabled = true;
            SaveSettings();
        }
    }
}
