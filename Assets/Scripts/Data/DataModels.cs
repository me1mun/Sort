using System;
using System.Collections.Generic;
using UnityEngine;

// Уровень: теперь это просто список групп, необходимых для прохождения
[Serializable]
public class LevelData
{
    public List<GroupData> requiredGroups;
}

[Serializable]
public class ProgressData
{
    public int currentLevel;

    public ProgressData()
    {
        currentLevel = 1;
    }
}

// Настройки игры
[Serializable]
public class SettingsData
{
    public float musicVolume;
    public float soundVolume;
    public string languageCode; // "en", "uk", etc.

    public SettingsData()
    {
        musicVolume = 0.75f;
        soundVolume = 0.75f;
        languageCode = null; // Будет определяться автоматически
    }
}

// Вспомогательный класс для парсинга JSON файлов локализации
[Serializable]
public class LocalizationData
{
    public List<LocalizationItem> items;
}

[Serializable]
public class LocalizationItem
{
    public string key;
    public string value;
}