using System;
using System.Collections.Generic;
using System.IO;
using Core.Jsons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core
{
    public static class Helper
    {
        private const string _dllModDescription =
            "This only a .dll file, please make mods into .zip when releasing the mod.";

        public static JObject JObjectTryParse(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JObject.Parse(content);
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }

        public static T DeserializeObject<T>(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {
                exception = e;
                return default;
            }
        }

        public static List<BaseItem> FindMods(string folder, bool isMyProjects = false)
        {
            List<BaseItem> foundMods = new List<BaseItem>();

            if (!Directory.Exists(folder))
            {
                Logger.Error("Couldn't find folder " + folder);
                return foundMods;
            }

            DirectoryInfo folders = new DirectoryInfo(folder);
            DirectoryInfo[] mods = folders.GetDirectories();

            BaseItem lastMod;
            string pathToCheck;
            for (int i = 0; i < mods.Length; i++)
            {
                if (isMyProjects)
                    pathToCheck = Path.Combine(mods[i].FullName, "Builds", "info.json");
                else
                    pathToCheck = Path.Combine(mods[i].FullName, "info.json");

                if (!File.Exists(pathToCheck))
                {
                    Logger.Log($"Mod: {mods[i].Name} doesn't have a info.json file");
                    continue;
                }

                lastMod = JsonConvert.DeserializeObject<BaseItem>(File.ReadAllText(pathToCheck));
                lastMod.Directory = mods[i];
                foundMods.Add(lastMod);
            }

            return foundMods;
        }

        public static List<BaseItem> FindSkins(string folder)
        {
            List<BaseItem> foundSkins = new List<BaseItem>();

            if (!Directory.Exists(folder))
            {
                Logger.Error("Couldn't find folder " + folder);
                return foundSkins;
            }

            DirectoryInfo downloadedSkins = new DirectoryInfo(folder);
            DirectoryInfo[] skins = downloadedSkins.GetDirectories();

            BaseItem lastSkin;
            for (int i = 0; i < skins.Length; i++)
            {
                if (!File.Exists(Path.Combine(skins[i].FullName, "info.json")))
                {
                    Logger.Log($"Skin: {skins[i].Name} doesn't have a info.json file");
                    continue;
                }

                lastSkin = JsonConvert.DeserializeObject<BaseItem>(
                    File.ReadAllText(
                        Path.Combine(skins[i].FullName, "info.json")));
                lastSkin.Directory = skins[i];
                foundSkins.Add(lastSkin);
            }

            return foundSkins;
        }

        public static List<BaseItem> FindDllMods(string folder)
        {
            List<BaseItem> foundMods = new List<BaseItem>();
            DirectoryInfo modsFolder = new DirectoryInfo(folder);

            FileInfo[] dlls = modsFolder.GetFiles("*.dll");

            BaseItem lastItem;
            for (int i = 0; i < dlls.Length; i++)
            {
                lastItem = new BaseItem();
                lastItem.Name = dlls[i].Name;
                lastItem.Description = _dllModDescription;
                lastItem.Directory = modsFolder;
                lastItem.DllPath = dlls[i].Name;
                foundMods.Add(lastItem);

                Logger.Log($"Added DLL mod {lastItem.Name} from {lastItem.DllPath}");
            }

            return foundMods;
        }
    }
}