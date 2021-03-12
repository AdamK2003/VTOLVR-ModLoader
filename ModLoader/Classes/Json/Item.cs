using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace ModLoader.Classes.Json
{
    class BaseItem : Core.Jsons.BaseItem
    {
        public const string DllOnlyDescription = "This only a .dll file, please make mods into .zip with a json file when releasing the mod.";
        [JsonIgnore]
        public Mod Mod;
        [JsonIgnore]
        public GameObject ListGO, SettingsGO, SettingsHolerGO;
        [JsonIgnore]
        public string ImagePath
        {
            get
            {
                string path = Path.Combine(Directory.FullName, "preview.png");
                if (File.Exists(path))
                    return path;
                return string.Empty;
            }
        }
        [JsonIgnore]
        public bool IsDevFolder = false;
        public string GetFullDllPath()
        {
            switch (ContentType)
            {
                case Core.Enums.ContentType.Mods:
                    return Path.Combine(Directory.FullName, DllPath);
                case Core.Enums.ContentType.MyMods:
                    return Path.Combine(Directory.FullName, "Builds", DllPath);
                default:
                    Debug.LogError($"GetFullDllPath was called when contenttype wasn't a mod! ContentType = {ContentType}");
                    return string.Empty;
            }
        }
        public Mod CreateMod()
        {
            string modFolder = Directory.FullName;
            if (ContentType == Core.Enums.ContentType.MyMods)
            {
                modFolder = Path.Combine(Directory.FullName, "Builds");
            }

            Mod = new Mod(Name, Description, Path.Combine(Directory.FullName, DllPath), modFolder);
            return Mod;
        }
        public static BaseItem ToBaseItem(Core.Jsons.BaseItem baseItem)
        {
            // For some reason casting doesn't work,
            // So this method is a replacement
            if (baseItem == null)
                return null;

            BaseItem item = new BaseItem
            {
                Name = baseItem.Name,
                Description = baseItem.Description,
                DllPath = baseItem.DllPath,
                LastEdit = baseItem.LastEdit,
                PublicID = baseItem.PublicID,
                JsonVersion = baseItem.JsonVersion,
                Version = baseItem.Version,
                Tagline = baseItem.Tagline,
                Source = baseItem.Source,
                IsPublic = baseItem.IsPublic,
                Unlisted = baseItem.Unlisted,
                PreviewImage = baseItem.PreviewImage,
                WebPreviewImage = baseItem.WebPreviewImage,
                Dependencies = baseItem.Dependencies,
                ModDependencies = baseItem.ModDependencies,
                Directory = baseItem.Directory
            };

            return item;
        }
    }
}
