using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Core.Enums;
using Core.Jsons;
using Launcher.Classes;
using Launcher.Windows;
using MdXaml;

namespace Launcher.Views
{
    /// <summary>
    /// Interaction logic for EditProject.xaml
    /// </summary>
    public partial class EditProject : UserControl
    {
        public Action<bool, string> _previewImageCallBack;
        public Action<bool, string> _webImageCallBack;
        private BaseItem _item;
        private string _currentPath => _item.Directory.FullName;
        private bool _isMod => _item.ContentType == ContentType.MyMods;
        
        // Changed values
        private bool _isPublic;
        private bool _unlisted;

        public EditProject(long lastEdit)
        {
            InitializeComponent();
            if (!TryGetItem(lastEdit))
            {
                Notification.Show("Can't find item", "Error");
                MainWindow._instance.Creator(null, null);
                return; 
            }
            SetValues();
            
            if (_isMod)
                LoadMod();
            else
                LoadSkin();

            _previewImageCallBack += PreviewImageCallBack;
            _webImageCallBack += WebImageCallBack;
            CheckForInternet();
            Helper.SentryLog("Created Edit Page", Helper.SentryLogCategory.EditProject);
            Title.Text = $"Editing {_item.Name}";
        }

        private bool TryGetItem(long lastEdit)
        {
            foreach (BaseItem item in Program.Items)
            {
                if (item.LastEdit != lastEdit)
                    continue;
                
                Console.Log(item.ToString());
                _item = item;
                return true;
            }

            return false;
        }

        private void SetValues()
        {
            NameInputBox.Text = _item.Name;
            TaglineInputBox.Text = _item.Tagline;
            DescriptionInputBox.Text = _item.Description;
            string path = Path.Combine(_item.Directory.FullName, _item.PreviewImage);
            Console.Log($"Searching for preview image ({_item.PreviewImage}) at {path}");
            if (File.Exists(path))
            {
                PreviewImage.Source = new BitmapImage().LoadImage(path);
            }

            path = Path.Combine(_currentPath, _item.WebPreviewImage);
            Console.Log($"Searching for web preview image ({_item.WebPreviewImage}) at {path}");
            if (File.Exists(path))
            {
                HeaderImage.Source = new BitmapImage().LoadImage(
                    _currentPath + @"\" + _item.WebPreviewImage);
            }

            SetIsPublic(_item.IsPublic);
            SetListed(_item.Unlisted);
            SourceCodeInputBox.Text = _item.Source;
            VersionInputBox.Text = _item.Version;
        }

        public async void CheckForInternet()
        {
            Helper.SentryLog("Checking for internet", Helper.SentryLogCategory.EditProject);
            if (!await HttpHelper.CheckForInternet())
            {
                SaveButton.Content = "Save Locally (Can't connect to server)";
            }
        }

        private void LoadMod()
        {
            Helper.SentryLog("Loading Mod", Helper.SentryLogCategory.EditProject);
            sourceText.Visibility = Visibility.Visible;
            modSource.Visibility = Visibility.Visible;

            modSource.Text = _item.Source;
            
            SkinsTitle.Visibility = Visibility.Collapsed;
            AddMaterialButton.Visibility = Visibility.Collapsed;
            MaterialsControl.Visibility = Visibility.Collapsed;
            SkinDivider.Visibility = Visibility.Collapsed;
        }

        private void LoadSkin()
        {
            Helper.SentryLog("Loading Skin", Helper.SentryLogCategory.EditProject);

            SourceCodeTitle.Visibility = Visibility.Collapsed;
            SourceCodeDescription.Visibility = Visibility.Collapsed;
            SourceCodeInputBox.Visibility = Visibility.Collapsed;
        }

        private void ProjectNameChanged(object sender, TextChangedEventArgs e)
        {
            NameInputBox.Text = NameInputBox.Text.RemoveSpecialCharacters();
            NameInputBox.CaretIndex = NameInputBox.Text.Length;
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
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Saving...";
            _item.Name = NameInputBox.Text;
            _item.Tagline = TaglineInputBox.Text;
            _item.Description = DescriptionInputBox.Text;
            _item.Version = VersionInputBox.Text;
            if (_isMod)
                _item.Source = modSource.Text;
            _item.LastEdit = DateTime.Now.Ticks;
            _item.IsPublic = _isPublic;
            _item.Unlisted = _unlisted;

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
            TimeSpan delay = TimeSpan.FromSeconds(2);
            var timer = new DispatcherTimer
            {
                Interval = delay
            };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                SaveButton.Content = "Save Changes";
                SaveButton.IsEnabled = true;
                timer.Stop();
            };
            MainWindow.ShowNotification("Saved Changes", delay);
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
                SaveButton.Content = "Save";
                return;
            }

