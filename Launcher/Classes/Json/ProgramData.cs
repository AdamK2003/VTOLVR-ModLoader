// unset

using System.IO;
using Newtonsoft.Json;

namespace Launcher.Classes.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProgramData
    {
        [JsonProperty("VTOL VR Path")] 
        public string VTOLPath = string.Empty;

        public static void Save(ProgramData data, string path)
        {
            using (TextWriter tw = new StreamWriter(path))
            {
                var js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(tw, data);
            }
        }
    }
}