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
using ModLoader.Classes.Json;
using Core;
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
            SceneManager.sceneLoaded += SceneLoaded;

            CreateAPI();
            FindProjectFolder();
            AttachCoreLogger();

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

            DevTools.ReadDevTools();

            _api.CreateCommand("quit", delegate { Application.Quit(); });
            _api.CreateCommand("print", PrintMessage);
            _api.CreateCommand("help", _api.ShowHelp);
            _api.CreateCommand("vrinteract", VRInteract);
            _api.CreateCommand("listinteract", ListInteractables);

            HarmonyInstance harmony = HarmonyInstance.Create("vtolvrmodding.modloader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            LoadLauncherSettings();
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
            try
            {
                _tcpClient.WriteLine($"[{type}]{condition}\n");
                /* The reason why there is a new line at the end is because
                 * sometimes it sends two log messages at once, so this is me
                 * just trying to split them in inside the launchers
                 * console.*/
            }
            catch (Exception e)
            {
                Application.logMessageReceived -= LogMessageReceived;
                Debug.LogError($"TCP Client failed.\n{e.Message}");
            }
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
                    if (DevTools.Scenario != null)
                    {
                        if (DevTools.Scenario.IsWorkshop)
                            DevTools.LoadWorkshopMission();
                        else
                            DevTools.LoadBuiltInMission();
                    }
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
        public void LoadMod(Core.Jsons.BaseItem item)
        {
            try
            {
                Debug.Log($"[Dev Tools] Loading {item.Name}");

                string path = string.Empty;
                if (item.ContentType == Core.Enums.ContentType.Mods)
                    path = Path.Combine(item.Directory.FullName, item.DllPath);
                else if (item.ContentType == Core.Enums.ContentType.MyMods)
                    path = Path.Combine(item.Directory.FullName, "Builds", item.DllPath);
                else
                {
                    Debug.LogError($"[Dev Tools] Content type didn't match mod, so item isn't loaded");
                    return;
                }

                byte[] dllBytes = File.ReadAllBytes(path);
                IEnumerable<Type> source =
                      from t in Assembly.Load(dllBytes).GetTypes()
                      where t.IsSubclassOf(typeof(VTOLMOD))
                      select t;
                if (source != null && source.Count() == 1)
                {
                    GameObject newModGo = new GameObject(item.DllPath, source.First());
                    VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                    mod.SetModInfo(new Mod(
                        item.Name,
                        item.Description,
                        Path.Combine(item.Directory.FullName, item.DllPath),
                        item.Directory.FullName));
                    newModGo.name = item.Name;
                    DontDestroyOnLoad(newModGo);
                    mod.ModLoaded();

                    LoadedModsCount++;
                    UpdateDiscord();
                    return;
                }
                else
                {
                    Debug.LogError("[Dev Tools] Source is null");
                }

                Debug.Log($"[Dev Tools] Failed to load {item.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Dev Tools] Error when loading {item.Name}\n{e}");
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
        private static void LoadLauncherSettings()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_ModLoader",
                "settings.json");

            Debug.Log($"Checking {path} for launcher settings file");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Couldn't find Mod Loaders Settings.json file");
                return;
            }

            LauncherSettings.LoadSettings(path);
        }
        private static void AttachCoreLogger()
        {
            Core.Logger.OnMessageLogged += CoreLog;
        }
        private static void CoreLog(object message, Core.Logger.LogType type)
        {
            switch (type)
            {
                case Core.Logger.LogType.Log:
                    Debug.Log($"[Core] {message}");
                    break;
                case Core.Logger.LogType.Warning:
                    Debug.LogWarning($"[Core] {message}");
                    break;
                case Core.Logger.LogType.Error:
                    Debug.LogError($"[Core] {message}");
                    break;
            }
        }
    }
}
