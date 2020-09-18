/*
This class is to collect data like missions and scenarios so that the mod loader
can use that data.

For example, this can collect all the missions so tha the mod loader can display
them in the dev tools section so if the game updates the list in the dev tools
is always uptodate after they have ran the game modded once.
 */

using ModLoader.Classes;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModLoader
{
    static class DataCollector
    {
        const string GameDataFile = @"\gamedata.json";
        private static JObject gameData = new JObject();
        public static void CollectData()
        {
            Debug.Log("Collecting Data");
            gameData = new JObject();
            CampaignsScenarios();
            FindMaterials();
            SaveGameData();
        }

        private static void CampaignsScenarios()
        {
            List<VTCampaignInfo> customCampaigns = VTResources.GetCustomCampaigns();
            Debug.Log("Collecting campaigns");
            CampaignsScenarios campaigns = new CampaignsScenarios(VTResources.GetBuiltInCampaigns());
            Debug.Log("Finished collecting camps");
            gameData.Add(new JProperty("Campaigns", JArray.FromObject(campaigns.Campaigns)));
            Debug.Log("Addded it");
        }

        private static void FindMaterials()
        {
            Debug.Log("Collecting Materials for skins");
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            List<string> names = new List<string>(mats.Length);
            for (int i = 0; i < mats.Length; i++)
            {
                try
                {
                    names.Add($"\"{mats[i].GetTexture("_MainTex").name}.png\" should be renamed to \"{mats[i].name}.png\"");
                }
                catch 
                {
                    Debug.Log($"Failed on {mats[i].name}");
                }
            }
            gameData.Add(new JProperty("Skin Texture Names", JArray.FromObject(names.ToArray())));
            Debug.Log("Finished Collecting Materials");
        }

        private static void SaveGameData()
        {
            Debug.Log("Saving the data we have collected!");
            File.WriteAllText(ModLoaderManager.RootPath + GameDataFile,gameData.ToString());
        }
    }
}
