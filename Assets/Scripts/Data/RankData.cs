using UnityEngine;

[CreateAssetMenu(fileName = "New Rank", menuName = "Game/Rank Data")]
public class RankData : ScriptableObject
{
    [Header("Rank Identity")]
    [Tooltip("Unique identifier for localization key: 'ranks.{rankKey}'")]
    public string rankKey;
    
    [Header("Visual")]
    public Sprite icon;
    
    [Header("Progression")]
    [Tooltip("Number of levels required to complete this rank")]
    [Min(1)]
    public int levelsRequired = 10;
}