using System.Threading.Tasks;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(GameDirector), "Update")]
internal static class CheckArenaAndRestoreBackup
{
    private static bool shouldRestoreBackup;
    private static bool hasLoggedArena;

    internal static bool HasPendingRestore => shouldRestoreBackup;

    private static void Postfix()
    {
        if (ModConfig.AutoLoadInMultiplayer.Value)
        {
            return;
        }
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        bool inArena = RunManager.instance.levelArena.Contains(RunManager.instance.levelCurrent);
        bool inLobby = RunManager.instance.levelCurrent == RunManager.instance.levelLobbyMenu;

        if (inArena && !hasLoggedArena)
        {
            hasLoggedArena = true;
            shouldRestoreBackup = true;
            Debug.Log("[NoSaveDelete] The game is on the arena. Enabling backup restoration after returning to the lobby.");
        }

        if (shouldRestoreBackup && inLobby)
        {
            ResetFlags();
            _ = DelayedRestoreBackup();
        }
    }

    private static async Task DelayedRestoreBackup()
    {
        Debug.Log("[NoSaveDelete] Waiting 3 seconds before restoring the backup...");
        await Task.Delay(3000);
        string? saveFileName = SaveFiles.CurrentSaveFileName;
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogWarning("[NoSaveDelete] No active save file!");
            return;
        }
        SaveFiles.RestoreBackup(saveFileName, reloadInMemory: true);
    }

    internal static void ResetFlags()
    {
        shouldRestoreBackup = false;
        hasLoggedArena = false;
        Debug.Log("[NoSaveDelete] Backup flags have been reset.");
    }
}
