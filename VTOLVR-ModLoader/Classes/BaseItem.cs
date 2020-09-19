using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VTOLVR_ModLoader.Classes
{
    public class BaseItem
    {
        public string Name { get; set; }
        public DirectoryInfo Directory { get; set; }
        public JObject Json { get; set; }

        public BaseItem(string name, DirectoryInfo directory, JObject json)
        {
            Name = name;
            Directory = directory;
            Json = json;
        }
    }
}
