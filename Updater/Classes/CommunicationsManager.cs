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

namespace AutoUpdater.Classes
{
    static class CommunicationsManager
    {
        private static string[] args;

        private static void Setup()
        {
            Program.SentryLog($"Setup", Program.SentryLogCategory.CommunicationsManager);
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
                Program.SentryLog($"No internet command ran", Program.SentryLogCategory.CommunicationsManager);
                Console.Log("Internet has been disabled");
                Program.disableInternet = true;
            }
        }
        public static void CheckCustomBranch()
        {
            if (CheckArgs("branch", out string line))
            {
                Program.SentryLog($"Custom Branch Found", Program.SentryLogCategory.CommunicationsManager);
                line = line.Replace("branch=", string.Empty);
                Program.branch = line;
                Program.ProgramName += $" [{Program.branch} Branch]";
            }
        }
        public static void CheckCustomURL()
        {
            if (CheckArgs("url", out string line))
            {
                Program.SentryLog($"Custom url set", Program.SentryLogCategory.CommunicationsManager);
                line = line.Replace("url=", string.Empty);
                Program.url = line;
            }
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
    }
}
