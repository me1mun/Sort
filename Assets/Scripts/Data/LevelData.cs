using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData_", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public List<GroupData> requiredGroups = new List<GroupData>();
}