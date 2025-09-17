using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Group", menuName = "Game/Group Data")]
public class GroupData : ScriptableObject
{
    public string groupKey;
    public List<ItemData> items = new List<ItemData>(4);
}