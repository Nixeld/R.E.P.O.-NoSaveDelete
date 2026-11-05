using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[HarmonyPatch(typeof(PlayerAvatar), "Start")]
internal static class InGameSaveSelectorInjector
{
    private static void Postfix(PlayerAvatar __instance)
    {
        if (__instance.GetComponent<InGameSaveSelector>() == null)
        {
            __instance.gameObject.AddComponent<InGameSaveSelector>();
            Debug.Log("[NoSaveDelete] InGameSaveSelector added to player!");
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar), "Start")]
internal static class QuickReloadInjector
{
    private static void Postfix(PlayerAvatar __instance)
    {
        if (__instance.GetComponent<QuickSaveReload>() == null)
        {
            __instance.gameObject.AddComponent<QuickSaveReload>();
            Debug.Log("[NoSaveDelete] QuickSaveReload added to player!");
        }
    }
}
