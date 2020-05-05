using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    [Serializable]
    public class Save
    {
        public SettingsSave SettingsSave { get; set; }
        public DevToolsSave DevToolsSave { get; set; }

        public Save()
        {
        }

        public Save(SettingsSave settingsSave, DevToolsSave devToolsSave)
        {
            SettingsSave = settingsSave;
            DevToolsSave = devToolsSave;
        }
    }
}
