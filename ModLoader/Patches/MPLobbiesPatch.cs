using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
// using VTOLVR.Multiplayer;
using UnityEngine;
using System.Collections;
using Steamworks;
using Steamworks.Data;
using VTOLVR.Multiplayer;

namespace ModLoader.Patches
{
    /// <summary>
    /// Adds extra information to the lobby data for modded clients
    /// </summary>
    [HarmonyPatch(typeof(VTMPMainMenu), nameof(VTMPMainMenu.LaunchMPGameForScenario))]
    class VTMPMainMenu_LaunchMPGameForScenario
    {
        private static void Prefix()
        {
            if (!VTOLMPLobbyManager.isLobbyHost)
                return;

            // Sets mods and mods count to check the same mods
            Debug.Log("Setting mod data: " + VTOLAPI.GetUsersOrderedMods());
            VTOLMPLobbyManager.currentLobby.SetData("lMods", VTOLAPI.GetUsersOrderedMods());
            VTOLMPLobbyManager.currentLobby.SetData("lModCount", VTOLAPI.GetUsersMods().Count.ToString());
        }

    }

    //Patches the join lobby button to check if the user has the same mods as the lobby
    [HarmonyPatch(typeof(VTMPMainMenu), nameof(VTMPMainMenu.JoinLobby))]
    class VTMPMainMenu_JoinLobby
    {
        private static bool Prefix(VTMPMainMenu __instance, Lobby l)
        {
            string loadedMods = l.GetData("lMods");
            //TODO: Parse the lobby mods to display which mods you need to load
            Debug.Log("Trying to join lobby, here are its mods: " + loadedMods);
            if (loadedMods != VTOLAPI.GetUsersOrderedMods())
            {
                Debug.Log("Unable to join because of mismatched mods: " + loadedMods);
                __instance.ShowError("Required mods for lobby: " + loadedMods);
                return false;
            }
            return true;
        }
    }
}
