using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using ModLoader.Classes;
using UnityEngine;

namespace ModLoader.Classes.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    class LauncherSettings
    {
        [JsonProperty("projectsFolder")]
        public string ProjectsFolder;
        [JsonProperty("token")]
        public string Token;
        [JsonProperty]
        public bool AutoUpdate = true;
        [JsonProperty("Launch SteamVR")]
        public bool LaunchSteamVR = true;

        [JsonProperty]
        public ObservableCollection<Item> Items = new ObservableCollection<Item>();

        private static LauncherSettings _settings;
        public static LauncherSettings Settings
        {
            get
            {
                if (_settings != null)
                    return _settings;
                _settings = new LauncherSettings();
                return _settings;
            }
            private set
            {
                _settings = value;
            }
        }
        public static void LoadSettings(string path)
        {
            try
            {
                Settings = JsonConvert.DeserializeObject<LauncherSettings>(
                    File.ReadAllText(path));
                Log($"Loaded Settings.\n{Settings}");
            }
            catch (Exception e)
            {
                Error($"Failed Reading Settings: {e.Message}");
                Settings = new LauncherSettings();
            }
        }
        private static void Log(object message) => Debug.Log($"[Launcher Settings]{message}");

        private static void Warning(object message) => Debug.LogWarning($"[Launcher Settings]{message}");

        private static void Error(object message) => Debug.LogError($"[Launcher Settings]{message}");
        public override string ToString()
        {
            return $"Projects Folder = {ProjectsFolder}|" +
                $"AutoUpdate = {AutoUpdate}|" +
                $"LaunchSteamVR = {LaunchSteamVR}|" +
                $"Items Count = {Items.Count}";
        }
    }
}
