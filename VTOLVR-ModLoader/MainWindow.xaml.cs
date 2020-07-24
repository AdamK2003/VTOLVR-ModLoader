using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using WpfAnimatedGif;
using System.Net;
using System.Xml.Serialization;
using System.ComponentModel;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Collections.Generic;
using VTOLVR_ModLoader.Views;
using VTOLVR_ModLoader.Windows;
using System.Runtime.InteropServices;
using VTOLVR_ModLoader.Classes;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Console = VTOLVR_ModLoader.Views.Console;
using System.Windows.Controls;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        public enum gifStates { Paused, Play, Frame }

        public static MainWindow _instance;

        //Pages
        public News news { get; private set; }
        public Views.Settings settings { get; private set; }
        public DevTools devTools { get; private set; }
        public Console console { get; private set; }
        public ProjectManager pManager { get; private set; }

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private bool isBusy;
        //Updates
        WebClient client;
        //URI
        private bool uriSet = false;
        private string uriDownload;
        private string uriFileName;
        //Notifications
        private NotificationWindow notification;
        //Storing completed tasks
        private int extractedMods = 0;
        private int extractedSkins = 0;
        private int movedDep = 0;

        private static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        #region Startup
        public MainWindow()
        {
            _instance = this;
            Startup.RunStartUp();
            Program.SetupAfterUI();
            InitializeComponent();
        }
        public void CreatePages()
        {
            console = new Console();
            news = new News();
            settings = new Views.Settings();
            devTools = new DevTools();
            pManager = new ProjectManager();
            DataContext = news;
        }     
