using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NoSaveDelete;

[BepInPlugin("Nixeld.NoSaveDelete", "NoSaveDelete", "1.0.0")]
public class NoSaveDelete : BaseUnityPlugin
{
    internal static NoSaveDelete Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        gameObject.transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        ModConfig.Init(Config);
        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        Logger.LogInfo($"Settings can be changed in BepInEx/config/{Info.Metadata.GUID}.cfg");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }
}
