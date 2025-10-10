using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private UIController uiController;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Camera mainCamera;

    [Header("UI")]
    [SerializeField] private LevelCompletePopup levelCompletePopup;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private UIButton hintUIButton;

    private LevelService _levelService;
    private HintService _hintService;

    private void Awake()
    {
        _levelService = new LevelService(levelManager, DataManager.Instance);
        _hintService = new HintService(gridController, hintUIButton.gameObject);
    }

    private void Start()
    {
        SubscribeToEvents();
        WireUpDependencies();
        StartCoroutine(StartLevelRoutine());
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void WireUpDependencies()
    {
        gridController.GetIndicatorWorldPositionProvider = GetIndicatorWorldPosition;
    }

    private void SubscribeToEvents()
    {
        gridController.OnFinalVictory += OnFinalVictory;
        gridController.OnGroupCollected += HandleGroupCollected;
        gridController.OnPropArrivedAtIndicator += uiController.NotifyIndicatorPropArrival;

        settingsButton.OnClick.AddListener(OpenSettings);
        hintUIButton.OnClick.AddListener(_hintService.OnHintButton);
    }

    private void UnsubscribeFromEvents()
    {
        gridController.OnFinalVictory -= OnFinalVictory;
        gridController.OnGroupCollected -= HandleGroupCollected;
        gridController.OnPropArrivedAtIndicator -= uiController.NotifyIndicatorPropArrival;

        if (settingsButton != null)
        {
            settingsButton.OnClick.RemoveListener(OpenSettings);
        }
        if (hintUIButton != null)
        {
            hintUIButton.OnClick.RemoveListener(_hintService.OnHintButton);
        }
    }

    private void OpenSettings()
    {
        settingsPopup.Open();
    }

    public Vector3 GetIndicatorWorldPosition(int index)
    {
        Vector3 indicatorScreenPos = uiController.GetIndicatorScreenPosition(index);
        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z - gridController.transform.position.z);
        return mainCamera.ScreenToWorldPoint(new Vector3(indicatorScreenPos.x, indicatorScreenPos.y, cameraDistance));
    }

    private void HandleGroupCollected(string collectedGroupKey)
    {
        uiController.ShowCollectedGroupName(collectedGroupKey);
        _hintService.OnGroupCollected(collectedGroupKey);
    }

    private void OnFinalVictory()
    {
        DataManager.Instance.AdvanceToNextLevel(levelManager.PredefinedLevelsCount);
        levelCompletePopup.Show(DataManager.Instance.Progress.DisplayLevel);
    }

    public void LoadNextLevel()
    {
        ScreenFader.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator StartLevelRoutine()
    {
        yield return null;

        LevelData levelData = _levelService.GetCurrentLevel();
        if (levelData != null)
        {
            uiController.UpdateLevelText(DataManager.Instance.Progress.DisplayLevel);
            uiController.InitializeUIForLevel(levelData);
            gridController.Initialize(levelData);
        }
    }
}