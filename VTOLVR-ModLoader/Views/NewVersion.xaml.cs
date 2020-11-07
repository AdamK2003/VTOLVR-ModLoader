using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
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
    /// Interaction logic for NewVersion.xaml
    /// </summary>
    public partial class NewVersion : UserControl
    {
        private JObject _currentJson;
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

            if (_currentJson[ProjectManager.jVersion] != null)
            {
                versionNumber.Text = _currentJson[ProjectManager.jVersion].ToString();
            }

            if (_currentJson[ProjectManager.jID] == null)
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
                uploadButton.Content = "Release";
                uploadButton.IsEnabled = true;
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
            if (versionNumber != null && _currentJson != null)
                _currentJson[ProjectManager.jVersion] = versionNumber.Text;
        }
        private async void Upload(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Uploading", Helper.SentryLogCategory.NewVersion);
            uploadButton.IsEnabled = false;
            MainWindow.SetBusy(true);
            SaveProject();
            if (_currentJson[ProjectManager.jID] != null)
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

            HttpHelper form = new HttpHelper(Program.url + Program.apiURL + (_isMod ? Program.modsURL : Program.skinsURL) + @"\");
            form.SetToken(Settings.Token);
            form.SetValue("version", _currentJson[ProjectManager.jVersion].ToString());
            form.SetValue("name", _currentJson[ProjectManager.jName].ToString());
            form.SetValue("tagline", _currentJson[ProjectManager.jTagline].ToString());
            form.SetValue("description", _currentJson[ProjectManager.jDescription].ToString());
            form.SetValue("unlisted", _currentJson[ProjectManager.jUnlisted].ToString());
            form.SetValue("is_public", _currentJson[ProjectManager.jPublic].ToString());
            if (_isMod)
                form.SetValue("repository", _currentJson[ProjectManager.jSource].ToString());

            form.AttachFile("header_image", _currentJson[ProjectManager.jWImage].ToString(), _currentPath + @"\" + _currentJson[ProjectManager.jWImage].ToString());
            form.AttachFile("thumbnail", _currentJson[ProjectManager.jPImage].ToString(), _currentPath + (_isMod ? @"\Builds\" : @"\") + _currentJson[ProjectManager.jPImage].ToString());
            form.AttachFile("user_uploaded_file", "test.zip", zipPath);

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

            HttpHelper form = new HttpHelper(Program.url + Program.apiURL + (_isMod ? Program.modsURL : Program.skinsURL) + "/" + _currentJson[ProjectManager.jID].ToString() + "/");
            form.SetToken(Settings.Token);
            form.SetValue("version", _currentJson[ProjectManager.jVersion].ToString());
            form.SetValue("name", _currentJson[ProjectManager.jName].ToString());
            form.SetValue("tagline", _currentJson[ProjectManager.jTagline].ToString());
            form.SetValue("description", _currentJson[ProjectManager.jDescription].ToString());
            form.SetValue("unlisted", _currentJson[ProjectManager.jUnlisted].ToString());
            form.SetValue("is_public", _currentJson[ProjectManager.jPublic].ToString());
            if (_isMod)
                form.SetValue("repository", _currentJson[ProjectManager.jSource].ToString());

            form.AttachFile("header_image", _currentJson[ProjectManager.jWImage].ToString(), _currentPath + @"\" + _currentJson[ProjectManager.jWImage].ToString());
            form.AttachFile("thumbnail", _currentJson[ProjectManager.jPImage].ToString(), _currentPath + (_isMod ? @"\Builds\" : @"\") + _currentJson[ProjectManager.jPImage].ToString());
            form.AttachFile("user_uploaded_file", $"{_currentJson[ProjectManager.jName]}.zip", zipPath);



            HttpResponseMessage result = await form.SendDataAsync(HttpHelper.HttpMethod.PUT);
            string response = await result.Content.ReadAsStringAsync();
            Console.Log($"Raw Response from {Program.url + Program.apiURL + (_isMod ? Program.modsURL : Program.skinsURL) + "/" + _currentJson[ProjectManager.jID].ToString() + "/"}\n{response}");
            JObject json = JObject.Parse(response);

            if (json["detail"] != null)
            {
                Notification.Show(json["detail"].ToString(), "Error");
                MainWindow.SetBusy(false);
                return;
            }

            HttpHelper form2 = new HttpHelper(Program.url + Program.apiURL + (_isMod ? Program.modsChangelogsURL : Program.skinsChangelogsURL) + "/" + _currentJson[ProjectManager.jID] + "/");
            form2.SetToken(Settings.Token);
            form2.SetValue("change_name", title.Text);
            form2.SetValue("change_log", description.Text);
            form2.SetValue("change_version", versionNumber.Text);
            form2.SetValue("version_increase", "true");

            Console.Log("Sending Change log");
            HttpResponseMessage changelogResult = await form2.SendDataAsync(HttpHelper.HttpMethod.PUT);
            response = await changelogResult.Content.ReadAsStringAsync();
            Console.Log($"Raw Response from {Program.url + Program.apiURL + (_isMod ? Program.modsChangelogsURL : Program.skinsChangelogsURL) + "/" + _currentJson[ProjectManager.jID] + "/"}\n{response}");

            if (changelogResult.IsSuccessStatusCode)
            {
                Notification.Show("Success!");
                Console.Log("Successfully updated!");
            }
            else
            {
                Notification.Show($"Error Code: {changelogResult.StatusCode}", "Error");
                Console.Log($"There was an error trying to submit a change log.\n Error Code: {changelogResult.StatusCode}");
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
                if (json["user_uploaded_file"].ToString().Equals("Incorrect extension (zip)"))
                {
                    Notification.Show(json["user_uploaded_file"].ToString(), "Failed");
                    Console.Log($"Failed to upload project\n{json["user_uploaded_file"]}");
                }
            }
            if (json["pub_id"] != null)
            {
                Process.Start($"{Program.url}/{(_isMod ? "mod" : "skin")}/{json["pub_id"]}/");
                Notification.Show("Uploaded!", "Success");
                Console.Log($"Uploaded new project at {Program.url}/{(_isMod ? "mod" : "skin")}/{json["pub_id"]}/");
                MainWindow._instance.Creator(null, null);

                if (_currentJson[ProjectManager.jID] == null)
                {
                    _currentJson[ProjectManager.jID] = json["pub_id"].ToString();

                    try
                    {
                        File.WriteAllText(_currentPath + (_isMod ? @"\Builds\info.json" : @"\info.json"), _currentJson.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.Log($"Failed to save project\n{e}");
                        MainWindow.SetBusy(false);
                        return;
                    }
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

            if (_currentJson[ProjectManager.jDll] == null)
            {
                Notification.Show("info.json seems to be missing the dll file name.\nMod was not uploaded.", "Missing Item");
                return false;
            }

            if (!File.Exists(_currentPath + @"\Builds\" + _currentJson[ProjectManager.jDll].ToString()))
            {
                Notification.Show($"Can't find {_currentJson[ProjectManager.jDll]}", $"Missing {_currentJson[ProjectManager.jDll]}");
                return false;
            }

            IEnumerable<Type> source =
                from t in Assembly.Load(File.ReadAllBytes(_currentPath + @"\Builds\" + _currentJson[ProjectManager.jDll].ToString())).GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;

            if (source == null)
            {
                Notification.Show($"It seems there is no class deriving from VTOLMOD in {_currentJson[ProjectManager.jDll]}",
                    "Missing VTOLMOD class");
                return false;
            }
            if (source.Count() > 1)
            {
                Notification.Show($"It seems there is two or more classes deriving from VTOLMOD in {_currentJson[ProjectManager.jDll]}",
                    "Too many VTOLMOD classes");
                return false;
            }
            else if (source.Count() == 0)
            {
                Notification.Show($"It seems there is no classes deriving from VTOLMOD in {_currentJson[ProjectManager.jDll]}",
                    "No VTOLMOD classes");
                return false;
            }

            return true;
        }
        private string ZipCurrentProject()
        {
            Helper.SentryLog("Zipping project", Helper.SentryLogCategory.NewVersion);
            if (File.Exists($"{_currentPath}\\{_currentJson[ProjectManager.jName]}.zip"))
                File.Delete($"{_currentPath}\\{_currentJson[ProjectManager.jName]}.zip");
            ZipArchive zip = ZipFile.Open($"{_currentPath}\\{_currentJson[ProjectManager.jName]}.zip", ZipArchiveMode.Update);

            if (_isMod)
            {
                DirectoryInfo buildFolder = new DirectoryInfo(_currentPath + @"\Builds");
                FileInfo[] files = buildFolder.GetFiles();

                for (int i = 0; i < files.Length; i++)
                {
                    zip.CreateEntryFromFile(files[i].FullName, files[i].Name);
                }

                if (_currentJson[ProjectManager.jDeps] != null)
                {
                    JArray array = _currentJson[ProjectManager.jDeps] as JArray;
                    for (int i = 0; i < array.Count; i++)
                    {
                        zip.CreateEntryFromFile(_currentPath + @"\Dependencies\" + array[i].ToString(), @"Dependencies\" + array[i].ToString()); ;
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
                zip.CreateEntryFromFile($"{_currentPath}\\info.json", "info.json");
            }
            zip.Dispose();
            return $"{_currentPath}\\{_currentJson[ProjectManager.jName]}.zip";
        }
        private void SaveProject()
        {
            Helper.SentryLog("Saving Project", Helper.SentryLogCategory.NewVersion);
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
