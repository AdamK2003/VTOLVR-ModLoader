using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LauncherCore.Classes;
using LauncherCore.Windows;
using Core.Jsons;

namespace LauncherCore.Views
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

                for (int i = 0; i < localMods.Count; i++)
                {
                    byte alpha = 100;
                    if (i % 2 == 0)
                        alpha = 0;
                    localMods[i].BackgroundColour = new SolidColorBrush(Color.FromArgb(alpha, 46, 46, 46));
                }

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
            List<BaseItem> baseItems = Helper.FindMyMods();

            BaseItem lastItem;
            for (int i = 0; i < baseItems.Count; i++)
            {
                lastItem = baseItems[i];
                localProjects.Add(new MyProject(lastItem.Name,
                    lastItem.Description,
                    lastItem.Directory.FullName,
                    openProjectText,
                    lastItem.PublicID == string.Empty ? releaseText : newReleaseText,
                    new DateTime(lastItem.LastEdit)));
            }
        }

        private void FindSkins(ref List<MyProject> localProjects)
        {
            Helper.SentryLog("Finding Skins", Helper.SentryLogCategory.ProjectManager);
            List<BaseItem> baseItems = Helper.FindMySkins();

            BaseItem lastItem;
            for (int i = 0; i < baseItems.Count; i++)
            {
                lastItem = baseItems[i];
                localProjects.Add(new MyProject(lastItem.Name,
                    lastItem.Description,
                    lastItem.Directory.FullName,
                    openFolderText,
                    lastItem.PublicID == string.Empty ? releaseText : newReleaseText,
                    new DateTime(lastItem.LastEdit)));
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
            public Brush BackgroundColour { get; set; }

            public MyProject(string name, string description, string path, string openProjectText,
                string newReleaseText, DateTime dateTime)
            {
                Name = name;
                Description = description;
                Path = path;
                LastEdit = dateTime.ToString();
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