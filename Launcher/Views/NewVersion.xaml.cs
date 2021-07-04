using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Core.Jsons;
using Launcher.Classes;
using Launcher.Windows;

namespace Launcher.Views
{
    /// <summary>
    /// Interaction logic for NewVersion.xaml
    /// </summary>
    public partial class NewVersion : UserControl
    {
        private BaseItem _item;
        private string _currentPath;
        private bool _isMod;
        private bool _hasInternet = true;

        public NewVersion(string currentPath)
        {
            _currentPath = currentPath;
            InitializeComponent();

            _isMod = Directory.Exists(_currentPath + @"\Builds");

            if (!File.Exists(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json")))
            {
                Notification.Show("Missing info.json", "Error");
                MainWindow._instance.Creator(null, null);
                return;
            }

            _item = Helper.GetBaseItem(_currentPath + (_isMod ? @"\Builds" : string.Empty));

            if (_item.Version != "N/A")
                versionNumber.Text = _item.Version;

            if (!_item.HasPublicID())
            {
                changeLogTitle.Visibility = Visibility.Hidden;
                titleHeader.Visibility = Visibility.Hidden;
                title.Visibility = Visibility.Hidden;
                descriptionHeader.Visibility = Visibility.Hidden;
                description.Visibility = Visibility.Hidden;

                for (int i = 2; i < 7; i++)
                {
                    grid.RowDefinitions.RemoveAt(2);
                }

                approvalWarning.SetValue(Grid.RowProperty, 2);
                contentGuidelines.SetValue(Grid.RowProperty, 3);

                if (_item.PreviewImage != string.Empty ||
                    _item.WebPreviewImage != string.Empty)
                {
                    uploadButton.Content = "Release";
                    uploadButton.IsEnabled = true;
                }
                else
                {
                    uploadButton.Content = "Please fill out all sections in \"Edit Info\" before releasing";
                    uploadButton.IsEnabled = false;
                }
            }
            else
            {
                uploadButton.IsEnabled = false;
                uploadButton.Content = "Please fill out all the sections before uploading";
            }

            CheckForInternet();
            Helper.SentryLog("Created New Version page", Helper.SentryLogCategory.NewVersion);
        }

        public async void CheckForInternet()
        {
            Helper.SentryLog("Checking for internet", Helper.SentryLogCategory.NewVersion);
            if (!await HttpHelper.CheckForInternet())
            {
                uploadButton.IsEnabled = false;
                uploadButton.Content = "Disabled (Can't connect to server)";
                _hasInternet = false;
            }
        }

        private void VersionNumberChanged(object sender, TextChangedEventArgs e)
        {
            if (versionNumber != null && _item != null)
                _item.Version = versionNumber.Text;
        }

        private async void Upload(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Uploading", Helper.SentryLogCategory.NewVersion);
            uploadButton.IsEnabled = false;
            MainWindow.SetBusy(true);
            SaveProject();
            if (_item.HasPublicID())
                await UpdateProject();
            else
                await UploadNewProject();
        }

        private async Task UploadNewProject()
        {
            Helper.SentryLog("Uploading new Project", Helper.SentryLogCategory.NewVersion);
            uploadButton.Content = "Uploading...";
            if (_isMod && !AssemblyChecks())
            {
                MainWindow.SetBusy(false);
                return;
            }

            Console.Log("Zipping up project");
            string zipPath = ZipCurrentProject();
            Console.Log("Filling out form to submit");

            HttpHelper form =
                new HttpHelper(Program.URL + Program.ApiURL + (_isMod ? Program.ModsURL : Program.SkinsURL) + @"\");
            form.SetToken(Settings.Token);
            form.SetValue("version", _item.Version);
            form.SetValue("name", _item.Name);
            form.SetValue("tagline", _item.Tagline);
            form.SetValue("description", _item.Description);
            form.SetValue("unlisted", _item.Unlisted.ToString());
            form.SetValue("is_public", _item.IsPublic.ToString());
            if (_isMod)
                form.SetValue("repository", _item.Source);

            form.AttachFile("header_image", _item.WebPreviewImage, _currentPath + @"\" + _item.WebPreviewImage);
            form.AttachFile("thumbnail", _item.PreviewImage,
                _currentPath + (_isMod ? @"\Builds\" : @"\") + _item.PreviewImage);
            form.AttachFile("user_uploaded_file", $"{_item.Name}.zip", zipPath);

            Console.Log("Sending Data");
            HttpResponseMessage result = await form.SendDataAsync(HttpHelper.HttpMethod.POST);
            string content = await result.Content.ReadAsStringAsync();
            Console.Log("Raw Response\n" + content);
            APIResult(JObject.Parse(content));
        }

        private async Task UpdateProject()
        {
            Helper.SentryLog("Updating existing project", Helper.SentryLogCategory.NewVersion);
            uploadButton.Content = "Updating...";
            if (_isMod && !AssemblyChecks())
            {
                MainWindow.SetBusy(false);
                return;
            }

            Console.Log("Zipping up project");
            string zipPath = ZipCurrentProject();

            HttpHelper form = new HttpHelper(
                Program.URL + Program.ApiURL + (_isMod ? Program.ModsURL : Program.SkinsURL) + "/" + _item.PublicID +
                "/");
            form.SetToken(Settings.Token);
            form.SetValue("version", _item.Version);
            form.SetValue("name", _item.Name);
            form.SetValue("tagline", _item.Tagline);
            form.SetValue("description", _item.Description);
            form.SetValue("unlisted", _item.Unlisted.ToString());
            form.SetValue("is_public", _item.IsPublic.ToString());
            if (_isMod)
                form.SetValue("repository", _item.Source);

            form.AttachFile("header_image", _item.WebPreviewImage, _currentPath + @"\" + _item.WebPreviewImage);
            form.AttachFile("thumbnail", _item.PreviewImage,
                _currentPath + (_isMod ? @"\Builds\" : @"\") + _item.PreviewImage);
            form.AttachFile("user_uploaded_file", $"{_item.Name}.zip", zipPath);


            HttpResponseMessage result = await form.SendDataAsync(HttpHelper.HttpMethod.PUT);
            string response = await result.Content.ReadAsStringAsync();
            Console.Log(
                $"Raw Response from {Program.URL + Program.ApiURL + (_isMod ? Program.ModsURL : Program.SkinsURL) + "/" + _item.PublicID + "/"}\n{response}");
            JObject json = JObject.Parse(response);

            if (json["detail"] != null)
            {
                Notification.Show(json["detail"].ToString(), "Error");
                MainWindow.SetBusy(false);
                return;
            }

            HttpHelper form2 = new HttpHelper(Program.URL + Program.ApiURL +
                                              (_isMod ? Program.ModsChangelogsURL : Program.SkinsChangelogsURL) + "/" +
                                              _item.PublicID + "/");
            form2.SetToken(Settings.Token);
            form2.SetValue("change_name", title.Text);
            form2.SetValue("change_log", description.Text);
            form2.SetValue("change_version", versionNumber.Text);
            form2.SetValue("version_increase", "true");

            Console.Log("Sending Change log");
            HttpResponseMessage changelogResult = await form2.SendDataAsync(HttpHelper.HttpMethod.PUT);
            response = await changelogResult.Content.ReadAsStringAsync();
            Console.Log(
                $"Raw Response from {Program.URL + Program.ApiURL + (_isMod ? Program.ModsChangelogsURL : Program.SkinsChangelogsURL) + "/" + _item.PublicID + "/"}\n{response}");

            if (changelogResult.IsSuccessStatusCode)
            {
                Notification.Show("Success!");
                Console.Log("Successfully updated!");
            }
            else
            {
                Notification.Show($"Error Code: {changelogResult.StatusCode}", "Error");
                Console.Log(
                    $"There was an error trying to submit a change log.\n Error Code: {changelogResult.StatusCode}");
            }

            MainWindow.SetBusy(false);
            MainWindow._instance.Creator(null, null);
        }

        private void APIResult(JObject json)
        {
            if (json["name"] != null)
            {
                if (json["name"].ToString().Equals("Mod with this name already exists.") ||
                    json["name"].ToString().Equals("Invalid Mod Name."))
                {
                    Notification.Show(json["name"].ToString(), "Failed");
                    Console.Log($"Failed to upload project\n{json["name"]}");
                }
            }

            if (json["version"] != null)
            {
                if (json["version"].ToString().StartsWith("Invalid version number"))
                {
                    Notification.Show(json["version"].ToString(), "Failed");
                    Console.Log($"Failed to upload project\n{json["version"]}");
                }
            }

            if (json["header_image"] != null)
            {
                if (json["header_image"].ToString().Equals("Mod image file too large ( > 2mb )") ||
                    json["header_image"].ToString().Equals("Incorrect format (png or jpg)") ||
                    json["header_image"].ToString().Equals("Couldn't read uploaded image"))
                {
                    Notification.Show(json["header_image"].ToString(), "Failed");
                    Console.Log($"Failed to upload project\n{json["header_image"]}");
                }
            }

            if (json["user_uploaded_file"] != null)
            {
                if (!json["user_uploaded_file"].ToString().Contains(Program.URL))
                {
                    Notification.Show(json["user_uploaded_file"].ToString(), "Failed");
                    Console.Log($"Failed to upload project\n{json["user_uploaded_file"]}");
                }
            }

            if (json["pub_id"] != null)
            {
                Process.Start($"{Program.URL}/{(_isMod ? "mod" : "skin")}/{json["pub_id"]}/");
                Notification.Show("Uploaded!", "Success");
                Console.Log($"Uploaded new project at {Program.URL}/{(_isMod ? "mod" : "skin")}/{json["pub_id"]}/");
                MainWindow._instance.Creator(null, null);

                if (!_item.HasPublicID())
                {
                    _item.PublicID = json["pub_id"].ToString();
                    _item.SaveFile();
                    Console.Log("Saved Project!");
                }
            }

            MainWindow.SetBusy(false);
            MainWindow._instance.Creator(null, null);
        }

        private bool AssemblyChecks()
        {
            Helper.SentryLog("Assembly Checks", Helper.SentryLogCategory.NewVersion);
            if (!_isMod)
            {
                Console.Log("Somehow Assembly checks ran in a skin project");
                return false;
            }

            if (!_item.HasDll())
            {
                Notification.Show("info.json seems to be missing the dll file name.\nMod was not uploaded.",
                    "Missing Item");
                return false;
            }

            if (!File.Exists(_currentPath + @"\Builds\" + _item.DllPath))
            {
                Notification.Show($"Can't find {_item.DllPath}", $"Missing {_item.DllPath}");
                return false;
            }

            IEnumerable<Type> source =
                from t in Assembly.Load(File.ReadAllBytes(_currentPath + @"\Builds\" + _item.DllPath)).GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;

            if (source == null)
            {
                Notification.Show($"It seems there is no class deriving from VTOLMOD in {_item.DllPath}",
                    "Missing VTOLMOD class");
                return false;
            }

            if (source.Count() > 1)
            {
                Notification.Show($"It seems there is two or more classes deriving from VTOLMOD in {_item.DllPath}",
                    "Too many VTOLMOD classes");
                return false;
            }
            else if (source.Count() == 0)
            {
                Notification.Show($"It seems there is no classes deriving from VTOLMOD in {_item.DllPath}",
                    "No VTOLMOD classes");
                return false;
            }

            return true;
        }

        private string ZipCurrentProject()
        {
            Helper.SentryLog("Zipping project", Helper.SentryLogCategory.NewVersion);
            if (File.Exists($"{_currentPath}\\{_item.Name}.zip"))
                File.Delete($"{_currentPath}\\{_item.Name}.zip");
            ZipArchive zip = ZipFile.Open($"{_currentPath}\\{_item.Name}.zip", ZipArchiveMode.Update);

            if (_isMod)
            {
                DirectoryInfo buildFolder = new DirectoryInfo(_currentPath + @"\Builds");
                FileInfo[] files = buildFolder.GetFiles();

                for (int i = 0; i < files.Length; i++)
                {
                    zip.CreateEntryFromFile(files[i].FullName, files[i].Name);
                }

                if (_item.Dependencies != null)
                {
                    for (int i = 0; i < _item.Dependencies.Count; i++)
                    {
                        zip.CreateEntryFromFile(_currentPath + @"\Dependencies\" + _item.Dependencies[i],
                            @"Dependencies\" + _item.Dependencies[i]);
                    }
                }
            }
            else
            {
                DirectoryInfo folder = new DirectoryInfo(_currentPath);
                FileInfo[] files = folder.GetFiles("*.png");
                for (int i = 0; i < files.Length; i++)
                {
                    if (!files[i].Name.Contains("web_preview.png"))
                        zip.CreateEntryFromFile(files[i].FullName, files[i].Name);
                }
            }

            zip.Dispose();
            return $"{_currentPath}\\{_item.Name}.zip";
        }

        private void SaveProject()
        {
            Helper.SentryLog("Saving Project", Helper.SentryLogCategory.NewVersion);
            _item.SaveFile();
            Console.Log("Saved Project!");
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_hasInternet)
                return;
            if (title != null && description != null &&
                title.Visibility == Visibility.Visible &&
                !string.IsNullOrEmpty(title.Text) && !string.IsNullOrEmpty(description.Text))
            {
                uploadButton.IsEnabled = true;
                uploadButton.Content = "Update";
            }
            else if (title.Visibility == Visibility.Visible)
            {
                uploadButton.IsEnabled = false;
                uploadButton.Content = "Please fill out all the sections before uploading";
            }
        }
    }
}