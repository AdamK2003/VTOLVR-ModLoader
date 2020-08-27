using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModLoader.Patches
{
    [HarmonyPatch(typeof(VTMapManager), "RestartCurrentScenario")]
    class VTMapManagerPatch
    {
        static void Postfix(VTMapManager __instance)
        {
            VTOLAPI.instance.WaitForScenarioReload();
        }
    }
}
