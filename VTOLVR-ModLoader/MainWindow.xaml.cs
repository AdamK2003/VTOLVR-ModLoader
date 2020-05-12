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
using System.Runtime.InteropServices;
using VTOLVR_ModLoader.Classes;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);
        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        private enum gifStates { Paused, Play, Frame }

        public static string modsFolder = @"\mods";
        private static string skinsFolder = @"\skins";
        private static string injector = @"\injector.exe";
        private static string updatesFile = @"\updates.xml";
        private static string updatesFileTemp = @"\updates_TEMP.xml";
        private static string updatesURL = @"/files/updates.xml";
        private string url = @"https://vtolvr-mods.com";
        public static string root;
        public static string vtolFolder;
        public readonly string savePath = @"settings.xml";
        public static MainWindow _instance;

        //Startup
        private string[] needFiles = new string[] { "SharpMonoInjector.dll", "injector.exe", "Updater.exe" };
        private string[] neededDLLFiles = new string[] { @"\Plugins\discord-rpc.dll", @"\Managed\0Harmony.dll" };
        private string[] args;
        private bool autoStart;

        //Pages
        private News news;
        private Settings settings;
        private DevTools devTools;
        private Console console;

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
            SearchForProcess();
            InitializeComponent();
            _instance = this;
        }
        private void SearchForProcess()
        {
            //Stopping their being more than one open (Yes this could close the other one half way through a download)
            Process[] p = Process.GetProcessesByName("VTOLVR-ModLoader");
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id != Process.GetCurrentProcess().Id)
                {
                    // check if the window is hidden / minimized
                    if (p[i].MainWindowHandle == IntPtr.Zero)
                    {
                        // the window is hidden so try to restore it before setting focus.
                        ShowWindow(p[i].Handle, ShowWindowEnum.Restore);
                    }

                    // set user the focus to the window
                    SetForegroundWindow(p[i].MainWindowHandle);
                    Quit();
                }
                
            }
        }

        private void Start(object sender, EventArgs e)
        {
            root = Directory.GetCurrentDirectory();
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");
            args = Environment.GetCommandLineArgs();
            WaitAsync();
        }

        private async void WaitAsync()
        {
            await Task.Delay(500);

            news = new News();
            settings = new Settings();
            devTools = new DevTools();
            console = new Views.Console();
            DataContext = news;

            if (args.Length == 2 && args[1].Contains("vtolvrml"))
                URICheck();
            else
                CheckBaseFolder();

            if (SettingsSaveExists())
                LoadSettings();
            else
                LoadDefaultSettings();

            news.LoadNews();

            if (CheckForArg("autostart"))
                autoStart = true;

        }

        private void LoadSettings()
        {
            using (FileStream stream = new FileStream(root + @"\" + savePath, FileMode.Open))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Save));
                Save save = (Save)xml.Deserialize(stream);


                devTools.pilotSelected = save.DevToolsSave.previousPilot;
                devTools.scenarioSelected = save.DevToolsSave.previousScenario;
            }
        }
        private void LoadDefaultSettings()
        {
            
        }
        private void SaveSettings()
        {
            using (FileStream stream = new FileStream(root + @"\" + savePath, FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Save));
                xml.Serialize(stream, new Save(
                    new SettingsSave(),
                    new DevToolsSave(devTools.pilotSelected, devTools.scenarioSelected, devTools.modsToLoad.ToArray())));
            }
            Console.Log("Saved Settings!");
        }

        private bool SettingsSaveExists()
        {
            return File.Exists(root + @"\" + savePath);
        }
        public bool CheckForArg(string arg)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().Equals(arg))
                    return true;
            }
            return false;
        }
        private void URICheck()
        {
            root = args[0];
            //This is removing the "\VTOLVR-ModLoader.exe" at the end, it will always be a fixed 21 characters
            root = root.Remove(root.Length - 21, 21);
            vtolFolder = root.Replace("VTOLVR_ModLoader", "");

            string argument = args[1].Remove(0, 11);
            if (argument.Contains("files"))
            {
                uriDownload = argument;
                uriSet = true;
            }
            else
                MessageBox.Show(argument, "URI Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CheckBaseFolder()
        {
            //Checking the folder which this is in
            string[] pathSplit = root.Split('\\');
            if (pathSplit[pathSplit.Length - 1] != "VTOLVR_ModLoader")
            {
                MessageBox.Show("It seems I am not in the folder \"VTOLVR_ModLoader\", place make sure I am in there other wise the in game menu won't load", "Wrong Folder");
                Quit();
            }

            //Now it should be in the correct folder, but just need to check if its in the games folder
            string vtolexe = root.Replace("VTOLVR_ModLoader", "VTOLVR.exe");
            if (!File.Exists(vtolexe))
            {
                MessageBox.Show("It seems the VTOLVR_ModLoader folder isn't with the other games files\nPlease move me to VTOL VR's game root directory.", "Wrong Folder Location");
                Quit();
            }

            CheckFolder();
        }
        /// <summary>
        /// Checks for files which the Mod Loader needs to work such as .dll files
        /// </summary>
        private void CheckFolder()
        {
            //Checking if the files we need to run are there
            foreach (string file in needFiles)
            {
                if (!File.Exists(root + @"\" + file))
                {
                    WrongFolder(file);
                    return;
                }
            }

            //Checking if the mods folder is there
            if (!Directory.Exists(root + modsFolder))
            {
                Directory.CreateDirectory(root + modsFolder);
            }

            //Checking the Managed Folder
            foreach (string file in neededDLLFiles)
            {
                if (!File.Exists(Directory.GetParent(root).FullName + @"\VTOLVR_Data" + file))
                {
                    MissingManagedFile(file);
                }
            }
        }
        private void WrongFolder(string file)
        {
            MessageBox.Show("I can't seem to find " + file + " in my folder. Make sure you place me in the same folder as this file.", "Missing File");
            Quit();
        }
        private void MissingManagedFile(string file)
        {
            MessageBox.Show("I can't seem to find " + file + " in VTOL VR > VTOLVR_Data, please make sure this file is here otherwise the mod loader won't work", "Missing File");
            Quit();
        }
        #endregion

        #region Auto Updater
        private void UpdatesProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage / 100, "Downloading data...");
        }
        private void UpdatesDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                if (File.Exists(root + updatesFile))
                    File.Delete(root + updatesFile);
                File.Move(root + updatesFileTemp, root + updatesFile);
                SetProgress(100, "Downloaded updates.xml");
            }
            else
            {
                SetProgress(100, "Failed to connect to server.");
                Console.Log("Failed getting feed \n" + e.Error.ToString());
                if (File.Exists(root + updatesFileTemp))
                    File.Delete(root + updatesFileTemp);
                SetPlayButton(true);
            }
            
            client.Dispose();
        }

        public bool CheckForInternet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return true;
                    }
                }

            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Launching Game
        private void OpenGame(object sender, RoutedEventArgs e)
        {
            if (isBusy)
                return;
            SetPlayButton(false);
            SetProgress(0, "Launching Game");
            GifState(gifStates.Play);
            SaveSettings();
            //Launching the game

            if ((devTools.pilotSelected != null && devTools.scenarioSelected != null) || 
                (devTools.modsToLoad != null && devTools.modsToLoad.Count > 0))
            {
                string regPath = (string)Registry.GetValue(
    @"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam",
    @"SteamPath",
    @"NULL");
                string args = string.Empty;
                if (devTools.pilotSelected != null && devTools.scenarioSelected != null &&
                    devTools.pilotSelected.Name != "No Selection" && devTools.scenarioSelected.Name != "No Selection")
                {
                    args += $" PILOT={devTools.pilotSelected.Name} SCENARIO_CID={devTools.scenarioSelected.cID} SCENARIO_ID={devTools.scenarioSelected.ID}";
                }


                if (devTools.modsToLoad != null && devTools.modsToLoad.Count > 0)
                {
                    for (int i = 0; i < devTools.modsToLoad.Count; i++)
                    {
                        args += " mod=" + devTools.modsToLoad[i] + ""; //Example " mod=NoGravity\NoGravity.dll "
                    }
                }

                Process process = new Process();
                process.StartInfo.FileName = regPath + @"\steam.exe";
                process.StartInfo.Arguments = @"-applaunch 667970" + args;
                process.Start();
            }
                
            else
            
            Process.Start("steam://run/667970");

            //Searching For Process
            WaitForProcess();

        }
        private void GifState(gifStates state, int frame = 0)
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
        private async void WaitForProcess()
        {
            int maxTries = 5;
            for (int i = 1; i <= maxTries; i++)
            {
                //Doing 5 tries to search for the process
                SetProgress(10 * i, "Searching for process...   (Attempt " + i + ")");
                await Task.Delay(5000);

                if (Process.GetProcessesByName("vtolvr").Length == 1)
                {
                    break;
                }

                if (i == maxTries)
                {
                    //If we couldn't find it, go back to how it was at the start
                    GifState(gifStates.Paused);
                    SetProgress(100, "Couldn't find VTOLVR process.");
                    SetPlayButton(false);
                    return;
                }
            }

            //A delay just to make sure the game has fully launched,
            SetProgress(50, "Waiting for game...");
            await Task.Delay(10000);

            //Injecting Default Mod
            SetProgress(75, "Injecting Mod Loader...");
            InjectDefaultMod();


            //Starting a new thread for the console
            console.StartTCPListener();
            //Thread tcpServer = new Thread(new ThreadStart(SetupConsole));
            //tcpServer.Start();
        }
        private void InjectDefaultMod()
        {
            //Injecting the default mod
            string defaultStart = string.Format("inject -p {0} -a {1} -n {2} -c {3} -m {4}", "vtolvr", "ModLoader.dll", "ModLoader", "Load", "Init");
            Process.Start(root + injector, defaultStart);
        }

        private void SetupConsole()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9999);
            listener.Start();

            //We only need to accept one client, the game. So need for loop
            TcpClient client = listener.AcceptTcpClient();
            Thread tcpHandlerThread = new Thread(new ParameterizedThreadStart(TCPHandler));
            tcpHandlerThread.Start(client);
        }

        private void TCPHandler(object client)
        {
            TcpClient mClient = (TcpClient)client;
            NetworkStream stream = mClient.GetStream();

            
            while(true)
            {
                byte[] message = new byte[mClient.ReceiveBufferSize];
                stream.Read(message, 0, message.Length);
                //Application.Current.Dispatcher.Invoke(new Action(() => { console.UpdateFeed(Encoding.ASCII.GetString(message)); }));
            }
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
            DirectoryInfo folder = new DirectoryInfo(root + modsFolder);
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
            DirectoryInfo folder = new DirectoryInfo(root + skinsFolder);
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
                if (autoStart)
                    OpenGame(null, null);
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
            if (autoStart)
                OpenGame(null, null);
        }

        private void MoveDependencies()
        {
            SetPlayButton(true);
            string[] modFolders = Directory.GetDirectories(root + modsFolder);

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

                            if (File.Exists(Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName))
                            {
                                string oldHash = CalculateMD5(Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                string newHash = CalculateMD5(depFiles[k]);
                                if (!oldHash.Equals(newHash))
                                {
                                    File.Copy(depFiles[k], Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName,
                                        true);
                                    movedDep++;
                                }
                            }
                            else
                            {
                                Console.Log("Moved file \n" + Directory.GetParent(root).FullName +
                                        @"\VTOLVR_Data\Managed\" + fileName);
                                File.Copy(depFiles[k], Directory.GetParent(root).FullName +
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
            client.DownloadFileAsync(new Uri(url + "/" + uriDownload), root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName);
        }

        private void FileDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                ShowNotification("Downloaded " + uriFileName);
                //Checking if they already had the mod extracted incase they wanted to update it
                bool isMod = uriDownload.Contains("mods");
                if (Directory.Exists(root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName.Split('.')[0]))
                {
                    Directory.Delete(root + (isMod ? modsFolder : skinsFolder) + @"\" + uriFileName.Split('.')[0], true);
                }
            }
            else
            {
                MessageBox.Show("Failed Downloading " + uriFileName + "\n" + e.Error.ToString(),
                    "Failed Downloading File", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void Quit()
        {
            Process.GetCurrentProcess().Kill();
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
            Process.Start(root);
            Console.Log("Mod Loader Folder Opened!");
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Quit();
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

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            if (settings == null)
                settings = new Settings();
            DataContext = settings;
        }

        private void UploadMod(object sender, RoutedEventArgs e)
        {
            //DataContext = new SettingsViewModel();
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
