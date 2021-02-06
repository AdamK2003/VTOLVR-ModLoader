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
using Core;
namespace ModLoader
{
    class ModReader
    {
        public static List<Core.Jsons.BaseItem> Items
        {
            get
            {
                if (_items == null)
                    _items = GetItems();
                return _items;
            }
        }
        private static List<Core.Jsons.BaseItem> _items;

        /// <summary>
        /// Gets all of the mods info located in the path into memory
        /// </summary>
        /// <param name="path">The folder to check for mods</param>
        /// <param name="isDevFolder">If we are checking through the users My Projects Folder</param>
        [Obsolete]
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
                    pathToCheck = Path.Combine(mods[i].FullName, "Builds");
                else
                    pathToCheck = mods[i].FullName;

                if (!File.Exists(Path.Combine(pathToCheck, "info.json")))
                {
                    Debug.Log($"Mod: {mods[i].Name} doesn't have a info.json file");
                    continue;
                }

                Core.Jsons.BaseItem item = Core.Jsons.BaseItem.GetItem(
                    File.ReadAllText(Path.Combine(pathToCheck, "info.json")));

                if (item != null)
                {
                    lastMod = BaseItem.ToBaseItem(item);
                    lastMod.Directory = new DirectoryInfo(pathToCheck);
                    lastMod.IsDevFolder = isDevFolder;
                    lastMod.CreateMod();
                    foundMods.Add(lastMod);
                    Debug.Log("[Mod Reader] Added " + item.Name);
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
        [Obsolete]
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

        private static List<Core.Jsons.BaseItem> GetItems()
        {
            List<Core.Jsons.BaseItem> items =
                Helper.FindMods(Path.Combine(ModLoaderManager.RootPath, "mods"));

            items.AddRange(
                Helper.FindMods(Path.Combine(ModLoaderManager.MyProjectsPath, "My Mods"), true));

            items.AddRange(
                Helper.FindSkins(Path.Combine(ModLoaderManager.RootPath, "skins")));

            items.AddRange(
                Helper.FindSkins(Path.Combine(ModLoaderManager.MyProjectsPath, "My Skins")));

            return items;
        }
    }
}
