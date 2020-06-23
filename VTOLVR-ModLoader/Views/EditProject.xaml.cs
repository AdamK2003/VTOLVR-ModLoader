using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

            if (_currentJson[ProjectManager.jName] != null)
            {
                projectName.Text = _currentJson[ProjectManager.jName].ToString();
            }
            if (_currentJson[ProjectManager.jDescription] != null)
            {
                projectDescription.Text = _currentJson[ProjectManager.jDescription].ToString();
            }
            if (_currentJson[ProjectManager.jPImage] != null)
            {
                if (File.Exists(_currentPath + (_isMod ? @"\Builds\" : @"\") + _currentJson[ProjectManager.jPImage].ToString()))
                {
                    previewImage.Source = new BitmapImage(
                    new Uri(_currentPath + (_isMod ? @"\Builds\" : @"\") + _currentJson[ProjectManager.jPImage].ToString()));
                    previewImageText.Visibility = Visibility.Hidden;
                }
            }
            if (_currentJson[ProjectManager.jWImage] != null)
            {
                if (File.Exists(_currentPath + @"\" + _currentJson[ProjectManager.jWImage].ToString()))
                {
                    webPageImage.Source = new BitmapImage(
                    new Uri(_currentPath + @"\" + _currentJson[ProjectManager.jWImage].ToString()));
                    webPageImageText.Visibility = Visibility.Hidden;
                }
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

            if (_currentJson[ProjectManager.jSource] != null)
            {
                modSource.Text = _currentJson[ProjectManager.jSource].ToString();
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
            UpdateDependencies();
            SaveProject();
        }

        private void Upload(object sender, RoutedEventArgs e)
        {
            UpdateDependencies();
            SaveProject();
            UploadProject();
        }

        private void UploadProject()
        {
            if (_isMod && !AssemblyChecks())
                return;
            ZipCurrentProject();


        }

        private void SaveProject()
        {
            _currentJson[ProjectManager.jName] = projectName.Text;
            _currentJson[ProjectManager.jDescription] = projectDescription.Text;
            if (_isMod)
                _currentJson[ProjectManager.jSource] = modSource.Text;
            _currentJson[ProjectManager.jEdit] = DateTime.Now.Ticks;

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

            if (File.Exists(_currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png"))
                File.Delete(_currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png");
            File.Copy(filePath, _currentPath + (_isMod? @"\Builds" : string.Empty) + @"\preview.png");
            filePath = _currentPath + (_isMod ? @"\Builds" : string.Empty) + @"\preview.png";

            if (_currentJson[ProjectManager.jPImage] != null)
                _currentJson[ProjectManager.jPImage] = "preview.png";
            else
                _currentJson.Add(ProjectManager.jPImage, "preview.png");

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

            if (_currentJson[ProjectManager.jWImage] != null)
                _currentJson[ProjectManager.jWImage] = "web_preview.png";
            else
                _currentJson.Add(ProjectManager.jWImage, "web_preview.png");

            webPageImageText.Visibility = Visibility.Hidden;

            webPageImage.Source = new BitmapImage(new Uri(filePath));
            SaveProject();
        }

        private bool PreviewImageChecks(string path)
        {
            Bitmap image = new Bitmap(path);
            FileInfo info = new FileInfo(path);

            if (info.Length > 2000000)
            {
                Notification.Show($"{info.Name} is over 2mb", "File not excepted");
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

            if (info.Length > 2000000)
            {
                Notification.Show($"{info.Name} is over 2mb", "File not excepted");
                return false;
            }
            return true;
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

        private void UpdateDependencies()
        {
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

            if (_currentJson[ProjectManager.jDeps] != null)
            {
                _currentJson[ProjectManager.jDeps] = JArray.FromObject(newDependencies.ToArray());
            }
            else
            {
                _currentJson.Add(new JProperty(ProjectManager.jDeps, JArray.FromObject(newDependencies.ToArray())));
            }
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
    }
}
