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
    // Удаляем ссылку на ScreenFader отсюда, он теперь глобальный
    // [SerializeField] private ScreenFader screenFader; 
    
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
        // Мы больше не вызываем FadeIn отсюда, 
        // так как он стал частью общего процесса загрузки сцены
        
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
            
            uiController.UpdateLevelText(_currentProgress.DisplayLevel);
            
            uiController.InitializeUIForLevel(activeLevelData);
            gridController.Initialize(activeLevelData, propPool, uiController, isTutorial);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {levelToLoad}! Check LevelManager configuration.");
        }

        // Возвращаем yield return null, так как анимация FadeIn теперь не здесь
        yield return null;
    }

    private void HandleLevelCompleted()
    {
        DataManager.Instance.AdvanceToNextLevel();
        levelCompletePopup.Show();
    }
    
    // Метод стал предельно простым
    public void LoadNextLevel()
    {
        // Просто просим ScreenFader загрузить текущую сцену заново
        ScreenFader.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    // Эта корутина больше не нужна
    // private IEnumerator LoadSceneRoutine() { ... }
}