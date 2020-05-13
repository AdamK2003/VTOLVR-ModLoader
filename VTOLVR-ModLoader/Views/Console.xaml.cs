﻿using System;
using System.Collections.Generic;
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

        public List<Feed> consoleFeed = new List<Feed>();
        public Console()
        {
            InitializeComponent();
            _instance = this;
        }

        public void StartTCPListener()
        {
            tcpListenerThread = new Thread(new ThreadStart(Listener));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        }

        private static void Listener()
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9999);
            tcpListener.Start();
            byte[] array = new byte[32000];
            Application.Current.Dispatcher.Invoke(new Action(() => { _instance.GameOpened(); }));
            while (true)
            {
                using (TcpClient = tcpListener.AcceptTcpClient())
                {
                    nwStream = TcpClient.GetStream();
                    int num;
                    while(TcpClient.Connected)
                    {
                        num = nwStream.Read(array, 0, array.Length);
                        if (num != 0)
                        {
                            byte[] array2 = new byte[num];
                            Array.Copy(array, 0, array2, 0, num);
                            Application.Current.Dispatcher.Invoke(new Action(() => { Log(Encoding.ASCII.GetString(array2)); }));
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(new Action(() => { _instance.GameClosed(); }));                
            }
        }

        public void UpdateFeed()
        {
            console.ItemsSource = consoleFeed.ToArray();
        }

        public static void Log(string message)
        {
            _instance.consoleFeed.Add(new Feed(message));
            _instance.console.ItemsSource = _instance.consoleFeed.ToArray();

        }
        private void SendCommand(object sender, RoutedEventArgs e)
        {
            if (!TcpClient.Connected)
            {
                Log("Connection with TCP client is false");
                GameClosed();
                return;
            }

            byte[] bytesToSend = Encoding.ASCII.GetBytes(inputBox.Text);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            inputBox.Text = string.Empty;
        }

        private void GameClosed()
        {
            Log("Game Closed");
            inputBox.IsEnabled = false;
            sendButton.IsEnabled = false;
        }

        private void GameOpened()
        {
            inputBox.Text = string.Empty;
            inputBox.IsEnabled = true;
            sendButton.IsEnabled = true;
        }

        public class Feed
        {
            public string message { get; set; }

            public Feed(string message)
            {
                this.message = message;
            }
        }
    }
}
