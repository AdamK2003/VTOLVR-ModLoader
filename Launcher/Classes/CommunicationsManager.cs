/* 
CommunicationsManager is meant to be the main class which receives the outside messages and converts them
It handles the command line arguments and other instances of the mod loader talking to it.

Some other classes though may talk to others such as the console page.

Possible URI's
- token/ndkahjsbdjahbfsdf
- mod/pub_id/filename.extention
- skin/pub_id/filename.extention

Possible Arguments
- nointernet
- branch=branchname
- url=https://url.com
- novr
 */

using SimpleTCP;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using Launcher.Views;
using Launcher.Windows;
using Console = Launcher.Views.Console;

namespace Launcher.Classes
{
    static class CommunicationsManager
    {
        private const int TCPPORT = 12000;
        public static SimpleTcpServer TcpServer { get; private set; }
        public static SimpleTcpClient TcpClient { get; private set; }
        public static TcpClient GameTcpClient { get; private set; }
        private static string[] args;
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
            Views.Console.Log(builder.ToString());
        }

        public static void CheckNoInternet()
        {
            if (CheckArgs("nointernet", out string line))
            {
                Views.Console.Log("Internet has been disabled");
                Program.DisableInternet = true;
            }
        }

        public static void CheckCustomBranch()
        {
            if (CheckArgs("branch", out string line))
            {
                line = line.Replace("branch=", string.Empty);
                Program.Branch = line;
                Program.ProgramName += $" [{Program.Branch} Branch]";
            }
        }

        public static void CheckCustomURL()
        {
            if (CheckArgs("url", out string line))
            {
                line = line.Replace("url=", string.Empty);
                Program.URL = line;
            }
        }

        public static void CheckAutoUpdate()
        {
            if (CheckArgs("autoupdate", out string line))
            {
                line = line.Replace("autoupdate=", string.Empty);
                if (bool.TryParse(line, out bool result))
                {
                    Views.Settings.SetAutoUpdate(result);
                }
                else
                {
                    Views.Console.Log($"Failed to convert {line} to bool");
                }
            }
        }

        public static async void CheckURI(string lineOveride = "")
        {
            if (CheckArgs("vtolvrml", out string result) || !string.IsNullOrEmpty(lineOveride))
            {
                if (!await HttpHelper.CheckForInternet())
                    return;
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
                            Views.Console.Log($"Downloading {currentDownloadFile}");
                            Downloads.DownloadFile(
                                $"{Program.URL}/download/{split[2]}/{split[3]}/",
                                Path.Combine(Program.Root, "mods", split[4]),
                                null,
                                DownloadDone);
                        });
                        break;
                    case "skin":
                        Program.Queue(delegate
                        {
                            MainWindow.SetProgress(0, $"Downloading {split[4]}");
                            SetDownloadFile($"skins/{split[4]}");
                            Console.Log($"Downloading {currentDownloadFile}");
                            Downloads.DownloadFile(
                                $"{Program.URL}/download/{split[2]}/{split[3]}/",
                                Path.Combine(Program.Root, "skins", split[4]),
                                null,
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

        private static void DownloadDone(CustomWebClient.RequestData data)
        {
            string path = Path.Combine(Program.Root, currentDownloadFile);
            if (!data.Cancelled && data.Error == null)
            {
                MainWindow.SetProgress(100, $"Ready");
                Console.Log($"Downloaded {currentDownloadFile}");
                Helper.SentryLog($"Finished downloading {currentDownloadFile}",
                    Helper.SentryLogCategory.CommunicationsManager);
                Program.ExtractItem(path, true, 1);
            }
            else
            {
                Helper.SentryLog($"Error when downloading {currentDownloadFile}\n{data.Error.Message}",
                    Helper.SentryLogCategory.CommunicationsManager);
                MainWindow.SetProgress(100, $"Ready");
                Notification.Show($"{data.Error.Message}", "Error when downloading file");
                Console.Log("Error:\n" + data.Error.ToString());
                if (File.Exists(path))
                    File.Delete(path);
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
                    TcpServer.Start(IPAddress.Parse("127.0.0.1"), TCPPORT);
                    TcpServer.DataReceived += TcpDataReceived;
                    TcpServer.ClientDisconnected += TcpClientDisconnected;
                    Console.Log("TCP Server started!");
                }
                catch (Exception e)
                {
                    Console.Log($"Failed to start TCP Server.\n{e}");
                }
            }
            else
            {
                if (CheckArgs("vtolvrml://", out string line))
                {
                    try
                    {
                        TcpClient = new SimpleTcpClient();
                        TcpClient.Connect("127.0.0.1", TCPPORT);
                        Console.Log($"Passing \"{line}\" to other instance");
                        TcpClient.WriteLine($"Command:{line}");
                        TcpClient.Disconnect();
                    }
                    catch (Exception e)
                    {
                        Console.Log($"Failed to connect to other instance. Reason: {e.Message}");
                    }
                }

                Program.Quit("Another Instance Found");
            }
        }

        private static void TcpClientDisconnected(object sender, TcpClient e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (GameTcpClient != null && e == GameTcpClient)
                {
                    Console.GameClosed();
                    MainWindow.GifState(MainWindow.gifStates.Paused);
                    MainWindow.SetProgress(100, "Ready");
                }
            }));
        }

        private static void TcpDataReceived(object sender, Message e)
        {
            string[] lines = e.MessageString.Split(new string[] {Environment.NewLine},
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                //I have no clue what '' is but it keeps showing up on the TCP message.
                //I'm just trying to remove it here
                lines[i] = lines[i].Replace("", string.Empty);
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Console.Log(lines[i].Remove(lines[i].Length - 1), false);
                    if (lines[i].StartsWith("Command:"))
                        ProcessCommand(lines[i], e.TcpClient);
                }));
            }
        }

        private static void ProcessCommand(string message, TcpClient client)
        {
            message = message.Replace("Command:", string.Empty);
            Console.Log($"Received command:{message}");
            if (message.StartsWith("vtolvrml://"))
            {
                CheckURI(message);
            }
            else if (message.StartsWith("isgame"))
            {
                GameTcpClient = client;
                Console.Log("Connected to game");
                MainWindow.SetProgress(100, "Launched!");
                Console.GameOpened();
            }
        }
    }
}