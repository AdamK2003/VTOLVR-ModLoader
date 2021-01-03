using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public bool HasPublicID()
        {
            return PublicID != string.Empty;
        }
        public bool HasDll()
        {
            return DllPath != string.Empty;
        }
        public Mod CreateMod()
        {
            Mod = new Mod(Name, Description, Path.Combine(Directory.FullName, DllPath), Directory.FullName);
            return Mod;
        }
    }
}
