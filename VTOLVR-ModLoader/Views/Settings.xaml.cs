using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
using VTOLVR_ModLoader.Windows;
using VTOLVR_ModLoader.Classes;
using System.Net.Http;
using System.Security.Principal;
using Microsoft.Win32;

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
        private const string uriPath = @"HKEY_CLASSES_ROOT\VTOLVRML";
        private const string jProjectsFolder = "projectsFolder";
        private const string jAutoUpdate = "AutoUpdate";
        private const string jSteamVR = "Launch SteamVR";
        private const string jToken = "token";



        public static bool tokenValid = false;
        private bool hideResult;
        private Action<bool, string> callBack;

        //Settings
        public static string Token;
        public static string ProjectsFolder;
        public static bool AutoUpdate = true;
        public static bool SteamVR = true;

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

            if (!CheckForAdmin())
            {
                oneclickInstallButton.Content = "(Admin Needed)";
                oneclickInstallButton.IsEnabled = false;
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
            Console.Log("Changed Token");
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
                Console.Log("Testing token");
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
                if (jObject[jToken] == null)
                    jObject.Add(jToken, Token);
                else
                    jObject[jToken] = Token;
            }

            if (!string.IsNullOrWhiteSpace(ProjectsFolder))
            {
                if (jObject[jProjectsFolder] == null)
                    jObject.Add(jProjectsFolder, ProjectsFolder);
                else
                    jObject[jProjectsFolder] = ProjectsFolder;
            }

            if (jObject[jAutoUpdate] == null)
                jObject.Add(jAutoUpdate, AutoUpdate);
            else
                jObject[jAutoUpdate] = AutoUpdate;

            if (jObject[jSteamVR] == null)
                jObject.Add(jSteamVR, SteamVR);
            else
                jObject[jSteamVR] = SteamVR;

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

            Console.Log("Saved Settings");
        }

        private void LoadSettings()
        {
            JObject json = null;
            if (!File.Exists(Program.root + savePath))
            {
                SaveSettings();
                return;
            }

            try
            {
                json = JObject.Parse(File.ReadAllText(Program.root + savePath));
            }
            catch (Exception e)
            {
                Console.Log($"Faield Reading Settings: {e.Message}");
                return;
            }

            if (json["projectsFolder"] != null)
            {
                Console.Log("Found the Proejcts Folder");
                ProjectsFolder = json["projectsFolder"].ToString();
            }

            if (json["AutoUpdate"] != null)
            {
                Console.Log("Found Auto Updates");
                if (bool.TryParse(json["AutoUpdate"].ToString(), out bool result))
                {
                    Console.Log($"Auto Updates is {result}");
                    AutoUpdate = result;
                }
                else
                {
                    Console.Log($"Failed to convert {json["AutoUpdate"]} to bool");
                }
            }

            if (json["Launch SteamVR"] != null)
            {
                Console.Log("Found SteamVR");
                if (bool.TryParse(json["Launch SteamVR"].ToString(), out bool result))
                {
                    Console.Log($"Launch Steam VR is {result}");
                    SteamVR = result;
                }
                else
                {
                    Console.Log($"Failed to convert {json["Launch SteamVR"]} to bool");
                }
            }

            if (json["token"] != null)
            {
                Console.Log("Found the token");
                Token = json["token"].ToString();
            }

            tokenBox.Password = Token;
            if (!string.IsNullOrWhiteSpace(ProjectsFolder))
                projectsText.Text = $"Projects Folder Set:\n{ProjectsFolder}";
            autoUpdateCheckbox.IsChecked = AutoUpdate;
            steamvrCheckbox.IsChecked = SteamVR;
            SaveSettings();
        }

        private void SetMyProjectsFolder(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ProjectsFolder))
                FolderDialog.Dialog(ProjectsFolder, callBack);
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
            ProjectsFolder = folder;
            projectsText.Text = "My Projects folder:\n" + ProjectsFolder;
            projectsButton.Content = "Change";
            MainWindow._instance.uploadModButton.IsEnabled = true;
            Directory.CreateDirectory(ProjectsFolder + ProjectManager.modsFolder);
            Directory.CreateDirectory(ProjectsFolder + ProjectManager.skinsFolder);
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

        private void SteamVRChanged(object sender, RoutedEventArgs e)
        {
            if (steamvrCheckbox.IsChecked != null && steamvrCheckbox.IsChecked == true)
                SetSteamVR(true);
            else if (steamvrCheckbox.IsChecked != null)
                SetSteamVR(false);

            Console.Log($"Changed Launching SteamVR to {SteamVR}");
            SaveSettings();
        }

        private void SetSteamVR(bool state)
        {
            if (Instance.steamvrCheckbox != null &&
                Instance.steamvrCheckbox.IsChecked != null)
            {
                Instance.steamvrCheckbox.IsChecked = state;
            }
            SteamVR = state;
        }

        private void SetOneClickInstall(object sender, RoutedEventArgs e)
        {
            CreateURI(Program.root);
        }

        /// <summary>
        /// Returns True if window is in admin mode
        /// </summary>
        /// <returns></returns>
        private bool CheckForAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
             .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CreateURI(string root)
        {
            Console.Log("Creating Registry entry for one click installing");
            string value = (string)Registry.GetValue(
                uriPath,
                @"",
                @"");
            Console.Log($"Setting Default to URL:VTOLVRML");
            Registry.SetValue(
            uriPath,
            @"",
            @"URL:VTOLVRML");
            Console.Log($"Setting {uriPath} key to \"URL Protocol\"");
            Registry.SetValue(
            uriPath,
            @"URL Protocol",
            @"");
            Console.Log($"Setting \"{uriPath}\\DefaultIcon\"" +
                $"to \"{root}\\VTOLVR-ModLoader.exe,1");
            Registry.SetValue(
                uriPath + @"\DefaultIcon",
                @"",
                root + @"\VTOLVR-ModLoader.exe,1");
            Console.Log($"Setting \"{uriPath}\\shell\\open\\command\"" +
                $"to \"\"{root}\\VTOLVR-ModLoader.exe\" \"%1\"");
            Registry.SetValue(
                uriPath + @"\shell\open\command",
                @"",
                "\"" + root + @"\VTOLVR-ModLoader.exe" + "\" \"" + @"%1" + "\"");
            Console.Log("Finished!");
            Notification.Show("Finished setting registry values for one click install", "Finished", Notification.Buttons.Ok);
        }
    }
}
