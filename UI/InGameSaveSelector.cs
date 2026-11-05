using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NoSaveDelete;

public class InGameSaveSelector : MonoBehaviour
{
    private class SaveFileInfo
    {
        public string fileName = "";
        public string teamName = "";
        public string dateTime = "";
        public int level;
        public int currency;
        public int totalHaul;
        public float timePlayed;
        public List<string> playerNames = new();
        public bool isValid;
        public List<string> backups = new();
    }

    private bool showUI;
    private Rect windowRect = new(Screen.width / 2 - 400, Screen.height / 2 - 350, 800f, 700f);
    private Vector2 scrollPosition = Vector2.zero;

    private GUIStyle windowStyle = null!;
    private GUIStyle labelStyle = null!;
    private GUIStyle buttonStyle = null!;
    private GUIStyle currentSaveStyle = null!;
    private GUIStyle saveButtonStyle = null!;
    private GUIStyle deleteButtonStyle = null!;
    private GUIStyle renameButtonStyle = null!;
    private GUIStyle scrollViewStyle = null!;
    private bool stylesInitialized;

    private readonly List<SaveFileInfo> saveFiles = new();
    private bool savesLoaded;
    private string currentSaveFileName = "";

    private bool showLoadConfirmation;
    private string selectedSaveToLoad = "";
    private bool showDeleteConfirmation;
    private string selectedSaveToDelete = "";
    private bool showNewSaveDialog;
    private string newSaveName = "";
    private bool showRenameDialog;
    private string selectedSaveToRename = "";
    private string renameSaveNewName = "";

