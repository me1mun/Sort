using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Initializing, InMenu, Playing, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    
    [Header("Game Configuration")]
    [SerializeField] private AvailableLanguages availableLanguages;
    [SerializeField] private string defaultLanguageCode = "en";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        CurrentState = GameState.Initializing;
        yield return null;

        InitializeLocalization();
        ApplyAudioSettings();
        
        SceneManager.LoadScene("Gameplay");
        CurrentState = GameState.Playing;
    }

    private void InitializeLocalization()
    {
        if (availableLanguages == null || availableLanguages.languages.Count == 0)
        {
            Debug.LogError("AvailableLanguages asset is not assigned or is empty in GameManager!");
            LocalizationManager.Instance.LoadLocalization(defaultLanguageCode);
            return;
        }

        Debug.Log("--- Available Languages ---");
        foreach (var lang in availableLanguages.languages)
        {
            Debug.Log($"{lang.languageName} ({lang.languageCode})");
        }
        
        string systemLangCode = GetSystemLanguageCode();
        
        bool isSystemLangAvailable = availableLanguages.languages.Any(lang => lang.languageCode == systemLangCode);
        string finalLangCode = isSystemLangAvailable ? systemLangCode : defaultLanguageCode;

        DataManager.Instance.Settings.languageCode = finalLangCode;
        DataManager.Instance.SaveSettings();
        
        LocalizationManager.Instance.LoadLocalization(finalLangCode);
        Debug.Log($"Current language set to: {finalLangCode}");
    }

    private void ApplyAudioSettings()
    {
        var settings = DataManager.Instance.Settings;
        var audioManager = AudioManager.Instance;
        
        audioManager.SetMusicVolume(settings.isMusicOn ? 1.0f : 0.0f);
        audioManager.SetSoundVolume(settings.isSfxOn ? 1.0f : 0.0f);
    }
    
    public void UpdateGameState(GameState newState)
    {
        CurrentState = newState;
    }

    private string GetSystemLanguageCode()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Russian: return "ru";
            case SystemLanguage.Ukrainian: return "uk";
            case SystemLanguage.Spanish: return "es";
            default: return "en";
        }
    }
}