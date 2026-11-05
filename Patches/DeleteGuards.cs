using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(MenuPageSaves), "OnDeleteGame")]
internal static class MenuPageSavesPatch
{
    internal static bool IsPlayerDeletingSave;

    private static bool Prefix()
    {
        Debug.Log($"[NoSaveDelete] Attempt to delete the save. AllowPlayerDelete = {ModConfig.AllowPlayerDelete.Value}");
        if (!ModConfig.AllowPlayerDelete.Value)
        {
            Debug.Log("[NoSaveDelete] The player is not allowed to delete the save (config prohibits it).");
            MenuManager.instance.PagePopUp("Delete blocked.", Color.red, "You cannot delete the save. Please change the mod config.", "OK", false);
            return false;
        }
        Debug.Log("[NoSaveDelete] The player is deleting the save.");
        IsPlayerDeletingSave = true;
        return true;
    }
}

[HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
internal static class SaveFileDeletePatch
{
    private static bool Prefix(string saveFileName)
    {
        if (MenuPageSavesPatch.IsPlayerDeletingSave)
        {
            Debug.Log("Player delete save " + saveFileName + ".");
            MenuPageSavesPatch.IsPlayerDeletingSave = false;
            return true;
        }
        if (ModConfig.AllowGameDelete.Value)
        {
            Debug.Log("Game delete save " + saveFileName + " (allowed by config).");
            return true;
        }
        Debug.Log("The game tried to delete the save " + saveFileName + ", but the mod blocked it.");
        return false;
    }
}
