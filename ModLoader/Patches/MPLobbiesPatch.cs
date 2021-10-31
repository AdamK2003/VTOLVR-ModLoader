using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using VTOLVR.Multiplayer;
using UnityEngine;
using System.Collections;
using Steamworks;
using Steamworks.Data;

namespace ModLoader.Patches
{

    //Patches the create lobby function to add the host's loaded mods to the lobby info
    [HarmonyPatch(typeof(VTMPMainMenu), nameof(VTMPMainMenu.LaunchMPGameForScenario))]
    class LobbyManagerPatch
    {
        static void Prefix()
        {
            if (VTOLMPLobbyManager.isLobbyHost)
            {
                Debug.Log("Setting mod data: " + VTOLAPI.GetUsersOrderedMods());
                VTOLMPLobbyManager.currentLobby.SetData("lMods", VTOLAPI.GetUsersOrderedMods());
                VTOLMPLobbyManager.currentLobby.SetData("lModCount", VTOLAPI.GetUsersMods().Count.ToString());

            }

        }

    }

    //Patches the join lobby button to check if the user has the same mods as the lobby
    [HarmonyPatch(typeof(VTMPMainMenu), nameof(VTMPMainMenu.JoinLobby))]
    class JoinLobbyPatch
    {

        static bool Prefix(VTMPMainMenu __instance, Lobby l)
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
