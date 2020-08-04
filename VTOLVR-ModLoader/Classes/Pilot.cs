using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    [Serializable]
    public class Pilot
    {
        public string Name { get; set; }
        public Pilot(string name)
        {
            Name = name;
        }

        public Pilot()
        {
        }
    }
}
