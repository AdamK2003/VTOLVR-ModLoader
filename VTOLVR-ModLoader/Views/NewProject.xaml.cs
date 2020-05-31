using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for NewProject.xaml
    /// </summary>
    public partial class NewProject : UserControl
    {
        private const string modBoilerplateURL = "https://gitlab.com/vtolvr-mods/vtolvr-mod-boilerplate/-/archive/master/vtolvr-mod-boilerplate-master.zip";

        private DirectoryInfo currentFolder;
        public NewProject()
        {
            InitializeComponent();
        }


        private void ProjectNameChanged(object sender, TextChangedEventArgs e)
        {
            nameBox.Text = nameBox.Text.RemoveSpecialCharacters();
            nameBox.CaretIndex = nameBox.Text.Length;

            if (nameBox.Text.Length == 0 && createButton != null)
                createButton.IsEnabled = false;
            else if (createButton != null)
                createButton.IsEnabled = true;

            //This is null for some reason at the start
            if (folderPreviewText != null)
            {
                if (!CheckIfProjectExists())
                {
                    folderPreviewText.Text = "Will be saved in: " + nameBox.Text;
                    createButton.IsEnabled = true;
                }
                else
                {
                    folderPreviewText.Text = "This project already exists";
                    createButton.IsEnabled = false;
                }
            }
                
        }

        private bool CheckIfProjectExists()
        {
            if (!Directory.Exists(Settings.projectsFolder + (dropdown.SelectedIndex == 0 ? ProjectManager.modsFolder : ProjectManager.skinsFolder) + @"\" + nameBox.Text))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void CreateProject(object sender, RoutedEventArgs e)
        {
            if (!CheckProjectFolder())
            {
                Notification.Show("Project Folder seems to not exist", "Missing Project Folder");
                return;
            }

            if (!CreateDefaultFolders())
                return;

            if (dropdown.SelectedIndex == 0)
                CreateModProject(nameBox.Text);
            else
                CreateSkinProject(nameBox.Text);
        }

        private bool CheckProjectFolder()
        {
            return Directory.Exists(Settings.projectsFolder);
        }

        private bool CreateDefaultFolders()
        {
            try
            {
                Directory.CreateDirectory(Settings.projectsFolder + ProjectManager.modsFolder);
                Directory.CreateDirectory(Settings.projectsFolder + ProjectManager.skinsFolder);
            }
            catch (Exception e)
            {
                Notification.Show("Error when trying to create the default folders for your projects", "Error");
                Console.Log("Error when trying to create the default folders for your projects");
                Console.Log(e.Message);
                return false;
            }
            return true;
        }

        private void CreateModProject(string name)
        {
            currentFolder = Directory.CreateDirectory(Settings.projectsFolder + ProjectManager.modsFolder + @"\" + name);
            if (Program.CheckForInternet())
            {
                DownloadModBoilerplate();
            }
            else
                ExtractModBoilerplate(true);

        }

        private void DownloadModBoilerplate()
        {
            progressBar.Visibility = Visibility.Visible;
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "VTOL VR Mod Loader");
            client.DownloadProgressChanged += DownloadProgress;
            client.DownloadFileCompleted += DownloadDone;
            client.DownloadFileAsync(new Uri(modBoilerplateURL), currentFolder.FullName + @"\boilerplate.zip");
        }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                ExtractModBoilerplate();
            }
            else
            {
                Console.Log("Error:\n" + e.Error.ToString());
                Console.Log("Using fallback offline version of the mod boiler plate");
                ExtractModBoilerplate(true); //Using fallback
            }
        }

        private void ExtractModBoilerplate(bool offline = false)
        {
            if (offline)
            {
                File.WriteAllBytes(currentFolder.FullName + @"\boilerplate.zip", Properties.Resources.vtolvr_mod_boilerplate_master);
            }

            ZipFile.ExtractToDirectory(currentFolder.FullName + @"\boilerplate.zip", currentFolder.FullName);

            //This first should just be the one folder called 
            // vtolvr-mod-boilerplate-master
            DirectoryInfo[] folders = new DirectoryInfo(currentFolder.FullName).GetDirectories();
            //Now we are moving everything out of that folder
            for (int i = 0; i < folders.Length; i++)
            {
                DirectoryInfo[] subFolders = folders[i].GetDirectories();
                for (int x = 0; x < subFolders.Length; x++)
                {
                    Directory.Move(subFolders[x].FullName, currentFolder.FullName + @"\" + subFolders[x].Name);
                }

                FileInfo[] subFiles = folders[i].GetFiles();
                for (int x = 0; x < subFiles.Length; x++)
                {
                    File.Move(subFiles[x].FullName, currentFolder.FullName + @"\" + subFiles[x].Name);
                }

                //Now delete that extra folder
                Directory.Delete(folders[i].FullName);
            }

            File.Delete(currentFolder.FullName + @"\boilerplate.zip");

            if (File.Exists(currentFolder.FullName + @"\README.md"))
                File.Delete(currentFolder.FullName + @"\README.md");

            MoveDlls(currentFolder.FullName + @"\Dependencies");
            ChangeFilesText();
            Directory.CreateDirectory(currentFolder.FullName + @"\Builds");

            CreateJson();
        }

        private void MoveDlls(string path)
        {
            TryCopy(Program.root + @"\ModLoader.dll", path + @"\ModLoader.dll");
            TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\Assembly-CSharp.dll", path + @"\Assembly-CSharp.dll");
            TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\UnityEngine.dll", path + @"\UnityEngine.dll");
            TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\UnityEngine.CoreModule.dll", path + @"\UnityEngine.CoreModule.dll");
        }

        private bool TryCopy(string sourceFileName, string destFileName)
        {
            try
            {
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Console.Log($"Failed to move file: {sourceFileName}\n{e}");
                return false;
            }
            return true;
        }

        private void ChangeFilesText()
        {
            string projectName = nameBox.Text.RemoveSpaces();

            string solutionFile = File.ReadAllText(currentFolder.FullName + @"\VTOLVR_MOD_Boilerplate.sln");
            solutionFile = solutionFile.Replace("VTOLVR_Mod_Boilerplate", projectName);
            File.WriteAllText(currentFolder.FullName + @"\" + projectName + ".sln", solutionFile);
            File.Delete(currentFolder.FullName + @"\VTOLVR_MOD_Boilerplate.sln");

            Directory.Move(currentFolder.FullName + @"\VTOLVR_Mod_Boilerplate", currentFolder.FullName + @"\" + projectName);

            string csproj = File.ReadAllText($"{currentFolder.FullName}\\{projectName}\\VTOLVR_Mod_Boilerplate.csproj");
            csproj = csproj.Replace("VTOLVR_Mod_Boilerplate", projectName);
            csproj = csproj.Replace("{{VTOLVR}}", Program.root + @"\VTOLVR-ModLoader.exe");
            csproj = csproj.Replace("{{MODPATH}}", currentFolder.FullName + @"\Builds\" + projectName + @".dll");
            File.WriteAllText($"{currentFolder.FullName}\\{projectName}\\{projectName}.csproj", csproj);
            File.Delete($"{currentFolder.FullName}\\{projectName}\\VTOLVR_Mod_Boilerplate.csproj");

            string maincs = File.ReadAllText($"{currentFolder.FullName}\\{projectName}\\Main.cs");
            string nameSpace = projectName;
            if (Regex.IsMatch(nameSpace[0].ToString(), @"^\d$"))
            {
                nameSpace = "_" + nameSpace;
            }
            maincs = maincs.Replace("VTOLVR_Mod_Boilerplate", nameSpace);
            File.WriteAllText($"{currentFolder.FullName}\\{projectName}\\Main.cs", maincs);
        }

        private void CreateJson(bool isMod = true)
        {
            JObject jObject = new JObject();
            jObject.Add("Name", nameBox.Text);
            jObject.Add("Description", descriptionBox.Text);
            if (isMod)
                jObject.Add("Dll File", nameBox.Text.RemoveSpaces() + ".dll");
            jObject.Add("Last Edit", DateTime.Now.Ticks);
            File.WriteAllText(currentFolder.FullName + (isMod? @"\Builds\" : @"\") + @"info.json", jObject.ToString());
        }

        private void CreateSkinProject(string name)
        {
            currentFolder = Directory.CreateDirectory(Settings.projectsFolder + ProjectManager.skinsFolder + @"\" + name);
            CreateJson(false);
        }
    }
}