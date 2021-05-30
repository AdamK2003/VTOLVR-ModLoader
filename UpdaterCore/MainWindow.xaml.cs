using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace UpdaterCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;

        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();

        public MainWindow()
        {
            Instance = this;
            Program.Start();
            InitializeComponent();
        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            Console.Log("Opening Log");
            if (File.Exists(Program.Root + Program.LogPath))
                Process.Start(Program.Root + Program.LogPath);
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        private void Quit()
        {
            if (MessageBox.Show("Are you sure you want to quit?\nThis will stop the update where it currently is",
                "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        #region Moving Window

        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(Application.Current.MainWindow);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(Application.Current.MainWindow).X - lm.X;
                this.Top += Mouse.GetPosition(Application.Current.MainWindow).Y - lm.Y;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
        }

        private void TopBarLeave(object sender, MouseEventArgs e)
        {
            holdingDown = false;
        }

        #endregion
    }
}