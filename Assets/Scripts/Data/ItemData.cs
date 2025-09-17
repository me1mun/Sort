using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemKey;
    public Sprite icon;
}