using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(StatsManager), "SaveGame")]
internal static class PreventSaveGameUltimate
{
    private static bool playerJustDied;
    private static bool allPlayersDeadInMultiplayer;

    private static bool Prefix(ref string fileName)
    {
        if (ModConfig.AllowGameDelete.Value)
        {
            return true;
        }
        if (RunManager.instance.levelArena.Contains(RunManager.instance.levelCurrent))
        {
            Debug.Log("[NoSaveDelete] Blocking loss of items, charges, HP, and more has been activated! Because AllowGameDelete = false!");
            return false;
        }
        if (allPlayersDeadInMultiplayer && SemiFunc.IsMultiplayer())
        {
            Debug.Log("[NoSaveDelete] Blocking save because all players are dead in multiplayer!");
            return false;
        }
        if (playerJustDied && !SemiFunc.IsMultiplayer())
        {
            Debug.Log("[NoSaveDelete] Blocking save because player just died in singleplayer!");
            return false;
        }
        return true;
    }

    internal static void SetPlayerDied()
    {
        playerJustDied = true;
        Debug.Log("[NoSaveDelete] Player death flag set - saves will be blocked");
    }

    internal static void ResetPlayerDied()
    {
        playerJustDied = false;
        Debug.Log("[NoSaveDelete] Player death flag reset - saves allowed");
    }

    internal static void SetAllPlayersDead()
    {
        allPlayersDeadInMultiplayer = true;
        Debug.Log("[NoSaveDelete] All players dead flag set - multiplayer saves will be blocked");
    }

    internal static void ResetAllPlayersDead()
    {
        allPlayersDeadInMultiplayer = false;
        Debug.Log("[NoSaveDelete] All players dead flag reset - multiplayer saves allowed");
    }
}
