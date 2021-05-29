using Newtonsoft.Json;

namespace LauncherCore.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Release
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("tag_name")] public string TagName { get; set; }
        [JsonProperty("body")] public string Body { get; set; }
        [JsonProperty("files")] public UpdateFile[] Files { get; set; }

        public Release()
        {
            Name = "No Internet Connection";
            Body = "Please connect to the internet to see the latest releases";
        }

        public Release(string name, string tag_Name, string body)
        {
            Name = name;
            TagName = tag_Name;
            Body = body;
        }

        public Release SetFiles(UpdateFile[] files)
        {
            this.Files = files;
            return this;
        }
    }
}