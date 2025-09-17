using System.Collections;
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

    private int _currentLevelNumber;

    private void Start()
    {
        gridController.OnLevelCompleted += HandleLevelCompleted;
        StartCoroutine(StartLevelRoutine());
    }

    private IEnumerator StartLevelRoutine()
    {
        levelCompletePopup.Hide();
        yield return StartCoroutine(screenFader.FadeIn());

        _currentLevelNumber = DataManager.Instance.Progress.currentLevel;
        LevelData levelData = levelManager.GetLevel(_currentLevelNumber);

        if (levelData != null)
        {
            uiController.InitializeUIForLevel(levelData);
            gridController.Initialize(levelData, propPool, uiController);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {_currentLevelNumber}! Check LevelManager configuration.");
        }
    }

    private void HandleLevelCompleted()
    {
        Debug.Log($"LEVEL {_currentLevelNumber} COMPLETED!");
        
        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        // Просто сообщаем DataManager, что нужно перейти на следующий уровень.
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