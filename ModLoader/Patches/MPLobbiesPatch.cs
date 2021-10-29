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
    [HarmonyPatch(typeof(VTOLMPLobbyManager), nameof(VTOLMPLobbyManager.CreateLobby))]
    class LobbyManagerPatch
    {
        static void Postfix(VTOLMPLobbyManager __instance, ref VTOLMPLobbyManager.LobbyTask __result)
        {

            VTOLMPLobbyManager.currentLobby.SetData("lMods", VTOLAPI.GetUsersOrderedMods());
            VTOLMPLobbyManager.currentLobby.SetData("lModCount", VTOLAPI.GetUsersMods().Count.ToString());

        }

    }

    //Patches the join lobby button to check if the user has the same mods as the lobby
    [HarmonyPatch(typeof(VTMPLobbyListItem), nameof(VTMPLobbyListItem.JoinButton))]
    class JoinLobbyPatch
    {
        private static VTUIErrorWindow errorUi = null;
        static bool Prefix(VTMPLobbyListItem __instance)
        {
            if (!errorUi)
            {
                errorUi = UnityEngine.Object.FindObjectOfType<VTUIErrorWindow>(true);
            }

            //TODO: Parse the lobby mods to display which mods you need to load
            if(UpdateLobbyPatch.lobbyMods != VTOLAPI.GetUsersOrderedMods())
            {
                errorUi.DisplayError("Required mods for lobby: ", null);
                return false;
            }

            return true;

        }
    }

    //Stores the lobby mods in a static string so another patch can have access to it
    [HarmonyPatch(typeof(VTMPLobbyListItem), nameof(VTMPLobbyListItem.UpdateForLobby))]
    class UpdateLobbyPatch
    {
        public static string lobbyMods = "";
        static void Postfix(Lobby l)
        {

            lobbyMods = l.GetData("lMods");

        }
    }
}
