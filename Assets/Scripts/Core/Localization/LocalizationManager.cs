using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public event Action OnLanguageChanged;
    public string CurrentLanguage { get; private set; }

    [Tooltip("Ссылка на ассет конфигурации языков.")]
    [SerializeField] private LanguageConfig _config;
    
    private Dictionary<string, string> _translations;
    private Dictionary<string, string> _fallbackTranslations;
    
    private const string FallbackLanguageCode = "en";

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
        LoadFallbackLanguage();
        
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

        if (_translations.TryGetValue(key, out string value))
        {
            return value;
        }
        
        if (_fallbackTranslations != null && _fallbackTranslations.TryGetValue(key, out string fallbackValue))
        {
            Debug.LogWarning($"Ключ '{key}' не найден в текущем языке '{CurrentLanguage}'. Используется фоллбэк с языка '{"en"}'!");
            return fallbackValue;
        }
        
        return $"[{key}]";
    }

    private void LoadAndApplyLanguage(string languageCode)
    {
        var languageDef = _config.Languages.FirstOrDefault(lang => lang.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        if (languageDef == null || languageDef.TranslationFile == null)
        {
            Debug.LogError($"Конфигурация для языка '{languageCode}' не найдена или файл перевода не назначен!");
            if (_translations == null)
            {
                _translations = new Dictionary<string, string>();
            }
            return;
        }

        _translations = LoadTranslationsFromFile(languageDef.TranslationFile);
        
        CurrentLanguage = languageCode;
        OnLanguageChanged?.Invoke();
        Debug.Log($"Язык изменен на: {CurrentLanguage}");
    }

    private void LoadFallbackLanguage()
    {
        string fallbackCode = FallbackLanguageCode; 

        if (!_config.IsLanguageSupported(fallbackCode))
        {
            Debug.LogError($"Конфигурация не поддерживает обязательный фоллбэк-язык '{fallbackCode}'. Фоллбэк будет недоступен.");
            _fallbackTranslations = new Dictionary<string, string>();
            return;
        }

        var languageDef = _config.Languages.FirstOrDefault(lang => lang.LanguageCode.Equals(fallbackCode, StringComparison.OrdinalIgnoreCase));
        
        if (languageDef == null || languageDef.TranslationFile == null)
        {
             Debug.LogError($"Конфигурация для фоллбэк-языка '{fallbackCode}' найдена, но файл перевода не назначен!");
             _fallbackTranslations = new Dictionary<string, string>();
             return;
        }
        
        _fallbackTranslations = LoadTranslationsFromFile(languageDef.TranslationFile);
        Debug.Log($"Фоллбэк-язык ({fallbackCode}) загружен.");
    }
    
    private Dictionary<string, string> LoadTranslationsFromFile(TextAsset file)
    {
        string jsonText = file.text;
        var newTranslations = new Dictionary<string, string>();
        
        try
        {
            LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonText);
            foreach (var item in data.items)
            {
                newTranslations[item.key] = item.value; 
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при парсинге файла перевода '{file.name}': {e.Message}");
        }
        
        return newTranslations;
    }
}

[Serializable] public class LocalizationData { public LocalizationItem[] items; }
[Serializable] public class LocalizationItem { public string key; public string value; }