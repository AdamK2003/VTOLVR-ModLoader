// BaseItem is the info.json file
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VTOLVR_ModLoader.Views;
using Console = VTOLVR_ModLoader.Views.Console;

namespace VTOLVR_ModLoader.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    class BaseItem
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        [JsonProperty("dll file")]
        public string DllPath { get; set; } = string.Empty;
        [JsonProperty("last edit")]
        public long LastEdit { get; set; }
        [JsonProperty("public id")]
        public string PublicID { get; set; } = string.Empty;
        [JsonProperty("version")]
        public string Version { get; set; } = "N/A";
        [JsonProperty("tagline")]
        public string Tagline { get; set; } = string.Empty;
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;
        [JsonProperty("is public")]
        public bool IsPublic { get; set; }
        [JsonProperty("unlisted")]
        public bool Unlisted { get; set; }
        [JsonProperty("preview image")]
        public string PreviewImage { get; set; } = string.Empty;
        [JsonProperty("web preview image")]
        public string WebPreviewImage { get; set; } = string.Empty;
        [JsonProperty("dependencies")]
        public List<string> Dependencies;

        [JsonIgnore]
        public DirectoryInfo Directory { get; set; }
        public bool HasPublicID()
        {
            return PublicID != string.Empty;
        }
        public bool HasDll()
        {
            return DllPath != string.Empty;
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
        public void FilloutForm(ref HttpHelper form, bool isMod, string currentPath)
        {
            form.SetValue("version", Version);
            form.SetValue("name", Name);
            form.SetValue("tagline", Tagline);
            form.SetValue("description", Description);
            form.SetValue("unlisted", Unlisted.ToString());
            form.SetValue("is_public", IsPublic.ToString());
            if (isMod)
                form.SetValue("repository", Source);

            form.AttachFile("header_image", WebPreviewImage, Path.Combine(currentPath, WebPreviewImage));
            form.AttachFile("thumbnail", PreviewImage, Path.Combine(Directory.FullName, PreviewImage));
            form.SetValue("user_uploaded_file", string.Empty);

        }
    }
}
