using System.Linq;
using UnityEngine;

public class LevelService
{
    private readonly LevelManager _levelManager;
    private readonly DataManager _dataManager;

    public LevelService(LevelManager levelManager, DataManager dataManager)
    {
        _levelManager = levelManager;
        _dataManager = dataManager;
    }

    public LevelData GetCurrentLevel()
    {
        int levelToLoad = GetLevelToLoad();
        LevelData originalLevelData = _levelManager.GetLevel(levelToLoad);

        if (originalLevelData == null)
        {
            Debug.LogError($"Failed to load level data for level {levelToLoad}!");
            return null;
        }

        bool isTutorial = (_dataManager.Progress.predefinedLevelIndex == 0);
        if (isTutorial)
        {
            return CreateTutorialLevel(originalLevelData);
        }

        return originalLevelData;
    }

    private int GetLevelToLoad()
    {
        int predefinedLevelsCount = _levelManager.PredefinedLevelsCount;
        return (_dataManager.Progress.predefinedLevelIndex < predefinedLevelsCount)
            ? _dataManager.Progress.predefinedLevelIndex + 1
            : predefinedLevelsCount + 1;
    }

    private LevelData CreateTutorialLevel(LevelData originalLevelData)
    {
        LevelData tutorialLevel = ScriptableObject.CreateInstance<LevelData>();
        tutorialLevel.requiredGroups = originalLevelData.requiredGroups
            .Take(3)
            .Select(g =>
            {
                var newGroup = ScriptableObject.CreateInstance<GroupData>();
                newGroup.groupKey = g.groupKey;
                newGroup.items = g.items.Take(3).ToList();
                return newGroup;
            })
            .ToList();
        return tutorialLevel;
    }
}