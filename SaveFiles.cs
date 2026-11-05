using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NoSaveDelete;

internal static class SaveFiles
{
    internal static string? CurrentSaveFileName => StatsManager.instance.saveFileCurrent;

    internal static void ReloadSaveInMemory(string saveFileName)
    {
        try
        {
            StatsManager.instance.LoadGame(saveFileName, null);
            Debug.Log("[NoSaveDelete] Save data reloaded in memory for " + saveFileName);
        }
        catch (Exception ex)
        {
            Debug.LogError("[NoSaveDelete] Error reloading save in memory: " + ex.Message);
        }
    }

    /// Copies the newest backup over the live save file. restoreBackup of
    /// AutoLoadMultiplayer/CheckArenaAndRestoreBackup also reloaded in memory;
    /// the quick-reload path reloads explicitly afterwards, so it passes false.
    internal static void RestoreBackup(string saveFileName, bool reloadInMemory)
    {
        string saveFolder = Path.Combine(Application.persistentDataPath, "saves", saveFileName);
        if (!Directory.Exists(saveFolder))
        {
            Debug.LogError("[NoSaveDelete] Save folder not found: " + saveFolder);
            return;
        }

        string? backupPath = FindLatestBackup(saveFolder, saveFileName);
        if (string.IsNullOrEmpty(backupPath))
        {
            Debug.LogWarning("[NoSaveDelete] No backup found! Using the standard save.");
            return;
        }

        try
        {
            string liveFile = Path.Combine(saveFolder, saveFileName + ".es3");
            if (File.Exists(liveFile))
            {
                File.Delete(liveFile);
            }
            File.Copy(backupPath, liveFile);
            Debug.Log("[NoSaveDelete] Backup restored from: " + backupPath);

            if (reloadInMemory)
            {
                ReloadSaveInMemory(saveFileName);
            }
        }
        catch (IOException ex)
        {
            Debug.LogError("[NoSaveDelete] Restore error: " + ex.Message);
        }
    }

    internal static string? FindLatestBackup(string directory, string saveFileName)
    {
        if (!Directory.Exists(directory))
        {
            Debug.LogError("[NoSaveDelete] Save directory is empty or does not exist!");
            return null;
        }

        string[] files = Directory.GetFiles(directory, saveFileName + "_BACKUP*.es3");
        if (files.Length == 0)
        {
            Debug.LogWarning("[NoSaveDelete] No backup files found.");
            return null;
        }

        Regex regex = new Regex("_BACKUP(\\d+)", RegexOptions.IgnoreCase);
        var latest = (from file in files
                      let number = ExtractBackupNumber(file, regex)
                      where number >= 0
                      orderby number descending
                      select new { FilePath = file, BackupNumber = number }).FirstOrDefault();

        if (latest == null)
        {
            Debug.LogWarning("[NoSaveDelete] Unable to determine the latest backup.");
            return null;
        }

        Debug.Log($"[NoSaveDelete] Selected backup: {latest.FilePath} (number {latest.BackupNumber})");
        return latest.FilePath;
    }

    private static int ExtractBackupNumber(string filePath, Regex regex)
    {
        Match match = regex.Match(Path.GetFileNameWithoutExtension(filePath));
        return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : -1;
    }
}
