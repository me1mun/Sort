using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public event Action OnLanguageChanged;
    public string CurrentLanguage { get; private set; }

    [Tooltip("Ссылка на ассет конфигурации языков.")]
    [SerializeField] private LanguageConfig _config;
    
    private Dictionary<string, string> _translations;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    private void Initialize()
    {
        string systemLangCode = _config.GetSystemLanguageCode();
        string initialLang = _config.DefaultLanguageCode;

        if (!string.IsNullOrEmpty(systemLangCode) && _config.IsLanguageSupported(systemLangCode))
        {
            initialLang = systemLangCode;
        }
        
        LoadAndApplyLanguage(initialLang);
    }

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !_config.IsLanguageSupported(languageCode) || languageCode == CurrentLanguage)
        {
            return;
        }
        LoadAndApplyLanguage(languageCode);
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key) || _translations == null)
        {
            return $"[{key}]";
        }
        return _translations.TryGetValue(key, out string value) ? value : $"[{key}]";
    }

    private void LoadAndApplyLanguage(string languageCode)
    {
        var languageDef = _config.Languages.FirstOrDefault(lang => lang.LanguageCode == languageCode);

        if (languageDef == null || languageDef.TranslationFile == null)
        {
            Debug.LogError($"Конфигурация для языка '{languageCode}' не найдена или файл перевода не назначен!");
            _translations = new Dictionary<string, string>();
            return;
        }

        string jsonText = languageDef.TranslationFile.text;
        
        var newTranslations = new Dictionary<string, string>();
        LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonText);
        foreach (var item in data.items)
        {
            newTranslations[item.key] = item.value;
        }
        _translations = newTranslations;
        
        CurrentLanguage = languageCode;
        // PlayerPrefs.SetString("language_code", CurrentLanguage); // <- СТРОКА УДАЛЕНА
        OnLanguageChanged?.Invoke();
        Debug.Log($"Язык изменен на: {CurrentLanguage}");
    }
}

// Вспомогательные классы для парсинга JSON (остаются без изменений)
[Serializable] public class LocalizationData { public LocalizationItem[] items; }
[Serializable] public class LocalizationItem { public string key; public string value; }