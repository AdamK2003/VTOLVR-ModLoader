using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Threading;
using VTOLVR_ModLoader.Views;

namespace VTOLVR_ModLoader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SentrySdk.Init("https://3796b92207d5410d93fffdbc359ea279@o411102.ingest.sentry.io/5434499");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SentrySdk.Close();
        }

        private void App_DispatcherUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException((Exception)e.ExceptionObject);
            String ErrorMessage = "Something went wrong! The issue has been logged and the modloader will close now\n\n" + ((Exception)e.ExceptionObject).Message;
            MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
