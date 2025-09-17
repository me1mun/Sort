using System.Collections;
using System.Linq; // <-- Добавьте эту строку для использования .Take()
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
        LevelData originalLevelData = levelManager.GetLevel(_currentLevelNumber);

        if (originalLevelData != null)
        {
            bool isTutorial = (_currentLevelNumber == 1);
            LevelData activeLevelData = originalLevelData;

            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
            // Если это туториал, мы создаем новый объект LevelData
            // который содержит только те группы, что реально используются в уровне.
            if (isTutorial)
            {
                // Создаем новый экземпляр данных уровня
                activeLevelData = new LevelData();
                // Копируем в него только первые 3 группы из оригинального уровня
                activeLevelData.requiredGroups = originalLevelData.requiredGroups.Take(3).ToList();
            }

            // Теперь оба контроллера получают одинаковые, корректные данные об уровне
            uiController.InitializeUIForLevel(activeLevelData);
            gridController.Initialize(activeLevelData, propPool, uiController, isTutorial);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {_currentLevelNumber}! Check LevelManager configuration.");
        }
    }

    private void HandleLevelCompleted()
    {
        Debug.Log($"LEVEL {_currentLevelNumber} COMPLETED!");
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