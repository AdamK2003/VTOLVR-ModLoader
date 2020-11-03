using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for ModCreator.xaml
    /// </summary>
    public partial class ProjectManager : UserControl
    {
        public const string modsFolder = @"\My Mods";
        public const string skinsFolder = @"\My Skins";
        public const string jName = "name";
        public const string jDescription = "description";
        public const string jTagline = "tagline";
        public const string jVersion = "version";
        public const string jDll = "dll file";
        public const string jEdit = "last edit";
        public const string jSource = "source";
        public const string jPImage = "preview image";
        public const string jWImage = "web preview image";
        public const string jDeps = "dependencies";
        public const string jID = "public id";
        public const string jPublic = "is public";
        public const string jUnlisted = "unlisted";

        private const string openFolderText = "Open Folder";
        private const string openProjectText = "Open Project";
        private const string releaseText = "Release";
        private const string newReleaseText = "New Release";

        public ProjectManager()
        {
            InitializeComponent();
            Helper.SentryLog("Created Project Manager Page", Helper.SentryLogCategory.ProjectManager);
        }

        public void SetUI()
        {
            Helper.SentryLog("Setting up UI", Helper.SentryLogCategory.ProjectManager);
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
            if (string.IsNullOrEmpty(Settings.ProjectsFolder))
                return false;
            settingsText.Visibility = Visibility.Hidden;
            newProjectButton.IsEnabled = true;
            return true;
        }

        private void NewProject(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Opening new project page", Helper.SentryLogCategory.ProjectManager);
            MainWindow.OpenPage(new NewProject());
        }

        private void FindMods(ref List<MyProject> localProjects)
        {
            Helper.SentryLog("Finding Mods", Helper.SentryLogCategory.ProjectManager);
            DirectoryInfo myMods = new DirectoryInfo(Settings.ProjectsFolder + modsFolder);
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
                        openProjectText,
                        jObject[jID] == null ? releaseText : newReleaseText,
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
            Helper.SentryLog("Finding Skins", Helper.SentryLogCategory.ProjectManager);
            DirectoryInfo mySkins = new DirectoryInfo(Settings.ProjectsFolder + skinsFolder);
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
                        openFolderText,
                        jObject[jID] == null ? releaseText : newReleaseText,
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
            public string OpenProjectText { get; set; }
            public string NewReleaseText { get; set; }
            public DateTime DateTime { get; set; }

            public MyProject(string name, string description, string path, string lastEdit, string openProjectText, string newReleaseText, DateTime dateTime)
            {
                Name = name;
                Description = description;
                Path = path;
                LastEdit = lastEdit;
                OpenProjectText = openProjectText;
                NewReleaseText = newReleaseText;
                DateTime = dateTime;
            }
        }

        private void EditProject(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            MainWindow.OpenPage(new EditProject(button.Tag.ToString()));
        }

        private void OpenProject(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string projectPath = button.Tag.ToString();
            FileInfo[] slns = new DirectoryInfo(projectPath).GetFiles("*.sln");
            if (slns.Length == 1)
            {
                try
                {
                    Process.Start(slns[0].FullName);
                }
                catch (Exception error)
                {
                    Notification.Show($"Error when opening .sln file\n{error.Message}", "Error");
                    Process.Start(projectPath);
                }
            }
            else
            {
                Process.Start(projectPath);
            }
        }

        private void UpdateProject(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            MainWindow.OpenPage(new NewVersion(button.Tag.ToString()));
        }
    }
}
