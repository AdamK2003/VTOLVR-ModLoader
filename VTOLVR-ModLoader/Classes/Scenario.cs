using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    [Serializable]
    public class Scenario
    {
        public string Name { get; set; }
        public string ID;
        public string cID;
        public Scenario(string name, string cID, string iD)
        {
            Name = name;
            ID = iD;
            this.cID = cID;
        }

        public Scenario()
        {
        }
    }
}
