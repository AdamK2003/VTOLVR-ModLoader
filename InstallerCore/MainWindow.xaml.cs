﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Path = System.IO.Path;
using Microsoft.Win32;
using Sentry;

namespace InstallerCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();

        //Pages
        public enum Page
        {
            About,
            SelectFolder,
            Confirm,
            Extracting,
            Finished,
            Error
        }

        private Page currentPage;

        //
        private string vtFolder;
        private static string uriPath = @"HKEY_CLASSES_ROOT\VTOLVRML";
        private bool inAdmin;

        public MainWindow()
        {
            InitializeComponent();
            SwitchPage();
            CheckForAdmin();

#pragma warning disable 4014
            FocusWindow();
#pragma warning restore 4014
        }

        private async Task FocusWindow()
        {
            await Task.Delay(500);

            Show();
            Activate();
        }

        private void CheckForAdmin()
        {
            if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator))
            {
                string AdminErrorText =
                    @"To support one click install for the website, the installer HAS to run as an administrator.
If you run without administrator you will NOT be able to use one click install on the website.

Restart the installer as an administrator?";

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

                        Quit();
                        break;
                    case MessageBoxResult.No:
                        inAdmin = false;
                        break;
                }
            }
            else
                inAdmin = true;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
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
                SentrySdk.AddBreadcrumb(e.Message);
                throw new Exception("Steam can't be found");
            }

            string[] contents = File.ReadAllText(steamInstallPath + @"\steamapps\libraryfolders.vdf").Split('"');
            string gameFolder = steamInstallPath;

            for (int i = 13;
                !Directory.Exists(gameFolder + "\\steamapps\\common\\VTOL VR\\") && i < contents.Length;
                i += 4) //Loops through all steamlibrary folders to check if the game is installed there
            {
                gameFolder = contents[i];
            }

            if (!Directory.Exists(gameFolder + "\\steamapps\\common\\VTOL VR\\")
            ) //Throws an error if the game can't be found
            {
                Error.Visibility = Visibility.Visible;

                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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

        /// <summary>
        /// If the file exists it will delete it other wise,
        /// it doesn't throw an error
        /// </summary>
        /// <param name="path"></param>
        private void TryDelete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private void InstallFiles()
        {
            //If they change the folder path by typing it in by hand
            vtFolder = folderBox.Text;
            SetProgress(0);
            TryDelete(vtFolder + @"ModLoader.zip");
            try
            {
                //Extracting the zip from resources to files
                File.WriteAllBytes(vtFolder + "ModLoader.zip", Properties.Resources.ModLoader);
                ExtractZipToDirectory(vtFolder + @"ModLoader.zip", vtFolder);
                SetProgress(50);
                File.Delete(vtFolder + @"ModLoader.zip");
                SetProgress(75);

                if (dShortcut.IsChecked == true)
                    CreateShortcut(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) +
                        @"\VTOL VR Mod Loader.lnk",
                        vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
                if (smShortcut.IsChecked == true)
                    CreateShortcut(
                        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\VTOL VR Mod Loader.lnk",
                        vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");

                if (inAdmin)
                    CreateURI(vtFolder + @"VTOLVR_ModLoader");
            }
            catch (Exception e)
            {
                errorTextBox.Text = e.Message;
                currentPage = Page.Error;
                backButton.Visibility = Visibility.Hidden;
                nextButotn.Visibility = Visibility.Hidden;
                cancelButton.Content = "Close";
                SwitchPage();
                Console.WriteLine(e.ToString());
                return;
            }

            SetProgress(100);
            currentPage++;
            SwitchPage();
        }

        private void CreateURI(string root)
        {
            string value = (string)Registry.GetValue(
                uriPath,
                @"",
                @"");
            if (value == null)
            {
                //Setting Default
                Registry.SetValue(
                    uriPath,
                    @"",
                    @"URL:VTOLVRML");
                //Setting URL Protocol
                Registry.SetValue(
                    uriPath,
                    @"URL Protocol",
                    @"");
                //Setting Default Icon
                Registry.SetValue(
                    uriPath + @"\DefaultIcon",
                    @"",
                    root + @"\VTOLVR-ModLoader.exe,1");
                //Setting Command
                Registry.SetValue(
                    uriPath + @"\shell\open\command",
                    @"",
                    "\"" + root + @"\VTOLVR-ModLoader.exe" + "\" \"" + @"%1" + "\"");
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath)
        {
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            using StreamWriter writer = new StreamWriter(deskDir + "\\" + "VTOLVR-ModLoader" + ".url");
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine("URL=file:///" + targetPath);
            writer.WriteLine("IconIndex=0");
            writer.WriteLine("WorkingDirectory=" + vtFolder + @"VTOLVR_ModLoader\");
            string icon = targetPath.Replace('\\', '/');
            writer.WriteLine("IconFile=" + icon);
        }

        private void SwitchPage()
        {
            aboutPage.Visibility = Visibility.Hidden;
            folderPage.Visibility = Visibility.Hidden;
            confirmPage.Visibility = Visibility.Hidden;
            extractingPage.Visibility = Visibility.Hidden;
            finishedPage.Visibility = Visibility.Hidden;
            errorPage.Visibility = Visibility.Hidden;
            switch (currentPage)
            {
                case Page.About:
                    aboutPage.Visibility = Visibility.Visible;
                    break;
                case Page.SelectFolder:
                    if (string.IsNullOrEmpty(vtFolder))
                        vtFolder = FindVTOL();
                    folderBox.Text = vtFolder;
                    folderPage.Visibility = Visibility.Visible;
                    break;
                case Page.Confirm:
                    confirmPage.Visibility = Visibility.Visible;
                    break;
                case Page.Extracting:
                    extractingPage.Visibility = Visibility.Visible;
                    InstallFiles();
                    break;
                case Page.Finished:
                    finishedPage.Visibility = Visibility.Visible;
                    cancelButton.Visibility = Visibility.Hidden;
                    backButton.Content = "Launch";
                    nextButotn.Content = "Close";
                    break;
                case Page.Error:
                    errorPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        private void Quit()
        {
            Process.GetCurrentProcess().Kill();
        }

        private void NextButotn_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == Page.Finished)
                Quit();
            if (currentPage == Page.Extracting)
                return;
            currentPage++;
            SwitchPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage <= 0)
                return;
            if (currentPage == Page.Finished)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(vtFolder + @"VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
                startInfo.WorkingDirectory = vtFolder + @"VTOLVR_ModLoader\";
                Process.Start(startInfo);
                Quit();
            }

            currentPage--;
            SwitchPage();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileBrowser();
        }

        private void SetProgress(float barValue)
        {
            progressBar.Value = barValue;
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
                    vtFolder = openFileDialog.FileName.Replace("VTOLVR.exe", "");
                    folderBox.Text = vtFolder;
                }
                else
                {
                    MessageBox.Show("Couldn't find VTOLVR.exe, please try again.");
                    OpenFileBrowser();
                }
            }
        }

        #region Moving Window

        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(Application.Current.MainWindow);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(Application.Current.MainWindow).X - lm.X;
                this.Top += Mouse.GetPosition(Application.Current.MainWindow).Y - lm.Y;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }

        #endregion

        public static void ExtractZipToDirectory(string zipPath, string extractPath)
        {
            using ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Read);
            List<ZipArchiveEntry> filesInZip = zip.Entries.ToList();
            foreach (var t in filesInZip)
            {
                if (!t.FullName.EndsWith("\\"))
                {
                    if (t.Name.Length == 0)
                    {
                        //This is just a folder
                        Directory.CreateDirectory(Path.Combine(extractPath, t.FullName));
                        continue;
                    }

                    //This is a file
                    Directory.CreateDirectory(Path.Combine(extractPath,
                        t.FullName.Replace(t.Name, string.Empty)));
                    t.ExtractToFile(Path.Combine(extractPath, t.FullName),
                        File.Exists(Path.Combine(extractPath, t.FullName)));
                }
                else if (!Directory.Exists(Path.Combine(extractPath, t.FullName)))
                    Directory.CreateDirectory(Path.Combine(extractPath, t.FullName));
            }
        }
    }
}