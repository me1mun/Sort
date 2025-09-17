using System.Collections.Generic;
using UnityEngine;

// Enum для удобного выбора словаря
public enum TranslationGroup
{
    UI,
    Groups,
    Items // Добавляем новую группу для предметов
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, string> _uiTerms;
    private Dictionary<string, string> _groupNames;
    private Dictionary<string, string> _itemNames; // Словарь для имен предметов

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // По умолчанию английский
        LoadLocalization("en"); 
    }

    // Загружает файлы локализации для указанного языка
    public void LoadLocalization(string langCode)
    {
        _uiTerms = LoadDictionary(langCode, "ui");
        _groupNames = LoadDictionary(langCode, "groups");
        _itemNames = LoadDictionary(langCode, "items"); // Загружаем новый словарь
        Debug.Log($"Localization loaded for language: {langCode}");
    }

    // Основной метод для получения перевода
    public string GetTranslation(string key, TranslationGroup group)
    {
        Dictionary<string, string> dictionary;
        switch (group)
        {
            case TranslationGroup.Groups:
                dictionary = _groupNames;
                break;
            case TranslationGroup.Items:
                dictionary = _itemNames;
                break;
            default: // TranslationGroup.UI
                dictionary = _uiTerms;
                break;
        }

        if (key != null && dictionary.TryGetValue(key, out string value))
        {
            return value;
        }
        Debug.LogWarning($"Translation key not found: '{key}' in dictionary '{group}'");
        return key; // Возвращаем ключ, если перевод не найден
    }

    // Вспомогательный метод для загрузки и парсинга JSON
    private Dictionary<string, string> LoadDictionary(string langCode, string dictionaryName)
    {
        var newDictionary = new Dictionary<string, string>();
        // Ищем файл в папке Resources/Localization/
        var jsonFile = Resources.Load<TextAsset>($"Localization/{langCode}_{dictionaryName}");

        if (jsonFile != null)
        {
            LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonFile.text);
            foreach (var item in data.items)
            {
                newDictionary[item.key] = item.value;
            }
        }
        else
        {
            Debug.LogWarning($"Localization file not found: Resources/Localization/{langCode}_{dictionaryName}.json");
        }
        return newDictionary;
    }
}