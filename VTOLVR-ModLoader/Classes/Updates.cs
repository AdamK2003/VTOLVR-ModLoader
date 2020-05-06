using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    public class Updates
    {
        public string name { get; set; }
        public string tagName { get; set; }
        public string body { get; set; }
        public Asset[] assets { get; set; }

        public Updates() { }
        public Updates(string name, string tagName, string body, Asset[] assets)
        {
            this.name = name;
            this.tagName = tagName;
            this.body = body;
            this.assets = assets;
        }
    }

    public class Asset
    {
        public string fileName { get; set; }
        public string downloadURL { get; set; }

        public Asset(string fileName, string downloadURL)
        {
            this.fileName = fileName;
            this.downloadURL = downloadURL;
        }
    }
}
