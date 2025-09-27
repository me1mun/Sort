using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayController : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private GridController gridController;
    [SerializeField] private UIController uiController;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private PropPool propPool;

    [Header("UI")]
    [SerializeField] private LevelCompletePopup levelCompletePopup;

    private ProgressData _currentProgress;

    private void Start()
    {
        DataManager.Instance.CachePredefinedLevelCount(levelManager.PredefinedLevelsCount);
        gridController.OnFinalVictory += OnFinalVictory;
        StartCoroutine(StartLevelRoutine());
    }
    
    private void OnDestroy()
    {
        if (gridController != null)
        {
             gridController.OnFinalVictory -= OnFinalVictory;
        }
    }

    private void OnFinalVictory()
    {
        DataManager.Instance.AdvanceToNextLevel();
        if (levelCompletePopup != null)
        {
            levelCompletePopup.Show(_currentProgress.DisplayLevel);
        }
    }
    
    public void LoadNextLevel()
    {
        ScreenFader.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }
    
    private IEnumerator StartLevelRoutine()
    {
        _currentProgress = DataManager.Instance.Progress;
        int predefinedLevelsCount = levelManager.PredefinedLevelsCount;
        int levelToLoad = (_currentProgress.predefinedLevelIndex < predefinedLevelsCount)
            ? _currentProgress.predefinedLevelIndex + 1
            : predefinedLevelsCount + 1;

        LevelData originalLevelData = levelManager.GetLevel(levelToLoad);
        if (originalLevelData != null)
        {
            LevelData activeLevelData = originalLevelData;
            bool isTutorial = (_currentProgress.predefinedLevelIndex == 0);

            if (isTutorial)
            {
                activeLevelData = new LevelData();
                activeLevelData.requiredGroups = originalLevelData.requiredGroups
                    .Take(3)
                    .Select(g => 
                    {
                        var newGroup = ScriptableObject.CreateInstance<GroupData>();
                        newGroup.groupKey = g.groupKey;
                        newGroup.items = g.items.Take(3).ToList();
                        return newGroup;
                    })
                    .ToList();
            }
            
            uiController.UpdateLevelText(_currentProgress.DisplayLevel);
            uiController.InitializeUIForLevel(activeLevelData);
            gridController.Initialize(activeLevelData, propPool, uiController);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {levelToLoad}!");
        }
        yield return null;
    }
}