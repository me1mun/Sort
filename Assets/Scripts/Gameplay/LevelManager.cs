using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    [Header("Созданные вручную уровни")]
    [SerializeField] private List<LevelData> predefinedLevels;
    
    public int PredefinedLevelsCount => predefinedLevels.Count;
    
    private const int GROUPS_IN_RANDOM_LEVEL = 7;
    private List<GroupData> _loadedGroups;

    private void Awake()
    {
        LoadAllGroups();
    }
    
    private void LoadAllGroups()
    {
        var uniqueGroups = new HashSet<GroupData>();

        foreach (var level in predefinedLevels)
        {
            if (level == null) continue;
            foreach (var group in level.requiredGroups)
            {
                if (group != null)
                {
                    uniqueGroups.Add(group);
                }
            }
        }

        _loadedGroups = uniqueGroups.ToList();

        if (_loadedGroups.Count == 0)
        {
            Debug.LogError("Не найдено ни одной группы в списке predefinedLevels. Случайные уровни не могут быть сгенерированы.");
        }
        else
        {
            Debug.Log($"Загружено {_loadedGroups.Count} уникальных групп из готовых уровней.");
        }
    }

    public LevelData GetLevel(int levelNumber)
    {
        int predefinedIndex = levelNumber - 1;
        if (predefinedIndex >= 0 && predefinedIndex < predefinedLevels.Count)
        {
            return predefinedLevels[predefinedIndex];
        }
        
        return GenerateRandomLevel();
    }
    
    private LevelData GenerateRandomLevel()
    {
        if (_loadedGroups == null || _loadedGroups.Count == 0)
        {
            return null;
        }

        int groupsToSelect = Mathf.Min(GROUPS_IN_RANDOM_LEVEL, _loadedGroups.Count);

        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            var shuffledGroups = _loadedGroups.OrderBy(g => Random.value).ToList();
            var newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.requiredGroups = new List<GroupData>();
            
            var usedItems = new HashSet<ItemData>();

            foreach (var group in shuffledGroups)
            {
                if (HasItemConflict(group, usedItems)) continue;

                newLevel.requiredGroups.Add(group);
                foreach (var item in group.items)
                {
                    usedItems.Add(item);
                }
                
                if (newLevel.requiredGroups.Count == groupsToSelect)
                {
                    return newLevel;
                }
            }
        }
        
        Debug.LogError($"Не удалось сгенерировать уровень за {maxAttempts} попыток. Возможно, у вас слишком много пересекающихся предметов в группах.");
        return null;
    }
    
    private bool HasItemConflict(GroupData group, HashSet<ItemData> usedItems)
    {
        foreach (var item in group.items)
        {
            if (item != null && usedItems.Contains(item))
            {
                return true;
            }
        }
        return false;
    }
}