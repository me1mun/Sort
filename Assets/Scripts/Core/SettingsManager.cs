using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public event Action<bool> OnMusicSettingChanged;
    public event Action<bool> OnSfxSettingChanged;

    private SettingsData _settings;
    private DataManager _dataManager;

    public bool IsMusicOn => _settings.isMusicOn;
    public bool IsSfxOn => _settings.isSfxOn;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _dataManager = DataManager.Instance;
        _settings = _dataManager.Settings;
        
        ApplyAllSettings();
    }

    public void SetMusicOn(bool isOn)
    {
        if (_settings.isMusicOn == isOn) return;
        _settings.isMusicOn = isOn;
        ApplyMusicSetting();
        OnMusicSettingChanged?.Invoke(isOn);
    }

    public void SetSfxOn(bool isOn)
    {
        if (_settings.isSfxOn == isOn) return;
        _settings.isSfxOn = isOn;
        ApplySfxSetting();
        OnSfxSettingChanged?.Invoke(isOn);
    }

    public void SaveSettings()
    {
        _dataManager.SaveSettings();
        Debug.Log("Settings saved.");
    }
    
    private void ApplyAllSettings()
    {
        ApplyMusicSetting();
        ApplySfxSetting();
    }

    private void ApplyMusicSetting()
    {
        AudioManager.Instance.SetMusicVolume(_settings.isMusicOn ? 1.0f : 0.0f);
    }

    private void ApplySfxSetting()
    {
        AudioManager.Instance.SetSoundVolume(_settings.isSfxOn ? 1.0f : 0.0f);
    }
}