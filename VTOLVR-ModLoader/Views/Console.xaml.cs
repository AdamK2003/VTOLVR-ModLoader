using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl
    {
        public static Console _instance { private set; get; }
        private static Thread tcpListenerThread;
        private static TcpListener tcpListener;
        private static TcpClient TcpClient;
        private static NetworkStream nwStream;
        private static Queue<Feed> consoleQueue = new Queue<Feed>();

        public List<Feed> consoleFeed = new List<Feed>();
        private List<string> storedMessages = new List<string>();
        public Console()
        {
            InitializeComponent();
            _instance = this;
            inputBox.KeyDown += inputBoxKeyDown;
            for (int i = 0; i < consoleQueue.Count; i++)
            {
                consoleFeed.Add(consoleQueue.Dequeue());
            }
            console.ItemsSource = _instance.consoleFeed.ToArray();
            scrollView.ScrollToBottom();
        }

        private void inputBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendCommand(null, null);
        }

        public void UpdateFeed()
        {
            console.ItemsSource = consoleFeed.ToArray();
            _instance.scrollView.ScrollToBottom();
        }

        public static void Log(string message, bool isApplication = true)
        {
            System.Console.WriteLine(message);
            if (_instance == null)
            {
                consoleQueue.Enqueue(new Feed(message));
            }
            else
            {
                string[] lines = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    _instance.consoleFeed.Add(new Feed(lines[i]));
                }
                _instance.console.ItemsSource = _instance.consoleFeed.ToArray();
                _instance.scrollView.ScrollToBottom();
            }

            if (isApplication)
            {
                try
                {
                    File.AppendAllText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), Program.LogName), $"[{DateTime.Now}]{message}\n");
                }
                catch 
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
            _instance.inputBox.IsEnabled = false;
            _instance.sendButton.IsEnabled = false;
            MainWindow.SetPlayButton(false);
        }

        public static void GameOpened()
        {
            _instance.inputBox.Text = string.Empty;
            _instance.inputBox.IsEnabled = true;
            _instance.sendButton.IsEnabled = true;
            MainWindow.SetPlayButton(true);
        }

        public struct Feed
        {
            public string message { get; set; }

            public Feed(string message)
            {
                this.message = message;
            }
        }
    }
}
