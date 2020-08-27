using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl
    {
        public static Console Instance { private set; get; }
        private static Queue<Feed> _consoleQueue = new Queue<Feed>();
        private static SolidColorBrush WarningBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        private static SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private static SolidColorBrush LogBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

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
            if (Instance == null)
            {
                _consoleQueue.Enqueue(new Feed(message));
            }
            else
            {
                string[] lines = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
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
                    File.AppendAllText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), Program.LogName), $"[{DateTime.Now}]{message}\n");
                }
                catch (Exception e)
                {

                }
                
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
            Instance.inputBox.IsEnabled = false;
            Instance.sendButton.IsEnabled = false;
            MainWindow.SetPlayButton(false);
        }

        public static void GameOpened()
        {
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
                if (Message.StartsWith("[Warning]"))
                    Colour = WarningBrush;
                else if (Message.StartsWith("[Error]"))
                    Colour = ErrorBrush;
                else
                    Colour = LogBrush;

            }
        }

        private void ClearConsole(object sender, RoutedEventArgs e)
        {
            ConsoleFeed = new List<Feed>();
            console.ItemsSource = ConsoleFeed.ToArray();
            scrollView.ScrollToBottom();
        }

        private void DeleteLog(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(Path.Combine(Program.root, Program.LogName)))
                    File.Delete(Path.Combine(Program.root, Program.LogName));
            }
            catch (Exception error)
            {
                Log($"Error when trying to delete ({Path.Combine(Program.root, Program.LogName)})\n{error}");
                Windows.Notification.Show("Error when deleting log\n" + error, "Error");
                return;
            }
            ConsoleFeed = new List<Feed>();
            console.ItemsSource = ConsoleFeed.ToArray();
            scrollView.ScrollToBottom();
        }
    }
}
