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
                    value.ScenarioID == string.Empty ||
                    value.CampaignID == string.Empty)
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

        private static void Log(object message) => Debug.Log($"[Dev Tools]{message}");

        private static void Warning(object message) => Debug.LogWarning($"[Dev Tools]{message}");

        private static void Error(object message) => Debug.LogError($"[Dev Tools]{message}");
    }
}
