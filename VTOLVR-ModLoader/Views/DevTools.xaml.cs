using Valve.Newtonsoft.Json.Linq;
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
using System.Xml.Serialization;
using VTOLVR_ModLoader.Classes;
using VTOLVR_ModLoader.Properties;
using VTOLVR_ModLoader.Classes.Json;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for DevTools.xaml
    /// </summary>
    public partial class DevTools : UserControl
    {
        private const string savePath = "/devtools.json";
        private const string gameData = "/gamedata.json";
        public static bool DevToolsEnabled { get; private set; } = false;
        public static Pilot PilotSelected;
        public static Scenario ScenarioSelected;
        public static List<string> ModsToLoad = new List<string>();
        public static string[] PilotsCFG;

        private static List<Scenario> _scenarios = new List<Scenario>();

        public DevTools()
        {
            InitializeComponent();
            LoadScenarios();
            try
            {
                LoadSettings();
            }
            catch (Exception e)
            {
                Console.Log("Failed Loading dev tool settings\n" + e);
            }
            Helper.SentryLog("Created Dev Tools Page", Helper.SentryLogCategory.DevToos);
        }
        public void SetUI()
        {
            Helper.SentryLog("Setting UI", Helper.SentryLogCategory.DevToos);
            LoadSettings();
            FindMods();
            if (!FindPilots())
                return;
            if (PilotSelected != null)
            {
                foreach (Pilot p in PilotDropdown.ItemsSource)
                {
                    if (p.Name == PilotSelected.Name)
                    {
                        PilotDropdown.SelectedItem = p;
                        break;
                    }
                }
            }
            if (ScenarioSelected != null)
            {
                foreach (Scenario s in ScenarioDropdown.ItemsSource)
                {
                    if (s.ID == ScenarioSelected.ID)
                    {
                        ScenarioDropdown.SelectedItem = s;
                        break;
                    }
                }
            }
        }

        private bool FindPilots()
        {
            Helper.SentryLog("Finding Pilots", Helper.SentryLogCategory.DevToos);
            if (!Directory.Exists(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Boundless Dynamics, LLC",
                        "VTOLVR",
                        "SaveData")) ||
                !File.Exists(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Boundless Dynamics, LLC",
                        "VTOLVR",
                        "SaveData",
                        "pilots.cfg")))
            {
                Console.Log($"Couldn't find pilots.cfg at\n" +
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Boundless Dynamics, LLC",
                        "VTOLVR",
                        "SaveData"));
                Windows.Notification.Show("Couldn't find pilots.cfg", "Error", closedCallback: delegate { MainWindow.News(); });

                return false;
            }
            if (PilotsCFG == null)
                PilotsCFG = File.ReadAllLines(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Boundless Dynamics, LLC",
                        "VTOLVR",
                        "SaveData",
                        "pilots.cfg"));
            string result;
            List<Pilot> pilots = new List<Pilot>(1) { new Pilot("No Selection") };
            for (int i = 0; i < PilotsCFG.Length; i++)
            {
                result = Helper.ClearSpaces(PilotsCFG[i]);
                if (result.Contains("pilotName="))
                {
                    pilots.Add(new Pilot(result.Replace("pilotName=", string.Empty)));
                }
            }

            if (pilots.Count > 0)
            {
                PilotDropdown.ItemsSource = pilots;
                PilotDropdown.SelectedIndex = 0;
            }
            return true;
        }

        private void PilotChanged(object sender, EventArgs e)
        {
            Helper.SentryLog("Pilot Changed", Helper.SentryLogCategory.DevToos);
            PilotSelected = (Pilot)PilotDropdown.SelectedItem;
            SaveSettings();
            IsDevToolsEnabled();
        }

        private void ScenarioChanged(object sender, EventArgs e)
        {
            Helper.SentryLog("Scenario Changed", Helper.SentryLogCategory.DevToos);
            ScenarioSelected = (Scenario)ScenarioDropdown.SelectedItem;
            SaveSettings();
            IsDevToolsEnabled();
        }

        private void FindMods()
        {
            Helper.SentryLog("Finding Mods", Helper.SentryLogCategory.DevToos);
            Console.Log("Finding Mods for Dev Tools");
            DirectoryInfo folder = new DirectoryInfo(Program.root + Program.modsFolder);
            FileInfo[] files = folder.GetFiles("*.dll");
            List<ModItem> mods = new List<ModItem>();
            for (int i = 0; i < files.Length; i++)
            {
                if (ModsToLoad.Contains(files[i].FullName))
                    mods.Add(new ModItem(files[i].FullName, true));
                else
                    mods.Add(new ModItem(files[i].FullName));
            }

            DirectoryInfo[] folders = folder.GetDirectories();
            for (int i = 0; i < folders.Length; i++)
            {
                if (File.Exists(folders[i].FullName + "\\" + folders[i].Name + ".dll"))
                {
                    if (ModsToLoad.Contains(folders[i].FullName + "\\" + folders[i].Name + ".dll"))
                    {
                        mods.Add(new ModItem(folders[i].FullName + "\\" + folders[i].Name + ".dll", true));
                    }
                    else
                        mods.Add(new ModItem(folders[i].FullName + "\\" + folders[i].Name + ".dll"));
                }
            }

            //Finding users my projects mods
            if (!string.IsNullOrEmpty(Settings.ProjectsFolder))
            {
                DirectoryInfo projectsFolder = new DirectoryInfo(Settings.ProjectsFolder + ProjectManager.modsFolder);
                folders = projectsFolder.GetDirectories();
                for (int i = 0; i < folders.Length; i++)
                {
                    if (!File.Exists(Path.Combine(folders[i].FullName, "Builds", "info.json")))
                    {
                        Console.Log("Missing info.json in " +
                            Path.Combine(folders[i].FullName, "Builds", "info.json"));
                        continue;
                    }
                    JObject json;
                    try
                    {
                        json = JObject.Parse(File.ReadAllText(
                            Path.Combine(folders[i].FullName, "Builds", "info.json")));
                    }
                    catch (Exception e)
                    {
                        Console.Log($"Failed to read {Path.Combine(folders[i].FullName, "Builds", "info.json")}" +
                            $"{e.Message}");
                        continue;
                    }

                    if (json[ProjectManager.jDll] == null)
                    {
                        Console.Log($"Missing {ProjectManager.jDll} in {Path.Combine(folders[i].FullName, "Builds", "info.json")}");
                        continue;
                    }

                    if (!File.Exists(Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString())))
                    {
                        Console.Log($"Couldn't find {json[ProjectManager.jDll]} at " +
                            $"{Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString())}");
                        continue;
                    }

                    if (ModsToLoad.Contains(Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString())))
                    {
                        mods.Add(new ModItem(Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString()), true));
                    }
                    else
                    {
                        mods.Add(new ModItem(Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString())));
                    }
                }
            }
            this.mods.ItemsSource = mods;
            Console.Log($"Found {mods.Count} mods");
        }

        private void ModChecked(object sender, RoutedEventArgs e)
        {
            Helper.SentryLog("Mod Checked", Helper.SentryLogCategory.DevToos);
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked == true)
            {
                ModsToLoad.Add(checkBox.ToolTip.ToString());
                Console.Log($"Added {checkBox.ToolTip}");
            }
            else if (checkBox.IsChecked == false)
            {
                ModsToLoad.Remove(checkBox.ToolTip.ToString());
                Console.Log($"Removed {checkBox.ToolTip}");
            }
            SaveSettings();
            IsDevToolsEnabled();
        }

        private void SaveSettings()
        {
            Helper.SentryLog("Saving Settings", Helper.SentryLogCategory.DevToos);
            JObject jObject = new JObject();
            if (PilotSelected != null)
                jObject.Add("pilot", PilotSelected.Name);
            if (ScenarioSelected != null)
            {
                JObject scenario = new JObject();
                scenario.Add("name", ScenarioSelected.Name);
                scenario.Add("id", ScenarioSelected.ID);
                scenario.Add("cid", ScenarioSelected.cID);
                jObject.Add(new JProperty("scenario", scenario));
            }

            if (ModsToLoad.Count > 0)
            {
                JArray previousMods = new JArray(ModsToLoad.ToArray());
                jObject.Add(new JProperty("previousMods", previousMods));
            }

            try
            {
                File.WriteAllText(Program.root + savePath, jObject.ToString());
            }
            catch (Exception e)
            {
                Console.Log($"Failed to save {savePath}");
                Console.Log(e.Message);
            }


        }

        private void LoadSettings()
        {
            if (!File.Exists(Program.root + savePath))
                return;
            Helper.SentryLog("Loading Settings", Helper.SentryLogCategory.DevToos);
            JObject json;
            try
            {
                json = JObject.Parse(File.ReadAllText(Program.root + savePath));
            }
            catch (Exception e)
            {
                Console.Log("Error when reading " + savePath);
                Console.Log(e.ToString());
                return;
            }

            if (json["pilot"] != null)
                PilotSelected = new Pilot(json["pilot"].ToString());

            if (json["scenario"] != null)
            {
                JObject scenario = json["scenario"] as JObject;
                for (int i = 0; i < _scenarios.Count; i++)
                {
                    if (_scenarios[i].Name.Equals(scenario["name"].ToString()) &&
                        _scenarios[i].ID.Equals(scenario["id"].ToString()) &&
                        _scenarios[i].cID.Equals(scenario["cid"].ToString()))
                    {
                        ScenarioDropdown.SelectedIndex = i;
                        ScenarioSelected = (Scenario)ScenarioDropdown.SelectedItem;
                        break;
                    }
                }
            }

            if (json["previousMods"] != null)
            {
                JArray mods = json["previousMods"] as JArray;
                for (int i = 0; i < mods.Count; i++)
                {
                    if (!ModsToLoad.Contains(mods[i].ToString()))
                    {
                        if (File.Exists(mods[i].ToString()))
                            ModsToLoad.Add(mods[i].ToString());
                        else
                            Console.Log($"{mods[i]} isn't there are more");
                    }

                }
            }

            IsDevToolsEnabled();

            //Resaving it because of if a enabled mod was deleted,
            //we need to update that json file
            SaveSettings();

            if (Settings.USettings.AcceptedDevtools)
                ToggleWarning(Visibility.Hidden);
            else
                ToggleWarning(Visibility.Visible);
        }

        private void LoadScenarios()
        {
            Helper.SentryLog("Loading Scenarios", Helper.SentryLogCategory.DevToos);
            JObject json = null;

            try
            {
                if (!File.Exists(Program.root + gameData))
                {
                    json = JObject.Parse(Properties.Resources.CampaignsJsonString);
                }
                else
                {
                    json = JObject.Parse(File.ReadAllText(Program.root + gameData));
                }
            }
            catch (Exception e)
            {
                Console.Log("Error when reading Campaigns");
                if (e.ToString() != null)
                    Console.Log(e.ToString());
            }
            if (json == null)
                return;
            AddScenarios(json);
        }

        private void AddScenarios(JObject json)
        {
            Helper.SentryLog("Adding Scenarios", Helper.SentryLogCategory.DevToos);
            _scenarios.Add(new Scenario("No Selection", string.Empty, string.Empty));
            if (json["Campaigns"] != null)
            {
                JArray campaignJArray = json["Campaigns"] as JArray;
                JArray scenariosJArray;
                for (int i = 0; i < campaignJArray.Count; i++)
                {
                    scenariosJArray = campaignJArray[i]["Scenarios"] as JArray;
                    for (int s = 0; s < scenariosJArray.Count; s++)
                    {
                        _scenarios.Add(new Scenario(
                            campaignJArray[i]["Vehicle"].ToString() + " " + scenariosJArray[s]["Name"].ToString(),
                            campaignJArray[i]["CampaignID"].ToString(),
                            scenariosJArray[s]["Id"].ToString()));
                    }
                }
            }

            ScenarioDropdown.ItemsSource = _scenarios.ToArray();
            ScenarioDropdown.SelectedIndex = 0;
        }

        private void IsDevToolsEnabled()
        {
            Helper.SentryLog("Checking if dev tools is enabled", Helper.SentryLogCategory.DevToos);
            DevToolsEnabled = false;
            if (PilotSelected != null &&
                ScenarioSelected != null &&
                !PilotSelected.Name.Equals("No Selection") &&
                !ScenarioSelected.Name.Equals("No Selection"))
            {
                DevToolsEnabled = true;
            }
            if (ModsToLoad != null && ModsToLoad.Count > 0)
            {
                DevToolsEnabled = true;
            }
            MainWindow.DevToolsWarning(DevToolsEnabled);
        }

        private void TakeBack(object sender, RoutedEventArgs e)
        {
            MainWindow.GoHome();
        }

        private void IsModCreator(object sender, RoutedEventArgs e)
        {
            Settings.USettings.AcceptedDevtools = true;
            Settings.SaveSettings();
            ToggleWarning(Visibility.Hidden);
        }
        private void ToggleWarning(Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                    _warning.Visibility = Visibility.Visible;

                    _title.Visibility = Visibility.Hidden;
                    _missionLoadingTitle.Visibility = Visibility.Hidden;
                    _missionGrid.Visibility = Visibility.Hidden;
                    _missionPilotGrid.Visibility = Visibility.Hidden;
                    _modLoadingTitle.Visibility = Visibility.Hidden;
                    _modLoadingScrollViewer.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Hidden:
                    _warning.Visibility = Visibility.Hidden;

                    _title.Visibility = Visibility.Visible;
                    _missionLoadingTitle.Visibility = Visibility.Visible;
                    _missionGrid.Visibility = Visibility.Visible;
                    _missionPilotGrid.Visibility = Visibility.Visible;
                    _modLoadingTitle.Visibility = Visibility.Visible;
                    _modLoadingScrollViewer.Visibility = Visibility.Visible;
                    break;
            }

        }
    }
    public class ModItem
    {
        public string ModName { get; set; }
        public bool LoadMod { get; set; }
        public ModItem(string modName)
        {
            ModName = modName;
        }

        public ModItem(string modName, bool loadMod)
        {
            ModName = modName;
            LoadMod = loadMod;
        }
    }
}
