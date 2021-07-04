using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LauncherCore.Classes;
using CoreCore.Jsons;
using CoreCore.Enums;
using LauncherCore.Classes.Workshop;

namespace LauncherCore.Views
{
    /// <summary>
    /// Interaction logic for DevTools.xaml
    /// </summary>
    public partial class DevTools : UserControl
    {
        private const string savePath = "/devtools.json";
        private const string gameData = "/gamedata.json";
        public static bool DevToolsEnabled { get; private set; } = false;
        public static string[] PilotsCFG;
        public static CoreCore.Jsons.DevTools Values;

        private static List<Scenario> _scenarios = new List<Scenario>();

        public DevTools()
        {
            InitializeComponent();
            LoadScenarios();
            //LoadWorkshop();

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

            ScenarioDropdown.ItemsSource = _scenarios.ToArray();
            ScenarioDropdown.SelectedIndex = 0;

            if (!FindPilots())
                return;
            if (Values.Scenario != null)
            {
                foreach (string pilotName in PilotDropdown.ItemsSource)
                {
                    if (pilotName == Values.Scenario.Pilot)
                    {
                        PilotDropdown.SelectedItem = pilotName;
                        break;
                    }
                }

                foreach (Scenario s in ScenarioDropdown.ItemsSource)
                {
                    if (s.Equals(Values.Scenario))
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
                Windows.Notification.Show("Couldn't find pilots.cfg", "Error",
                    closedCallback: delegate { MainWindow.News(); });

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
            List<string> pilots = new List<string>(1) {"No Selection"};
            for (int i = 0; i < PilotsCFG.Length; i++)
            {
                result = Helper.ClearSpaces(PilotsCFG[i]);
                if (result.Contains("pilotName="))
                {
                    pilots.Add(result.Replace("pilotName=", string.Empty));
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
            Values.Scenario.Pilot = PilotDropdown.SelectedItem.ToString();
            SaveSettings();
            IsDevToolsEnabled();
        }

        private void ScenarioChanged(object sender, EventArgs e)
        {
            Helper.SentryLog("Scenario Changed", Helper.SentryLogCategory.DevToos);
            string pilot = Values.Scenario.Pilot;
            Values.Scenario = (Scenario)ScenarioDropdown.SelectedItem;
            Values.Scenario.Pilot = pilot;
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

                if (Values.PreviousMods.Contains(items[i].Directory.FullName))
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
                Values.PreviousMods.Add(checkBox.ToolTip.ToString());
                Console.Log($"Added {checkBox.ToolTip}");
            }
            else if (checkBox.IsChecked == false)
            {
                Values.PreviousMods.Remove(checkBox.ToolTip.ToString());
                Console.Log($"Removed {checkBox.ToolTip}");
            }

            SaveSettings();
            IsDevToolsEnabled();
        }

        private void SaveSettings()
        {
            Helper.SentryLog("Saving Settings", Helper.SentryLogCategory.DevToos);
            Values.SaveFile(Program.Root + savePath);
        }

        private void LoadSettings()
        {
            if (!File.Exists(Program.Root + savePath))
            {
                Helper.SentryLog("Creating new devtools.json", Helper.SentryLogCategory.DevToos);

                var newDevTools = new CoreCore.Jsons.DevTools() {Scenario = new Scenario()};

                newDevTools.SaveFile(Program.Root + savePath);
            }

            Helper.SentryLog("Loading Settings", Helper.SentryLogCategory.DevToos);

            Values = CoreCore.Jsons.DevTools.GetDevTools(
                File.ReadAllText(Program.Root + savePath));

            if (Values == null)
                return;
            Console.Log("Loading Dev Tools");

            if (Values.PreviousMods != null)
            {
                List<string> mods = Values.PreviousMods;
                for (int i = 0; i < mods.Count; i++)
                {
                    if (!Values.PreviousMods.Contains(mods[i].ToString()))
                    {
                        if (Directory.Exists(mods[i].ToString()))
                        {
                            Values.PreviousMods.Add(mods[i].ToString());
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

        private void LoadWorkshop()
        {
            List<WorkshopItem> items = VTWorkshopDecoder.GetWorkshopScenarios();
            for (int i = 0; i < items.Count; i++)
            {
                _scenarios.Add(new Scenario()
                {
                    ScenarioName = items[i].ScenarioName, ScenarioID = items[i].ScenarioID, IsWorkshop = true
                });
            }
        }


        private void AddScenarios(JObject json)
        {
            Helper.SentryLog("Adding Scenarios", Helper.SentryLogCategory.DevToos);
            _scenarios.Add(new Scenario() {ScenarioName = "No Selection"});
            if (json["Campaigns"] != null)
            {
                JArray campaignJArray = json["Campaigns"] as JArray;
                JArray scenariosJArray;
                for (int i = 0; i < campaignJArray.Count; i++)
                {
                    scenariosJArray = campaignJArray[i]["Scenarios"] as JArray;
                    for (int s = 0; s < scenariosJArray.Count; s++)
                    {
                        _scenarios.Add(new Scenario()
                        {
                            ScenarioName = campaignJArray[i]["Vehicle"].ToString() + " " +
                                           scenariosJArray[s]["Name"].ToString(),
                            CampaignID = campaignJArray[i]["CampaignID"].ToString(),
                            ScenarioID = scenariosJArray[s]["Id"].ToString()
                        });
                    }
                }
            }
        }

        private void IsDevToolsEnabled()
        {
            Helper.SentryLog("Checking if dev tools is enabled", Helper.SentryLogCategory.DevToos);
            DevToolsEnabled = false;
            if (Values.PreviousMods != null && Values.PreviousMods.Count != 0)
            {
                DevToolsEnabled = true;
            }

            if (Values.Scenario is not {Pilot: "No Selection" or "", ScenarioName: "No Selection" or ""})
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