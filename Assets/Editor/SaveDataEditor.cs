using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDataEditor
{
    private const string ProgressFileName = "progress.json";
    private const string SettingsFileName = "settings.json";

    [MenuItem("My Tools/Save Data/Reset All Save Data")]
    private static void ResetSaveData()
    {
        string progressPath = Path.Combine(Application.persistentDataPath, ProgressFileName);
        string settingsPath = Path.Combine(Application.persistentDataPath, SettingsFileName);

        bool fileDeleted = false;

        if (File.Exists(progressPath))
        {
            File.Delete(progressPath);
            Debug.Log($"Удален файл прогресса: {progressPath}");
            fileDeleted = true;
        }

        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
            Debug.Log($"Удален файл настроек: {settingsPath}");
            fileDeleted = true;
        }

        if (fileDeleted)
        {
            Debug.LogWarning("Сохранения сброшены. Перезапустите игру, чтобы изменения вступили в силу.");
        }
        else
        {
            Debug.Log("Файлы сохранения не найдены. Нечего сбрасывать.");
        }
        
        // Обновляем окно проекта, чтобы отразить удаление файлов, если они там отображались
        AssetDatabase.Refresh();
    }

    [MenuItem("My Tools/Save Data/Open Save Data Folder")]
    private static void OpenSaveDataFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}