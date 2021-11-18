// unset

using Harmony;
using UnityEngine;

namespace ModLoader.Patches
{
    [HarmonyPatch(typeof(LoadingSceneController), nameof(LoadingSceneController.Start))]
    public class LoadingSceneControllerPatch
    {
        static void Postfix(LoadingSceneController __instance)
        {
            if (DevTools.IsEnabled)
            {
                __instance.PlayerReady();
                Debug.Log($"Dev tools has made the player ready up");
            }
        }
    }
}