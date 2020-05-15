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
        public Pilot pilotSelected;
        public Scenario scenarioSelected;
        public List<string> modsToLoad = new List<string>();
        public string[] pilotsCFG;

        public DevTools()
        {
            InitializeComponent();
            AddDefaultScenarios();
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

            Directory.CreateDirectory(MainWindow.root + $"\\mods\\{modName.Text}");

            using (FileStream stream = new FileStream(MainWindow.root + $"\\mods\\{modName.Text}\\info.xml", FileMode.Create))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Mod));
                xml.Serialize(stream, newMod);
            }

            MessageBox.Show("Created info.xml in \n\"" + MainWindow.root + $"\\mods\\{modName.Text}\"", "Created Info.xml", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FindPilots()
        {
            if (pilotsCFG == null)
                pilotsCFG = File.ReadAllLines(MainWindow.vtolFolder + @"\SaveData\pilots.cfg");
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

        private void AddDefaultScenarios()
        {
            ScenarioDropdown.ItemsSource = new Scenario[]
            {
                new Scenario("No Selection","",""),
                new Scenario("AV-42C - Preparations", "av42cTheIsland", "01_preparations"),
                new Scenario("AV-42C - Minesweeper", "av42cTheIsland", "02_minesweeper"),
                new Scenario("AV-42C - Redirection", "av42cTheIsland", "03_redirection"),
                new Scenario("AV-42C - Open Water", "av42cTheIsland", "04_openWater"),
                new Scenario("AV-42C - Silent Island", "av42cTheIsland", "05_silentIsland"),
                new Scenario("AV-42C - Darkness", "av42cTheIsland", "06_darkness"),
                new Scenario("AV-42C - Island Defense", "av42cTheIsland", "07_islandDefense"),
                new Scenario("AV-42C - Free Flight", "av42cQuickFlight", "freeFlight"),
                new Scenario("AV-42C - Target Practice", "av42cQuickFlight", "targetPractice"),
                new Scenario("AV-42C - Aerial Refueling Practice", "av42cQuickFlight", "aerialRefuelPractice"),
                new Scenario("AV-42C - Naval Landing Practice", "av42cQuickFlight", "carrierLanding"),
                new Scenario("F/A-26B - Free Flight", "fa26bFreeFlight", "Free Flight"),
                new Scenario("F/A-26B - Target Practice", "fa26bFreeFlight", "targetPractice"),
                new Scenario("F/A-26B - Carrier Landing Practice", "fa26bFreeFlight", "carrierLandingPractice"),
                new Scenario("F/A-26B - FA-26 Aerial Refuel Practice", "fa26bFreeFlight", "fa26Refuel"),
                new Scenario("F/A-26B - 2v2 Air Combat", "fa26bFreeFlight", "2v2dogfight"),
                new Scenario("F/A-26B - Difficult Mission", "fa26bFreeFlight", "FA26Difficult"),
                new Scenario("F/A-26B - July 4th", "j4Campaign", "j4"),
                new Scenario("F/A-26B - Base Defense", "fa26-opDesertCobra", "mission1"),
                new Scenario("F/A-26B - Retalliation", "fa26-opDesertCobra", "mission2"),
                new Scenario("F/A-26B - Strike on Naval Test Lake", "fa26-opDesertCobra", "mission3"),
                new Scenario("F/A-26B - Tanker Escort", "fa26-opDesertCobra", "mission4"),
                new Scenario("F/A-26B - Departure", "fa26-opDesertCobra", "mission5"),
                new Scenario("F/A-26B - Northern Assault", "fa26-opDesertCobra", "mission6"),
                new Scenario("F/A-26B - Striking Oil", "fa26-opDesertCobra", "mission7"),
                new Scenario("F-45A - Free Flight", "f45-quickFlight", "f45-freeFlight"),
                new Scenario("F-45A - Stealth Strike", "f45-quickFlight", "f45_quickMission1")
            };
            ScenarioDropdown.SelectedIndex = 0;
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
            DirectoryInfo folder = new DirectoryInfo(MainWindow.root + MainWindow.modsFolder);
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
                File.WriteAllText(MainWindow.root + savePath, jObject.ToString());
            }
            catch (Exception e)
            {
                Console.Log($"Failed to save {savePath}");
                Console.Log(e.Message);
            }

            
        }

        private void LoadSettings()
        {
            if (!File.Exists(MainWindow.root + savePath))
                return;
            JObject json;
            try
            {
                json = JObject.Parse(File.ReadAllText(MainWindow.root + savePath));
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
                    modsToLoad.Add(mods[i].ToString());
                }
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
