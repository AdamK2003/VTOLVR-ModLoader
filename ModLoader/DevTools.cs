using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Jsons;
using System.IO;
using UnityEngine;

namespace ModLoader
{
    static class DevTools
    {
        public static Scenario Scenario
        {
            get
            {
                return _scenario;
            }
            private set
            {
                if (value.Pilot == "No Selection" ||
                    value.ScenarioName == "No Selection" ||
                    value.ScenarioID == string.Empty)
                {
                    _scenario = null;
                    Warning("Scenario is null");
                    return;
                }
                _scenario = value;
            }
        }
        private static Scenario _scenario;

        public static void ReadDevTools()
        {
            string path = Path.Combine(ModLoaderManager.RootPath, "devtools.json");
            if (!File.Exists(path))
            {
                Warning($"Dev Tools file doesn't exist.\nChecked:{path}");
                return;
            }

            Log("Reading Dev Tools from " + path);
            Core.Jsons.DevTools devTools = Core.Jsons.DevTools.GetDevTools(File.ReadAllText(path));

            if (devTools == null)
                return;

            if (devTools.PreviousMods != null)
                LoadMods(devTools.PreviousMods);

            Scenario = devTools.Scenario;
            Log("This is scenario: " + devTools.Scenario);
        }

        private static void LoadMods(List<string> mods)
        {
            Log($"Found {mods.Count} mods to load");
            for (int i = 0; i < mods.Count; i++)
            {
                for (int j = 0; j < ModReader.Items.Count; j++)
                {
                    if (mods[i].ToString() == ModReader.Items[j].Directory.FullName)
                    {
                        ModLoaderManager.Instance.LoadMod(ModReader.Items[j]);
                        break;
                    }
                }
            }
        }

        private static async Task SelectPilot()
        {
            Log("Loading Pilots");
            PilotSaveManager.LoadPilotsFromFile();
            await Task.Delay(TimeSpan.FromSeconds(2));

            VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
            Log("Setting Pilot");
            PilotSaveManager.current = PilotSaveManager.pilots[Scenario.Pilot];
        }

        public static async void LoadWorkshopMission()
        {
            await SelectPilot();

            VTResources.SteamWorkshopItemRequest<VTScenarioInfo> result =
                VTResources.LoadSteamWorkshopScenarios();

            Log("Loading workshop scenarios");
            while (!result.done)
            {
                Log("Loading " + result.progress);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Log("Finished loading workshop Scenarios");


            Log("Going through all workshop campaigns");
            VTScenarioInfo scenarioInfo =
                VTResources.GetSteamWorkshopStandaloneScenario(Scenario.ScenarioID);

            if (scenarioInfo == null)
            {
                Debug.LogError($"Couldn't find {Scenario.ScenarioID}");
                return;
            }

            VTScenario.currentScenarioInfo = scenarioInfo;
            PlayerVehicle vehicle = scenarioInfo.vehicle;
            PilotSaveManager.currentVehicle = vehicle;
            PilotSaveManager.current.lastVehicleUsed = vehicle.vehicleName;

            VTScenario.LaunchScenario(scenarioInfo);
            await Task.Delay(TimeSpan.FromSeconds(5));
            ReadyUp();
            Log("Player is ready");
        }

        public static async void LoadBuiltInMission()
        {
            await SelectPilot();
            Debug.Log("Going though All built in campaigns");
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
                {
                    if (info.campaignID == DevTools.Scenario.CampaignID)
                    {
                        Debug.Log("Setting Campaign");
                        PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                        Debug.Log("Setting Vehicle");
                        PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                        break;
                    }
                }
            }
            else
                Debug.Log("Campaigns are null");

            Debug.Log("Going though All missions in that campaign");
            foreach (CampaignScenario cs in PilotSaveManager.currentCampaign.missions)
            {
                if (cs.scenarioID == DevTools.Scenario.ScenarioID)
                {
                    Debug.Log("Setting Scenario");
                    PilotSaveManager.currentScenario = cs;
                    break;
                }
            }

            VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

            Debug.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName, DevTools.Scenario.Pilot));

            VTScenario.LaunchScenario(VTScenario.currentScenarioInfo);

            await Task.Delay(TimeSpan.FromSeconds(5));
            ReadyUp();
            Log("Player Ready");
        }

        private static void ReadyUp() => LoadingSceneController.instance.PlayerReady();

        private static void Log(object message) => Debug.Log($"[Dev Tools]{message}");

        private static void Warning(object message) => Debug.LogWarning($"[Dev Tools]{message}");

        private static void Error(object message) => Debug.LogError($"[Dev Tools]{message}");
    }
}
