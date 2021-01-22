using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using VTOLVR_ModLoader.Views;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader.Classes.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserSettings
    {
        [JsonProperty("projectsFolder")]
        public string ProjectsFolder;
        [JsonProperty("token")]
        public string Token;
        [JsonProperty]
        public bool AutoUpdate = true;
        [JsonProperty("Launch SteamVR")]
        public bool LaunchSteamVR = true;
        [JsonProperty("Branches")]
        public List<string> Branches;
        [JsonProperty("Active Branch")]
        public int ActiveBranch = 0;

        [JsonProperty]
        public ObservableCollection<Manager.Item> Mods = new ObservableCollection<Manager.Item>();
        [JsonProperty]
        public ObservableCollection<Manager.Item> Skins = new ObservableCollection<Manager.Item>();

        private static UserSettings _settings;
        public static UserSettings Settings
        {
            get
            {
                if (_settings != null)
                    return _settings;
                _settings = new UserSettings();
                return _settings;
            }
            private set
            {
                _settings = value;
            }
        }

        public static void SaveSettings(string path)
        {
            using (TextWriter tw = new StreamWriter(path))
            {
                var js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(tw, Settings);
            }
        }
        public static void LoadSettings(string path)
        {
            if (!File.Exists(path))
            {
                SaveSettings(path);
                return;
            }

            try
            {
                Settings = JsonConvert.DeserializeObject<UserSettings>(
                    File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Console.Log($"Failed Reading Settings: {e.Message}");
                Settings = new UserSettings();
            }
        }
    }
}
