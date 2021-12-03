// This is the current json format as of 4.0.0

using System;
using System.Collections.Generic;
using System.IO;
using Core.Classes;
using Core.Enums;
using Valve.Newtonsoft.Json;

namespace Core.Jsons
{
    [JsonConverter(typeof(BaseItemConverter))]
    public class BaseItem
    {
        public const string JName = "name";
        public const string JDescription = "description";
        public const string JDllFile = "dll_file";
        public const string JLastEdit = "last_edit";
        public const string JPublicID = "pub_id";
        public const string JJsonVersion = "json_version";
        public const string JVersion = "version";
        public const string JTagline = "tagline";
        public const string JRepository = "repository";
        public const string JIsPublic = "is_public";
        public const string JUnlisted = "unlisted";
        public const string JPreviewImage = "preview_image";
        public const string JWebPreviewImage = "web_preview_image";
        public const string JDependencies = "dependencies";
        public const string JModDependencies = "mod_dependencies";
        public const string JSkinMaterials = "skin_materials";

        [JsonProperty(JName)] public string Name { get; set; } = string.Empty;
        [JsonProperty(JDescription)] public string Description { get; set; } = string.Empty;
        [JsonProperty(JDllFile)] public string DllPath { get; set; } = string.Empty;
        [JsonProperty(JLastEdit)] public long LastEdit { get; set; }
        [JsonProperty(JPublicID)] public string PublicID { get; set; } = string.Empty;
        [JsonProperty(JJsonVersion)] public string JsonVersion { get; set; } = string.Empty;
        [JsonProperty(JVersion)] public string Version { get; set; } = string.Empty;
        [JsonProperty(JTagline)] public string Tagline { get; set; } = string.Empty;
        [JsonProperty(JRepository)] public string Source { get; set; } = string.Empty;
        [JsonProperty(JIsPublic)] public bool IsPublic { get; set; }
        [JsonProperty(JUnlisted)] public bool Unlisted { get; set; }
        [JsonProperty(JPreviewImage)] public string PreviewImage { get; set; } = string.Empty;
        [JsonProperty(JWebPreviewImage)] public string WebPreviewImage { get; set; } = string.Empty;
        [JsonProperty(JDependencies)] public List<string> Dependencies { get; set; }
        [JsonProperty(JModDependencies)] public List<BaseItem> ModDependencies { get; set; }

        [JsonProperty(JSkinMaterials)]
        public List<Material> SkinMaterials { get; set; } = new List<Material>();
        [JsonIgnore] public DirectoryInfo Directory { get; set; }

        [JsonIgnore]
        public ContentType ContentType
        {
            get
            {
                if (_contentType == ContentType.None)
                    _contentType = GetContentType();
                return _contentType;
            }
        }

        [JsonIgnore] private ContentType _contentType;

        public bool HasPublicID()
        {
            return PublicID != string.Empty;
        }

        public bool HasDll()
        {
            if (string.IsNullOrEmpty(DllPath))
                return false;
            if (ContentType == ContentType.MyMods)
                return File.Exists(Path.Combine(Directory.FullName, "Builds", DllPath));
            return File.Exists(Path.Combine(Directory.FullName, DllPath));
        }

        public void SaveFile()
        {
            using (TextWriter tw = new StreamWriter(Path.Combine(Directory.FullName, "info.json")))
            {
                var js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(tw, this);
            }
        }

        private ContentType GetContentType()
        {
            var projectDir = Directory.Name.ToLower() == "builds" ? Directory.Parent : Directory;

            switch (projectDir.Parent.Name.ToLower())
            {
                case "mods":
                    return ContentType.Mods;
                case "skins":
                    return ContentType.Skins;
                case "my mods":
                    return ContentType.MyMods;
                case "my skins":
                    return ContentType.MySkins;
            }

            if (DllPath != string.Empty && DllPath.EndsWith(".dll"))
                return ContentType.Mods;

            Logger.Error($"Error, couldn't match {projectDir.Parent.Name.ToLower()} to a Content Type");
            return ContentType.None;
        }

        public static BaseItem GetItem(string json)
        {
            BaseItem item = Helper.DeserializeObject<BaseItem>(json, out Exception error);

            if (item.DllPath == null)
                item.DllPath = string.Empty;

            if (error == null)
            {
                return item;
            }

            Logger.Error("Failed to get base item.\nError:" + error);
            return null;
        }
    }
}