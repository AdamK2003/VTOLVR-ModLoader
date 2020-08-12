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

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for DevTools.xaml
    /// </summary>
    public partial class DevTools : UserControl
    {
        private const string savePath = "/devtools.json";
        private const string gameData = "/gamedata.json";
        public Pilot pilotSelected;
        public Scenario scenarioSelected;
        public List<string> modsToLoad = new List<string>();
        public string[] pilotsCFG;

        private List<Scenario> _scenarios = new List<Scenario>();

        public DevTools()
        {
            InitializeComponent();
            LoadScenarios();
            LoadSettings();
        }
        public void SetUI()
        {
            LoadSettings();
            FindMods();
            if (!FindPilots())
                return;
            if (pilotSelected != null)
            {
                foreach (Pilot p in PilotDropdown.ItemsSource)
                {
                    if (p.Name == pilotSelected.Name)
                    {
                        PilotDropdown.SelectedItem = p;
                        break;
                    }
                }
            }
            if (scenarioSelected != null)
            {
                foreach (Scenario s in ScenarioDropdown.ItemsSource)
                {
                    if (s.ID == scenarioSelected.ID)
                    {
                        ScenarioDropdown.SelectedItem = s;
                        break;
                    }
                }
            }
        }

        private bool FindPilots()
        {
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
            if (pilotsCFG == null)
                pilotsCFG = File.ReadAllLines(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Boundless Dynamics, LLC",
                        "VTOLVR",
                        "SaveData",
                        "pilots.cfg"));
            string result;
            List<Pilot> pilots = new List<Pilot>(1) { new Pilot("No Selection") };
            for (int i = 0; i < pilotsCFG.Length; i++)
            {
                result = Helper.ClearSpaces(pilotsCFG[i]);
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
            pilotSelected = (Pilot)PilotDropdown.SelectedItem;
            SaveSettings();
        }

        private void ScenarioChanged(object sender, EventArgs e)
        {
            scenarioSelected = (Scenario)ScenarioDropdown.SelectedItem;
            SaveSettings();
        }

        private void FindMods()
        {
            Console.Log("Finding Mods for Dev Tools");
            DirectoryInfo folder = new DirectoryInfo(Program.root + Program.modsFolder);
            FileInfo[] files = folder.GetFiles("*.dll");
            List<ModItem> mods = new List<ModItem>();
            for (int i = 0; i < files.Length; i++)
            {
                if (modsToLoad.Contains(files[i].Name))
                    mods.Add(new ModItem(files[i].Name, true));
                else
                    mods.Add(new ModItem(files[i].Name));
            }

            DirectoryInfo[] folders = folder.GetDirectories();
            for (int i = 0; i < folders.Length; i++)
            {
                if (File.Exists(folders[i].FullName + "/" + folders[i].Name + ".dll"))
                {
                    if (modsToLoad.Contains(folders[i].FullName + "/" + folders[i].Name + ".dll"))
                    {
                        mods.Add(new ModItem(folders[i].FullName + "/" + folders[i].Name + ".dll", true));
                    }
                    else
                        mods.Add(new ModItem(folders[i].FullName + "/" + folders[i].Name + ".dll"));
                }
            }

            //Finding users my projects mods
            if (!string.IsNullOrEmpty(Settings.projectsFolder))
            {
                DirectoryInfo projectsFolder = new DirectoryInfo(Settings.projectsFolder + ProjectManager.modsFolder);
                folders = projectsFolder.GetDirectories();
                for (int i = 0; i < folders.Length; i++)
                {
                    if (!File.Exists(Path.Combine(folders[i].FullName,"Builds", "info.json")))
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

                    if (modsToLoad.Contains(Path.Combine(folders[i].FullName, "Builds", json[ProjectManager.jDll].ToString())))
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
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked == true)
            {
                modsToLoad.Add(checkBox.ToolTip.ToString());
                Console.Log($"Added {checkBox.ToolTip}");
            }
            else if (checkBox.IsChecked == false)
            {
                modsToLoad.Remove(checkBox.ToolTip.ToString());
                Console.Log($"Removed {checkBox.ToolTip}");
            }
            SaveSettings();
        }

        private void SaveSettings()
        {
            JObject jObject = new JObject();
            if (pilotSelected != null)
                jObject.Add("pilot", pilotSelected.Name);
            if (scenarioSelected != null)
            {
                JObject scenario = new JObject();
                scenario.Add("name", scenarioSelected.Name);
                scenario.Add("id", scenarioSelected.ID);
                scenario.Add("cid", scenarioSelected.cID);
                jObject.Add(new JProperty("scenario", scenario));
            }

            if (modsToLoad.Count > 0)
            {
                JArray previousMods = new JArray(modsToLoad.ToArray());
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
            

            pilotSelected = new Pilot(json["pilot"].Value<string>()) ?? null;

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
                        scenarioSelected = (Scenario)ScenarioDropdown.SelectedItem;
                        break;
                    }
                }
            }

            if (json["previousMods"] != null)
            {
                JArray mods = json["previousMods"] as JArray;
                for (int i = 0; i < mods.Count; i++)
                {
                    if (!modsToLoad.Contains(mods[i].ToString()))
                        modsToLoad.Add(mods[i].ToString());
                }
            }
        }

        private void LoadScenarios()
        {
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
