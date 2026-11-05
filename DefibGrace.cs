using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NoSaveDelete;

/// Shared wait used by the singleplayer and multiplayer auto-reload paths after
/// the whole team is dead. Extends the reload only while a Defibro drone (ItemReviveItem)
/// is actually attempting a rescue; an out-of-reach or absent drone never delays it
/// beyond the original schedule.
internal static class DefibGrace
{
    // The drone re-scans for bodies every 0.2 s; this is the margin given to a
    // settled body to be targeted before the mod declares the drone out of reach.
    private const float RescueScanMarginSeconds = 0.5f;

    // Hard ceiling on waiting for an engaged rescue (a measured live rescue took ~8 s).
    private const float RescueWaitCapSeconds = 15f;

    /// Returns true when the reload should proceed, false when a revive cancelled the wipe.
    internal static async Task<bool> WaitForOutcome(CancellationToken token, float minimumWaitSeconds)
    {
        try
        {
            return await WaitCore(token, minimumWaitSeconds);
        }
        catch (TaskCanceledException)
        {
            Debug.Log("[NoSaveDelete] Revive hook canceled the pending auto-load.");
            return false;
        }
    }

    private static async Task<bool> WaitCore(CancellationToken token, float minimumWaitSeconds)
    {
        // Let the death RPC land first: the singleplayer prefix runs before the
        // original PlayerDeath sets deadSet.
        await Task.Delay(250, token);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool sawRescueAttempt = false;
        float? bodiesSettledAt = null;

        while (!token.IsCancellationRequested)
        {
            float elapsed = (float)stopwatch.Elapsed.TotalSeconds;

            if (AnyPlayerRevived())
            {
                Debug.Log("[NoSaveDelete] A player was revived. Auto-load canceled, run continues.");
                return false;
            }

            if (elapsed >= RescueWaitCapSeconds)
            {
                Debug.LogWarning("[NoSaveDelete] Defibro grace period timed out mid-rescue. Reloading anyway.");
                return true;
            }

            ItemReviveItem? drone = UnityEngine.Object.FindFirstObjectByType<ItemReviveItem>();
            if (drone == null)
            {
                if (sawRescueAttempt)
                {
                    Debug.Log("[NoSaveDelete] The Defibro's rescue ended without a survivor. Restarting save...");
                    return true;
                }
                if (elapsed >= minimumWaitSeconds)
                {
                    return true; // no Defibro in this run: original reload timing
                }
            }
            else if (drone.hasTarget)
            {
                if (!sawRescueAttempt)
                {
                    sawRescueAttempt = true;
                    Debug.Log("[NoSaveDelete] Defibro is attempting a rescue. Waiting for it before restarting the level...");
                }
            }
            else if (AllBodiesSettled())
            {
                bodiesSettledAt ??= elapsed;
                float giveUpAt = Math.Max(minimumWaitSeconds, bodiesSettledAt.Value + RescueScanMarginSeconds);
                if (elapsed >= giveUpAt)
                {
                    Debug.Log("[NoSaveDelete] Defibro is out of reach of the bodies. Restarting save on the normal schedule...");
                    return true;
                }
            }

            await Task.Delay(250, token);
        }

        Debug.Log("[NoSaveDelete] Revive hook canceled the pending auto-load.");
        return false;
    }

    private static bool AnyPlayerRevived()
    {
        foreach (PlayerAvatar player in SemiFunc.PlayerGetList())
        {
            if (player != null && !player.deadSet)
            {
                return true;
            }
        }
        return false;
    }

    // The drone can only target bodies whose death finished animating (isDisabled).
    // Until then, a nearby drone has not had its chance yet.
    private static bool AllBodiesSettled()
    {
        foreach (PlayerAvatar player in SemiFunc.PlayerGetList())
        {
            if (player != null && !player.isDisabled)
            {
                return false;
            }
        }
        return true;
    }
}
