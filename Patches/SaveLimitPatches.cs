using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(MenuPageSaves), "OnNewGame")]
internal static class RemoveSaveLimitInMenuPatch
{
    private static bool Prefix()
    {
        if (SemiFunc.MainMenuIsMultiplayer())
        {
            SemiFunc.MenuActionHostGame(null, null);
        }
        else
        {
            SemiFunc.MenuActionSingleplayerGame(null, null);
        }
        Debug.Log("[NoSaveDelete] Creating new save (limit bypass active)");
        return false;
    }
}

[HarmonyPatch(typeof(MenuPageSaves), "Start")]
internal static class SaveLimitRemovalLogger
{
    private static void Postfix()
    {
        Debug.Log("[NoSaveDelete] ========================================");
        Debug.Log("[NoSaveDelete] Save limit (10 max) has been REMOVED!");
        Debug.Log("[NoSaveDelete] You can now create UNLIMITED saves!");
        Debug.Log("[NoSaveDelete] ========================================");
    }
}
