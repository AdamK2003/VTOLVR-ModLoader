using System;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;

namespace Core.Jsons
{
    public class DevTools
    {
        [JsonProperty("previous_mods")] public List<string> PreviousMods = new List<string>();
        [JsonProperty("scenario")] public Scenario Scenario;

        public static DevTools GetDevTools(string json)
        {
            DevTools item = Helper.DeserializeObject<DevTools>(json, out Exception error);
            if (error == null)
            {
                return item;
            }
            Logger.Error("Failed to get dev tools.\nError:" + error);
            return null;
        }

        public void SaveFile(string path)
        {
            using (TextWriter tw = new StreamWriter(path))
            {
                var js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(tw, this);
            }
        }
    }
}
