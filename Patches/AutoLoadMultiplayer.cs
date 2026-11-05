using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathRPC")]
internal static class AutoLoadMultiplayer
{
    private static int reloadPending;

    private static async void Postfix()
    {
        if (!SemiFunc.IsMultiplayer())
        {
            return;
        }
        if (!ModConfig.AutoLoadInMultiplayer.Value)
        {
            Debug.Log("[NoSaveDelete] Auto-load disabled due to AutoLoadInMultiplayer being disabled.");
            return;
        }
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        foreach (PlayerAvatar player in SemiFunc.PlayerGetList())
        {
            if (!player.deadSet)
            {
                Debug.Log("[NoSaveDelete] Not all players are dead. Reload is not performed.");
                return;
            }
        }

        // Synchronized deaths fire this postfix once per player; only one reload may be pending.
        if (Interlocked.CompareExchange(ref reloadPending, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await ReloadAfterGrace();
        }
        finally
        {
            Interlocked.Exchange(ref reloadPending, 0);
        }
    }

    private static async Task ReloadAfterGrace()
    {
        PreventSaveGameUltimate.SetAllPlayersDead();

        string? saveFileName = SaveFiles.CurrentSaveFileName;
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogWarning("[NoSaveDelete] No save file to load!");
            PreventSaveGameUltimate.ResetAllPlayersDead();
            return;
        }

        // Give the Defibro drone time to save the run before ending the level.
        if (!await DefibGrace.WaitForOutcome(CancellationToken.None, minimumWaitSeconds: 0f))
        {
            PreventSaveGameUltimate.ResetAllPlayersDead();
            return;
        }

        if (RunManager.instance.levelShop.Contains(RunManager.instance.levelCurrent))
        {
            Debug.Log("[NoSaveDelete] All players died in the SHOP. Restarting save WITHOUT restoring backup...");
            await Task.Delay(650);
            SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
            PreventSaveGameUltimate.ResetAllPlayersDead();
        }
        else
        {
            Debug.Log("[NoSaveDelete] All players died. Restoring backup...");
            SaveFiles.RestoreBackup(saveFileName, reloadInMemory: true);
            await Task.Delay(650);
            SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
            await Task.Delay(1500);
            SaveFiles.ReloadSaveInMemory(saveFileName);
            PreventSaveGameUltimate.ResetAllPlayersDead();
        }
    }
}
