using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ModLoader;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections;
using VTOLVR.Multiplayer;

/// <summary>
/// This is the VTOL VR Modding API which aims to simplify repetitive tasks.
/// </summary>
public class VTOLAPI : MonoBehaviour
{
    public enum ErrorResult { None, NotRegistered, KeyNotFound }

    /// <summary>
    /// This is the current instance of the API in the game world.
    /// </summary>
    public static VTOLAPI instance { get; private set; }

    public static Action<string> ModRegistered;
    public static Action<string, string, object> ModValueChanged;

    private static Dictionary<VTOLMOD, Dictionary<string, object>> _dataSharing = new Dictionary<VTOLMOD, Dictionary<string, object>>();

    private string gamePath;
    private string modsPath = @"\VTOLVR_ModLoader\mods";
    private Dictionary<string, Action<string>> commands = new Dictionary<string, Action<string>>();

    /// <summary>
    /// This gets invoked when the scene has changed and finished loading. 
    /// This should be the safest way to start running code when a level is loaded.
    /// </summary>
    public static UnityAction<VTOLScenes> SceneLoaded;

    /// <summary>
    /// This gets invoked when the mission as been reloaded by the player.
    /// </summary>
    public static UnityAction MissionReloaded;

    /// <summary>
    /// The current scene which is active.
    /// </summary>
    public static VTOLScenes currentScene { get; private set; }

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(this.gameObject);
        instance = this;
        gamePath = Directory.GetCurrentDirectory();
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    private void ActiveSceneChanged(Scene current, Scene next)
    {
        Debug.Log($"Active Scene Changed to [{next.buildIndex}]{next.name}");
        switch (next.buildIndex)
        {
            case 0:
                CallSceneLoaded(VTOLScenes.SplashScene);
                break;
            case 1:
                CallSceneLoaded(VTOLScenes.SamplerScene);
                break;
            case 2:
                CallSceneLoaded(VTOLScenes.ReadyRoom);
                break;
            case 3:
                CallSceneLoaded(VTOLScenes.VehicleConfiguration);
                break;
            case 4:
                CallSceneLoaded(VTOLScenes.LoadingScene);
                break;
            case 5:
                CallSceneLoaded(VTOLScenes.MeshTerrain);
                break;
            case 6:
                CallSceneLoaded(VTOLScenes.OpenWater);
                break;
            case 7:
                StartCoroutine(WaitForScenario(VTOLScenes.Akutan));
                break;
            case 8:
                CallSceneLoaded(VTOLScenes.VTEditMenu);
                break;
            case 9:
                CallSceneLoaded(VTOLScenes.VTEditLoadingScene);
                break;
            case 10:
                CallSceneLoaded(VTOLScenes.VTMapEditMenu);
                break;
            case 11:
                StartCoroutine(WaitForScenario(VTOLScenes.CustomMapBase));
                break;
            case 12:
                CallSceneLoaded(VTOLScenes.CommRadioTest);
                break;
            case 13:
                CallSceneLoaded(VTOLScenes.ShaderVariantsScene);
                break;
            case 14:
                StartCoroutine(WaitForScenario(VTOLScenes.CustomMapBase_OverCloud));
                break;
        }
    }

