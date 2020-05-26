using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VTOLVR_ModLoader.Windows
{
    /// <summary>
    /// Interaction logic for FolderDialog.xaml
    /// </summary>
    public partial class FolderDialog : Window
    {
        private string currentPath;
        private DirectoryInfo parent;
        private Action<bool, string> callBack;
        public FolderDialog(string startingPath, Action<bool, string> callback)
        {
            callBack = callback;
            InitializeComponent();
            OpenPath(startingPath);
            SetLFolders();

            EventManager.RegisterClassHandler(typeof(TextBox),
                TextBox.KeyUpEvent,
                new KeyEventHandler(TextBox_KeyUp));
        }

        public static void Dialog(string startingPath, Action<bool, string> callback)
        {
            FolderDialog window = new FolderDialog(startingPath, callback);
            window.Show();
        }

        private void OpenPath(string path)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            currentPath = folder.FullName;
            urlBox.Text = currentPath;
            DirectoryInfo[] folders = folder.GetDirectories();
            this.folders.ItemsSource = folders;

            parent = folder.Parent;
            if (parent == null)
                backButton.IsEnabled = false;
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            OpenPath(parent.FullName);
        }

        private void Selected(object sender, RoutedEventArgs e)
        {
            if (callBack != null)
                callBack.Invoke(true, currentPath);
            Close();
        }

        #region Moving Window
        //Moving Window
        private bool holdingDown;
        private Point lm = new Point();
        private void TopBarDown(object sender, MouseButtonEventArgs e)
        {
            holdingDown = true;
            lm = Mouse.GetPosition(this);
        }

        private void TopBarUp(object sender, MouseButtonEventArgs e)
        {
            holdingDown = false;
        }

        private void TopBarMove(object sender, MouseEventArgs e)
        {
            if (holdingDown)
            {
                this.Left += Mouse.GetPosition(this).X - lm.X;
                this.Top += Mouse.GetPosition(this).Y - lm.Y;
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

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (callBack != null)
                callBack.Invoke(false, string.Empty);
            Close();
        }

        private void FolderButton(object sender, RoutedEventArgs e)
        {
            OpenPath(((Button)sender).Tag as string);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;

            if (Directory.Exists(urlBox.Text))
                OpenPath(urlBox.Text);
            else
                OpenPath(currentPath);
        }

        private void SetLFolders()
        {
            List<DirectoryInfo> directories = new List<DirectoryInfo>();

            directories.Add(new DirectoryInfo(Program.vtolFolder));

            directories.Add(
                new DirectoryInfo(Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory)));

            directories.Add(
                new DirectoryInfo(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments)));

            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                directories.Add(drives[i].RootDirectory);
            }

            lFolders.ItemsSource = directories.ToArray();
        }
    }
}
