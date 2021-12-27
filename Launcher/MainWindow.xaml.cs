using System;
using System.Windows;
using System.Diagnostics;
using WpfAnimatedGif;
using Console = Launcher.Views.Console;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Launcher.Classes;
using Launcher.Views;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum gifStates { Paused, Play, Frame }

        public static MainWindow _instance;

        //Pages
        public News news { get; private set; }
        public Views.Settings settings { get; private set; }
        public DevTools devTools { get; private set; }
        public Views.Console console { get; private set; }
        public ProjectManager pManager { get; private set; }
        public Manager ItemManager { get; private set; }
        public Downloads Downloads { get; private set; }
        public Setup Setup { get; set; }
        
        // Notifications
        public enum Results { None, Ok, No, Yes, Cancel }

        public enum Buttons { None, Ok, NoYes, OkCancel }

        public MainWindow()
        {
            Helper.SentryLog("Program Opened", Helper.SentryLogCategory.MainWindow);
            _instance = this;
            if (!Startup.RunStartUp())
                return;

            if (CommunicationsManager.CheckArgs("uninstall", out string result))
            {
                Console.Log("Found uninstall argument");
                OpenSettings(null, null);
                settings.Uninstall();
                return;
            }

            CommunicationsManager.StartTCP(!Startup.SearchForProcess());
            Program.SetupAfterUI();
            InitializeComponent();
            HideNotification(true);
        }

        public void CreatePages()
        {
            Helper.SentryLog("Creating Pages", Helper.SentryLogCategory.MainWindow);
            console = new Views.Console();
            if (news == null)
                news = new News();
            settings = new Views.Settings();
            devTools = new DevTools();
            pManager = new ProjectManager();
            ItemManager = new Manager();
            if (Downloads == null)
                Downloads = new Downloads();
            DataContext = ItemManager;
        }

        public void RunSetup()
        {
            news = new News();
            Downloads = new Downloads();
            Setup = new Setup();
            DataContext = Setup;
            modsButton.IsEnabled = false;
            skinsButton.IsEnabled = false;
            homeButton.IsEnabled = false;
            openFButton.IsEnabled = false;
            uploadModButton.IsEnabled = false;
            devTButton.IsEnabled = false;
            settingsButton.IsEnabled = false;
            launchButton.IsEnabled = false;
            consoleButton.IsEnabled = false;
            downloadsButton.IsEnabled = false;
        }

        public static void SetProgress(int barValue, string text)
        {
            _instance.progressText.Text = text;
            _instance.progressBar.Value = barValue;
        }

        public static void SetPlayButton(bool disabled)
        {
            _instance.launchButton.Content = disabled ? "Busy" : "Play";
            Program.IsBusy = disabled;
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

        private void OpenDownloads(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Downloads", Helper.SentryLogCategory.MainWindow);
            if (Downloads == null)
                Downloads = new Downloads();
            DataContext = Downloads;
        }

        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opened Folder", Helper.SentryLogCategory.MainWindow);
            Process.Start("explorer.exe", Program.Root);
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
            bool isMods = button.Tag.ToString() != "Skins";
            ItemManager ??= new Manager();
            ItemManager.UpdateUI(isMods);
            _instance.DataContext = ItemManager;
        }

        public static void GoHome()
        {
            if (_instance.ItemManager == null)
                _instance.ItemManager = new Manager();
            _instance.ItemManager.UpdateUI(true);
            _instance.DataContext = _instance.ItemManager;
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
                image.UriSource = new Uri("/Launcher;component/Resources/LogoHalloweenSpinning.gif",
                    UriKind.Relative);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(LogoGif, image);
                Console.Log("Set Halloween Logo");
            }
            else if (now.Month.Equals(12))
            {
                Helper.SentryLog("Setting Christmas Logo", Helper.SentryLogCategory.MainWindow);

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("/Launcher;component/Resources/LogoChristmasSpinning.gif",
                    UriKind.Relative);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(LogoGif, image);
                Console.Log("Set Christmas Logo");
            }
        }

        private void ModLoaderEnableButton(object sender, RoutedEventArgs e)
        {
            settings.ToggleModLoader();
        }

        private void OpenDocs(object sender, RoutedEventArgs e)
        {
            Console.Log($"Opening Docs");
            Helper.SentryLog("Opening Docs", Helper.SentryLogCategory.MainWindow);
            Helper.OpenURL("https://docs.vtolvr-mods.com");
        }

        public static void ShowNotification(string message, TimeSpan time)
        {
            _instance.NotificationBackground.Visibility = Visibility.Visible;
            _instance.NotificationButtons.Visibility = Visibility.Collapsed;
            _instance.NotificationText.Visibility = Visibility.Visible;
            _instance.NotificationText.Text = message;
            
            DoubleAnimation animation = new DoubleAnimation(
                0f,
                85f, 
                TimeSpan.FromSeconds(0.3f));

            _instance.NotificationBackground.BeginAnimation(HeightProperty, animation);
            _instance.NotificationButtons.BeginAnimation(HeightProperty, animation);
            _instance.NotificationText.BeginAnimation(HeightProperty, animation);
            
            var timer = new DispatcherTimer
            {
                Interval = time
            };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                HideNotification();
                timer.Stop();
            };
        }

        public static void ShowNotification(string message, Buttons buttons,
            Action<Results> userDecided)
        {
            _instance.NotificationBackground.Visibility = Visibility.Visible;
            _instance.NotificationButtons.Visibility = Visibility.Visible;
            _instance.NotificationText.Visibility = Visibility.Visible;
            _instance.NotificationText.Text = message;
            
            switch (buttons)
            {
                case Buttons.Ok:
                    _instance.NotificationButtonMiddle.Visibility = Visibility.Visible;
                    _instance.NotificationButtonMiddle.Content = "Ok";
                    _instance.NotificationButtonMiddle.Click += OkClicked;
                    _instance.NotificationButtonMiddle.Tag = userDecided;
                    break;
                case Buttons.NoYes:
                    _instance.NotificationButtonTop.Visibility = Visibility.Visible;
                    _instance.NotificationButtonBottom.Visibility = Visibility.Visible;
                    
                    _instance.NotificationButtonTop.Content = "Yes";
                    _instance.NotificationButtonBottom.Content = "No";
                    
                    _instance.NotificationButtonTop.Click += YesClicked;
                    _instance.NotificationButtonBottom.Click += NoClicked;
                    _instance.NotificationButtonTop.Tag = userDecided;
                    _instance.NotificationButtonBottom.Tag = userDecided;
                    break;
                case Buttons.OkCancel:
                    _instance.NotificationButtonTop.Visibility = Visibility.Visible;
                    _instance.NotificationButtonBottom.Visibility = Visibility.Visible;
                    
                    _instance.NotificationButtonTop.Content = "Ok";
                    _instance.NotificationButtonBottom.Content = "Cancel";
                    
                    _instance.NotificationButtonTop.Click += OkClicked;
                    _instance.NotificationButtonBottom.Click += CanceledClicked;
                    _instance.NotificationButtonTop.Tag = userDecided;
                    _instance.NotificationButtonBottom.Tag = userDecided;
                    break;
            }
            
            DoubleAnimation animation = new DoubleAnimation(
                0f,
                85f, 
                TimeSpan.FromSeconds(0.3f));
            
            _instance.NotificationBackground.BeginAnimation(HeightProperty, animation);
            _instance.NotificationButtons.BeginAnimation(HeightProperty, animation);
            _instance.NotificationText.BeginAnimation(HeightProperty, animation);
            
            
        }

        private static void CanceledClicked(object sender, RoutedEventArgs e)
        {
            _instance.NotificationButtonBottom.Click -= CanceledClicked;
            HideNotification();
            
            // The callback is stored in the tag of each button
            Button button = sender as Button;
            Action<Results> callback = button.Tag as Action<Results>;
            callback?.Invoke(Results.Cancel);
        }

        private static void NoClicked(object sender, RoutedEventArgs e)
        {
            _instance.NotificationButtonBottom.Click -= NoClicked;
            HideNotification();
            
            // The callback is stored in the tag of each button
            Button button = sender as Button;
            Action<Results> callback = button.Tag as Action<Results>;
            callback?.Invoke(Results.No);
        }

        private static void YesClicked(object sender, RoutedEventArgs e)
        {
            _instance.NotificationButtonTop.Click -= YesClicked;
            HideNotification();
            
            // The callback is stored in the tag of each button
            Button button = sender as Button;
            Action<Results> callback = button.Tag as Action<Results>;
            callback?.Invoke(Results.Yes);
        }

        private static void OkClicked(object sender, RoutedEventArgs e)
        {
            _instance.NotificationButtonMiddle.Click -= OkClicked;
            HideNotification();
            
            // The callback is stored in the tag of each button
            Button button = sender as Button;
            Action<Results> callback = button.Tag as Action<Results>;
            callback?.Invoke(Results.Ok);
        }

        public static void HideNotification(bool skipAnimation = false)
        {
            if (skipAnimation)
            {
                _instance.NotificationBackground.Visibility = Visibility.Collapsed;
                _instance.NotificationButtons.Visibility = Visibility.Collapsed;
                _instance.NotificationText.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonTop.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonMiddle.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonBottom.Visibility = Visibility.Collapsed;
                return;
            }

            DoubleAnimation animation = new DoubleAnimation(
                85f,
                0f,
                TimeSpan.FromSeconds(0.3f));

            _instance.NotificationBackground.BeginAnimation(HeightProperty, animation);
            _instance.NotificationButtons.BeginAnimation(HeightProperty, animation);
            _instance.NotificationText.BeginAnimation(HeightProperty, animation);

            var timer = new DispatcherTimer { Interval = animation.Duration.TimeSpan };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                _instance.NotificationBackground.Visibility = Visibility.Collapsed;
                _instance.NotificationButtons.Visibility = Visibility.Collapsed;
                _instance.NotificationText.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonTop.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonMiddle.Visibility = Visibility.Collapsed;
                _instance.NotificationButtonBottom.Visibility = Visibility.Collapsed;
                timer.Stop();
            };

        }
    }
}