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
    class BaseItem
    {
        public const string DllOnlyDescription = "This only a .dll file, please make mods into .zip with a json file when releasing the mod.";
        [JsonProperty("name")]
        public string Name = string.Empty;
        [JsonProperty("description")]
        public string Description = string.Empty;
        [JsonProperty("dll file")]
        public string DllPath = string.Empty;
        [JsonProperty("last edit")]
        public long LastEdit;
        [JsonProperty("public id")]
        public string PublicID = string.Empty;
        [JsonProperty("version")]
        public string Version = "N/A";
        [JsonProperty("tagline")]
        public string Tagline = string.Empty;
        [JsonProperty("source")]
        public string Source = string.Empty;
        [JsonProperty("is public")]
        public bool IsPublic;
        [JsonProperty("unlisted")]
        public bool Unlisted;
        [JsonProperty("preview image")]
        public string PreviewImage = string.Empty;
        [JsonProperty("web preview image")]
        public string WebPreviewImage = string.Empty;
        [JsonProperty("dependencies")]
        public List<string> Dependencies;

        [JsonIgnore]
        public DirectoryInfo Directory;
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
        public bool HasPublicID() => PublicID != string.Empty;
        public bool HasDll() => DllPath != string.Empty;
        public string GetFullDllPath() => Path.Combine(Directory.FullName, DllPath);
        public Mod CreateMod()
        {
            Mod = new Mod(Name, Description, Path.Combine(Directory.FullName, DllPath), Directory.FullName);
            return Mod;
        }
    }
}
