// This is the current json format as of 4.0.0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace Core.Jsons
{
    [JsonConverter(typeof(BaseItemConverter))]
    class BaseItem
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
    }
}
