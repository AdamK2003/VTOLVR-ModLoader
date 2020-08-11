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
        /// <param name="isDevFolder">If we are checking through the users My Projects Folder</param>
        public static List<Mod> GetMods(string path, bool isDevFolder = false)
        {
            List<Mod> mods = new List<Mod>();
            string[] folders = Directory.GetDirectories(path);
            
            //Files used in loop
            Assembly lastAssembly;
            IEnumerable<Type> source;
            for (int i = 0; i < folders.Length; i++)
            {
                if (isDevFolder)
                    folders[i] = Path.Combine(folders[i], "Builds");
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
                        json = JObject.Parse(File.ReadAllText($"{folders[i]}/info.json"));
                        hasInfo = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to read json file {folders[i]}/info.json\n{e}");
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
                    else if (File.Exists(Path.Combine(folders[i], json["Dll File"].ToString())))
                    {
                        currentMod.dllPath = Path.Combine(folders[i], json["Dll File"].ToString());
                        hasDLL = true;
                    }
                    if (json["Preview Image"] != null)
                    {
                        currentMod.imagePath = folders[i] + @"\" + json["Preview Image"].ToString();
                    }
                }
                currentMod.ModFolder = folders[i];
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

                currentMod.ModFolder = path;
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
                string[] pathSpit = currentMod.dllPath.Split('\\');
                json.Add("Dll File", pathSpit[pathSpit.Length - 1]);
                if (hasPreview)
                {
                    pathSpit = currentMod.imagePath.Split('\\');
                    json.Add("Preview Image", pathSpit[pathSpit.Length - 1]);
                }
                    
                File.WriteAllText(folder + @"\info.json", json.ToString());
                File.Delete(folder + @"\info.xml");
            }
        }
    }
}
/// <summary>
/// The information stored about a mod which is used by the mod loader
/// and can be used by mods with the API command GetUsersMods
/// </summary>
public class Mod
{
    /// <summary>
    /// The name of the mod which displays on the mods page.
    /// </summary>
    public string name;
    /// <summary>
    /// The description of the mod which displays when the mod is selected. 
    /// </summary>
    public string description;
    /// <summary>
    /// The location of the .dll file of this mod.
    /// </summary>
    public string dllPath;
    /// <summary>
    /// GameObjects used by the mod loader.
    /// </summary>
    public GameObject listGO, settingsGO, settingsHolerGO;
    /// <summary>
    /// If the mod is currently loaded.
    /// </summary>
    public bool isLoaded;
    /// <summary>
    /// The path to the preview image if one exists.
    /// </summary>
    public string imagePath;
    /// <summary>
    /// The folder which the mods dll and other files are stored.
    /// </summary>
    public string ModFolder;

    public Mod() { }

    public Mod(string name, string description, string dllPath, string modFolder)
    {
        this.name = name;
        this.description = description;
        this.dllPath = dllPath;
        ModFolder = modFolder;
    }
}

