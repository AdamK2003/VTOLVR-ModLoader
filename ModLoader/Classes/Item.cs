using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;

namespace ModLoader.Classes
{
    [JsonObject(MemberSerialization.OptIn)]
    class Item
    {
        [JsonProperty("Load On Start Check")]
        public bool LoadOnStartCheck { get; set; }
        [JsonProperty("Auto Update Check")]
        public bool AutoUpdateCheck { get; set; }
        [JsonProperty("Folder Directory")]
        public string FolderDirectory { get; set; }
    }
}
