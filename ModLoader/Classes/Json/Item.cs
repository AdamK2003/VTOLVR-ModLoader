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
    [JsonObject(MemberSerialization.OptIn)]
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
                string path = Path.Combine(Directory.FullName, PreviewImage);
                if (File.Exists(path))
                    return path;
                return string.Empty;
            }
        }
        [JsonIgnore]
        public bool IsDevFolder = false;
        public string GetFullDllPath() => Path.Combine(Directory.FullName, DllPath);
        public Mod CreateMod()
        {
            Mod = new Mod(Name, Description, Path.Combine(Directory.FullName, DllPath), Directory.FullName);
            return Mod;
        }
    }
}
