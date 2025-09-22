using System;
using System.Collections.Generic;
using UnityEngine;

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
    public bool isMusicOn;
    public bool isSfxOn;
    public string languageCode;

    public SettingsData()
    {
        isMusicOn = true;
        isSfxOn = true;
        languageCode = "en";
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