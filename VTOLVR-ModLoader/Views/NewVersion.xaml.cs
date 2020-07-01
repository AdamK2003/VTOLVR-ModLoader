using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
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
                uploadButton.Content = "Release";
            }
        }
        private void VersionNumberChanged(object sender, TextChangedEventArgs e)
        {
            if (versionNumber != null && _currentJson != null)
                _currentJson[ProjectManager.jVersion] = versionNumber.Text;
        }
        private void Upload(object sender, RoutedEventArgs e)
        {
            SaveProject();
            if (_currentJson[ProjectManager.jID] != null)
                UpdateProject();
            else
                UploadNewProject();
        }
        private void UploadNewProject()
        {
            if (_isMod && !AssemblyChecks())
                return;
            string zipPath = ZipCurrentProject();

            HttpForm form = new HttpForm(Program.url + Program.apiURL + Program.modsURL + @"\");
            form.SetToken(Settings.Token);
            form.SetValue("version", _currentJson[ProjectManager.jVersion].ToString());
            form.SetValue("name", _currentJson[ProjectManager.jName].ToString());
            form.SetValue("tagline", _currentJson[ProjectManager.jTagline].ToString());
            form.SetValue("description", _currentJson[ProjectManager.jDescription].ToString());
            form.SetValue("unlisted", _currentJson[ProjectManager.jUnlisted].ToString());
            form.SetValue("is_public", _currentJson[ProjectManager.jPublic].ToString());
            if (_isMod)
                form.SetValue("repository", _currentJson[ProjectManager.jSource].ToString());
            else
                form.SetValue("repository", "");

            form.AttachFile("header_image", _currentPath + @"\" + _currentJson[ProjectManager.jWImage].ToString());
            form.AttachFile("thumbnail", _currentPath + (_isMod ? @"\Builds\" : @"\") + _currentJson[ProjectManager.jPImage].ToString());
            form.AttachFile("user_uploaded_file", zipPath);

            HttpWebResponse responce = form.Submit();
        }
        private void UpdateProject()
        {

        }
        private bool AssemblyChecks()
        {
            if (!_isMod)
            {
                Console.Log("Somehow Assemblychecks ran in a skin project");
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
    }
}