    private void Awake()
    {
        Debug.Log("[NoSaveDelete] InGameSaveSelector Awake() called");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F7))
        {
            showUI = !showUI;
            if (showUI)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (!savesLoaded)
                {
                    _ = LoadSaveFiles();
                }
            }
            else
            {
                ReleaseCursorIfInRun();
            }
            Debug.Log($"[NoSaveDelete] Save selector UI toggled: {showUI}");
        }

        if (!showUI || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (showLoadConfirmation)
        {
            showLoadConfirmation = false;
            return;
        }
        if (showDeleteConfirmation)
        {
            showDeleteConfirmation = false;
            return;
        }
        if (showNewSaveDialog)
        {
            showNewSaveDialog = false;
            return;
        }
        if (showRenameDialog)
        {
            showRenameDialog = false;
            return;
        }

        showUI = false;
        ReleaseCursorIfInRun();
    }

    private void ReleaseCursorIfInRun()
    {
        if (!SemiFunc.IsMainMenu() && !SemiFunc.RunIsLobbyMenu())
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnGUI()
    {
        if (!showUI)
        {
            return;
        }

        InitializeStyles();

        if (showLoadConfirmation)
        {
            DrawLoadConfirmationWindow();
        }
        else if (showDeleteConfirmation)
        {
            DrawDeleteConfirmationWindow();
        }
        else if (showNewSaveDialog)
        {
            DrawNewSaveDialog();
        }
        else if (showRenameDialog)
        {
            DrawRenameDialog();
        }
        else
        {
            windowRect = GUI.Window(54321, windowRect, DrawMainWindow, "Save File Manager (No_Save_Delete)", windowStyle);
        }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized)
        {
            return;
        }

        windowStyle = new GUIStyle(GUI.skin.window)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        windowStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true
        };
        labelStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(10, 10, 8, 8)
        };

        currentSaveStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            padding = new RectOffset(10, 10, 10, 10)
        };
        currentSaveStyle.normal.textColor = Color.green;
        currentSaveStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0.3f, 0f, 0.5f));

        saveButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(6, 6, 5, 5),
            margin = new RectOffset(2, 2, 2, 2)
        };

        deleteButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(6, 6, 5, 5),
            margin = new RectOffset(2, 2, 2, 2)
        };
        deleteButtonStyle.normal.textColor = Color.red;

        renameButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(6, 6, 5, 5),
            margin = new RectOffset(2, 2, 2, 2)
        };

        scrollViewStyle = new GUIStyle(GUI.skin.scrollView);

        stylesInitialized = true;
        Debug.Log("[NoSaveDelete] Save selector styles initialized");
    }

    private void DrawMainWindow(int windowID)
    {
        GUILayout.Space(10f);

        currentSaveFileName = SaveFiles.CurrentSaveFileName ?? "";
        if (!string.IsNullOrEmpty(currentSaveFileName))
        {
            GUILayout.BeginVertical(currentSaveStyle);
            GUILayout.Label("✅ Current Save: " + currentSaveFileName, labelStyle);
            GUILayout.EndVertical();
            GUILayout.Space(5f);
        }

        GUILayout.Label($"🎮 Available Save Files: ({saveFiles.Count})", labelStyle);
        GUILayout.Space(5f);

        if (!savesLoaded)
        {
            GUILayout.Label("⏳ Loading saves...", labelStyle);
        }
        else if (saveFiles.Count == 0)
        {
            GUILayout.Label("❌ No save files found!", labelStyle);
        }
        else
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollViewStyle, GUILayout.Height(450f));
            foreach (SaveFileInfo saveFile in saveFiles)
            {
                DrawSaveFileEntry(saveFile);
                GUILayout.Space(3f);
            }
            GUILayout.EndScrollView();
        }

        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ New Save", buttonStyle))
        {
            newSaveName = "";
            showNewSaveDialog = true;
        }
        if (GUILayout.Button("🔄 Refresh", buttonStyle))
        {
            savesLoaded = false;
            _ = LoadSaveFiles();
        }
        if (GUILayout.Button("❌ Close [F7 / ESC]", buttonStyle))
        {
            showUI = false;
            ReleaseCursorIfInRun();
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    private void DrawSaveFileEntry(SaveFileInfo save)
    {
        bool isCurrent = save.fileName == currentSaveFileName;
        Color previousBackground = GUI.backgroundColor;
        if (isCurrent)
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        }
        GUILayout.BeginVertical(GUI.skin.box);
        GUI.backgroundColor = previousBackground;

        GUILayout.BeginHorizontal();
        GUILayout.Label((isCurrent ? "🟢 " : "⚪ ") + save.teamName, new GUIStyle(labelStyle)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        });
        GUILayout.FlexibleSpace();

        if (!isCurrent && save.isValid)
        {
            if (GUILayout.Button("📂 Load", saveButtonStyle, GUILayout.Width(70f)))
            {
                selectedSaveToLoad = save.fileName;
                showLoadConfirmation = true;
            }
        }
        else if (isCurrent)
        {
            GUILayout.Label("✅ Active", saveButtonStyle, GUILayout.Width(70f));
        }

        if (save.isValid && GUILayout.Button("Rename", renameButtonStyle, GUILayout.Width(80f)))
        {
            selectedSaveToRename = save.fileName;
            renameSaveNewName = save.teamName;
            showRenameDialog = true;
        }

        if (GUILayout.Button("🗑️ Delete", deleteButtonStyle, GUILayout.Width(70f)))
        {
            selectedSaveToDelete = save.fileName;
            showDeleteConfirmation = true;
        }
        GUILayout.EndHorizontal();

        if (save.isValid)
        {
            GUILayout.Label("📅 " + save.dateTime, labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"🚚 Level: {save.level + 1}", labelStyle, GUILayout.Width(150f));
            GUILayout.Label($"💰 Money: {save.currency}k", labelStyle, GUILayout.Width(150f));
            GUILayout.Label("⏱️ Time: " + FormatTime(save.timePlayed), labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label($"💵 Total Haul: ${save.totalHaul}k", labelStyle);
            if (save.playerNames is { Count: > 0 })
            {
                GUILayout.Label("👥 Players: " + string.Join(", ", save.playerNames), labelStyle);
            }
            if (save.backups is { Count: > 0 })
            {
                GUILayout.Label($"💾 Backups: {save.backups.Count}", labelStyle);
            }
        }
        else
        {
            GUIStyle corruptedStyle = new(labelStyle);
            corruptedStyle.normal.textColor = Color.red;
            GUILayout.Label("⚠️ CORRUPTED SAVE FILE", corruptedStyle);
        }

        GUILayout.EndVertical();
    }

    private void DrawLoadConfirmationWindow()
    {
        Rect rect = new(Screen.width / 2 - 200, Screen.height / 2 - 100, 400f, 200f);
        GUI.Window(54322, rect, DrawLoadConfirmation, "⚠️ Load Save?", windowStyle);
    }

    private void DrawLoadConfirmation(int windowID)
    {
        GUILayout.Space(15f);
        GUILayout.Label("Are you sure you want to load this save?", new GUIStyle(labelStyle)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(5f);
        GUILayout.Label("Save: " + selectedSaveToLoad, new GUIStyle(labelStyle)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(10f);

        GUIStyle warningStyle = new(labelStyle);
        warningStyle.normal.textColor = Color.yellow;
        warningStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("⚠️ Current progress will be lost!", warningStyle);

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("✅ Yes, Load Save", buttonStyle, GUILayout.Height(40f)))
        {
            LoadSelectedSave();
            showLoadConfirmation = false;
        }
        if (GUILayout.Button("❌ Cancel", buttonStyle, GUILayout.Height(40f)))
        {
            showLoadConfirmation = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        GUILayout.Label("Press ESC to cancel", labelStyle);
    }

    private void DrawDeleteConfirmationWindow()
    {
        Rect rect = new(Screen.width / 2 - 200, Screen.height / 2 - 100, 400f, 200f);
        GUI.Window(54323, rect, DrawDeleteConfirmation, "⚠️ Delete Save?", windowStyle);
    }

    private void DrawDeleteConfirmation(int windowID)
    {
        GUILayout.Space(15f);
        GUILayout.Label("Are you sure you want to DELETE this save?", new GUIStyle(labelStyle)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(5f);
        GUILayout.Label("Save: " + selectedSaveToDelete, new GUIStyle(labelStyle)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(10f);

        GUIStyle dangerStyle = new(labelStyle);
        dangerStyle.normal.textColor = Color.red;
        dangerStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("⚠️ This action CANNOT be undone!", dangerStyle);

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🗑️ Yes, Delete Forever", deleteButtonStyle, GUILayout.Height(40f)))
        {
            DeleteSelectedSave();
            showDeleteConfirmation = false;
        }
        if (GUILayout.Button("❌ Cancel", buttonStyle, GUILayout.Height(40f)))
        {
            showDeleteConfirmation = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        GUILayout.Label("Press ESC to cancel", labelStyle);
    }

    private void DrawNewSaveDialog()
    {
        Rect rect = new(Screen.width / 2 - 250, Screen.height / 2 - 100, 500f, 200f);
        GUI.Window(54324, rect, DrawNewSave, "➕ Create New Save", windowStyle);
    }

    private void DrawNewSave(int windowID)
    {
        GUILayout.Space(15f);
        GUILayout.Label("Enter team name for new save:", labelStyle);
        GUILayout.Space(5f);
        GUI.SetNextControlName("NewSaveNameField");
        newSaveName = GUILayout.TextField(newSaveName, 30, GUILayout.Height(30f));
        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("✅ Create Save", buttonStyle, GUILayout.Height(40f)))
        {
            CreateNewSave();
        }
        if (GUILayout.Button("❌ Cancel", buttonStyle, GUILayout.Height(40f)))
        {
            showNewSaveDialog = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        GUILayout.Label("Press ESC to cancel", labelStyle);
        GUI.FocusControl("NewSaveNameField");
    }

    private void DrawRenameDialog()
    {
        Rect rect = new(Screen.width / 2 - 250, Screen.height / 2 - 100, 500f, 200f);
        GUI.Window(54325, rect, DrawRename, "✏️ Rename Save", windowStyle);
    }

    private void DrawRename(int windowID)
    {
        GUILayout.Space(15f);
        GUILayout.Label("Renaming: " + selectedSaveToRename, labelStyle);
        GUILayout.Space(5f);
        GUILayout.Label("New team name:", labelStyle);
        GUILayout.Space(5f);
        GUI.SetNextControlName("RenameField");
        renameSaveNewName = GUILayout.TextField(renameSaveNewName, 30, GUILayout.Height(30f));
        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("✅ Rename", buttonStyle, GUILayout.Height(40f)))
        {
            RenameSelectedSave();
        }
        if (GUILayout.Button("❌ Cancel", buttonStyle, GUILayout.Height(40f)))
        {
            showRenameDialog = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        GUILayout.Label("Press ESC to cancel", labelStyle);
        GUI.FocusControl("RenameField");
    }

    private async Task LoadSaveFiles()
    {
        savesLoaded = false;
        saveFiles.Clear();
        Debug.Log("[NoSaveDelete] Loading save files...");

        List<StatsManager.SaveFolder> folders = await StatsManager.instance.SaveFileGetAllAsync();
        foreach (StatsManager.SaveFolder folder in folders)
        {
            SaveFileInfo info = new()
            {
                fileName = folder.name,
                backups = folder.backups
            };

            string? teamName = StatsManager.instance.SaveFileGetTeamName(folder.name, null);
            string? dateAndTime = StatsManager.instance.SaveFileGetDateAndTime(folder.name, null);
            int level = 0;
            int currency = 0;
            int totalHaul = 0;
            bool parsed = int.TryParse(StatsManager.instance.SaveFileGetRunLevel(folder.name, null), out level)
                && int.TryParse(StatsManager.instance.SaveFileGetRunCurrency(folder.name, null), out currency)
                && int.TryParse(StatsManager.instance.SaveFileGetTotalHaul(folder.name, null), out totalHaul);

            if (parsed)
            {
                info.isValid = true;
                info.teamName = teamName ?? folder.name;
                info.dateTime = dateAndTime ?? "Unknown date";
                info.level = level;
                info.currency = currency;
                info.totalHaul = totalHaul;
                info.timePlayed = StatsManager.instance.SaveFileGetTimePlayed(folder.name, null);
                info.playerNames = StatsManager.instance.SaveFileGetPlayerNames(folder.name, null);
            }
            else
            {
                info.isValid = false;
                info.teamName = folder.name + " (CORRUPTED)";
                info.dateTime = "N/A";
            }

            saveFiles.Add(info);
        }

        savesLoaded = true;
        Debug.Log($"[NoSaveDelete] Loaded {saveFiles.Count} save files");
    }

    private async void LoadSelectedSave()
    {
        if (string.IsNullOrEmpty(selectedSaveToLoad))
        {
            Debug.LogWarning("[NoSaveDelete] No save selected to load!");
            return;
        }

        Debug.Log("[NoSaveDelete] Loading save: " + selectedSaveToLoad);
        bool multiplayer = SemiFunc.IsMultiplayer();
        if (multiplayer)
        {
            PreventSaveGameUltimate.SetAllPlayersDead();
        }
        else
        {
            PreventSaveGameUltimate.SetPlayerDied();
        }

        SaveFileInfo? saveInfo = saveFiles.FirstOrDefault(s => s.fileName == selectedSaveToLoad);
        if (saveInfo != null)
        {
            SemiFunc.MenuActionSingleplayerGame(saveInfo.fileName, saveInfo.backups);
            await Task.Delay(1500);
            SaveFiles.ReloadSaveInMemory(saveInfo.fileName);
        }

        if (multiplayer)
        {
            PreventSaveGameUltimate.ResetAllPlayersDead();
        }
        else
        {
            PreventSaveGameUltimate.ResetPlayerDied();
        }

        showUI = false;
        ReleaseCursorIfInRun();
    }

    private void DeleteSelectedSave()
    {
        if (string.IsNullOrEmpty(selectedSaveToDelete))
        {
            Debug.LogWarning("[NoSaveDelete] No save selected to delete!");
            return;
        }

        Debug.Log("[NoSaveDelete] Deleting save from UI: " + selectedSaveToDelete);
        MenuPageSavesPatch.IsPlayerDeletingSave = true;
        StatsManager.instance.SaveFileDelete(selectedSaveToDelete);
        MenuPageSavesPatch.IsPlayerDeletingSave = false;
        savesLoaded = false;
        _ = LoadSaveFiles();
        Debug.Log("[NoSaveDelete] Save deleted successfully!");
    }

    private void CreateNewSave()
    {
        if (string.IsNullOrWhiteSpace(newSaveName))
        {
            newSaveName = "R.E.P.O.";
        }

        Debug.Log("[NoSaveDelete] Creating new save with name: " + newSaveName);
        StatsManager.instance.teamName = newSaveName;
        StatsManager.instance.SaveFileCreate("", false);
        showNewSaveDialog = false;
        savesLoaded = false;
        _ = LoadSaveFiles();
        Debug.Log("[NoSaveDelete] New save created successfully!");
    }

    private void RenameSelectedSave()
    {
        if (string.IsNullOrEmpty(selectedSaveToRename) || string.IsNullOrWhiteSpace(renameSaveNewName))
        {
            Debug.LogWarning("[NoSaveDelete] Invalid rename parameters!");
            return;
        }

        Debug.Log("[NoSaveDelete] Renaming save " + selectedSaveToRename + " to " + renameSaveNewName);
        StatsManager.instance.LoadGame(selectedSaveToRename, null);
        StatsManager.instance.teamName = renameSaveNewName;
        StatsManager.instance.SaveGame(selectedSaveToRename);
        showRenameDialog = false;
        savesLoaded = false;
        _ = LoadSaveFiles();
        Debug.Log("[NoSaveDelete] Save renamed successfully!");
    }

    private static string FormatTime(float timeInSeconds)
    {
        int hours = (int)(timeInSeconds / 3600f);
        int minutes = (int)(timeInSeconds % 3600f / 60f);
        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }

    private static Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D texture = new(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
