using UnityEngine;

[CreateAssetMenu(fileName = "Language", menuName = "Game/Language Data")]
public class LanguageData : ScriptableObject
{
    public string languageName;
    public string languageCode;
    public Sprite flagIcon;
}