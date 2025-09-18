using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    public List<GroupData> requiredGroups;
}

[Serializable]
public class ProgressData
{
    public int predefinedLevelIndex;
    public int randomLevelCount;

    public ProgressData()
    {
        predefinedLevelIndex = 0;
        randomLevelCount = 0;
    }
    
    public int DisplayLevel => predefinedLevelIndex + randomLevelCount + 1;
}

[Serializable]
public class SettingsData
{
    public float musicVolume;
    public float soundVolume;
    public string languageCode;

    public SettingsData()
    {
        musicVolume = 0.75f;
        soundVolume = 0.75f;
        languageCode = null;
    }
}

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