            Console.Log("Project has been synced with vtolvr-mods.com");
            SaveButton.Content = "Saved and Updated on vtolvr-mods.com";
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                SaveButton.Content = "Save";
                SaveButton.IsEnabled = true;
            };
        }

        private void PreviewImageButton(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Preview Image Button Pressed", Helper.SentryLogCategory.EditProject);
            FileDialog.Dialog(Directory.GetCurrentDirectory(), _previewImageCallBack, new string[] {"png"});
        }

        private void WebPageImageButton(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Web Preview Image Button Pressed", Helper.SentryLogCategory.EditProject);
            FileDialog.Dialog(Directory.GetCurrentDirectory(), _webImageCallBack, new string[] {"png"});
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

            PreviewImage.Source = new BitmapImage().LoadImage(filePath);
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

            HeaderImage.Source = new BitmapImage().LoadImage(filePath);
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

        private void ProjectDescription_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (MarkdownViewer == null)
                return;
            MarkdownViewer.Markdown = DescriptionInputBox.Text;
        }

        private void SkinMaterialClicked(object sender, RoutedEventArgs e)
        {
            MaterialWindow window = new (new DirectoryInfo(_currentPath), "Test Material",ref _item);
            window.Show();
        }

        private void SaveProject(object sender, RoutedEventArgs e) => SaveProject();

        private void MakePublicPressed(object sender, RoutedEventArgs e) => SetIsPublic(!_isPublic);

        private void SetIsPublic(bool newValue)
        {
            if (_item == null)
                return;
            
            _isPublic = newValue;
            PublicButton.Content = newValue ? "Public" : "Private";
            PublicWarningText.Visibility = newValue ? Visibility.Collapsed : Visibility.Visible;
        }
        
        private void UnlistedPressed(object sender, RoutedEventArgs e) => SetListed(!_unlisted);

        private void SetListed(bool newValue)
        {
            if (_item == null)
                return;

            _unlisted = newValue;
            UnlistedButton.Content = newValue ? "Unlisted" : "Listed";
        }

        private void CancelChanges(object sender, RoutedEventArgs e) => CheckChanges();

        private void CheckChanges()
        {
            int changes = 0;

            if (!NameInputBox.Text.Equals(_item.Name))
                changes++;
            if (!TaglineInputBox.Text.Equals(_item.Tagline))
                changes++;
            if (!SourceCodeInputBox.Text.Equals(_item.Source))
                changes++;
            if (!DescriptionInputBox.Text.Equals(_item.Description))
                changes++;
            if (_isPublic != _item.IsPublic)
                changes++;
            if (_unlisted != _item.Unlisted)
                changes++;
            if (!VersionInputBox.Text.Equals(_item.Version))
                changes++;
            
            // Header Image
            // Preview Image
            // Skin Materials

            if (changes == 0)
            {
                Close();
                return;
            }
            
            MainWindow.ShowNotification(
                $"There is {changes} unsaved {(changes == 1? "change" : "changes")}. " +
                $"Are you sure you want to lose {(changes == 1? "this change" : "these changes")}?",
                MainWindow.Buttons.NoYes,
                UserDecided);
        }

        private void UserDecided(MainWindow.Results results)
        {
            if (results == MainWindow.Results.No)
            {
                MainWindow.HideNotification();
                return;
            }
            // The only other answer can be yes
            Close();
        }

        private void Close() =>  MainWindow._instance.Creator(null, null);
    }
}