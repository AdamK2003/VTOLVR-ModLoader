using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LauncherCore.Classes;

namespace LauncherCore.Views
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl
    {
        public static Console Instance { private set; get; }
        private static Queue<Feed> _consoleQueue = new Queue<Feed>();
        private static SolidColorBrush _warningBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        private static SolidColorBrush _errorBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private static SolidColorBrush _logBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        public List<Feed> ConsoleFeed = new List<Feed>();
        private List<string> _storedMessages = new List<string>();

        public Console()
        {
            InitializeComponent();
            Instance = this;
            inputBox.KeyDown += inputBoxKeyDown;
            for (int i = 0; i < _consoleQueue.Count; i++)
            {
                AddToFeed(_consoleQueue.Dequeue());
            }

            console.ItemsSource = Instance.ConsoleFeed.ToArray();
            scrollView.ScrollToBottom();
            Helper.SentryLog("Created Console", Helper.SentryLogCategory.Console);
        }

        private void inputBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendCommand(null, null);
        }

        public void UpdateFeed()
        {
            console.ItemsSource = ConsoleFeed;
            Instance.scrollView.ScrollToBottom();
        }

        public static void Log(string message, bool isApplication = true)
        {
            System.Console.WriteLine(message);
            if (isApplication)
                message = $"[{DateTime.Now}]{message}";
            if (Instance == null)
            {
                _consoleQueue.Enqueue(new Feed(message));
            }
            else
            {
                string[] lines = message.Split(new string[] {Environment.NewLine},
                    StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    Instance.AddToFeed(new Feed(lines[i]));
                }

                Instance.console.ItemsSource = Instance.ConsoleFeed.ToArray();
                Instance.scrollView.ScrollToBottom();
            }

            if (isApplication)
            {
                try
                {
                    File.AppendAllText(Path.Combine(Program.Root, Program.LogName),
                        $"{message}\n");
                }
                catch {}
            }
        }

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            if (CommunicationsManager.TcpServer == null)
                return;
            CommunicationsManager.TcpServer.BroadcastLine(inputBox.Text);
            inputBox.Text = string.Empty;
        }

        public static void GameClosed()
        {
            Log("Game Closed");
            Helper.SentryLog("VTOL VR has closed", Helper.SentryLogCategory.Console);
            Instance.inputBox.IsEnabled = false;
            Instance.sendButton.IsEnabled = false;
            MainWindow.SetPlayButton(false);
        }

        public static void GameOpened()
        {
            Helper.SentryLog("VTOL VR has opened", Helper.SentryLogCategory.Console);
            Instance.inputBox.Text = string.Empty;
            Instance.inputBox.IsEnabled = true;
            Instance.sendButton.IsEnabled = true;
            MainWindow.SetPlayButton(true);
        }

        public void AddToFeed(Feed feed)
        {
            if (ConsoleFeed == null)
                return;

            if (ConsoleFeed.Count > 0 && ConsoleFeed[ConsoleFeed.Count - 1].Colour == feed.Colour)
            {
                ConsoleFeed[ConsoleFeed.Count - 1].Message += $"\n{feed.Message}";
            }
            else
            {
                ConsoleFeed.Add(feed);
            }
        }

        public class Feed
        {
            public string Message { get; set; }
            public Brush Colour { get; set; }

            public Feed(string message)
            {
                Message = message;
                if (Message.Contains("[Warning]"))
                    Colour = _warningBrush;
                else if (Message.Contains("[Error]"))
                    Colour = _errorBrush;
                else
                    Colour = _logBrush;
            }
        }

        private void ClearConsole(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Clearing the console", Helper.SentryLogCategory.Console);
            ConsoleFeed = new List<Feed>();
            console.ItemsSource = ConsoleFeed.ToArray();
            scrollView.ScrollToBottom();
        }

        private void DeleteLog(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Deleting Console Log File", Helper.SentryLogCategory.Console);
            try
            {
                if (File.Exists(Path.Combine(Program.Root, Program.LogName)))
                    File.Delete(Path.Combine(Program.Root, Program.LogName));
            }
            catch (Exception error)
            {
                Log($"Error when trying to delete ({Path.Combine(Program.Root, Program.LogName)})\n{error}");
                Windows.Notification.Show("Error when deleting log\n" + error, "Error");
                return;
            }

            ConsoleFeed = new List<Feed>();
            console.ItemsSource = ConsoleFeed.ToArray();
            scrollView.ScrollToBottom();
        }

        private void OpenGameLog(object sender, RoutedEventArgs e)
        {
            string path = Helper.PlayerLogPath();
            if (!File.Exists(path))
            {
                Log($"Couldn't find {path}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo {FileName = path, UseShellExecute = true};
                Process.Start(psi);
                Log($"Opened Player.log");
            }
            catch (Exception error)
            {
                Log("Failed to open Player.log. " + error.Message);
            }
        }

        private void OpenLauncherLog(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Program.Root, Program.LogName);
            if (!File.Exists(path))
            {
                Log($"Couldn't find {path}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo {FileName = path, UseShellExecute = true};
                Process.Start(psi);
                Log($"Opened {Program.LogName}");
            }
            catch (Exception error)
            {
                Log("Failed to open Launcher Log. " + error.Message);
            }
        }
    }
}