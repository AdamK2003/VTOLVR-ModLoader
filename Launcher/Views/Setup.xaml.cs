// unset

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Launcher.Classes;
using Launcher.Classes.Json;
using Launcher.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Launcher.Views
{
    public partial class Setup : UserControl
    {
        private bool _inAdminMode = false;
        public Setup()
        {
            InitializeComponent();
            CheckForAdmin();
        }
        
        private void CheckForAdmin()
        {
            if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator))
            {
                string AdminErrorText =
                    @"To support one click install for the website, the installer HAS to run as an administrator.
If you run without administrator you will NOT be able to use one click install on the website.

Restart the Mod Loader as an administrator?";

                MessageBoxResult result = MessageBox.Show(AdminErrorText, "Missing Permissions",
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        var processStartInfo = new ProcessStartInfo();
                        processStartInfo.WorkingDirectory = Environment.CurrentDirectory;
                        processStartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                        processStartInfo.UseShellExecute = true;
                        processStartInfo.WindowStyle = ProcessWindowStyle.Normal;

                        processStartInfo.Verb = "runas";

                        Process.Start(processStartInfo);

                        Process.GetCurrentProcess().Kill();
                        break;
                    case MessageBoxResult.No:
                        _ociText.Visibility = Visibility.Visible;
                        _ociTitle.Visibility = Visibility.Visible;
                        _ociTextSmall.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
            {
                _inAdminMode = true;
            }
        }

        private void BrowseButtonPressed(object sender, RoutedEventArgs e)
        {
            OpenFileBrowser();
        }

        private void AutoDectectButtonPressed(object sender, RoutedEventArgs e)
        {
            _pathBox.Text = FindVTOL();
        }
        
        private string FindVTOL()
        {
            string steamInstallPath;
            try
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                    steamInstallPath = localKey.OpenSubKey("SOFTWARE").OpenSubKey("Wow6432Node").OpenSubKey("Valve")
                        .OpenSubKey("Steam")
                        .GetValue("InstallPath").ToString();
                }
                else
                {
                    var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                    steamInstallPath = localKey.OpenSubKey("SOFTWARE").OpenSubKey("Valve")
                        .OpenSubKey("Steam")
                        .GetValue("InstallPath").ToString();
                }
            }
            catch (Exception e)
            {
                return $"Error, can not find VTOL VR. ({e.Message})";
            }

            string[] contents = File.ReadAllText(steamInstallPath + @"\steamapps\libraryfolders.vdf").Split('"');
            string gameFolder = steamInstallPath;

            for (int i = 13;
                !Directory.Exists(gameFolder + "\\steamapps\\common\\VTOL VR\\") && i < contents.Length;
                i += 4) //Loops through all steamlibrary folders to check if the game is installed there
            {
                gameFolder = contents[i];
            }

            string path = gameFolder + "\\steamapps\\common\\VTOL VR\\";
            if (!Directory.Exists(path)) 
                //Throws an error if the game can't be found
            {
                return $"Error, can not find VTOL VR. (Folder doesn't exist at {path})";
            }

            string[] split = gameFolder.Split('\\');
            string result = "";
            for (int i = 0; i < split.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(split[i]))
                    result += split[i] + @"\";
            }

            result += @"steamapps\common\VTOL VR\";
            return result;
        }
        
        private void OpenFileBrowser()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            openFileDialog.Filter = "exe files (*.exe)|*.exe";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Open the VTOLVR.exe";
            openFileDialog.FileName = "VTOLVR.exe";

            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileName.Contains("VTOLVR.exe"))
                {
                    _pathBox.Text = openFileDialog.FileName.Replace("VTOLVR.exe", "");
                }
                else
                {
                    MessageBox.Show("Couldn't find VTOLVR.exe, please try again.");
                    OpenFileBrowser();
                }
            }
        }

        private void InstallButtonPressed(object sender, RoutedEventArgs e)
        {
            string vtolPath = _pathBox.Text;

            if (!Directory.Exists(vtolPath))
            {
                Notification.Show($"\"{vtolPath}\"\n Is not a valid directory", "Invalid Directory");
                return;
            }

            vtolPath = Path.Combine(vtolPath, "VTOLVR.exe");
            if (!File.Exists(vtolPath))
            {
                Notification.Show($"Could not find the VTOL VR exe at:\n{vtolPath}");
                return;
            }
            
            Install();
        }

        private void Install()
        {
            Helper.SentryLog("Creating Directories", Helper.SentryLogCategory.Setup);
            MainWindow.SetProgress(0, "Creating Directories");

            DirectoryInfo modLoaderFolder = 
                Directory.CreateDirectory(Path.Combine(_pathBox.Text, "VTOLVR_ModLoader"));

            modLoaderFolder.CreateSubdirectory("mods");
            modLoaderFolder.CreateSubdirectory("skins");
            
            Helper.SentryLog("Creating Program Data", Helper.SentryLogCategory.Setup);

            ProgramData data = new(){VTOLPath = _pathBox.Text};
            ProgramData.Save(data);
            
            Startup.Data = data;
            Startup.SetPaths();
            
            if (_inAdminMode)
            {
                Helper.SentryLog("SetupOCI", Helper.SentryLogCategory.Setup);
                SetupOCI(Program.ExePath);
            }
            
            MainWindow.SetProgress(10, "Downloading Files");
            Helper.SentryLog("Downloading Files", Helper.SentryLogCategory.Setup);
            Updater.CheckForUpdates(true, FinishedUpdating);
        }

        private void FinishedUpdating()
        {
            MainWindow._instance.CreatePages();
            MainWindow._instance.modsButton.IsEnabled = true;
            MainWindow._instance.skinsButton.IsEnabled = true;
            MainWindow._instance.homeButton.IsEnabled = true;
            MainWindow._instance.openFButton.IsEnabled = true;
            MainWindow._instance.uploadModButton.IsEnabled = true;
            MainWindow._instance.devTButton.IsEnabled = true;
            MainWindow._instance.settingsButton.IsEnabled = true;
            MainWindow._instance.launchButton.IsEnabled = true;
            MainWindow._instance.consoleButton.IsEnabled = true;
            MainWindow._instance.downloadsButton.IsEnabled = true;
            MainWindow._instance.ItemManager.UpdateUI(true);
            MainWindow._instance.DataContext = MainWindow._instance.ItemManager;
            MainWindow.SetProgress(100, "Finished Installing");
        }

        public static void SetupOCI(string root)
        {
            //Setting Default
            Registry.SetValue(
                Settings.OCIPath,
                @"",
                @"URL:VTOLVRML");
            //Setting URL Protocol
            Registry.SetValue(
                Settings.OCIPath,
                @"URL Protocol",
                @"");
            //Setting Default Icon
            Registry.SetValue(
                Settings.OCIPath + @"\DefaultIcon",
                @"",
                root + @",1");
            //Setting Command
            Registry.SetValue(
                Settings.OCIPath + @"\shell\open\command",
                @"",
                "\"" + root + "\" \"" + @"%1" + "\"");
        }
    }
}