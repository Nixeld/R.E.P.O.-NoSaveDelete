using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(PlayerAvatar), "PlayerDeath")]
internal static class AutoLoadSingleplayer
{
    private static CancellationTokenSource? _cancellationTokenSource;

    internal static bool IsReloadPending => _cancellationTokenSource != null;

    internal static void CancelPendingReload()
    {
        _cancellationTokenSource?.Cancel();
    }

    private static async void Prefix()
    {
        if (ModConfig.AllowGameDelete.Value)
        {
            Debug.Log("[NoSaveDelete] Auto-load disabled due to AllowGameDelete being enabled.");
            return;
        }
        if (SemiFunc.IsMultiplayer())
        {
            return;
        }
        if (!SemiFunc.RunIsLevel() && !SemiFunc.RunIsArena())
        {
            Debug.Log("[NoSaveDelete] Player died in lobby/shop. Auto-load disabled.");
            return;
        }

        PreventSaveGameUltimate.SetPlayerDied();

        string? saveFileName = SaveFiles.CurrentSaveFileName;
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogWarning("[NoSaveDelete] No save file to load!");
            PreventSaveGameUltimate.ResetPlayerDied();
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        Debug.Log("[NoSaveDelete] The player has died. Waiting for any Defibro revive, then reloading: " + saveFileName);

        // Give the Defibro drone time to save the run; the old fixed 4 s wait
        // is kept as the floor when no drone is around.
        bool proceed = await DefibGrace.WaitForOutcome(token, minimumWaitSeconds: 4f);

        PreventSaveGameUltimate.ResetPlayerDied();
        if (!proceed)
        {
            return;
        }

        SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
        await Task.Delay(1000);
        SaveFiles.ReloadSaveInMemory(saveFileName);
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}

[HarmonyPatch(typeof(PlayerAvatar), "Revive")]
internal static class CancelAutoLoadOnRevive
{
    private static void Prefix()
    {
        if (AutoLoadSingleplayer.IsReloadPending)
        {
            AutoLoadSingleplayer.CancelPendingReload();
            Debug.Log("[NoSaveDelete] Player revived. Auto-load canceled.");
        }
        PreventSaveGameUltimate.ResetPlayerDied();
    }
}
