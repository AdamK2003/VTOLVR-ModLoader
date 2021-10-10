using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
using System.Net.Http;
using System.Security.Principal;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Windows.Media;
using Launcher.Classes;
using Launcher.Classes.Json;
using Launcher.Windows;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Salaros.Configuration;

namespace Launcher.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        private static readonly string[] _modLoaderFiles = new string[]
        {
            "winhttp.dll", "doorstop_config.ini", "discord-rpc.dll", "0Harmony.dll", "Core.dll", "SimpleTCP.dll",
            "Newtonsoft.Json.dll"
        };

        private struct UninstallResult
        {
            public bool IsSusscessful;
            public string Error;
        }

        private string _exeTempPath = string.Empty;

        public static Settings Instance;

        public const string SavePath = @"\settings.json";
        private const string userURL = "/get-token";
        public const string OCIPath = @"HKEY_CLASSES_ROOT\VTOLVRML";
        private const string _patcherConsole = "UnPatcher.exe";

        public static bool tokenValid = false;
        private bool hideResult;
        private Action<bool, string> callBack;
        private List<string> _branches;

        private SolidColorBrush _yellowBrush = new SolidColorBrush(Color.FromRgb(255, 250, 101));
        private SolidColorBrush _whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private bool _finishedSetup = false;

        //Settings
        public static UserSettings USettings { get; private set; }
        public static string Token;
        public static string ProjectsFolder;
        public static bool AutoUpdate = true;
        public static bool SteamVR = true;
        public static bool ModLoaderEnabled = true;

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
                    Program.URL + Program.ApiURL + userURL + Program.JsonFormat,
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

            UserSettings.SaveSettings(Program.Root + SavePath);
            Console.Log("Saved Settings");
        }

        private void LoadSettings()
        {
            Helper.SentryLog("Loading Settings", Helper.SentryLogCategory.Settings);
            UserSettings.LoadSettings(Program.Root + SavePath);

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

            CheckDoorstepConfig();
            SetupBranchesFromSettings();
            SaveSettings();
        }

        private void SetMyProjectsFolder(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opening folder dialogue", Helper.SentryLogCategory.Settings);
            if (!string.IsNullOrEmpty(ProjectsFolder))
                FolderDialog.Dialog(ProjectsFolder, callBack);
            else
                FolderDialog.Dialog(Program.Root, callBack);
        }

        public void SetProjectsFolder(bool set, string path)
        {
            if (!set)
            {
                return;
            }

            if (path == Program.VTOLFolder + "VTOLVR_ModLoader")
            {
                Notification.Show("Project folder can't be the ModLoader's folder");
                return;
            }

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
            Console.Log("Creating Registry entry for one click installing");
            Setup.SetupOCI(Program.ExePath);
            Console.Log("Finished!");
            Notification.Show("Finished setting registry values for one click install", "Finished",
                Notification.Buttons.Ok);
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
                Notification.Show(
                    $"Active branch {USettings.ActiveBranch} was outside the count of branches {USettings.Branches.Count}. Selected branch None");
                _branchesBox.SelectedIndex = 0;
                Console.Log(
                    $"Active branch {USettings.ActiveBranch} was outside the count of branches {USettings.Branches.Count}");
            }
            else
            {
                _branchesBox.SelectedIndex = USettings.ActiveBranch;
                Program.Branch = USettings.Branches[USettings.ActiveBranch];
            }

            _finishedSetup = true;
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
            Clipboard.SetText(Program.URL + Program.ApiURL + Program.ReleasesURL + "/" + $"?branch={branch}");
            HttpHelper.DownloadStringAsync(
                Program.URL + Program.ApiURL + Program.ReleasesURL + "/" + $"?branch={branch}",
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
            if (!_finishedSetup)
                return;

            SaveSettings();
            string oldBranch = Program.Branch;
            Program.Branch = _branches[_branchesBox.SelectedIndex];
            if (Program.Branch == "None")
                Program.Branch = string.Empty;
            if (oldBranch != Program.Branch)
            {
                Program.GetReleases();
            }
        }

        private void UninstallButton(object sender, RoutedEventArgs e)
        {
            if (CheckForAdmin())
            {
                Notification.Show($"Are you sure you want to uninstall the Mod Loader?",
                    $"Are you sure? :(",
                    Notification.Buttons.NoYes, yesNoResultCallback: UninstallNotificationResult);
            }
            else
            {
                string message =
                    @"We need administrator permission to fully uninstall the Mod Loader.
You can also uninstall the Mod Loader without it, but this will leave a few files on your computer. In practice, this won't affect you or the performance of your computer.

Do you want to restart the Mod Loader as an administrator?";

                Notification.Show(message,
                    $"Admin privilege",
                    Notification.Buttons.NoYes, yesNoResultCallback: UninstallRestartOrIncomplete);
            }
        }

        private void UninstallNotificationResult(bool result)
        {
            if (!result)
                return;

            Uninstall();
        }

        private void UninstallRestartOrIncomplete(bool restart)
        {
            if (restart)
            {
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.WorkingDirectory = Environment.CurrentDirectory;
                processStartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                processStartInfo.UseShellExecute = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Normal;

                processStartInfo.Verb = "runas";

                processStartInfo.Arguments += " uninstall";

                Process.Start(processStartInfo);

                Process.GetCurrentProcess().Kill();
            }
            else
            {
                Uninstall();
            }
        }

        public void Uninstall()
        {
            bool uninstallingAsAdmin = CheckForAdmin();

            Helper.SentryLog("Uninstalling", Helper.SentryLogCategory.Settings);
            Console.Log($"Uninstalling");


            if (uninstallingAsAdmin)
            {
                Console.Log("Deleting Registry Entries");
                RegistryKey key = Registry.ClassesRoot.OpenSubKey("VTOLVRML");
                if (key != null)
                {
                    Registry.ClassesRoot.DeleteSubKeyTree("VTOLVRML");
                    Console.Log("Deleted Keys in Registry");
                }
                else
                {
                    Console.Log($"There was no key in the registry.");
                }
            }

            Console.Log("Deleting Files");
            DirectoryInfo vtolFolder = new(Program.VTOLFolder);
            SearchForFiles(vtolFolder);

            FileInfo exe = new(Program.ExePath);
            if (exe.Directory?.Name is "VTOLVR_ModLoader")
            {
                string newPath = Path.Combine(exe.Directory.Parent.FullName, exe.Name);
                Console.Log("Moving outside of modloader folder.\n" + newPath);
                Helper.TryMove(exe.FullName, newPath);
                _exeTempPath = newPath;
            }
            else
            {
                _exeTempPath = exe.FullName;
            }

            Console.Log("Deleting Mod Loader Folder\n" + Program.Root);
            Directory.Delete(Program.Root, true);

            Console.Log("Deleting Appdata files");
            ProgramData.Delete();

            UninstallComplete();
        }

        private void SearchForFiles(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                if (_modLoaderFiles.Contains(files[i].Name))
                {
                    Console.Log($"Deleting {files[i].Name}");
                    Helper.TryDelete(files[i].FullName);
                }
            }

            DirectoryInfo[] folders = folder.GetDirectories();

            for (int i = 0; i < folders.Length; i++)
            {
                SearchForFiles(folders[i]);
            }
        }

        private void UninstallComplete()
        {
            Notification.Show($"Uninstall Complete.", "Uninstall Complete",
                Notification.Buttons.Ok, ClosedCallback);
        }

        private void ClosedCallback()
        {
            Process.Start("cmd.exe",
                $"/C echo Deleting the mod loader exe & " +
                $"choice /C Y /N /D Y /T 3 & Del \"{_exeTempPath}\"");

            Process.GetCurrentProcess().Kill();
        }

        private void DisableButtonClicked(object sender, RoutedEventArgs e)
        {
            ToggleModLoader();
        }

        public void ToggleModLoader()
        {
            string filePath = Path.Combine(Program.VTOLFolder, "doorstop_config.ini");
            if (!File.Exists(filePath))
            {
                Notification.Show($"Could not find doorstop_config.ini in games root.", "Missing File");
                Console.Log($"Couldn't find doorstep config file at {filePath}");
                ModLoaderEnabled = true;
                return;
            }

            ConfigParser config = new(filePath);
            bool result = config.GetValue("UnityDoorstop", "enabled", true);

            ModLoaderEnabled = !ModLoaderEnabled;

            if (result != ModLoaderEnabled)
            {
                config.SetValue("UnityDoorstop", "enabled", ModLoaderEnabled);
                config.Save();
            }

            _disableButton.Content = ModLoaderEnabled ? "Disable" : "Enable";
            Console.Log($"Changed doorstep config to {ModLoaderEnabled}");
            MainWindow._instance.WarningMessage.Visibility = ModLoaderEnabled ? Visibility.Hidden : Visibility.Visible;

            if (!ModLoaderEnabled)
            {
                string consolePath = Path.Combine(Program.Root, _patcherConsole);
                if (File.Exists(consolePath))
                {
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = consolePath,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    Process process = new Process();
                    process.OutputDataReceived += OnOutputDataReceived;
                    process.ErrorDataReceived += OnOutputDataReceived;
                    process.StartInfo = info;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            
            SaveSettings();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                Console.Log(e.Data);
            });
        }

        private void CheckDoorstepConfig()
        {
            string filePath = Path.Combine(Program.VTOLFolder, "doorstop_config.ini");
            if (!File.Exists(filePath))
            {
                Console.Log($"Couldn't find doorstep config file at {filePath}");
                return;
            }
            
            ConfigParser config = new (filePath);
            ModLoaderEnabled = config.GetValue("UnityDoorstop", "enabled", true);  
            _disableButton.Content = ModLoaderEnabled ? "Disable" : "Enable";
            MainWindow._instance.WarningMessage.Visibility = ModLoaderEnabled ? Visibility.Hidden : Visibility.Visible;
        }
    }
}