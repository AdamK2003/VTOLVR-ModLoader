﻿using Newtonsoft.Json.Linq;
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

        public const string SavePath = @"\settings.json";
        private const string userURL = "/get-token";
        private const string uriPath = @"HKEY_CLASSES_ROOT\VTOLVRML";

        public static bool tokenValid = false;
        private bool hideResult;
        private Action<bool, string> callBack;

        //Settings
        public static UserSettings USettings { get; private set; }
        public static string Token;
        public static string ProjectsFolder;
        public static bool AutoUpdate = true;
        public static bool SteamVR = true;

        public Settings()
        {
            Instance = this;
            callBack += SetProjectsFolder;
            USettings = UserSettings.Settings;
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
            Helper.SentryLog("Created Settings Page", Helper.SentryLogCategory.Settings);
        }
        public async void UpdateButtons()
        {
            Helper.SentryLog("UpdateButtons", Helper.SentryLogCategory.Settings);
            if (!await HttpHelper.CheckForInternet())
            {
                updateButton.Content = "Disabled";
                updateButton.IsEnabled = false;
            }
        }
        public void SetUserToken(string token)
        {
            Helper.SentryLog("Setting User Token", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Testing user token", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Finished testing token", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("No Internet", Helper.SentryLogCategory.Settings);
            updateButton.Content = "Disabled";
            updateButton.IsEnabled = false;
        }
        private static void SaveSettings()
        {
            Helper.SentryLog("Saving Settings", Helper.SentryLogCategory.Settings);

            USettings.Token = Token;
            USettings.ProjectsFolder = ProjectsFolder;
            USettings.AutoUpdate = AutoUpdate;
            USettings.LaunchSteamVR = SteamVR;

            UserSettings.SaveSettings(Program.root + SavePath);
            Console.Log("Saved Settings");
        }

        private void LoadSettings()
        {
            Helper.SentryLog("Loading Settings", Helper.SentryLogCategory.Settings);
            UserSettings.LoadSettings(Program.root + SavePath);

            USettings = UserSettings.Settings;
            ProjectsFolder = USettings.ProjectsFolder;
            AutoUpdate = USettings.AutoUpdate;
            SteamVR = USettings.LaunchSteamVR;
            Token = USettings.Token;

            tokenBox.Password = Token;
            if (!string.IsNullOrWhiteSpace(ProjectsFolder))
                projectsText.Text = $"Projects Folder Set:\n{ProjectsFolder}";
            autoUpdateCheckbox.IsChecked = AutoUpdate;
            steamvrCheckbox.IsChecked = SteamVR;
            SaveSettings();
        }

        private void SetMyProjectsFolder(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opening folder dialogue", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Setting my projects folder", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Changed auto updates", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Changed Steam VR state", Helper.SentryLogCategory.Settings);
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
            Helper.SentryLog("Setting one click install", Helper.SentryLogCategory.Settings);
            CreateURI(Program.root);
        }

        /// <summary>
        /// Returns True if window is in admin mode
        /// </summary>
        /// <returns></returns>
        private bool CheckForAdmin()
        {
            Helper.SentryLog("Checking for admin", Helper.SentryLogCategory.Settings);
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
             .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CreateURI(string root)
        {
            Helper.SentryLog("Creating URL", Helper.SentryLogCategory.Settings);
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

        private void CreateDiagnosticsZip(object sender, RoutedEventArgs e)
        {
            Helper.CreateDiagnosticsZip();
        }
    }
}
