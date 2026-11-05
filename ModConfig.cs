using BepInEx.Configuration;

namespace NoSaveDelete;

public static class ModConfig
{
    public static ConfigEntry<bool> AllowPlayerDelete = null!;
    public static ConfigEntry<bool> AllowGameDelete = null!;
    public static ConfigEntry<bool> AutoLoadInMultiplayer = null!;

    public static void Init(ConfigFile config)
    {
        AllowPlayerDelete = config.Bind("Settings", "AllowPlayerDelete", true,
            "Allow the player to delete saves? (true - yes, false - no)");
        AllowGameDelete = config.Bind("Settings", "AllowGameDelete", false,
            "Allow the game to delete saves? (true - yes, false - no)");
        AutoLoadInMultiplayer = config.Bind("Settings", "AutoLoadInMultiplayer", false,
            "Enable automatically load the last save in Multiplayer if ALL players die? (true - yes, false - no)");
    }
}
