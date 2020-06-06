using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Windows;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for EditProject.xaml
    /// </summary>
    public partial class EditProject : UserControl
    {
        public Action<bool, string> previewImageCallBack, webImageCallBack;
        private JObject _currentJson;
        private string _currentPath;
        private bool _isMod;

        public EditProject(string path)
        {
            _currentPath = path;
            InitializeComponent();

            _isMod = Directory.Exists(_currentPath + @"\Builds");

            if (!File.Exists(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json")))
            {
                Notification.Show("Missing info.json","Error");
                MainWindow._instance.Creator(null, null);
                return;
            }

            try
            {
                _currentJson = JObject.Parse(File.ReadAllText(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json")));
            }
            catch (Exception e)
            {
                Notification.Show("Failed to Parse info.json", "Error");
                Console.Log("Failed to parse info.json\n" + e.ToString());
                MainWindow._instance.Creator(null, null);
                return;
            }

            if (_currentJson["Name"] != null)
            {
                projectName.Text = _currentJson["Name"].ToString();
            }
            if (_currentJson["Description"] != null)
            {
                projectDescription.Text = _currentJson["Description"].ToString();
            }
            if (_currentJson["Preview Image"] != null)
            {
                previewImage.Source = new BitmapImage(
                    new Uri(_currentPath + @"\" + _currentJson["Preview Image"].ToString()));
                previewImageText.Visibility = Visibility.Hidden;
            }
            if (_currentJson["Web Preview Image"] != null)
            {
                webPageImage.Source = new BitmapImage(
                    new Uri(_currentPath + @"\" + _currentJson["Web Preview Image"].ToString()));
                webPageImageText.Visibility = Visibility.Hidden;
            }

            if (_isMod)
                LoadMod();
            else
                LoadSkin();

            previewImageCallBack += PreviewImageCallBack;
            webImageCallBack += WebImageCallBack;
        }

        private void LoadMod()
        {
            sourceText.Visibility = Visibility.Visible;
            modSource.Visibility = Visibility.Visible;

            if (_currentJson["Source"] != null)
            {
                modSource.Text = _currentJson["Source"].ToString();
            }
        }

        private void LoadSkin()
        {
            grid.RowDefinitions[6].Height = new GridLength(0);
            grid.RowDefinitions[7].Height = new GridLength(0);
        }

        private void ProjectNameChanged(object sender, TextChangedEventArgs e)
        {
            projectName.Text = projectName.Text.RemoveSpecialCharacters();
            projectName.CaretIndex = projectName.Text.Length;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            SaveProject();
        }

        private void Upload(object sender, RoutedEventArgs e)
        {

        }

        private void SaveProject()
        {
            _currentJson["Name"] = projectName.Text;
            _currentJson["Description"] = projectDescription.Text;
            if (_isMod)
                _currentJson["Source"] = modSource.Text;
            _currentJson["Last Edit"] = DateTime.Now.Ticks;

            try
            {
                File.WriteAllText(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json"), _currentJson.ToString());
            }
            catch (Exception e)
            {
                Notification.Show($"Failed to save project\n{e.Message}", "Error");
                Console.Log($"Failed to save project\n{e}");
                return;
            }
            Console.Log("Saved Project!");
        }

        private void PreviewImageButton(object sender, RoutedEventArgs e)
        {
            FileDialog.Dialog(Directory.GetCurrentDirectory(), previewImageCallBack, new string[] { "png" });
        }

        private void WebPageImageButton(object sender, RoutedEventArgs e)
        {
            FileDialog.Dialog(Directory.GetCurrentDirectory(), webImageCallBack, new string[] { "png" });
        }

        public void PreviewImageCallBack(bool selected, string filePath)
        {
            if (!selected)
                return;

            if (!PreviewImageChecks(filePath))
                return;

            if (File.Exists(_currentPath + @"\preview.png"))
                File.Delete(_currentPath + @"\preview.png");
            File.Copy(filePath, _currentPath + @"\preview.png");
            filePath = _currentPath + @"\preview.png";

            if (_currentJson["Preview Image"] != null)
                _currentJson["Preview Image"] = "preview.png";
            else
                _currentJson.Add("Preview Image", "preview.png");

            previewImageText.Visibility = Visibility.Hidden;

            previewImage.Source = new BitmapImage(new Uri(filePath));
            SaveProject();
        }
        public void WebImageCallBack(bool selected, string filePath)
        {
            if (!selected)
                return;

            if (!WebImageChecks(filePath))
                return;
            if (File.Exists(_currentPath + @"\web_preview.png"))
                File.Delete(_currentPath + @"\web_preview.png");
            File.Copy(filePath, _currentPath + @"\web_preview.png");
            filePath = _currentPath + @"\web_preview.png";

            if (_currentJson["Web Preview Image"] != null)
                _currentJson["Web Preview Image"] = "web_preview.png";
            else
                _currentJson.Add("Web Preview Image", "web_preview.png");

            webPageImageText.Visibility = Visibility.Hidden;

            webPageImage.Source = new BitmapImage(new Uri(filePath));
            SaveProject();
        }

        private bool PreviewImageChecks(string path)
        {
            Bitmap image = new Bitmap(path);
            FileInfo info = new FileInfo(path);

            if (info.Length > 1000000)
            {
                Notification.Show($"{info.Name} is over 1mb", "File not excepted");
                return false;
            }

            if (image.Height != image.Width)
            {
                Notification.Show($"{info.Name} is not a square image\n{image.Width}x{image.Height}", "File not excepted");
                return false;
            }

            return true;
        }

        private bool WebImageChecks(string path)
        {
            FileInfo info = new FileInfo(path);

            if (info.Length > 1000000)
            {
                Notification.Show($"{info.Name} is over 1mb", "File not excepted");
                return false;
            }
            return true;
        }
    }
}