    private IEnumerator WaitForScenario(VTOLScenes Scene)
    {
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady)
        {
            yield return null;
        }
        CallSceneLoaded(Scene);
    }

    private void CallSceneLoaded(VTOLScenes Scene)
    {
        currentScene = Scene;
        if (SceneLoaded != null)
            SceneLoaded.Invoke(Scene);
    }

    #region Steam Related Methods
    
    /// <summary>
    /// Returns the steam ID of the player which is using this mod.
    /// </summary>
    [Obsolete]
    public ulong GetSteamID() => SteamClient.SteamId;

    /// <summary>
    /// Returns the steam ID of the player which is using this mod.
    /// </summary>
    public static ulong SteamId() => SteamClient.SteamId;
    
    /// <summary>
    /// Returns the current name of the steam user, if they change their name during play session, this doesn't update.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public string GetSteamName() => SteamClient.Name;

    /// <summary>
    /// Returns the current name of the steam user, if they change their name during play session, this doesn't update.
    /// </summary>
    public static string SteamName() => SteamClient.Name;

    #endregion
    
    
    /// <summary>
    /// [MP Supported]
    /// Searches for the game object of the player by using the prefab name appending (Clone).
    /// For multiplayer it uses the lobby manager to get the local player
    /// </summary>
    /// <returns></returns>
    public static GameObject GetPlayersVehicleGameObject()
    {
        if (VTOLMPUtils.IsMultiplayer())
        {
            return VTOLMPLobbyManager.localPlayerInfo.vehicleObject;
        }

        string vehicleName = PilotSaveManager.currentVehicle.vehiclePrefab.name;
        return GameObject.Find($"{vehicleName}(Clone)");
    }

    /// <summary>
    /// Returns which vehicle the player is using in a Enum.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public static VTOLVehicles GetPlayersVehicleEnum()
    {
        if (PilotSaveManager.currentVehicle == null)
            return VTOLVehicles.None;

        string vehicleName = PilotSaveManager.currentVehicle.vehicleName;
        switch (vehicleName)
        {
            case "AV-42C":
                return VTOLVehicles.AV42C;
            case "F/A-26B":
                return VTOLVehicles.FA26B;
            case "F-45A":
                return VTOLVehicles.F45A;
            case "AH-94":
                return VTOLVehicles.AH94;
            default:
                return VTOLVehicles.None;
        }
    }

    /// <summary>
    /// Creates a settings page in the `mod settings` tab.
    /// Make sure to fully create your settings before calling this as you 
    /// can't change it onces it's created.
    /// </summary>
    /// <param name="newSettings"></param>
    public static void CreateSettingsMenu(Settings newSettings)
    {
        if (ModLoader.ModLoader.instance == null)
        {
            Debug.LogError("The Mod Loaders Instance is null. We haven't reached the Main Room Scene yet");
            return;
        }
        ModLoader.ModLoader.instance.CreateSettingsMenu(newSettings);
    }

    public void CheckConsoleCommand(string command)
    {
        string[] lastCommand = command.Split(' ');
        if (lastCommand == null || lastCommand.Length == 0)
        {
            Debug.LogError("The command seemed to be less than 0");
            return;
        }
        if (commands.ContainsKey(lastCommand[0].ToLower()))
        {
            commands[lastCommand[0].ToLower()].Invoke(command);
        }
    }

    public void CreateCommand(string command, Action<string> callBack)
    {
        commands.Add(command.ToLower(), callBack);
        Debug.Log("ML API: Created command: " + command.ToLower());
    }

    public void RemoveCommand(string command)
    {
        commands.Remove(command.ToLower());
        Debug.LogWarning("ML API: Removed command: " + command.ToLower());
    }

    public void ShowHelp(string input)
    {
        StringBuilder stringBuilder = new StringBuilder("ML API Console Commands\n");
        List<string> list = commands.Keys.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            stringBuilder.AppendLine("Command: " + list[i]);
        }
        Debug.Log(stringBuilder.ToString());
    }

    /// <summary>
    /// Returns a list of mods which are currently loaded
    /// </summary>
    /// <returns></returns>
    public static List<Mod> GetUsersMods()
    {
        return ModLoader.ModLoader.instance.ModsLoaded;
    }


    /// <summary>
    /// Returns an ordered string of mods which are currently loaded
    /// </summary>
    /// <returns></returns>
    public static string GetUsersOrderedMods()
    {
        List<Mod> mods = GetUsersMods();

        if(mods.Count == 0)
        {
            return "";
        }

        var sortedMods = mods.OrderBy(x => x.name);

        string loadedMods = "";
        foreach (Mod m in sortedMods)
        {
            loadedMods += m.name.ToLower() + ",";
        }

        return loadedMods.Remove(loadedMods.Length - 1);

    }
    /// <summary>
    /// Please don't use this, this is for the mod loader only.
    /// </summary>
    public void WaitForScenarioReload()
    {
        StartCoroutine(Wait());
    }

    private IEnumerator Wait()
    {
        while (!VTMapManager.fetch.scenarioReady)
        {
            yield return null;
        }
        if (MissionReloaded != null)
            MissionReloaded.Invoke();
    }

    /// <summary>
    /// You need to Register your mod before you start setting values inside your Shared Data
    /// </summary>
    /// <param name="mod">Your mod's class (use "this")</param>
    public static void RegisterMod(VTOLMOD mod)
    {
        Dictionary<string, object> data;
        bool exists = IsRegistered(mod, out data);
        if (exists)
        {
            Warning($"{mod.name} is already registered");
            return;
        }

        data = new Dictionary<string, object>();
        _dataSharing.Add(mod, data);
        Log($"Registered {mod.name}");
        ModRegistered?.Invoke(mod.name);
    }

    private static bool IsRegistered(VTOLMOD mod, out Dictionary<string, object> data)
    {
        return _dataSharing.TryGetValue(mod, out data);
    }

    private static bool IsRegistered(string modName, out Dictionary<string, object> data)
    {
        foreach (var item in _dataSharing.Keys)
        {
            if (item.name == modName)
            {
                return _dataSharing.TryGetValue(item, out data);
            }
        }
        data = null;
        return false;
    }

    /// <summary>
    /// Sets a value for your mods shared data.
    /// </summary>
    /// <param name="mod">Your mod (use "this")</param>
    /// <param name="key">The key you want to use. if it's already existing, it will be overridden</param>
    /// <param name="value">the value you want to set it to</param>
    /// <param name="isSuccessful">if it was successful in setting it</param>
    public static void SetValue(VTOLMOD mod, string key, object value, out bool isSuccessful)
    {
        Dictionary<string, object> data;
        isSuccessful = false;
        bool exists = IsRegistered(mod, out data);

        if (!exists)
        {
            NotRegistered(mod.name);
            return;
        }

        exists = data.TryGetValue(key, out object oldValue);
        if (exists)
        {
            data[key] = value;
            Log($"Changed value on key \"{key}\" to \"{mod.name}\"");
        }
        else
        {
            data.Add(key, value);
            Log($"Added new key \"{key}\" to \"{mod.name}\"");
        }
        isSuccessful = true;
        ModValueChanged?.Invoke(mod.name, key, value);
    }

    /// <summary>
    /// Gets a value stored in the Mod Shared Data inside the API
    /// </summary>
    /// <param name="modName">The name of the mod. CASE SENSITIVE</param>
    /// <param name="key">The key in the dictionary</param>
    /// <param name="isSuccessful">If the value was found</param>
    /// <param name="value">The stored value inside the dictionary if it was there. This will be null if it wasn't found</param>
    /// <param name="error">Reason why it couldn't find the key, this will be None if it was found</param>
    public static void GetValue(string modName, string key, out bool isSuccessful, out object value, out ErrorResult error)
    {
        Dictionary<string, object> data;
        bool exists = IsRegistered(modName, out data);
        isSuccessful = false;
        value = null;
        error = ErrorResult.None;

        if (!exists)
        {
            NotRegistered(modName);
            error = ErrorResult.NotRegistered;
            return;
        }

        exists = data.TryGetValue(key, out value);

        if (!exists)
        {
            Error($"Couldn't find a key matching \"{key}\" in \"{modName}\"");
            error = ErrorResult.KeyNotFound;
            return;
        }

        isSuccessful = true;
    }

    private static void NotRegistered(string modName) =>
        Error($"{modName} is not registered. Please register it first with VTOLAPI.RegisterMod(this);");

    private static void Log(object message) => Debug.Log($"[VTOL API]{message}");

    private static void Warning(object message) => Debug.LogWarning($"[VTOL API]{message}");

    private static void Error(object message) => Debug.LogError($"[VTOL API]{message}");
}

