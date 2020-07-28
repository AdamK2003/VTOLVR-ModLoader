using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    class Update
    {
        public string name { get; set; }
        public string tag_Name { get; set; }
        public string body { get; set; }
        public UpdateFile[] files;

        public Update() 
        {
            name = "No Internet Connection";
            body = "Please connect to the internet to see the latest releases";
        }

        public Update(string name, string tag_Name, string body)
        {
            this.name = name;
            this.tag_Name = tag_Name;
            this.body = body;
        }

        public Update SetFiles(UpdateFile[] files)
        {
            this.files = files;
            return this;
        }
    }
    class UpdateFile
    {
        public readonly string Name;
        public readonly string Hash;
        public readonly string Location;
        public readonly string Url;

        public UpdateFile(string name, string hash, string location, string url)
        {
            Name = name;
            Hash = hash;
            Location = location;
            Url = url;
        }
    }
}
