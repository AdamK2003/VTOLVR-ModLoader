using Newtonsoft.Json;
namespace VTOLVR_ModLoader.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateFile
    {
        [JsonProperty("file_name")]
        public string Name;
        [JsonProperty("file_hash")]
        public string Hash;
        [JsonProperty("file_location")]
        public string Location;
        [JsonProperty("file")]
        public string Url;

        public UpdateFile(string name, string hash, string location, string url)
        {
            Name = name;
            Hash = hash;
            Location = location;
            Url = url;
        }
    }
}
