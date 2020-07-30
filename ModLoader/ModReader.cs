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

namespace ModLoader
{
    public class ModReader : MonoBehaviour
    {
        /// <summary>
        /// Gets all of the mods info localed in the path into memory
        /// </summary>
        /// <param name="path">The folder to check for mods</param>
        public static List<Mod> GetMods(string path)
        {
            List<Mod> mods = new List<Mod>();
            string[] folders = Directory.GetDirectories(path);
            
            //Files used in loop
            string[] subFiles;
            Assembly lastAssembly;
            IEnumerable<Type> source;
            for (int i = 0; i < folders.Length; i++)
            {
                Mod currentMod = new Mod();
                bool hasDLL = false;
                bool hasInfo = false;

                if (File.Exists($"{folders[i]}/info.xml"))
                {
                    ConvertOldMod(folders[i]);
                }
                
                if (File.Exists($"{folders[i]}/info.json"))
                {
                    JObject json;
                    try
                    {
                        json = JObject.Parse($"{folders[i]}/info.json");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to read json file {folders[i]}/info.json");
                        continue;
                    }
                    
                    if (json["Name"] == null)
                    {
                        Debug.LogError($"Name is missing in json");
                    }
                    else
                    {
                        currentMod.name = json["Name"].ToString();
                    }
                    if (json["Description"] == null)
                    {
                        Debug.LogError($"Description is missing in json");
                    }
                    else
                    {
                        currentMod.description = json["Description"].ToString();
                    }
                    if (json["Dll File"] == null)
                    {
                        Debug.LogError($"Dll is missing in json");
                    }
                    else
                    {
                        currentMod.dllPath =  json["Dll File"].ToString();
                    }
                }
                if (hasInfo && hasDLL)
                    mods.Add(currentMod);


            }

            //Searching for just .dll mods

            string[] dllFiles = Directory.GetFiles(path, "*.dll");
            string currentName;
            for (int i = 0; i < dllFiles.Length; i++)
            {
                Mod currentMod = new Mod();
                bool hasDLL = false;
                currentName = dllFiles[i].Split('\\').Last();
                try
                {
                    lastAssembly = Assembly.Load(File.ReadAllBytes(dllFiles[i]));
                    source = from t in lastAssembly.GetTypes()
                             where t.IsSubclassOf(typeof(VTOLMOD))
                             select t;
                    
                    if (source.Count() != 1)
                    {
                        Debug.LogError("The mod " + currentName + " doesn't specify a mod class or specifies more than one");
                        continue;
                    }
                    else
                    {
                        currentMod.name = currentName;
                        currentMod.description = "This only a .dll file, please make mods into .zip with a xml file when releasing the mod.";
                        currentMod.dllPath = dllFiles[i];
                        hasDLL = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("There was an error when trying to load a .dll mod.\n" +
                        currentName + " doesn't seem to derive from VTOLMOD");
                    continue;
                }
                

                if (hasDLL)
                    mods.Add(currentMod);
            }
            return mods;
        }

        /// <summary>
        /// Add the mods to the list without effecting the current mods
        /// </summary>
        /// <param name="path">Folder where the mods are located</param>
        /// <param name="currentMods">The current list of mods</param>
        /// <returns>True if there where new mods</returns>
        public static bool GetNewMods(string path, ref List<Mod> currentMods)
        {
            List<Mod> mods = GetMods(path);
            Dictionary<string,Mod> currentModsDictionary = currentMods.ToDictionary(x => x.name);
            bool newMods = false;
            foreach (Mod mod in mods)
            {
                if (!currentModsDictionary.ContainsKey(mod.name))
                {
                    newMods = true;
                    currentModsDictionary.Add(mod.name, mod);
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
                json.Add("Name", currentMod.name);
                json.Add("Description", currentMod.description);
                json.Add("Dll File", currentMod.dllPath);
                if (hasPreview)
                    json.Add("Preview Image", currentMod.imagePath);
                File.WriteAllText(folder + @"\info.json", json.ToString());
                File.Delete(folder + @"\info.xml");
            }
        }
    }
}

public class Mod
{
    public string name;
    public string description;
    [XmlIgnore]
    public string dllPath;
    [XmlIgnore]
    public GameObject listGO, settingsGO, settingsHolerGO;
    [XmlIgnore]
    public bool isLoaded;
    [XmlIgnore]
    public string imagePath;

    public Mod() { }

    public Mod(string name, string description, string dllPath)
    {
        this.name = name;
        this.description = description;
        this.dllPath = dllPath;
    }
}

