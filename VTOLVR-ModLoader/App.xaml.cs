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
        private void App_DispatcherUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException((Exception)e.ExceptionObject);
            MessageBox.Show(((Exception)e.ExceptionObject).Message);
        }
    }
}
