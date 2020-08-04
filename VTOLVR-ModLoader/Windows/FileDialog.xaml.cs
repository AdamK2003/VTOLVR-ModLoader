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
using VTOLVR_ModLoader.Views;

namespace VTOLVR_ModLoader.Windows
{
    /// <summary>
    /// Interaction logic for FileDialog.xaml
    /// </summary>
    public partial class FileDialog : Window
    {
        private static FileDialog window;
        private Action<bool, string> callback;
        private string[] fileTypes;
        private string currentPath;
        private DirectoryInfo parent;
        public FileDialog(string startingPath, Action<bool, string> callback, string[] fileTypes)
        {
            InitializeComponent();
            this.callback = callback;
            if (fileTypes != null)
            {
                this.fileTypes = fileTypes;
                SetFileTypesText();
            }
            OpenPath(startingPath);
            SetLFolders();

            EventManager.RegisterClassHandler(typeof(TextBox),
                TextBox.KeyUpEvent,
                new KeyEventHandler(TextBox_KeyUp));
        }

        public static void Dialog(Action<bool, string> callback)
        {
            Dialog(Directory.GetCurrentDirectory(), callback);
        }
        public static void Dialog(string startingPath, Action<bool, string> callback)
        {
            Dialog(startingPath, callback, null);
        }
        public static void Dialog(string startingPath, Action<bool, string> callback, string[] fileTypes)
        {
            window = new FileDialog(startingPath, callback, fileTypes);
            window.Show();
        }

        private void SetFileTypesText()
        {
            StringBuilder builder = new StringBuilder("(");
            for (int i = 0; i < fileTypes.Length; i++)
            {
                builder.Append($".{fileTypes[i]}");
                if (i != fileTypes.Length - 1)
                    builder.Append(" , ");
            }
            builder.Append(")");
            typeText.Text = builder.ToString();
        }

        private void OpenPath(string path)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            currentPath = folder.FullName;
            urlBox.Text = currentPath;
            List<Item> items = new List<Item>();

            DirectoryInfo[] folders = folder.GetDirectories();
            for (int i = 0; i < folders.Length; i++)
            {
                items.Add(new Item(folders[i].FullName, folders[i].Name, true));
            }
            
            if (fileTypes != null)
            {
                for (int i = 0; i < fileTypes.Length; i++)
                {
                    FileInfo[] files = folder.GetFiles($"*.{fileTypes[i]}");
                    for (int y = 0; y < files.Length; y++)
                    {
                        items.Add(new Item(files[y].FullName, files[y].Name, false));
                    }
                }
            }
            else
            {
                FileInfo[] files = folder.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    items.Add(new Item(files[i].FullName, files[i].Name, false));
                }
            }
            
            
            this.folders.ItemsSource = items.ToArray();

            parent = folder.Parent;
            if (parent == null)
                backButton.IsEnabled = false;
        }

        private void SetLFolders()
        {
            List<Item> directories = new List<Item>();

            DirectoryInfo lastDirectory = new DirectoryInfo(Views.Settings.projectsFolder);
            directories.Add(new Item(lastDirectory.FullName, lastDirectory.Name, true));

            lastDirectory = new DirectoryInfo(Program.vtolFolder);
            directories.Add(new Item(lastDirectory.FullName, lastDirectory.Name, true));

            lastDirectory = new DirectoryInfo(Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory));
            directories.Add(new Item(lastDirectory.FullName, lastDirectory.Name, true));

            lastDirectory = new DirectoryInfo(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments));
            directories.Add(new Item(lastDirectory.FullName, lastDirectory.Name, true));

            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                directories.Add(new Item(drives[i].RootDirectory.FullName, drives[i].RootDirectory.Name, true));
            }

            lFolders.ItemsSource = directories.ToArray();
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

        private void Back(object sender, RoutedEventArgs e)
        {
            OpenPath(parent.FullName);
        }
        private void SelectedFile(string filePath)
        {
            if (callback != null)
                callback.Invoke(true, filePath);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (callback != null)
                callback.Invoke(false, string.Empty);
            Close();
        }

        private void FolderButton(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button.ToolTip.Equals("Folder"))
                OpenPath(button.Tag as string);
            else
                SelectedFile(button.Tag as string);
        }

        private static void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (window == null)
                return;

            if (e.Key != Key.Enter) return;
            e.Handled = true;

            if (Directory.Exists(window.urlBox.Text))
                window.OpenPath(window.urlBox.Text);
            else
                window.OpenPath(window.currentPath);
        }

        private class Item
        {
            public string FullName { get; set; }
            public string Name { get; set; }
            public string ToolTip { get; set; }

            public Item(string fullName, string name, bool isFolder)
            {
                FullName = fullName;
                Name = name;
                if (isFolder)
                    ToolTip = "Folder";
                else
                    ToolTip = "File";
            }
        }

    }
}
