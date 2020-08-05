using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
        }
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show($"Sorry, it seems that we have crashed.\n" +
                $"If this continues to happen, you can report it in the " +
                $"modding discord or email support@vtolvr-mods.com by sending " +
                $"a print screen of this message box with a short description of " +
                $"what you were trying to do.\n\n" +
                $"Crash at {DateTime.Now} on {Program.ProgramName}\nMessage:{e.Message}\nStackTrack:{e.StackTrace}", $"CRASH {Program.ProgramName}",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
