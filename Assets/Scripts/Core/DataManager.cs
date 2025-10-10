using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public ProgressData Progress { get; private set; }
    public SettingsData Settings { get; private set; }

    private string _progressSavePath;
    private string _settingsSavePath;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _progressSavePath = Path.Combine(Application.persistentDataPath, "progress.json");
        _settingsSavePath = Path.Combine(Application.persistentDataPath, "settings.json");

        LoadAllData();
    }

    public void AdvanceToNextLevel(int predefinedLevelsCount)
    {
        if (Progress.predefinedLevelIndex < predefinedLevelsCount)
        {
            Progress.predefinedLevelIndex++;
        }
        else
        {
            Progress.randomLevelCount++;
        }
        // Сохраняем только прогресс
        SaveProgress();
    }

    // --- Методы загрузки ---

    public void LoadAllData()
    {
        LoadProgress();
        LoadSettings();
    }

    public void LoadProgress()
    {
        Progress = Load<ProgressData>(_progressSavePath);
    }

    public void LoadSettings()
    {
        Settings = Load<SettingsData>(_settingsSavePath);
    }

    // --- Методы сохранения ---

    public void SaveProgress()
    {
        Save(Progress, _progressSavePath);
    }

    public void SaveSettings()
    {
        Save(Settings, _settingsSavePath);
    }

    // --- Приватные обобщенные методы ---

    private T Load<T>(string path) where T : new()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        return new T();
    }

    private void Save<T>(T data, string path)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }
}