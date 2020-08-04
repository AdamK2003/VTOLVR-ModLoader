using Valve.Newtonsoft.Json.Linq;
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
        public static Settings Instance;
        private const string userURL = "/get-token";
        private const string savePath = @"\settings.json";
        public static bool tokenValid = false;
        private bool hideResult;
        private Action<bool, string> callBack;

        //Settings
        public static string Token { get; private set; }
        public static string projectsFolder { get; private set; }
        public static bool AutoUpdate { get; private set; } = true;
        public Settings()
        {
            Instance = this;
            callBack += SetProjectsFolder;
            InitializeComponent();
            LoadSettings();
            if (CommunicationsManager.CheckArgs("vtolvrml", out string line))
            {
                if (!line.Contains("token"))
                    TestToken(true);
            }
        }
        public async void UpdateButtons()
        {
            if (!await HttpHelper.CheckForInternet())
            {
                updateButton.Content = "Disabled";
                updateButton.IsEnabled = false;
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
            updateButton.Content = "Disabled";
            updateButton.IsEnabled = false;
        }
        private static void SaveSettings()
        {
            JObject jObject;

            if (File.Exists(Program.root + savePath))
            {
                try
                {
                    jObject = JObject.Parse(File.ReadAllText(Program.root + savePath));
                }
                catch
                {
                    Console.Log("Failed to read settings, overiding it.");
                    jObject = new JObject();
                }
            }
            else
            {
                jObject = new JObject();
            }
            if (!string.IsNullOrEmpty(Token))
            {
                if (jObject["token"] == null)
                    jObject.Add("token", Token);
                else
                    jObject["token"] = Token;
            }

            if (!string.IsNullOrWhiteSpace(projectsFolder))
            {
                if (jObject["projectsFolder"] == null)
                    jObject.Add("projectsFolder", projectsFolder);
                else
                    jObject["projectsFolder"] = projectsFolder;
            }

            if (jObject["AutoUpdate"] == null)
                jObject.Add("AutoUpdate", AutoUpdate);
            else
                jObject["AutoUpdate"] = AutoUpdate;

            try
            {
                File.WriteAllText(Program.root + savePath, jObject.ToString());
                Console.Log("Saved Settings");
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
                    SetProjectsFolder(path, true);
                else
                    Notification.Show($"Projects Folder in settings.json is not valid\n({path})", "Invalid Folder");
            }
            else
            {
                projectsText.Text = "My Projects folder not set.";
                projectsButton.Content = "Set";
            }

            if (json["AutoUpdate"] != null)
            {
                if (bool.TryParse(json["AutoUpdate"].ToString(), out bool result))
                {
                    autoUpdateCheckbox.IsChecked = result;
                    AutoUpdate = result;
                }
                else
                    Console.Log("Failed to convert AutoUpdate setting to bool");
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

        private void SetProjectsFolder(string folder, bool dontSave = false)
        {
            projectsFolder = folder;
            projectsText.Text = "My Projects folder:\n" + projectsFolder;
            projectsButton.Content = "Change";
            MainWindow._instance.uploadModButton.IsEnabled = true;
            Directory.CreateDirectory(projectsFolder + ProjectManager.modsFolder);
            Directory.CreateDirectory(projectsFolder + ProjectManager.skinsFolder);
            if (!dontSave)
                SaveSettings();
        }
        private void AutoUpdateChanged(object sender, RoutedEventArgs e)
        {
            if (autoUpdateCheckbox.IsChecked != null && autoUpdateCheckbox.IsChecked == true)
                SetAutoUpdate(true);
            else if (autoUpdateCheckbox.IsChecked != null)
                SetAutoUpdate(false);

            Console.Log($"Changed Auto Update to {AutoUpdate}");
            SaveSettings();
        }

        public static void SetAutoUpdate(bool state)
        {
            if (Instance.autoUpdateCheckbox != null &&
                Instance.autoUpdateCheckbox.IsChecked != null)
            {
                Instance.autoUpdateCheckbox.IsChecked = state;
            }
            AutoUpdate = state;
        }
    }
}
