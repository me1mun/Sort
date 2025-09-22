using System;
using System.Collections.Generic;
using UnityEngine;

public enum TranslationGroup { UI, Groups, Items }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public static event Action OnLanguageChanged;

    private Dictionary<string, string> _uiTerms;
    private Dictionary<string, string> _groupNames;
    private Dictionary<string, string> _itemNames;

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

    public void LoadLocalization(string langCode)
    {
        _uiTerms = LoadDictionary(langCode, "ui");
        _groupNames = LoadDictionary(langCode, "groups");
        _itemNames = LoadDictionary(langCode, "items");
        
        OnLanguageChanged?.Invoke();
        Debug.Log($"Localization loaded for language: {langCode}");
    }

    public string GetTranslation(string key, TranslationGroup group)
    {
        Dictionary<string, string> dictionary;
        switch (group)
        {
            case TranslationGroup.Groups: dictionary = _groupNames; break;
            case TranslationGroup.Items: dictionary = _itemNames; break;
            default: dictionary = _uiTerms; break;
        }

        if (key != null && dictionary.TryGetValue(key, out string value))
        {
            return value;
        }
        return key;
    }

    private Dictionary<string, string> LoadDictionary(string langCode, string dictionaryName)
    {
        var newDictionary = new Dictionary<string, string>();
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