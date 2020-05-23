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
using System.Xml.Serialization;
using VTOLVR_ModLoader.Classes;

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
            FindPilots();
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

        private void CreateInfo(object sender, RoutedEventArgs e)
        {
            Mod newMod = new Mod(modName.Text, modDescription.Text);

            Directory.CreateDirectory(Program.root + $"\\mods\\{modName.Text}");

            using (FileStream stream = new FileStream(Program.root + $"\\mods\\{modName.Text}\\info.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Mod));
                xml.Serialize(stream, newMod);
            }

            MessageBox.Show("Created info.xml in \n\"" + Program.root + $"\\mods\\{modName.Text}\"", "Created Info.xml", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FindPilots()
        {
            if (pilotsCFG == null)
                pilotsCFG = File.ReadAllLines(Program.vtolFolder + @"\SaveData\pilots.cfg");
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
                    if (modsToLoad.Contains(folders[i].Name + "/" + folders[i].Name + ".dll"))
                    {
                        mods.Add(new ModItem(folders[i].Name + "/" + folders[i].Name + ".dll", true));
                    }
                    else
                        mods.Add(new ModItem(folders[i].Name + "/" + folders[i].Name + ".dll"));
                }
            }
            this.mods.ItemsSource = mods;
        }

        private void ModChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked == true)
            {
                modsToLoad.Add(checkBox.ToolTip.ToString());
            }
            else if (checkBox.IsChecked == false)
            {
                modsToLoad.Remove(checkBox.ToolTip.ToString());
                Console.Log($"Removed {checkBox.ToolTip.ToString()}");
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
                scenarioSelected = new Scenario(scenario["name"].ToString(),
                                                scenario["id"].ToString(),
                                                scenario["cid"].ToString());
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
            List<Scenario> scenarios = new List<Scenario>();
            scenarios.Add(new Scenario("No Selection", string.Empty, string.Empty));
            if (json["Campaigns"] != null)
            {
                JArray campaignJArray = json["Campaigns"] as JArray;
                JArray scenariosJArray;
                for (int i = 0; i < campaignJArray.Count; i++)
                {
                    scenariosJArray = campaignJArray[i]["Scenarios"] as JArray;
                    for (int s = 0; s < scenariosJArray.Count; s++)
                    {
                        scenarios.Add(new Scenario(
                            campaignJArray[i]["Vehicle"].ToString() + " " + scenariosJArray[s]["Name"].ToString(),
                            campaignJArray[i]["CampaignID"].ToString(),
                            scenariosJArray[s]["Id"].ToString()));
                    }
                }
            }

            ScenarioDropdown.ItemsSource = scenarios.ToArray();
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
