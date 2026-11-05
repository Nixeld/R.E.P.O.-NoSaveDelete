using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(RunManager), "LeaveToMainMenu")]
internal static class ResetBackupFlagsOnMainMenuLeave
{
    private static void Prefix()
    {
        if (CheckArenaAndRestoreBackup.HasPendingRestore)
        {
            CheckArenaAndRestoreBackup.ResetFlags();
            Debug.Log("[NoSaveDelete] The host has left the arena and returned to the main menu. Resetting flags...");
        }
        else
        {
            Debug.Log("[NoSaveDelete] Returning to the main menu. Flags were not set — no reset needed.");
        }
    }
}
