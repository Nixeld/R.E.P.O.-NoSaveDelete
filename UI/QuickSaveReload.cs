using System.Threading.Tasks;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace NoSaveDelete;

public class QuickSaveReload : MonoBehaviour
{
    private bool isReloading;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9) && !isReloading)
        {
            Debug.Log("[NoSaveDelete] F9 pressed - Quick reload initiated!");
            _ = QuickReloadSave();
        }
    }

    private async Task QuickReloadSave()
    {
        if (isReloading)
        {
            Debug.Log("[NoSaveDelete] Reload already in progress!");
            return;
        }
        isReloading = true;

        string? saveFileName = SaveFiles.CurrentSaveFileName;
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogWarning("[NoSaveDelete] No active save file to reload!");
            isReloading = false;
            return;
        }

        Debug.Log("[NoSaveDelete] Quick reloading save: " + saveFileName);

        if (SemiFunc.IsMultiplayer())
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[NoSaveDelete] Only master client can reload in multiplayer!");
                isReloading = false;
                return;
            }

            PreventSaveGameUltimate.SetAllPlayersDead();
            if (RunManager.instance.levelShop.Contains(RunManager.instance.levelCurrent))
            {
                Debug.Log("[NoSaveDelete] Quick reload in SHOP. Restarting without backup restore...");
                await Task.Delay(650);
                SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
                PreventSaveGameUltimate.ResetAllPlayersDead();
                isReloading = false;
                return;
            }

            Debug.Log("[NoSaveDelete] Restoring backup for multiplayer quick reload...");
            SaveFiles.RestoreBackup(saveFileName, reloadInMemory: false);
            await Task.Delay(650);
            SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
            await Task.Delay(1500);
            SaveFiles.ReloadSaveInMemory(saveFileName);
            PreventSaveGameUltimate.ResetAllPlayersDead();
        }
        else
        {
            PreventSaveGameUltimate.SetPlayerDied();
            Debug.Log("[NoSaveDelete] Quick reloading in singleplayer...");
            await Task.Delay(650);
            SemiFunc.MenuActionSingleplayerGame(saveFileName, null);
            await Task.Delay(1000);
            SaveFiles.ReloadSaveInMemory(saveFileName);
            PreventSaveGameUltimate.ResetPlayerDied();
        }

        isReloading = false;
        Debug.Log("[NoSaveDelete] Quick reload completed!");
    }
}
