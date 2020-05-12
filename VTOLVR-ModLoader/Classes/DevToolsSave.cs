using System;

namespace VTOLVR_ModLoader.Classes
{
    [Serializable]
    public class DevToolsSave
    {
        public Pilot previousPilot { get; set; }
        public Scenario previousScenario { get; set; }
        public string[] previousModsLoaded { get; set; }

        public DevToolsSave()
        {
        }

        public DevToolsSave(Pilot previousPilot, Scenario previousScenario, string[] previousModsLoaded)
        {
            this.previousPilot = previousPilot;
            this.previousScenario = previousScenario;
            this.previousModsLoaded = previousModsLoaded;
        }
    }
}
