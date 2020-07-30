/* 
CommunicationsManager is meant to be the main class which recives the outside messages and converts them
It handles the command line arguments and other instances of the mod loader talking to it.

Some other classes though may talk to others such as the console page.

Possiable URI's
- token/ndkahjsbdjahbfsdf
- mod/pub_id/filename.extention
- skin/pub_id/filename.extention

Possiable Args
- nointernet
- branch=branchname
- url=https://url.com
- novr
 */
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VTOLVR_ModLoader.Views;
using VTOLVR_ModLoader.Windows;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader.Classes
{
    static class CommunicationsManager
    {
        public static SimpleTcpServer TcpServer { get; private set; }
        public static SimpleTcpClient TcpClient { get; private set; }
        public static TcpClient GameTcpClient { get; private set; }
        private static TcpListener TcpListener;
        private static string[] args;
        private static Thread tcpListenerThread;
        private static string currentDownloadFile;

        private static void Setup()
        {
            args = Environment.GetCommandLineArgs();
            StringBuilder builder = new StringBuilder("Started with \"");
            for (int i = 0; i < args.Length; i++)
            {
                builder.Append(args[i] + " ");
            }
            builder.Append("\"");
            Console.Log(builder.ToString());
        }
        public static void CheckNoInternet()
        {
            if (CheckArgs("nointernet", out string line))
            {
                Views.Console.Log("Internet has been disabled");
                Program.disableInternet = true;
            }
        }
        public static void CheckCustomBranch()
        {
            if (CheckArgs("branch", out string line))
            {
                line = line.Replace("branch=", string.Empty);
                Program.branch = line;
                Program.ProgramName += $" [{Program.branch} Branch]";
            }
        }
        public static void CheckCustomURL()
        {
            if (CheckArgs("url", out string line))
            {
                line = line.Replace("url=", string.Empty);
                Program.url = line;
            }
        }

        public static void CheckURI(string lineOveride = "")
        {
            if (CheckArgs("vtolvrml", out string result) || !string.IsNullOrEmpty(lineOveride))
            {
                string line;
                if (!string.IsNullOrEmpty(lineOveride))
                    line = lineOveride;
                else
                    line = result;

                string[] split = line.Replace("vtolvrml:///", string.Empty).Split('/');
                if (split.Length == 2 || string.IsNullOrEmpty(split[2]))
                {
                    Notification.Show("It seems that URI was missing some extra details\n" + line, "Error with URI");
                    return;
                }

                switch (split[2])
                {
                    case "token":
                        MainWindow._instance.settings.SetUserToken(split[3]);
                        break;
                    case "mod": //URI EG: vtolvrml://mod/jwu6447r/nogravity.zip
                        Program.Queue(delegate
                        {
                            MainWindow.SetProgress(0, $"Downloading {split[4]}");
                            SetDownloadFile($"mods/{split[4]}");
                            HttpHelper.DownloadFile(
                                $"{Program.url}/download/{split[2]}/{split[3]}/",
                                Path.Combine(Program.root, "mods", split[4]),
                                DownloadProgress,
                                DownloadDone);
                        });
                        break;
                    case "skin":
                        Program.Queue(delegate
                        {
                            MainWindow.SetProgress(0, $"Downloading {split[4]}");
                            SetDownloadFile($"skins/{split[4]}");
                            HttpHelper.DownloadFile(
                                $"{Program.url}/download/{split[2]}/{split[3]}/",
                                Path.Combine(Program.root, "skins", split[4]),
                                DownloadProgress,
                                DownloadDone);
                        });
                        break;

                }
            }
        }
        private static void SetDownloadFile(string file)
        {
            currentDownloadFile = file;
        }
        private static void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            MainWindow._instance.progressBar.Value = e.ProgressPercentage;
        }
        private static void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                MainWindow.SetProgress(100, $"Ready");
                //Program.Queue(Program.ExtractMods);
            }
            else
            {
                MainWindow.SetProgress(100, $"Ready");
                Notification.Show($"{e.Error.Message}", "Error when downloading file");
                Console.Log("Error:\n" + e.Error.ToString());
                if (File.Exists(Path.Combine(Program.root, currentDownloadFile)))
                    File.Delete(Path.Combine(Program.root, currentDownloadFile));
            }
        }
        public static bool CheckSteamVR()
        {
            return !CheckArgs("novr", out string line);
        }

        public static bool CheckArgs(string search, out string contents)
        {
            if (args == null)
                Setup();

            contents = string.Empty;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains(search))
                {
                    contents = args[i];
                    return true;
                }
            }
            return false;
        }

        public static void StartTCP(bool isServer)
        {
            if (isServer)
            {
                try
                {
                    TcpServer = new SimpleTcpServer();
                    TcpServer.Start(IPAddress.Parse("127.0.0.1"), 9999);
                    TcpServer.DataReceived += TcpDataReceived;
                    TcpServer.ClientDisconnected += TcpClientDisconnected;
                }
                catch (Exception e)
                {
                    Console.Log($"Failed to start TCP Server.\n{e}");
                }
            }
            else
            {
                TcpClient = new SimpleTcpClient();
                TcpClient.Connect("127.0.0.1", 9999);
                if (CheckArgs("vtolvrml://", out string line))
                {
                    TcpClient.WriteLine($"Command:{line}");
                }
                TcpClient.Disconnect();
                MainWindow.Quit();
            }
        }

        private static void TcpClientDisconnected(object sender, TcpClient e)
        {
            if (GameTcpClient != null && e == GameTcpClient)
            {
                Console.GameClosed();
                MainWindow.GifState(MainWindow.gifStates.Paused);
                MainWindow.SetProgress(100, "Ready");
            }
        }

        private static void TcpDataReceived(object sender, Message e)
        {
            Console.Log(e.MessageString, false);
            if (e.MessageString.StartsWith("Command:"))
                ProcessCommand(e.MessageString);
        }

        private static void ProcessCommand(string message, TcpClient client = null)
        {
            message = message.Replace("Command:", string.Empty);
            Console.Log($"Recevied command:{message}");
            if (message.StartsWith("vtolvrml://"))
            {
                CheckURI(message);
            }
            else if (message.StartsWith("isgame") && client != null)
            {
                GameTcpClient = client;
                Console.Log("Connected to game");
                MainWindow.SetProgress(100, "Launched!");
                Console.GameOpened();
            }
        }
    }
}
