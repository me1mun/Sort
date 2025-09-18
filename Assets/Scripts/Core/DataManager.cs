using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public ProgressData Progress { get; private set; }
    public SettingsData Settings { get; private set; }

    private string _progressSavePath;
    private string _settingsSavePath;
    
    private int _cachedPredefinedLevelsCount;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _progressSavePath = Path.Combine(Application.persistentDataPath, "progress.json");
        _settingsSavePath = Path.Combine(Application.persistentDataPath, "settings.json");
        
        LoadProgress();
        LoadSettings();
    }
    
    public void CachePredefinedLevelCount(int count)
    {
        _cachedPredefinedLevelsCount = count;
    }

    public void AdvanceToNextLevel()
    {
        if (Progress.predefinedLevelIndex < _cachedPredefinedLevelsCount)
        {
            Progress.predefinedLevelIndex++;
        }
        else
        {
            Progress.randomLevelCount++;
        }
        
        SaveProgress();
    }

    public void LoadProgress()
    {
        if (File.Exists(_progressSavePath))
        {
            string json = File.ReadAllText(_progressSavePath);
            Progress = JsonUtility.FromJson<ProgressData>(json);
        }
        else
        {
            Progress = new ProgressData();
        }
    }

    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(Progress, true);
        File.WriteAllText(_progressSavePath, json);
    }

    public void LoadSettings()
    {
        if (File.Exists(_settingsSavePath))
        {
            string json = File.ReadAllText(_settingsSavePath);
            Settings = JsonUtility.FromJson<SettingsData>(json);
        }
        else
        {
            Settings = new SettingsData();
        }
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(Settings, true);
        File.WriteAllText(_settingsSavePath, json);
    }
    
    public void SetMusicVolume(int step)
    {
        Settings.musicVolume = step / 3.0f;
        AudioManager.Instance.SetMusicVolume(Settings.musicVolume);
        SaveSettings();
    }
    
    public void SetSoundVolume(int step)
    {
        Settings.soundVolume = step / 3.0f;
        AudioManager.Instance.SetSoundVolume(Settings.soundVolume);
        SaveSettings();
    }
}