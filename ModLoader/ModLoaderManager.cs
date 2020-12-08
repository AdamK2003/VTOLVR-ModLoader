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
using Valve.Newtonsoft.Json.Linq;
using SimpleTCP;
using Harmony;
using ModLoader.Classes;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            CrashReportHandler.enableCaptureExceptions = false;
            if (!SteamAuthentication.IsTrusted(Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Plugins",
                "steam_api64.dll")))
            {
                Debug.LogError("Unexpected Error, please contact vtolvr-mods.com staff\nError code: 667970");
                Application.Quit();
                return;
            }
            PlayerLogText();
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
on the offical VTOL VR Discord or post an issue on gitlab. 

VTOL VR Modding Discord Server: https://discord.gg/XZeeafp
Mod Loader Gitlab: https://gitlab.com/vtolvr-mods/ModLoader
Mod Loader Website: https://vtolvr-mods.com/

Special Thanks to Ketkev and Nebriv for their continuous support to the mod loader and the website.

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
        private const int TCPPORT = 12000;
        public static ModLoaderManager Instance { get; private set; }
        public static string RootPath, MyProjectsPath;
        public static int LoadedModsCount;

        private static SimpleTcpClient _tcpClient;
        private static List<Action> _pending = new List<Action>();

        private VTOLAPI _api;
        private string[] _args;

        private bool _loadMission;
        private string _pilotName = "";
        private string _cID = "";
        private string _sID = "";


        //Discord
        private DiscordController _discord;
        public string _discordDetail, _discordState;

        private void Awake()
        {
            if (!Check())
                return;
            if (Instance)
                Destroy(this.gameObject);

            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            SetPaths();
            Debug.Log("This is the first mod loader manager");
            _args = Environment.GetCommandLineArgs();

            CreateAPI();
            FindProjectFolder();

            try
            {
                _tcpClient = new SimpleTcpClient();
                _tcpClient.Connect("127.0.0.1", TCPPORT);
                _tcpClient.WriteLine("Command:isgame");
                _tcpClient.DataReceived += TcpDataReceived;
                Application.logMessageReceived += LogMessageReceived;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            _discord = gameObject.AddComponent<DiscordController>();
            _discordDetail = "Launching Game";
            _discordState = ". Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();

            CheckDevTools();

            SceneManager.sceneLoaded += SceneLoaded;

            _api.CreateCommand("quit", delegate { Application.Quit(); });
            _api.CreateCommand("print", PrintMessage);
            _api.CreateCommand("help", _api.ShowHelp);
            _api.CreateCommand("vrinteract", VRInteract);
            _api.CreateCommand("listinteract", ListInteractables);

            HarmonyInstance harmony = HarmonyInstance.Create("vtolvrmodding.modloader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        private void TcpDataReceived(object sender, Message e)
        {
            lock (_pending)
            {
                _pending.Add(delegate { VTOLAPI.instance.CheckConsoleCommand(e.MessageString.Remove(e.MessageString.Length - 1)); });
            }
        }
        private void InvokePending()
        {
            lock (_pending)
            {
                foreach (Action action in _pending)
                {
                    action();
                }

                _pending.Clear();
            }
        }
        private void Update()
        {
            InvokePending();
        }
        private void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _tcpClient.WriteLine($"[{type}]{condition}\n");
            /* The reason why there is a new line at the end is because
             * sometimes it sends two log messages at once, so this is me
             * just trying to split them in inside the launchers
             * console.*/
        }

        private void CreateAPI()
        {
            _api = gameObject.AddComponent<VTOLAPI>();
        }
        private void SetPaths()
        {
            RootPath = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string sceneName = arg0.name;
            Debug.Log("Scene Loaded = " + sceneName);
            switch (sceneName)
            {
                case "SamplerScene":
                    DataCollector.CollectData();
                    _discordDetail = "Selecting mods";
                    StartCoroutine(CreateModLoader());
                    if (_loadMission)
                        StartCoroutine(LoadLevel());
                    break;
                case "Akutan":
                    if (PilotSaveManager.currentVehicle == null || PilotSaveManager.currentCampaign == null)
                    {
                        _discordDetail = "In the editor";
                        _discordState = "Akutan";
                        break;
                    }
                    _discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    _discordState = "Akutan: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "CustomMapBase":
                    if (PilotSaveManager.currentVehicle == null || PilotSaveManager.currentCampaign == null)
                    {
                        _discordDetail = "In the editor";
                        _discordState = "Custom Map";
                        break;
                    }
                    _discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    _discordState = "CustomMap: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "LoadingScene":
                    _discordDetail = "Loading into mission";
                    break;
                case "VTEditLoadingScene":
                    _discordDetail = "In the editor";
                    break;
                case "VTEditMenu":
                    _discordDetail = "In the editor";
                    break;
                case "ReadyRoom":
                    if (LoadedModsCount == 0)
                    {
                        _discordDetail = "In Main Menu";
                    }
                    else
                    {
                        _discordDetail = "In Main Menu with " + LoadedModsCount + (LoadedModsCount == 0 ? " mod" : " mods");
                    }
                    break;
                case "VehicleConfiguration":
                    _discordDetail = "Configuring " + PilotSaveManager.currentVehicle.vehicleName;
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
            _discord.UpdatePresence(LoadedModsCount, _discordDetail, _discordState);
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
            message = message.Replace("vrinteract ", "");
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
            Harmony.Traverse.Create(interactable).Method("StartInteraction").GetValue();
            Debug.Log($"Invoked OnInteract on GameObject {message}");
        }
        public void LoadMod(string path)
        {
            try
            {
                Debug.Log($"Loading mod from {path}");
                IEnumerable<Type> source =
          from t in Assembly.Load(File.ReadAllBytes(path)).GetTypes()
          where t.IsSubclassOf(typeof(VTOLMOD))
          select t;
                if (source != null && source.Count() == 1)
                {
                    GameObject newModGo = new GameObject(path, source.First());
                    VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                    mod.SetModInfo(new Mod(path, "STARTUPMOD", path, new FileInfo(path).DirectoryName));
                    newModGo.name = path;
                    DontDestroyOnLoad(newModGo);
                    mod.ModLoaded();

                    LoadedModsCount++;
                    UpdateDiscord();
                }
                else
                {
                    Debug.LogError("Source is null");
                }

                Debug.Log("Loaded Startup mod from path = " + path);
            }
            catch (Exception e)
            {
                Debug.LogError("Error when loading startup mod\n" + e.ToString());
            }
        }
        private void CheckDevTools()
        {
            if (File.Exists(RootPath + "/devtools.json"))
            {
                ReadDevTools(File.ReadAllText(RootPath + "/devtools.json"));
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

            Debug.Log("DevTools: Checking for any mods or scenarios");


            if (json["scenario"] != null && json["pilot"] != null)
            {
                _pilotName = json["pilot"].Value<string>() ?? null;
                if (_pilotName != "No Selection" && !string.IsNullOrEmpty(_pilotName))
                {
                    JObject scenario = json["scenario"] as JObject;
                    string scenarioName = scenario["name"].ToString();
                    _sID = scenario["id"].ToString();
                    _cID = scenario["cid"].ToString();

                    if (scenarioName != "No Selection" && !string.IsNullOrEmpty(_sID))
                    {
                        _loadMission = true;
                        Debug.Log($"Devtools - Pilot={_pilotName} ScenarioName={scenarioName}" +
                            $"sID={_sID} cID={_cID}");
                    }
                }
            }

            if (json["previousMods"] != null)
            {
                JArray mods = JArray.FromObject(json["previousMods"]);
                Debug.Log($"Devtools: Found {mods.Count} mods to load");
                for (int i = 0; i < mods.Count; i++)
                {
                    LoadMod(mods[i].ToString());
                }
            }
        }
        private IEnumerator LoadLevel()
        {
            _loadMission = false;
            Debug.Log("Loading Pilots from file");
            PilotSaveManager.LoadPilotsFromFile();
            yield return new WaitForSeconds(2);

            Debug.Log($"Loading Level\nPilot={_pilotName}\ncID={_cID}\nsID={_sID}");
            VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
            Debug.Log("Setting Pilot");
            PilotSaveManager.current = PilotSaveManager.pilots[_pilotName];
            Debug.Log("Going though All built in campaigns");
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
                {
                    if (info.campaignID == _cID)
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
                if (cs.scenarioID == _sID)
                {
                    Debug.Log("Setting Scenario");
                    PilotSaveManager.currentScenario = cs;
                    break;
                }
            }

            VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

            Debug.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName, _pilotName));

            VTScenario.LaunchScenario(VTScenario.currentScenarioInfo);
            yield return new WaitForSeconds(5); // Waiting for us to be in the loader scene
            LoadingSceneController.instance.PlayerReady(); //<< Auto Ready
            Debug.Log("Player is ready");

            while (SceneManager.GetActiveScene().buildIndex != 7)
            {
                //Pausing this method till the loader scene is unloaded
                yield return null;
            }
        }
        private void FindProjectFolder()
        {
            if (!File.Exists(Path.Combine(RootPath, "settings.json")))
                return;
            JObject json;
            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Combine(RootPath, "settings.json")));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read settings.json\n{e.Message}");
                return;
            }

            if (json["projectsFolder"] != null)
            {
                MyProjectsPath = json["projectsFolder"].ToString();
            }
            else
            {
                Debug.LogWarning($"Couldn't find projects folder in settings.json");
            }
        }
        private static void ListInteractables(string message)
        {
            VRInteractable[] interactables = GameObject.FindObjectsOfType<VRInteractable>();
            StringBuilder builder = new StringBuilder($"Found {interactables.Length} interactables\n");
            for (int i = 0; i < interactables.Length; i++)
            {
                builder.AppendLine($"{interactables[i].name}");
            }
            Debug.Log(builder.ToString());
        }
        /*
         * This check should always return true but I've left it in as a backup
         * incase people get passed the first check
         */
        private static bool Check()
        {
            if (File.Exists(Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Plugins",
                "steam_appid.txt")))
            {
                Debug.Log("Unexpected Error, please contact vtolvr-mods.com staff\nError code: 667970");
                Application.Quit();
                return false;
            }
            if (File.Exists(Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Plugins",
                "steam_api64.dll")))
            {
                FileInfo steamapi = new FileInfo(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "VTOLVR_Data",
                    "Plugins",
                    "steam_api64.dll"));
                if (steamapi.Length > 300000)
                {
                    Debug.Log($"Unexpected Error, please contact vtolvr-mods.com staff\nError code: {steamapi.Length}");
                    Application.Quit();
                    return false;
                }
            }
            return true;
        }
    }
}
