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
using Sentry;
using System.Windows.Media.Imaging;
using System.Drawing;

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
        public Manager ItemManager { get; private set; }

        public MainWindow()
        {
            Helper.SentryLog("Program Opened", Helper.SentryLogCategory.MainWindow);
            _instance = this;
            if (!Startup.RunStartUp())
                return;
            CommunicationsManager.StartTCP(!Startup.SearchForProcess());
            Program.SetupAfterUI();
            InitializeComponent();

        }
        public void CreatePages()
        {
            Helper.SentryLog("Creating Pages", Helper.SentryLogCategory.MainWindow);
            console = new Console();
            news = new News();
            settings = new Views.Settings();
            devTools = new DevTools();
            pManager = new ProjectManager();
            ItemManager = new Manager();
            DataContext = ItemManager;
        }
        public static void SetProgress(int barValue, string text)
        {
            _instance.progressText.Text = text;
            _instance.progressBar.Value = barValue;
        }
        public static void SetPlayButton(bool disabled)
        {
            _instance.launchButton.Content = disabled ? "Busy" : "Play";
            Program.isBusy = disabled;
        }
        public static void GifState(gifStates state, int frame = 0)
        {
            //Changing the gif's state
            var controller = ImageBehavior.GetAnimationController(_instance.LogoGif);
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

        private void Website(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Website", Helper.SentryLogCategory.MainWindow);
            Process.Start("https://vtolvr-mods.com");
            Console.Log("Website Opened!");
        }
        private void Discord(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Discord", Helper.SentryLogCategory.MainWindow);
            Process.Start("https://discord.gg/49HDD7m");
            Console.Log("Discord Opened!");
        }
        private void Patreon(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Patreon", Helper.SentryLogCategory.MainWindow);
            Process.Start("https://www.patreon.com/vtolvrmods");
            Console.Log("Patreon Opened!");
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Folder", Helper.SentryLogCategory.MainWindow);
            Process.Start(Program.root);
            Console.Log("Mod Loader Folder Opened!");
        }
        private void OpenGame(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Game", Helper.SentryLogCategory.MainWindow);
            Program.Queue(Program.LaunchGame);
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Settings", Helper.SentryLogCategory.MainWindow);
            if (settings == null)
                settings = new Views.Settings();
            settings.UpdateButtons();
            DataContext = settings;
        }

        public void Creator(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Creator", Helper.SentryLogCategory.MainWindow);
            if (pManager == null)
                pManager = new ProjectManager();
            pManager.SetUI();
            DataContext = pManager;
        }

        private void News(object sender, RoutedEventArgs e)
        {
            News();
        }
        public static void News()
        {
            Helper.SentryLog("Opened News", Helper.SentryLogCategory.MainWindow);
            if (_instance.news == null)
                _instance.news = new News();
            _instance.DataContext = _instance.news;
        }

        private void OpenTools(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Dev Tools", Helper.SentryLogCategory.MainWindow);
            if (devTools == null)
                devTools = new DevTools();
            devTools.SetUI();
            DataContext = devTools;
        }

        private void OpenConsole(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Console", Helper.SentryLogCategory.MainWindow);
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
        public static void DevToolsWarning(bool visable)
        {
            _instance.DevToolsText.Visibility = visable ? Visibility.Visible : Visibility.Hidden;
        }
        private void Manager(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Manager", Helper.SentryLogCategory.MainWindow);
            Button button = (Button)sender;
            bool isMods = true;
            if (button.Tag.ToString() == "Skins")
                isMods = false;
            if (ItemManager == null)
                ItemManager = new Manager();
            ItemManager.UpdateUI(isMods);
            _instance.DataContext = ItemManager;
        }

        public void CheckForEvent()
        {
            Helper.SentryLog("Checking for event", Helper.SentryLogCategory.MainWindow);
            DateTime now = DateTime.Now;
            if (now.Month.Equals(10))
            {
                Helper.SentryLog("Setting halloween logo", Helper.SentryLogCategory.MainWindow);

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("/VTOLVR-ModLoader;component/Resources/LogoHalloweenSpinning.gif", UriKind.Relative);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(LogoGif, image);
                Console.Log("Set Halloween Logo");
            }
            else if (now.Month.Equals(12))
            {
                Helper.SentryLog("Setting Christmas Logo", Helper.SentryLogCategory.MainWindow);

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("/VTOLVR-ModLoader;component/Resources/LogoChristmasSpinning.gif", UriKind.Relative);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(LogoGif, image);
                Console.Log("Set Christmas Logo");
            }
        }
    }
}
