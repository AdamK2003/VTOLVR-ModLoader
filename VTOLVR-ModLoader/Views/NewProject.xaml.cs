﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            Helper.SentryLog("Created New Project Page", Helper.SentryLogCategory.NewProject);
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
            if (!Directory.Exists(Settings.ProjectsFolder + (dropdown.SelectedIndex == 0 ? ProjectManager.modsFolder : ProjectManager.skinsFolder) + @"\" + nameBox.Text))
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
            Helper.SentryLog("Creating New Project", Helper.SentryLogCategory.NewProject);
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
            Helper.SentryLog("Checking Projects Folder", Helper.SentryLogCategory.NewProject);
            return Directory.Exists(Settings.ProjectsFolder);
        }

        private bool CreateDefaultFolders()
        {
            Helper.SentryLog("Creating Default Folder", Helper.SentryLogCategory.NewProject);
            try
            {
                Directory.CreateDirectory(Settings.ProjectsFolder + ProjectManager.modsFolder);
                Directory.CreateDirectory(Settings.ProjectsFolder + ProjectManager.skinsFolder);
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

        private async void CreateModProject(string name)
        {
            Helper.SentryLog("Creating Mod", Helper.SentryLogCategory.NewProject);
            currentFolder = Directory.CreateDirectory(Settings.ProjectsFolder + ProjectManager.modsFolder + @"\" + name);
            if (await HttpHelper.CheckForInternet())
            {
                DownloadModBoilerplate();
            }
            else
                ExtractModBoilerplate(true);

        }

        private void DownloadModBoilerplate()
        {
            Helper.SentryLog("Downloading Mod Boilerplate", Helper.SentryLogCategory.NewProject);
            progressBar.Visibility = Visibility.Visible;
            HttpHelper.DownloadFile(modBoilerplateURL,
                currentFolder.FullName + @"\boilerplate.zip",
                DownloadProgress,
                DownloadDone);
        }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        private void DownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            Helper.SentryLog("Finished downloading boilerplate", Helper.SentryLogCategory.NewProject);
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
            Helper.SentryLog($"Exporting Mod Boiler plate offline={offline}", Helper.SentryLogCategory.NewProject);
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
            Finished();
        }

        private void MoveDlls(string path)
        {
            Helper.SentryLog("Moving Dlls", Helper.SentryLogCategory.NewProject);
            Helper.TryCopy(Program.root + @"\ModLoader.dll", path + @"\ModLoader.dll");
            Helper.TryCopy(Program.root + @"\ModLoader.xml", path + @"\ModLoader.xml");
            Helper.TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\Assembly-CSharp.dll", path + @"\Assembly-CSharp.dll");
            Helper.TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\UnityEngine.dll", path + @"\UnityEngine.dll");
            Helper.TryCopy(Program.vtolFolder + @"\VTOLVR_Data\Managed\UnityEngine.CoreModule.dll", path + @"\UnityEngine.CoreModule.dll");
        }

        private void ChangeFilesText()
        {
            Helper.SentryLog("Changing Text in files", Helper.SentryLogCategory.NewProject);
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
            Helper.SentryLog("Creating Json", Helper.SentryLogCategory.NewProject);
            JObject jObject = new JObject();
            jObject.Add("Name", nameBox.Text);
            jObject.Add("Description", descriptionBox.Text);
            if (isMod)
                jObject.Add("Dll File", nameBox.Text.RemoveSpaces() + ".dll");
            jObject.Add("Last Edit", DateTime.Now.Ticks);
            File.WriteAllText(currentFolder.FullName + (isMod ? @"\Builds\" : @"\") + @"info.json", jObject.ToString());
        }

        private void CreateSkinProject(string name)
        {
            Helper.SentryLog("Creating Skin Project", Helper.SentryLogCategory.NewProject);
            currentFolder = Directory.CreateDirectory(Settings.ProjectsFolder + ProjectManager.skinsFolder + @"\" + name);
            CreateJson(false);
            Finished();
        }

        private void Finished()
        {
            Helper.SentryLog("Finished", Helper.SentryLogCategory.NewProject);
            MainWindow._instance.Creator(null, null);
        }
    }
}