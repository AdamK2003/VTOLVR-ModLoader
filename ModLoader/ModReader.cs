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
        public static List<BaseItem> GetMods()
        {
            List<BaseItem> foundMods = new List<BaseItem>();

            BaseItem lastMod;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ContentType == Core.Enums.ContentType.Skins ||
                    Items[i].ContentType == Core.Enums.ContentType.MySkins)
                    continue;

                if (Items[i].HasDll())
                {
                    lastMod = BaseItem.ToBaseItem(Items[i]);
                    lastMod.IsDevFolder = Items[i].ContentType == Core.Enums.ContentType.MyMods;
                    lastMod.CreateMod();
                    foundMods.Add(lastMod);
                    Debug.Log($"[Mod Reader] Added {lastMod.Name}");
                }
            }
            return foundMods;
        }

        private static List<Core.Jsons.BaseItem> GetItems()
        {
            string modsFolder = Path.Combine(ModLoaderManager.RootPath, "mods");

            List<Core.Jsons.BaseItem> items = Helper.FindMods(modsFolder);

            items.AddRange(Helper.FindDllMods(modsFolder));

            if (!string.IsNullOrEmpty(ModLoaderManager.MyProjectsPath) &&
                Directory.Exists(Path.Combine(ModLoaderManager.MyProjectsPath, "My Mods")))
            {
                items.AddRange(
                    Helper.FindMods(Path.Combine(ModLoaderManager.MyProjectsPath, "My Mods"), true));
            }

            items.AddRange(
                Helper.FindSkins(Path.Combine(ModLoaderManager.RootPath, "skins")));

            if (!string.IsNullOrEmpty(ModLoaderManager.MyProjectsPath) &&
                Directory.Exists(Path.Combine(ModLoaderManager.MyProjectsPath, "My Skins")))
            {
                items.AddRange(
                    Helper.FindSkins(Path.Combine(ModLoaderManager.MyProjectsPath, "My Skins")));
            }


            return items;
        }
    }
}
