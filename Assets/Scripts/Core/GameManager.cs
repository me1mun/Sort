using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Initializing,
    InMenu,
    Playing,
    Paused
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

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

        ApplyAllSettings();
        
        SceneManager.LoadScene("Gameplay");
        CurrentState = GameState.Playing;
    }

    private void ApplyAllSettings()
    {
        var dataManager = DataManager.Instance;
        var audioManager = AudioManager.Instance;
        
        audioManager.SetMusicVolume(dataManager.Settings.musicVolume);
        audioManager.SetSoundVolume(dataManager.Settings.soundVolume);
        
        if (string.IsNullOrEmpty(dataManager.Settings.languageCode))
        {
            dataManager.Settings.languageCode = GetSystemLanguageCode();
            dataManager.SaveSettings();
        }
        
        LocalizationManager.Instance.LoadLocalization(dataManager.Settings.languageCode);
    }
    
    public void UpdateGameState(GameState newState)
    {
        CurrentState = newState;
    }

    private string GetSystemLanguageCode()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Ukrainian: return "uk";
            case SystemLanguage.Spanish: return "es";
            // Добавьте другие языки по необходимости
            default: return "en";
        }
    }
}