#endregion

        #region Handeling Mods
        private void ExtractMods()
        {
            if (uriSet)
            {
                DownloadFile();
                return;
            }
            SetPlayButton(true);
            SetProgress(0, "Extracting  mods...");
            DirectoryInfo folder = new DirectoryInfo(Program.root + Program.modsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                SetPlayButton(false);
                SetProgress(100, "No new mods were found");
                MoveDependencies();
                return;
            }
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting mods... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                //We don't want to overide any mod folder incase of user data
                //So mod users have to update by hand
                if (Directory.Exists(currentFolder))
                    continue;

                Directory.CreateDirectory(currentFolder);
                ZipFile.ExtractToDirectory(files[i].FullName, currentFolder);
                extractedMods++;

                //Deleting the zip
                //File.Delete(files[i].FullName);
            }

            SetPlayButton(false);
            SetProgress(100, extractedMods == 0 ? "No mods were extracted" : "Extracted " + extractedMods +
                (extractedMods == 1 ? " mod" : " mods"));
            MoveDependencies();

        }
        private void ExtractSkins()
        {
            SetPlayButton(true);
            SetProgress(0, "Extracting skins...");
            DirectoryInfo folder = new DirectoryInfo(Program.root + Program.skinsFolder);
            FileInfo[] files = folder.GetFiles("*.zip");
            if (files.Length == 0)
            {
                SetPlayButton(false);
                SetProgress(100,
                    (extractedMods == 0 ? "0 Mods" : (extractedMods == 1 ? "1 Mod" : extractedMods + " Mods")) +
                    " and " +
                    (extractedSkins == 0 ? "0 Skins" : (extractedSkins == 1 ? "1 Skin" : extractedSkins + " Skins")) +
                    " extracted" +
                    " and " +
                    (movedDep == 0 ? "0 Dependencies" : (movedDep == 1 ? "1 Dependencies" : movedDep + " Dependencies")) +
                    " moved");
                
                return;
            }
            float zipAmount = 100 / files.Length;
            string currentFolder;

            for (int i = 0; i < files.Length; i++)
            {
                SetProgress((int)Math.Ceiling(zipAmount * i), "Extracting skins... [" + files[i].Name + "]");
                //This should remove the .zip at the end for the folder path
                currentFolder = files[i].FullName.Split('.')[0];

                //We don't want to overide any mod folder incase of user data
                //So mod users have to update by hand
                if (Directory.Exists(currentFolder))
                    continue;

                Directory.CreateDirectory(currentFolder);
                ZipFile.ExtractToDirectory(files[i].FullName, currentFolder);
                extractedSkins++;
            }

            SetPlayButton(false);
            //This is the final text displayed in the progress text
            SetProgress(100,
                (extractedMods == 0 ? "0 Mods" : (extractedMods == 1 ? "1 Mod" : extractedMods + " Mods")) +
                " and " +
                (extractedSkins == 0 ? "0 Skins" : (extractedSkins == 1 ? "1 Skin" : extractedSkins + " Skins")) +
                " extracted" +
                " and " +
                (movedDep == 0 ? "0 Dependencies" : (movedDep == 1 ? "1 Dependencies" : movedDep + " Dependencies")) +
                " moved");
            
        }

        private void MoveDependencies()
        {
            SetPlayButton(true);
            string[] modFolders = Directory.GetDirectories(Program.root + Program.modsFolder);

            string fileName;
            string[] split;
            for (int i = 0; i < modFolders.Length; i++)
            {
                string[] subFolders = Directory.GetDirectories(modFolders[i]);
                for (int j = 0; j < subFolders.Length; j++)
                {
                    Console.Log("Checking " + subFolders[j].ToLower());
                    if (subFolders[j].ToLower().Contains("dependencies"))
                    {
                        Console.Log("Found the folder dependencies");
                        string[] depFiles = Directory.GetFiles(subFolders[j], "*.dll");
                        for (int k = 0; k < depFiles.Length; k++)
                        {
                            split = depFiles[k].Split('\\');
                            fileName = split[split.Length - 1];

                            if (File.Exists(Directory.GetParent(Program.root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName))
                            {
                                string oldHash = CalculateMD5(Directory.GetParent(Program.root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                string newHash = CalculateMD5(depFiles[k]);
                                if (!oldHash.Equals(newHash))
                                {
                                    File.Copy(depFiles[k], Directory.GetParent(Program.root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName,
                                        true);
                                    movedDep++;
                                }
                            }
                            else
                            {
                                Console.Log("Moved file \n" + Directory.GetParent(Program.root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                File.Copy(depFiles[k], Directory.GetParent(Program.root).FullName +
                                            @"\VTOLVR_Data\Managed\" + fileName,
                                            true);
                                movedDep++;
                            }
                        }
                        break;
                    }
                }
            }

            SetPlayButton(false);
            SetProgress(100, movedDep == 0 ? "Checked Dependencies" : "Moved " + movedDep
                + (movedDep == 1 ? " dependency" : " dependencies"));

            ExtractSkins();
        }

        private void DownloadFile()
        {
            if (uriDownload.Equals(string.Empty) || uriDownload.Split('/').Length < 4)
                return;

            uriFileName = uriDownload.Split('/')[3];
            bool isMod = uriDownload.Contains("mods");
            client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FileProgress);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(FileDone);
            client.DownloadFileAsync(new Uri(Program.url + "/" + uriDownload), Program.root + (isMod ? Program.modsFolder : Program.skinsFolder) + @"\" + uriFileName);
        }

        private void FileDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                ShowNotification("Downloaded " + uriFileName);
                //Checking if they already had the mod extracted incase they wanted to update it
                bool isMod = uriDownload.Contains("mods");
                if (Directory.Exists(Program.root + (isMod ? Program.modsFolder : Program.skinsFolder) + @"\" + uriFileName.Split('.')[0]))
                {
                    Directory.Delete(Program.root + (isMod ? Program.modsFolder : Program.skinsFolder) + @"\" + uriFileName.Split('.')[0], true);
                }
            }
            else
            {
                Notification.Show("Failed Downloading " + uriFileName + "\n" + e.Error.ToString(),
                    "Failed Downloading File");
            }

            uriSet = false;
            SetProgress(100, "Downloaded " + uriFileName);
            SetPlayButton(false);
            ExtractMods();
        }

        private void FileProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading " + uriFileName + "...");
        }

#endregion

        private void ShowNotification(string text)
        {
            if (notification != null)
            {
                notification.Close();
            }
            notification = new NotificationWindow(text, this, 5);
            notification.Owner = this;
            notification.Show();
        }
        public void SetProgress(int barValue, string text)
        {
            Console.Log(text);
            progressText.Text = text;
            progressBar.Value = barValue;
        }
        public void SetPlayButton(bool disabled)
        {
            launchButton.Content = disabled ? "Busy" : "Play";
            isBusy = disabled;
        }
        public void GifState(gifStates state, int frame = 0)
        {
            //Changing the gif's state
            var controller = ImageBehavior.GetAnimationController(LogoGif);
            switch (state)
            {
                case gifStates.Paused:
                    controller.Pause();
                    break;
                case gifStates.Play:
                    controller.Play();
                    break;
                case gifStates.Frame:
                    controller.GotoFrame(frame);
                    break;
            }
        }

        public static void Quit()
        {
            Program.Quit();
        }

        private void Website(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vtolvr-mods.com");
            Console.Log("Website Opened!");
        }
        private void Discord(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/49HDD7m");
            Console.Log("Discord Opened!");
        }
        private void Patreon(object sender, RoutedEventArgs e)
        {
            
            Process.Start("https://www.patreon.com/vtolvrmods");
            Console.Log("Patreon Opened!");
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(Program.root);
            Console.Log("Mod Loader Folder Opened!");
        }
        private void OpenGame(object sender, RoutedEventArgs e)
        {
            Program.LaunchGame();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            if (settings == null)
                settings = new Views.Settings();
            settings.UpdateButtons();
            DataContext = settings;
        }

        public void Creator(object sender, RoutedEventArgs e)
        {
            if (pManager == null)
                pManager = new ProjectManager();
            pManager.SetUI();
            DataContext = pManager;
        }

        private void News(object sender, RoutedEventArgs e)
        {
            if (news == null)
                news = new News();
            DataContext = news;
        }

        private void OpenTools(object sender, RoutedEventArgs e)
        {
            if (devTools == null)
                devTools = new DevTools();
            devTools.SetUI();
            DataContext = devTools;
        }

        private void OpenConsole(object sender, RoutedEventArgs e)
        {
            if (console == null)
                console = new Views.Console();
            console.UpdateFeed();
            DataContext = console;
        }

        public static void OpenPage(UserControl control)
        {
            _instance.DataContext = control;
        }
        public static void SetBusy(bool isBusy)
        {
            _instance.homeButton.IsEnabled = !isBusy;
            _instance.consoleButton.IsEnabled = !isBusy;
            _instance.uploadModButton.IsEnabled = !isBusy;
            _instance.devTButton.IsEnabled = !isBusy;
            _instance.settingsButton.IsEnabled = !isBusy;
            _instance.launchButton.IsEnabled = !isBusy;
            _instance.launchButton.Content = isBusy ? "Busy" : "Play";
        }
    }

    public class Mod
    {
        public string name;
        public string description;
        public Mod() { }

        public Mod(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}
