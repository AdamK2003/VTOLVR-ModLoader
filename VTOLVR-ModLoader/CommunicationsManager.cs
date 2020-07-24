﻿/* 
CommunicationsManager is meant to be the main class which recives the outside messages and converts them
It handles the command line arguments and other instances of the mod loader talking to it.

Some other classes though may talk to others such as the console page.

Possiable URI's
- token/ndkahjsbdjahbfsdf

 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VTOLVR_ModLoader.Windows;

namespace VTOLVR_ModLoader
{
    static class CommunicationsManager
    {
        private static string[] args;

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

        public static void CheckURI()
        {
            if (CheckArgs("vtolvrml", out string line))
            {
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
                    case "mod":
                        Notification.Show("mod");
                        break;
                    case "skin":
                        Notification.Show("skin");
                        break;

                }
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
    }
}
