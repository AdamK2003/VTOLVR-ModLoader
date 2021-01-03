using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using ModLoader.Classes.Json;

namespace ModLoader
{
    class ModReader : MonoBehaviour
    {
        /// <summary>
        /// Gets all of the mods info located in the path into memory
        /// </summary>
        /// <param name="path">The folder to check for mods</param>
        /// <param name="isDevFolder">If we are checking through the users My Projects Folder</param>
        public static List<BaseItem> GetMods(string path, bool isDevFolder = false)
        {
            List<BaseItem> foundMods = new List<BaseItem>();
            DirectoryInfo folders = new DirectoryInfo(path);
            DirectoryInfo[] mods = folders.GetDirectories();

            BaseItem lastMod;
            string pathToCheck;
            for (int i = 0; i < mods.Length; i++)
            {
                if (isDevFolder)
                    pathToCheck = Path.Combine(mods[i].FullName, "Builds", "info.json");
                else
                    pathToCheck = Path.Combine(mods[i].FullName, "info.json");

                if (!File.Exists(pathToCheck))
                {
                    Debug.Log($"Mod: {mods[i].Name} doesn't have a info.json file");
                    continue;
                }
                if (TryGetBaseItem(mods[i].FullName, out BaseItem item))
                {
                    lastMod = item;
                    foundMods.Add(lastMod);
                }
            }

            //Searching for just .dll mods
            FileInfo[] dllFiles = folders.GetFiles("*.dll");
            for (int i = 0; i < dllFiles.Length; i++)
            {
                lastMod = new BaseItem();
                lastMod.Name = dllFiles[i].Name;
                lastMod.Description = BaseItem.DllOnlyDescription;
                lastMod.Directory = folders;
                lastMod.CreateMod();
                foundMods.Add(lastMod);
            }
            return foundMods;
        }

        /// <summary>
        /// Add the mods to the list without effecting the current mods
        /// </summary>
        /// <param name="path">Folder where the mods are located</param>
        /// <param name="currentMods">The current list of mods</param>
        /// <returns>True if there where new mods</returns>
        public static bool GetNewMods(string path, ref List<BaseItem> currentMods)
        {
            List<BaseItem> mods = GetMods(path);
            Dictionary<string, BaseItem> currentModsDictionary = currentMods.ToDictionary(x => x.Name);
            bool newMods = false;
            foreach (BaseItem mod in mods)
            {
                if (!currentModsDictionary.ContainsKey(mod.Name))
                {
                    newMods = true;
                    currentModsDictionary.Add(mod.Name, mod);
                }
            }
            currentMods = currentModsDictionary.Values.ToList();

            return newMods;
        }

        private static void ConvertOldMod(string folder)
        {
            Debug.LogWarning("Converting " + folder);
            Mod currentMod;
            bool hasInfo = false;
            bool hasDLL = false;
            bool hasPreview = false;


            using (FileStream stream = new FileStream(folder + @"\info.xml", FileMode.Open))
            {
                XmlSerializer xml = new XmlSerializer(typeof(Mod));
                currentMod = (Mod)xml.Deserialize(stream);
                hasInfo = true;
            }

            string[] subFiles = Directory.GetFiles(folder, "*.dll");
            Assembly lastAssembly;
            IEnumerable<Type> source;

            for (int j = 0; j < subFiles.Length; j++)
            {
                lastAssembly = Assembly.Load(File.ReadAllBytes(subFiles[j]));
                source = from t in lastAssembly.GetTypes()
                         where t.IsSubclassOf(typeof(VTOLMOD))
                         select t;
                if (source.Count() != 1)
                {
                    Debug.LogError("The mod " + subFiles[j] + " doesn't specify a mod class or specifies more than one");
                    break;
                }
                hasDLL = true;
                currentMod.dllPath = subFiles[j];
                break;
            }

            if (File.Exists(folder + @"\preview.png"))
            {
                currentMod.imagePath = folder + @"\preview.png";
                hasPreview = true;
            }

            if (hasInfo && hasDLL)
            {
                JObject json = new JObject();
                json.Add("name", currentMod.name);
                json.Add("description", currentMod.description);
                string[] pathSpit = currentMod.dllPath.Split('\\');
                json.Add("dll file", pathSpit[pathSpit.Length - 1]);
                if (hasPreview)
                {
                    pathSpit = currentMod.imagePath.Split('\\');
                    json.Add("preview image", pathSpit[pathSpit.Length - 1]);
                }

                File.WriteAllText(folder + @"\info.json", json.ToString());
                File.Delete(folder + @"\info.xml");
            }
        }
        private static bool TryGetBaseItem(string folder, out BaseItem item)
        {
            try
            {
                item = JsonConvert.DeserializeObject<BaseItem>(File.ReadAllText(Path.Combine(folder, "info.json")));
                item.Directory = new DirectoryInfo(folder);
            }
            catch (Exception e)
            {
                Debug.LogError("[Mod Reader] Failed to read base item. Exception:\n" + e);
                item = null;
                return false;
            }
            return true;
        }
    }
}
