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
    [SerializeField] private Camera mainCamera;

    [Header("UI")]
    [SerializeField] private LevelCompletePopup levelCompletePopup;
    [SerializeField] private GameObject hintButton;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private SettingsPopup settingsPopup;

    private ProgressData _currentProgress;
    private string _hintedGroupKey;

    private void Start()
    {
        DataManager.Instance.CachePredefinedLevelCount(levelManager.PredefinedLevelsCount);
        SubscribeToGridEvents();
        SubscribeToButtonEvents();
        WireUpDependencies();
        StartCoroutine(StartLevelRoutine());
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromGridEvents();
        UnsubscribeFromButtonEvents();
    }

    private void WireUpDependencies()
    {
        if (gridController != null && uiController != null)
        {
            gridController.GetIndicatorWorldPositionProvider = GetIndicatorWorldPosition;
        }
    }

    private void SubscribeToGridEvents()
    {
        if (gridController != null)
        {
             gridController.OnFinalVictory += OnFinalVictory;
             gridController.OnGroupCollected += HandleGroupCollected;
             gridController.OnPropArrivedAtIndicator += uiController.NotifyIndicatorPropArrival;
        }
    }

    private void UnsubscribeFromGridEvents()
    {
        if (gridController != null)
        {
             gridController.OnFinalVictory -= OnFinalVictory;
             gridController.OnGroupCollected -= HandleGroupCollected;
             gridController.OnPropArrivedAtIndicator -= uiController.NotifyIndicatorPropArrival;
        }
    }
    
    private void SubscribeToButtonEvents()
    {
        if (hintButton != null)
        {
            hintButton.GetComponent<UIButton>().OnClick.AddListener(OnHintButton);
        }
        if (settingsButton != null)
        {
            settingsButton.OnClick.AddListener(OpenSettings);
        }
    }

    private void UnsubscribeFromButtonEvents()
    {
        if (hintButton != null && hintButton.GetComponent<UIButton>() != null)
        {
            hintButton.GetComponent<UIButton>().OnClick.RemoveListener(OnHintButton);
        }
        if (settingsButton != null)
        {
            settingsButton.OnClick.RemoveListener(OpenSettings);
        }
    }
    
    public void OnHintButton()
    {
        if (!string.IsNullOrEmpty(_hintedGroupKey)) return;

        string foundHintKey = gridController.FindCompletableGroupKey();
        
        if (!string.IsNullOrEmpty(foundHintKey))
        {
            _hintedGroupKey = foundHintKey;
            gridController.AnimateHintForGroup(foundHintKey);
            SetHintButtonVisibility(true);
        }
    }
    
    private void OpenSettings()
    {
        if (settingsPopup != null)
        {
            settingsPopup.Open();
        }
    }
    
    public Vector3 GetIndicatorWorldPosition(int index)
    {
        if (uiController == null || mainCamera == null || gridController == null) return Vector3.zero;
        Vector3 indicatorScreenPos = uiController.GetIndicatorScreenPosition(index);
        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - gridController.transform.position.z);
        return mainCamera.ScreenToWorldPoint(new Vector3(indicatorScreenPos.x, indicatorScreenPos.y, cameraDistance));
    }

    private void HandleGroupCollected(string collectedGroupKey)
    {
        uiController.ShowCollectedGroupName(collectedGroupKey);
        
        if (!string.IsNullOrEmpty(_hintedGroupKey) && _hintedGroupKey == collectedGroupKey)
        {
            _hintedGroupKey = null;
            gridController.StopAllHintAnimations();
            SetHintButtonVisibility(false);
        }
    }

    private void SetHintButtonVisibility(bool hideButton)
    {
        if (hintButton != null)
        {
            hintButton.SetActive(!hideButton);
        }
    }

    private void OnFinalVictory()
    {
        DataManager.Instance.AdvanceToNextLevel();
        if (levelCompletePopup != null)
        {
            levelCompletePopup.Show(DataManager.Instance.Progress.DisplayLevel);
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
                activeLevelData = ScriptableObject.CreateInstance<LevelData>();
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
            gridController.Initialize(activeLevelData);
        }
        else
        {
            Debug.LogError($"Failed to load level data for level {levelToLoad}!");
        }
        yield return null;
    }
}