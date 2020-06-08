using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for ModCreator.xaml
    /// </summary>
    public partial class ProjectManager : UserControl
    {
        public const string modsFolder = @"\My Mods";
        public const string skinsFolder = @"\My Skins";
        public const string jName = "Name";
        public const string jDescription = "Description";
        public const string jDll = "Dll File";
        public const string jEdit = "Last Edit";
        public const string jSource = "Source";
        public const string jPImage = "Preview Image";
        public const string jWImage = "Web Preview Image";
        public ProjectManager()
        {
            InitializeComponent();
        }

        public void SetUI()
        {
            settingsText.Visibility = Visibility.Visible;
            newProjectButton.IsEnabled = false;

            if (CheckProjectPath())
            {
                List<MyProject> localMods = new List<MyProject>();

                FindMods(ref localMods);
                FindSkins(ref localMods);

                localMods = SortProjects(localMods);
                folders.ItemsSource = localMods.ToArray();
            }
        }

        private bool CheckProjectPath()
        {
            if (string.IsNullOrEmpty(Settings.projectsFolder))
                return false;
            settingsText.Visibility = Visibility.Hidden;
            newProjectButton.IsEnabled = true;
            return true;
        }

        private void NewProject(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenPage(new NewProject());
        }

        private void FindMods(ref List<MyProject> localProjects)
        {
            DirectoryInfo myMods = new DirectoryInfo(Settings.projectsFolder + modsFolder);
            DirectoryInfo[] mods = myMods.GetDirectories();

            for (int i = 0; i < mods.Length; i++)
            {
                if (Directory.Exists(mods[i].FullName + @"\Builds") &&
                    File.Exists(mods[i].FullName + @"\Builds\info.json"))
                {
                    JObject jObject;
                    try
                    {
                        jObject = JObject.Parse(File.ReadAllText(mods[i].FullName + @"\Builds\info.json"));
                    }
                    catch (Exception e)
                    {
                        Console.Log($"Failed to parse {mods[i].FullName}\\Builds\\info.json\n{e.Message}");
                        continue;
                    }

                    if (jObject[jName] != null || jObject[jDescription] != null)
                    {
                        string lastedit = string.Empty;
                        long result = 0;
                        if (jObject[jEdit] != null)
                        {
                            if (long.TryParse(jObject[jEdit].ToString(), out result))
                            {
                                lastedit = new DateTime(result).ToString();
                            }
                        }
                        localProjects.Add(new MyProject(jObject[jName].ToString(),
                        jObject[jDescription].ToString(),
                        mods[i].FullName,
                        lastedit,
                        new DateTime(result)));
                    }
                    else
                    {
                        Console.Log($"{mods[i].Name} is missing something in it's info.json file");
                    }                    
                }
                else
                {
                    Console.Log($"{mods[i].Name} doesn't seem to have a builds folder or a info.json, ignoring folder");
                }
            }
        }

        private void FindSkins(ref List<MyProject> localProjects)
        {
            DirectoryInfo mySkins = new DirectoryInfo(Settings.projectsFolder + skinsFolder);
            DirectoryInfo[] skins = mySkins.GetDirectories();

            for (int i = 0; i < skins.Length; i++)
            {
                if (File.Exists(skins[i].FullName + @"\info.json"))
                {
                    JObject jObject;
                    try
                    {
                        jObject = JObject.Parse(File.ReadAllText(skins[i].FullName + @"\info.json"));
                    }
                    catch (Exception e)
                    {
                        Console.Log($"Failed to parse {skins[i].FullName}\\info.json\n{e.Message}");
                        continue;
                    }

                    if (jObject[jName] != null || jObject[jDescription] != null)
                    {
                        string lastedit = string.Empty;
                        long result = 0;
                        if (jObject[jEdit] != null)
                        {
                            if (long.TryParse(jObject[jEdit].ToString(), out result))
                            {
                                lastedit = new DateTime(result).ToString();
                            }
                        }
                        localProjects.Add(new MyProject(jObject[jName].ToString(),
                        jObject[jDescription].ToString(),
                        skins[i].FullName,
                        lastedit,
                        new DateTime(result)));
                    }
                    else
                    {
                        Console.Log($"{skins[i].Name} is missing something in it's info.json file");
                    }
                }
                else
                {
                    Console.Log($"{skins[i].Name} doesn't seem to have a info.json, ignoring folder");
                }
            }
        }

        private static List<MyProject> SortProjects(List<MyProject> myProjects)
        {
            myProjects.Sort((a, b) => b.DateTime.CompareTo(a.DateTime));
            return myProjects;
        }

        private class MyProject
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Path { get; set; }
            public string LastEdit { get; set; }
            public DateTime DateTime { get; set; }

            public MyProject(string name, string description, string path, string lastEdit, DateTime dateTime)
            {
                Name = name;
                Description = description;
                Path = path;
                LastEdit = lastEdit;
                DateTime = dateTime;
            }
        }

        private void EditProject(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            MainWindow.OpenPage(new EditProject(button.Tag.ToString()));
        }
    }
}
