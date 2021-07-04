using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LauncherCore.Classes;
using LauncherCore.Windows;
using Core.Jsons;

namespace LauncherCore.Views
{
    /// <summary>
    /// Interaction logic for EditProject.xaml
    /// </summary>
    public partial class EditProject : UserControl
    {
        public Action<bool, string> previewImageCallBack, webImageCallBack;
        private BaseItem _item;
        private string _currentPath;
        private bool _isMod;

        public EditProject(string path)
        {
            _currentPath = path;
            InitializeComponent();

            _isMod = Directory.Exists(_currentPath + @"\Builds");

            if (!File.Exists(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json")))
            {
                Notification.Show("Missing info.json", "Error");
                MainWindow._instance.Creator(null, null);
                return;
            }

            _item = Helper.GetBaseItem(_currentPath + (_isMod ? @"\Builds" : string.Empty));
            projectName.Text = _item.Name;
            tagline.Text = _item.Tagline;
            projectDescription.Text = _item.Description;

            if (File.Exists(_currentPath + (_isMod ? @"\Builds\" : @"\") + _item.PreviewImage))
            {
                previewImage.Source = new BitmapImage().LoadImage(
                    _currentPath + (_isMod ? @"\Builds\" : @"\") + _item.PreviewImage);
                previewImageText.Visibility = Visibility.Hidden;
            }

            if (File.Exists(_currentPath + @"\" + _item.WebPreviewImage))
            {
                webPageImage.Source = new BitmapImage().LoadImage(
                    _currentPath + @"\" + _item.WebPreviewImage);
                webPageImageText.Visibility = Visibility.Hidden;
            }

            isPublic.IsChecked = _item.IsPublic;
            unlisted.IsChecked = _item.Unlisted;

            if (_isMod)
                LoadMod();
            else
                LoadSkin();

            previewImageCallBack += PreviewImageCallBack;
            webImageCallBack += WebImageCallBack;
            CheckForInternet();
            Helper.SentryLog("Created Edit Page", Helper.SentryLogCategory.EditProject);
        }

        public async void CheckForInternet()
        {
            Helper.SentryLog("Checking for internet", Helper.SentryLogCategory.EditProject);
            if (!await HttpHelper.CheckForInternet())
            {
                saveButton.Content = "Save Locally (Can't connect to server)";
            }
        }

        private void LoadMod()
        {
            Helper.SentryLog("Loading Mod", Helper.SentryLogCategory.EditProject);
            sourceText.Visibility = Visibility.Visible;
            modSource.Visibility = Visibility.Visible;

            modSource.Text = _item.Source;
        }

        private void LoadSkin()
        {
            Helper.SentryLog("Loading Skin", Helper.SentryLogCategory.EditProject);
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
            Helper.SentryLog("Saving", Helper.SentryLogCategory.EditProject);
            UpdateDependencies();
            SaveProject();
        }

        private void UploadDataComplete(object sender, UploadValuesCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                Notification.Show(e.Result.Length + "");
            }
            else
            {
                Console.Log("Error:\n" + e.Error.ToString());
                Notification.Show(e.Error.ToString());
            }
        }

        private void SaveProject()
        {
            Helper.SentryLog("Saving Project", Helper.SentryLogCategory.EditProject);
            saveButton.IsEnabled = false;
            saveButton.Content = "Saving...";
            _item.Name = projectName.Text;
            _item.Tagline = tagline.Text;
            _item.Description = projectDescription.Text;
            _item.Version = projectVersion.Text;
            if (_isMod)
                _item.Source = modSource.Text;
            _item.LastEdit = DateTime.Now.Ticks;
            _item.IsPublic = isPublic.IsChecked.Value;
            _item.Unlisted = unlisted.IsChecked.Value;

            _item.SaveFile();

            if (_item.HasPublicID() && !Program.DisableInternet)
            {
                if (string.IsNullOrWhiteSpace(_item.WebPreviewImage))
                {
                    Console.Log(
                        $"Not submitting changes to website because missing website \"{ProjectManager.jWImage}\"");
                }
                else if (string.IsNullOrWhiteSpace(_item.PreviewImage))
                {
                    Console.Log(
                        $"Not submitting changes to website because missing website \"{ProjectManager.jPImage}\"");
                }
                else
                {
                    Helper.SentryLog("Submitting changes to website", Helper.SentryLogCategory.EditProject);
                    Console.Log("Submitting changes to website");
                    HttpHelper form =
                        new HttpHelper(
                            $"{Program.URL + Program.ApiURL + (_isMod ? Program.ModsURL : Program.SkinsURL)}/{_item.PublicID}/");
                    form.SetToken(Settings.Token);
                    _item.FilloutForm(ref form, _isMod, _currentPath);
                    form.SendDataAsync(HttpHelper.HttpMethod.PUT, UpdateSent);
                    return;
                }
            }

            Console.Log("Saved Project!");
            saveButton.Content = "Saved (Locally)";
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                saveButton.Content = "Save";
                saveButton.IsEnabled = true;
            };
        }

        private async void UpdateSent(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Notification.Show(
                    $"Failed to update your {(_isMod ? "mod" : "skin")} on the website.\nError Code: {response.StatusCode}\nChanges have been saved locally, please try again later.",
                    "Failed to update on website");
                Console.Log("There was an error when trying to submit the saved data to the website.\n" +
                            $"Error Code: {response.StatusCode}\n" +
                            $"URL: {Program.URL + Program.ApiURL + (_isMod ? Program.ModsURL : Program.SkinsURL)}/{_item.PublicID}/\n" +
                            $"Raw Response: {await response.Content.ReadAsStringAsync()}");
                saveButton.Content = "Save";
                return;
            }

            Console.Log("Project has been synced with vtolvr-mods.com");
            saveButton.Content = "Saved and Updated on vtolvr-mods.com";
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                saveButton.Content = "Save";
                saveButton.IsEnabled = true;
            };
        }

        private void PreviewImageButton(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Preview Image Button Pressed", Helper.SentryLogCategory.EditProject);
            FileDialog.Dialog(Directory.GetCurrentDirectory(), previewImageCallBack, new string[] {"png"});
        }

        private void WebPageImageButton(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Web Preview Image Button Pressed", Helper.SentryLogCategory.EditProject);
            FileDialog.Dialog(Directory.GetCurrentDirectory(), webImageCallBack, new string[] {"png"});
        }

        public void PreviewImageCallBack(bool selected, string filePath)
        {
            if (!selected)
                return;

            if (!PreviewImageChecks(filePath))
                return;

            if (File.Exists(_currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png"))
                File.Delete(_currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png");
            File.Copy(filePath, _currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png");
            filePath = _currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png";

            _item.PreviewImage = "preview.png";

            previewImageText.Visibility = Visibility.Hidden;

            previewImage.Source = new BitmapImage().LoadImage(filePath);
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

            _item.WebPreviewImage = "web_preview.png";

            webPageImageText.Visibility = Visibility.Hidden;

            webPageImage.Source = new BitmapImage().LoadImage(filePath);
            SaveProject();
        }

        private bool PreviewImageChecks(string path)
        {
            Helper.SentryLog("Preview Image Checks", Helper.SentryLogCategory.EditProject);
            Bitmap image = new Bitmap(path);
            FileInfo info = new FileInfo(path);

            if (info.Length > 2000000)
            {
                Notification.Show($"{info.Name} is over 2mb", "File not excepted");
                return false;
            }

            if (image.Height != image.Width)
            {
                Notification.Show($"{info.Name} is not a square image\n{image.Width}x{image.Height}",
                    "File not excepted");
                return false;
            }

            return true;
        }

        private bool WebImageChecks(string path)
        {
            Helper.SentryLog("Web Image Checks", Helper.SentryLogCategory.EditProject);
            FileInfo info = new FileInfo(path);

            if (info.Length > 2000000)
            {
                Notification.Show($"{info.Name} is over 2mb", "File not excepted");
                return false;
            }

            return true;
        }

        private void UnlistedChanged(object sender, RoutedEventArgs e)
        {
            if (_item != null)
                _item.Unlisted = unlisted.IsChecked.Value;
        }

        private void PublicChanged(object sender, RoutedEventArgs e)
        {
            if (_item != null)
                _item.IsPublic = isPublic.IsChecked.Value;
        }

        private void UpdateDependencies()
        {
            Helper.SentryLog("Updating Dependencies", Helper.SentryLogCategory.EditProject);
            if (!_isMod)
            {
                return;
            }

            if (!Directory.Exists(_currentPath + @"\Dependencies"))
            {
                Console.Log($"There is no dependencies folder in {_currentPath}");
                return;
            }

            FileInfo[] dependencies = new DirectoryInfo(_currentPath + @"\Dependencies").GetFiles("*.dll");
            JArray jArray = JArray.Parse(Properties.Resources.BaseDLLS);
            List<string> newDependencies = new List<string>();
            bool hasDefaultDep = false;
            for (int i = 0; i < dependencies.Length; i++)
            {
                hasDefaultDep = false;
                for (int j = 0; j < jArray.Count; j++)
                {
                    if (jArray[j].ToString().Equals(dependencies[i].Name))
                        hasDefaultDep = true;
                }

                if (dependencies[i].Name.Equals("ModLoader.dll") || hasDefaultDep)
                    continue;
                newDependencies.Add(dependencies[i].Name);
            }

            _item.Dependencies = newDependencies;
        }
    }
}