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
            using (SentrySdk.Init("https://f010662cc39a43e4bf370253027a17e4@o411102.ingest.sentry.io/5434499"))
            {
                int a = 0;
                int b = 1;
                int c = b / a;
                _instance = this;
                if (!Startup.RunStartUp())
                    return;
                CommunicationsManager.StartTCP(!Startup.SearchForProcess());
                Program.SetupAfterUI();
                InitializeComponent();
            }
        }
        public void CreatePages()
        {
            console = new Console();
            news = new News();
            settings = new Views.Settings();
            devTools = new DevTools();
            pManager = new ProjectManager();
            ItemManager = new Manager();
            DataContext = news;
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
            Program.Queue(Program.LaunchGame);
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
            News();
        }
        public static void News()
        {
            if (_instance.news == null)
                _instance.news = new News();
            _instance.DataContext = _instance.news;
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
        public static void DevToolsWarning(bool visable)
        {
            _instance.DevToolsText.Visibility = visable ? Visibility.Visible : Visibility.Hidden;
        }
        private void Manager(object sender, RoutedEventArgs e)
        {
            if (ItemManager == null)
                ItemManager = new Manager();
            ItemManager.UpdateUI();
            _instance.DataContext = ItemManager;
        }
    }
}
