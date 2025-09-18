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

    [Header("UI Components")]
    [SerializeField] private LevelCompletePopup levelCompletePopup;
    [SerializeField] private ScreenFader screenFader;
    
    private ProgressData _currentProgress;

    private void Start()
    {
        DataManager.Instance.CachePredefinedLevelCount(levelManager.PredefinedLevelsCount);
        
        gridController.OnLevelCompleted += HandleLevelCompleted;
        StartCoroutine(StartLevelRoutine());
    }

    private IEnumerator StartLevelRoutine()
    {
        levelCompletePopup.Hide();
        yield return StartCoroutine(screenFader.FadeIn());

        _currentProgress = DataManager.Instance.Progress;
        int predefinedLevelsCount = levelManager.PredefinedLevelsCount;
        
        int levelToLoad;
        if (_currentProgress.predefinedLevelIndex < predefinedLevelsCount)
        {
            levelToLoad = _currentProgress.predefinedLevelIndex + 1;
        }
        else
        {
            levelToLoad = predefinedLevelsCount + 1;
        }

        LevelData originalLevelData = levelManager.GetLevel(levelToLoad);

        if (originalLevelData != null)
        {
            bool isTutorial = (_currentProgress.predefinedLevelIndex == 0);
            LevelData activeLevelData = originalLevelData;

            if (isTutorial)
            {
                activeLevelData = new LevelData();
                activeLevelData.requiredGroups = originalLevelData.requiredGroups.Take(3).ToList();
            }
            
            uiController.InitializeUIForLevel(activeLevelData);
            gridController.Initialize(activeLevelData, propPool, uiController, isTutorial);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {levelToLoad}! Check LevelManager configuration.");
        }
    }

    private void HandleLevelCompleted()
    {
        DataManager.Instance.AdvanceToNextLevel();
        levelCompletePopup.Show();
    }
    
    public void LoadNextLevel()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        yield return StartCoroutine(screenFader.FadeOut());
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}