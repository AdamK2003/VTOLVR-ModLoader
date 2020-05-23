using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.CrashReportHandler;
using Steamworks;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using System.Net;
using System.ComponentModel;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            PlayerLogText();
            CrashReportHandler.enableCaptureExceptions = false;
            new GameObject("Mod Loader Manager", typeof(ModLoaderManager), typeof(SkinManager));
        }
        private static void PlayerLogText()
        {
            string playerLogMessage = @" 
                                                                                                         
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #     #                                              #     #                                            
 ##   ##   ####   #####   #####   ######  #####       #     #  ######  #####    ####   #   ####   #    # 
 # # # #  #    #  #    #  #    #  #       #    #      #     #  #       #    #  #       #  #    #  ##   # 
 #  #  #  #    #  #    #  #    #  #####   #    #      #     #  #####   #    #   ####   #  #    #  # #  # 
 #     #  #    #  #    #  #    #  #       #    #       #   #   #       #####        #  #  #    #  #  # # 
 #     #  #    #  #    #  #    #  #       #    #        # #    #       #   #   #    #  #  #    #  #   ## 
 #     #   ####   #####   #####   ######  #####          #     ######  #    #   ####   #   ####   #    # 

Thank you for download VTOL VR Mod loader by . Marsh.Mello .

Please don't report bugs unless you can reproduce them without any mods loaded
if you are having any issues with mods and would like to report a bug, please contact @. Marsh.Mello .#0001 
on the offical VTOL VR Discord or post an issue on github. 

VTOL VR Modding Discord Server: https://discord.gg/XZeeafp
Mod Loader Github: https://github.com/MarshMello0/VTOLVR-ModLoader
Mod Loader Website: https://vtolvr-mods.com/

Special Thanks to Ketkev for his continuous support to the mod loader and the website.

 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
";
            Debug.Log(playerLogMessage);
        }
    }

    /// <summary>
    /// This class is to handle the changes between scenes
    /// </summary>
    class ModLoaderManager : MonoBehaviour
    {
        public static ModLoaderManager instance { get; private set; }


        private VTOLAPI api;
        public string rootPath;
        private string[] args;

        private bool loadMission;
        private string pilotName = "";
        private string cID = "";
        private string sID = "";


        //Discord
        private DiscordController discord;
        public string discordDetail, discordState;
        public int loadedModsCount;

        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            SetPaths();
            Debug.Log("This is the first mod loader manager");
            args = Environment.GetCommandLineArgs();

            CreateAPI();

            gameObject.AddComponent<TCPConsole>();


            discord = gameObject.AddComponent<DiscordController>();
            discordDetail = "Launching Game";
            discordState = ". Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();

            CheckDevTools();

            SceneManager.sceneLoaded += SceneLoaded;

            //gameObject.AddComponent<CSharp>();

            api.CreateCommand("quit", delegate { Application.Quit(); });
            api.CreateCommand("print", PrintMessage);
            api.CreateCommand("help", api.ShowHelp);
            api.CreateCommand("vrinteract", VRInteract);
            api.CreateCommand("loadmod", LoadMod);
        }

        private void CreateAPI()
        {
            api = gameObject.AddComponent<VTOLAPI>();
        }
        private void SetPaths()
        {
            rootPath = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string sceneName = arg0.name;
            Debug.Log("Scene Loaded = " + sceneName);
            switch (sceneName)
            {
                case "SamplerScene":
                    DataCollector.CollectData();
                    discordDetail = "Selecting mods";
                    StartCoroutine(CreateModLoader());
                    if (loadMission)
                        StartCoroutine(LoadLevel());
                    break;
                case "Akutan":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "Akutan: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "CustomMapBase":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "CustomMap: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "LoadingScene":
                    discordDetail = "Loading into mission";
                    break;
                case "ReadyRoom":
                    if (loadedModsCount == 0)
                    {
                        discordDetail = "In Main Menu";
                    }
                    else
                    {
                        discordDetail = "In Main Menu with " + loadedModsCount + (loadedModsCount == 0 ? " mod" : " mods");
                    }
                    break;
                case "VehicleConfiguration":
                    discordDetail = "Configuring " + PilotSaveManager.currentVehicle.vehicleName;
                    break;
                case "LaunchSplashScene":
                    break;
                default:
                    Debug.Log("ModLoader.cs | Scene not found (" + sceneName + ")");
                    break;
            }
            UpdateDiscord();
        }
        public void UpdateDiscord()
        {
            Debug.Log("Updating Discord...");
            discord.UpdatePresence(loadedModsCount, discordDetail, discordState);
        }
        private IEnumerator CreateModLoader()
        {
            Debug.Log("Creating Mod Loader");
            while (SceneManager.GetActiveScene().name != "SamplerScene")
            {
                Debug.Log("Waiting for active Scene");
                yield return null;
            }
            Debug.Log("Creating new gameobject");
            GameObject modloader = new GameObject("Mod Loader", typeof(ModLoader));
            DontDestroyOnLoad(modloader);
        }

        public static void PrintMessage(string obj)
        {
            obj.Remove(0, 5);
            Debug.Log(obj);
        }
        public static void VRInteract(string message)
        {
            message = message.Replace("vrinteract ","");
            Debug.Log($"Searching for gameobject :{message}");
            GameObject go = GameObject.Find(message);
            if (go == null)
            {
                Debug.LogError($"Couldn't find gameobject :{message}");
                return;
            }
            VRInteractable interactable = go.GetComponent<VRInteractable>();
            if (interactable == null)
            {
                Debug.LogError($"The object ({message}) does not have a VRInteractable attached");
                return;
            }
            HarmonyLib.Traverse.Create(interactable).Method("StartInteraction").GetValue();
            Debug.Log($"Invoked OnInteract on GameObject {message}");
        }
        public void LoadMod(string message)
        {
            message = message.Replace("loadmod ", string.Empty);
            try
            {
                Debug.Log(rootPath + @"\mods\" + message);
                IEnumerable<Type> source =
          from t in Assembly.Load(File.ReadAllBytes(rootPath + @"\mods\" + message)).GetTypes()
          where t.IsSubclassOf(typeof(VTOLMOD))
          select t;
                if (source != null && source.Count() == 1)
                {
                    GameObject newModGo = new GameObject(message, source.First());
                    VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                    mod.SetModInfo(new Mod(message, "STARTUPMOD", message));
                    newModGo.name = message;
                    DontDestroyOnLoad(newModGo);
                    mod.ModLoaded();

                    ModLoaderManager.instance.loadedModsCount++;
                    ModLoaderManager.instance.UpdateDiscord();
                }
                else
                {
                    Debug.LogError("Source is null");
                }

                Debug.Log("Loaded Startup mod from path = " + message);
            }
            catch (Exception e)
            {
                Debug.LogError("Error when loading startup mod\n" + e.ToString());
            }
        }
        private void CheckDevTools()
        {
            if (File.Exists(rootPath + "/devtools.json"))
            {
                ReadDevTools(File.ReadAllText(rootPath + "/devtools.json"));
            }
        }
        private void ReadDevTools(string jsonString)
        {
            JObject json;
            try
            {
                json = JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                Debug.LogError("Error when reading devtools.json");
                Debug.LogError(e.ToString());
                return;
            }


            if (json["scenario"] != null)
            {
                pilotName = json["pilot"].Value<string>() ?? null;
                if (pilotName == "No Selection" || string.IsNullOrEmpty(pilotName))
                    return;

                JObject scenario = json["scenario"] as JObject;
                string scenarioName = scenario["name"].ToString();
                string sID = scenario["id"].ToString();
                string cID = scenario["cid"].ToString();

                if (scenarioName == "No Selection" || string.IsNullOrEmpty(sID))
                    return;
                loadMission = true;
            }
        }
        private IEnumerator LoadLevel()
        {
            loadMission = false;
            Debug.Log("Loading Pilots from file");
            PilotSaveManager.LoadPilotsFromFile();
            yield return new WaitForSeconds(2);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("PILOT="))
                {
                    pilotName = args[i].Replace("PILOT=", "");
                }
                else if (args[i].Contains("SCENARIO_CID="))
                {
                    cID = args[i].Replace("SCENARIO_CID=", "");
                }
                else if (args[i].Contains("SCENARIO_ID="))
                {
                    sID = args[i].Replace("SCENARIO_ID=", "");
                }
            }

            Debug.Log($"Loading Level\nPilot={pilotName}\ncID={cID}\nsID={sID}");
            VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
            LoadingSceneController.LoadScene(7);

            yield return new WaitForSeconds(5);
            //After here we should be in the loader scene

            Debug.Log("Setting Pilot");
            PilotSaveManager.current = PilotSaveManager.pilots[pilotName];
            Debug.Log("Going though All built in campaigns");
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
                {
                    if (info.campaignID == cID)
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
                if (cs.scenarioID == sID)
                {
                    Debug.Log("Setting Scenario");
                    PilotSaveManager.currentScenario = cs;
                    break;
                }
            }

            VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

            Debug.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName, pilotName));

            LoadingSceneController.instance.PlayerReady(); //<< Auto Ready
            Debug.Log("Player is ready");

            while (SceneManager.GetActiveScene().buildIndex != 7)
            {
                //Pausing this method till the loader scene is unloaded
                yield return null;
            }
        }
    }
}
