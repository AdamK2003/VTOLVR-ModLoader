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
                FindMods();
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

        private void FindMods()
        {
            DirectoryInfo myMods = new DirectoryInfo(Settings.projectsFolder + modsFolder);
            DirectoryInfo[] mods = myMods.GetDirectories();

            List<MyMod> localMods = new List<MyMod>();

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

                    if (jObject["Name"] != null || jObject["Description"] != null)
                    {
                        string lastedit = string.Empty;
                        if (jObject["Last Edit"] != null)
                        {
                            if (long.TryParse(jObject["Last Edit"].ToString(), out long result))
                            {
                                lastedit = new DateTime(result).ToString();
                            }
                        }
                        localMods.Add(new MyMod(jObject["Name"].ToString(),
                        jObject["Description"].ToString(),
                        mods[i].FullName,
                        lastedit));
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

            folders.ItemsSource = localMods.ToArray();
        }

        private class MyMod
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Path { get; set; }
            public string LastEdit { get; set; }

            public MyMod(string name, string description, string path, string lastEdit)
            {
                Name = name;
                Description = description;
                Path = path;
                LastEdit = lastEdit;
            }
        }
    }
}
