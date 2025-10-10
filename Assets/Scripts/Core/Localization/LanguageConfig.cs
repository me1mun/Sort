using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LanguageConfig", menuName = "Localization/Language Config")]
public class LanguageConfig : ScriptableObject
{
    [Tooltip("Код языка, который будет использоваться, если системный язык не поддерживается.")]
    [SerializeField] private string _defaultLanguageCode = "en";
    
    [Tooltip("Список всех поддерживаемых языков в игре.")]
    [SerializeField] private List<LanguageDefinition> _languages;

    public string DefaultLanguageCode => _defaultLanguageCode;
    public IReadOnlyList<LanguageDefinition> Languages => _languages;

    public bool IsLanguageSupported(string languageCode)
    {
        return _languages.Any(lang => lang.LanguageCode == languageCode);
    }

    public string GetSystemLanguageCode()
    {
        var systemLanguage = Application.systemLanguage;
        var definition = _languages.FirstOrDefault(lang => lang.SystemLanguage == systemLanguage);
        return definition?.LanguageCode;
    }
}

[Serializable]
public class LanguageDefinition
{
    public string LanguageName;
    public string LanguageCode;
    public SystemLanguage SystemLanguage;
    
    [Tooltip("Перетащите сюда JSON-файл с переводом для этого языка.")]
    public TextAsset TranslationFile; // <- ВОТ ИЗМЕНЕНИЕ
}