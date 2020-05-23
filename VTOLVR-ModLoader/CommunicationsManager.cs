/* 
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

namespace VTOLVR_ModLoader
{
    static class CommunicationsManager
    {
        private static string[] args;

        private static void Setup()
        {
            args = Environment.GetCommandLineArgs();
        }

        public static void CheckURI()
        {
            if (CheckArgs("vtolvrml", out string line))
            {
                string[] split = line.Replace("vtolvrml:///", string.Empty).Split('/');
                if (split.Length == 2 || string.IsNullOrEmpty(split[2]))
                {
                    MessageBox.Show("It seems that URI was missing some extra details\n" + line, "Error with URI");
                    return;
                }

                switch (split[2])
                {
                    case "token":
                        MainWindow._instance.settings.SetUserToken(split[3]);
                        break;
                    case "mod":
                        MessageBox.Show("mod");
                        break;
                    case "skin":
                        MessageBox.Show("skin");
                        break;

                }
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
