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
        private static string[] args;
        private static Thread tcpListenerThread;
        private static TcpListener tcpListener;
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
        public static void ConnectToInstance()
        {
            if (CheckArgs("vtolvrml://", out string line))
            {
                Console.Log("Connecting to other instance");
                //Port in use. There must be another instant of the mod loader open.
                TcpClient client = new TcpClient("127.0.0.1", 9999);

                if (client == null || !client.Connected)
                {
                    Console.Log("Failed to connect to other instance");
                    return;
                }
                //lock ensure that no other threads try to use the stream at the same time.
                lock (client.GetStream())
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    writer.Write($"Command:{line}");
                    writer.Flush();
                }
                client.GetStream().Close();
                client.Close();
            }
            MainWindow.Quit();
        }

        public static void StartTCP()
        {
            tcpListenerThread = new Thread(new ThreadStart(Listener));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        }

        private static void Listener()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9999);
                tcpListener.Start();
                while (true)
                {
                    ThreadPool.QueueUserWorkItem(TCPClient, tcpListener.AcceptTcpClient());
                }
            }
            catch
            {
                
            }
            
        }
        private static void TCPClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream nwStream = client.GetStream();
            int num;
            byte[] array = new byte[32000];
            while (client.Connected && nwStream.CanRead)
            {
                num = nwStream.Read(array, 0, array.Length);
                if (num != 0)
                {
                    byte[] array2 = new byte[num];
                    Array.Copy(array, 0, array2, 0, num);
                    Application.Current.Dispatcher.Invoke(new Action(() => { Console.Log(Encoding.ASCII.GetString(array2), false); }));
                    Application.Current.Dispatcher.Invoke(new Action(() => { CheckTcpMessage(Encoding.ASCII.GetString(array2)); }));
                }
            }
        }

        private static void CheckTcpMessage(string message)
        {
            if (message.Contains("Command:"))
            {
                message = message.Replace("Command:", string.Empty);
                Console.Log($"Recevied command:{message}");
                if (message.Contains("vtolvrml://"))
                {
                    CheckURI(message);
                }
            }
        }
    }
}
