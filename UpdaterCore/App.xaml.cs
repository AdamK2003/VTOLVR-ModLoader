using System;
using System.Windows;
using System.Windows.Threading;
using Sentry;

namespace UpdaterCore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
#if !DEBUG
            this.DispatcherUnhandledException +=
 new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
#endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if !DEBUG
            SentrySdk.Init(UpdaterCore.Properties.Resources.Dsn);
#endif
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SentrySdk.Close();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string ErrorMessage = $@"Something went wrong!

{e.Exception.Message}

Would you like to share this crash with us? It'd help us tremendously.

The data that will be sent to us won't contain any identifiable information, only what went wrong and what you were trying to do.";

            MessageBoxResult result =
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                SentrySdk.CaptureException(e.Exception);
            }

            e.Handled = true;
            Environment.Exit(0);
        }
    }
}