using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvailableLanguages", menuName = "Game/Available Languages")]
public class AvailableLanguages : ScriptableObject
{
    public List<LanguageData> languages;
}