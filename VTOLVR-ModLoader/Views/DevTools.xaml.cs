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
using Core.Jsons;
using Core.Enums;
using Scenario = VTOLVR_ModLoader.Classes.Scenario;

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

            if (Settings.USettings.AcceptedDevtools)
                ToggleWarning(Visibility.Hidden);
            else
                ToggleWarning(Visibility.Visible);

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
            List<BaseItem> items = Program.Items;
            List<ModItem> mods = new List<ModItem>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ContentType == ContentType.Skins ||
                    items[i].ContentType == ContentType.MySkins)
                    continue;

                if (ModsToLoad.Contains(items[i].Directory.FullName))
                    mods.Add(new ModItem(items[i].Directory.FullName, true));
                else
                    mods.Add(new ModItem(items[i].Directory.FullName));
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
            Core.Jsons.DevTools devTools = new Core.Jsons.DevTools();
            devTools.Scenario = new Core.Jsons.Scenario();

            if (PilotSelected != null)
                devTools.Scenario.Pilot = PilotSelected.Name;

            if (ScenarioSelected != null)
            {
                devTools.Scenario.ScenarioName = ScenarioSelected.Name;
                devTools.Scenario.ScenarioID = ScenarioSelected.ID;
                devTools.Scenario.CampaignID = ScenarioSelected.cID;
            }

            devTools.PreviousMods = ModsToLoad;

            devTools.SaveFile(Program.Root + savePath);
        }

        private void LoadSettings()
        {
            if (!File.Exists(Program.Root + savePath))
                return;
            Helper.SentryLog("Loading Settings", Helper.SentryLogCategory.DevToos);

            Core.Jsons.DevTools devTools = Core.Jsons.DevTools.GetDevTools(
                File.ReadAllText(Program.Root + savePath));

            if (devTools == null)
                return;

            if (devTools.Scenario != null)
            {
                if (devTools.Scenario.Pilot != string.Empty)
                    PilotSelected = new Pilot(devTools.Scenario.Pilot);

                if (devTools.Scenario.ScenarioID != string.Empty &&
                    devTools.Scenario.CampaignID != string.Empty)
                {
                    for (int i = 0; i < _scenarios.Count; i++)
                    {
                        if (_scenarios[i].Name.Equals(devTools.Scenario.ScenarioName) &&
                            _scenarios[i].ID.Equals(devTools.Scenario.ScenarioID) &&
                            _scenarios[i].cID.Equals(devTools.Scenario.CampaignID))
                        {
                            ScenarioDropdown.SelectedIndex = i;
                            ScenarioSelected = (Scenario)ScenarioDropdown.SelectedItem;
                            break;
                        }
                    }
                }

            }

            if (devTools.PreviousMods != null)
            {
                List<string> mods = devTools.PreviousMods;
                for (int i = 0; i < mods.Count; i++)
                {
                    if (!ModsToLoad.Contains(mods[i].ToString()))
                    {
                        if (Directory.Exists(mods[i].ToString()))
                        {
                            ModsToLoad.Add(mods[i].ToString());
                        }
                        else
                            Console.Log($"{mods[i]} isn't there are more");
                    }
                }
            }

            IsDevToolsEnabled();

            //Resaving it because of if a enabled mod was deleted,
            //we need to update that json file
            SaveSettings();
        }

        private void LoadScenarios()
        {
            Helper.SentryLog("Loading Scenarios", Helper.SentryLogCategory.DevToos);
            JObject json = null;

            try
            {
                if (!File.Exists(Program.Root + gameData))
                {
                    json = JObject.Parse(Properties.Resources.CampaignsJsonString);
                }
                else
                {
                    json = JObject.Parse(File.ReadAllText(Program.Root + gameData));
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
