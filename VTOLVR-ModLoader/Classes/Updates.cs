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
        public string tag_Name { get; set; }
        public string body { get; set; }
        public string installer_file { get; set; }
        public string zip_file { get; set; }
        public int download_count { get; set; }

        public Updates() { }

        public Updates(string name, string tag_Name, string body, string installer_file, string zip_file, int download_count)
        {
            this.name = name;
            this.tag_Name = tag_Name;
            this.body = body;
            this.installer_file = installer_file;
            this.zip_file = zip_file;
            this.download_count = download_count;
        }
    }
}
