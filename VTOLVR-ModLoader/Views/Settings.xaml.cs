using Valve.Newtonsoft.Json.Linq;
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
using VTOLVR_ModLoader.Classes.Json;
using Valve.Newtonsoft.Json;
using System.Windows.Media;

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
        private List<string> _branches;

        private SolidColorBrush _yellowBrush = new SolidColorBrush(Color.FromRgb(241, 241, 39));
        private SolidColorBrush _whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

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
        public static void SaveSettings()
        {
            Helper.SentryLog("Saving Settings", Helper.SentryLogCategory.Settings);

            USettings.Token = Token;
            USettings.ProjectsFolder = ProjectsFolder;
            USettings.AutoUpdate = AutoUpdate;
            USettings.LaunchSteamVR = SteamVR;
            USettings.ActiveBranch = Instance._branchesBox.SelectedIndex;

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
            SetupBranchesFromSettings();
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
        private void SetupBranches()
        {
            Helper.SentryLog("Setting up default branches", Helper.SentryLogCategory.Settings);
            Console.Log("Setting up default branches");
            _branches = new List<string>();
            _branches.Add("None");
            _branchesBox.ItemsSource = _branches;
            _branchesBox.SelectedIndex = 0;
        }
        private void SetupBranchesFromSettings()
        {
            if (USettings.Branches == null)
            {
                Console.Log("Branches in setting file where null");
                SetupBranches();
                return;
            }
            Helper.SentryLog("Setting up branches from settings", Helper.SentryLogCategory.Settings);
            Console.Log("Setting up branches from settings file");
            _branches = USettings.Branches;
            _branchesBox.ItemsSource = _branches;
            if (USettings.ActiveBranch > USettings.Branches.Count - 1)
            {
                Notification.Show($"Active branch {USettings.ActiveBranch} was outside the count of branches {USettings.Branches.Count}. Selected branch None");
                _branchesBox.SelectedIndex = 0;
                Console.Log($"Active branch {USettings.ActiveBranch} was outside the count of branches {USettings.Branches.Count}");
            }
            else
            {
                _branchesBox.SelectedIndex = USettings.ActiveBranch;
                Program.branch = USettings.Branches[USettings.ActiveBranch];
            }

        }
        private void AddBranch(string branch)
        {
            Console.Log("Adding branch " + branch);
            _branches.Add(branch);
            _branchesBox.ItemsSource = _branches.ToArray();
            USettings.Branches = _branches;
            SaveSettings();
        }
        private void CheckBranch(object sender, RoutedEventArgs e)
        {
            _newBranchCodeBox.IsEnabled = false;
            _branchCheckButton.IsEnabled = false;
            CheckBranch(_newBranchCodeBox.Text);
        }
        private void CheckBranch(string branch)
        {
            Helper.SentryLog($"Checking Branch {branch}", Helper.SentryLogCategory.Settings);
            Clipboard.SetText(Program.url + Program.apiURL + Program.releasesURL + "/" + $"?branch={branch}");
            HttpHelper.DownloadStringAsync(
                Program.url + Program.apiURL + Program.releasesURL + "/" + $"?branch={branch}",
                CheckBranchDone);
        }
        private async void CheckBranchDone(HttpResponseMessage response)
        {
            Helper.SentryLog($"Got branch result {response.StatusCode}", Helper.SentryLogCategory.Settings);
            if (response.IsSuccessStatusCode)
            {
                IsBranchValid(
                    JsonConvert.DeserializeObject<List<Release>>(
                        await response.Content.ReadAsStringAsync()));
            }
            else
            {
                //Failed
                Console.Log("Error:\n" + response.StatusCode);
            }
        }
        private void IsBranchValid(List<Release> releases)
        {
            if (releases.Count == 0)
            {
                _branchResultText.Text = _newBranchCodeBox.Text + " is not a valid branch";
                _branchResultText.Foreground = _yellowBrush;
                _branchResultText.Visibility = Visibility.Visible;

                DelayHide(_branchResultText, 4);
                DelayEnable(_newBranchCodeBox, 4);
                DelayEnable(_branchCheckButton, 4);
                return;
            }
            AddBranch(_newBranchCodeBox.Text);

            _branchResultText.Text = _newBranchCodeBox.Text + " is a valid branch";
            _branchResultText.Foreground = _whiteBrush;
            _branchResultText.Visibility = Visibility.Visible;

            DelayHide(_branchResultText, 4);
            DelayEnable(_newBranchCodeBox, 4);
            DelayEnable(_branchCheckButton, 4);
            _newBranchCodeBox.Text = string.Empty;
        }
        private async void DelayHide(UIElement uiElement, float delayInSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
            uiElement.Visibility = Visibility.Hidden;
        }
        private async void DelayEnable(UIElement uiElement, float delayInSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
            uiElement.IsEnabled = true;
        }
        private void BranchChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SaveSettings();
            Program.branch = _branches[_branchesBox.SelectedIndex];
            if (Program.branch == "None")
                Program.branch = string.Empty;
            Program.GetReleases();
        }
    }
}
