using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CanvasGroup))]
public class LevelCompletePopup : MonoBehaviour, IPointerClickHandler
{
    [Header("Components")]
    [SerializeField] private GameplayController gameplayController;
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI victoryText;
    [SerializeField] private CanvasGroup tapToContinueCanvasGroup;
    
    [Header("Rank Progress")]
    [SerializeField] private RankProgressUI rankProgressUI;

    [Header("Animation Settings")]
    private const float CharsPerSecond = 60f;
    private const float BlinkSpeed = 2.5f;
    private const float RankProgressDelay = 0.0f;

    private bool _isInteractable;
    private Animator _animator;
    private CanvasGroup _popupCanvasGroup;
    private Coroutine _showSequenceCoroutine;
    
    private int _completedLevelNumber;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _popupCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(int completedLevelNumber)
    {
        _completedLevelNumber = completedLevelNumber;
        gameObject.SetActive(true);

        if (levelNumberText != null)
        {
            levelNumberText.text = completedLevelNumber.ToString();
        }

        if (victoryText != null)
        {
            victoryText.text = LocalizationManager.Instance.Get("ui.levelComplete");
        }
        
        _popupCanvasGroup.interactable = false;
        _isInteractable = false;

        _animator.SetTrigger("Show");
        
        if (_showSequenceCoroutine != null)
        {
            StopCoroutine(_showSequenceCoroutine);
        }
        _showSequenceCoroutine = StartCoroutine(ShowSequence());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractable) return;

        _isInteractable = false;
        _popupCanvasGroup.interactable = false;

        StopAllCoroutines();
        if (tapToContinueCanvasGroup != null)
        {
            tapToContinueCanvasGroup.gameObject.SetActive(false);
        }

        if (gameplayController != null)
        {
            gameplayController.LoadNextLevel();
        }
        else
        {
            Debug.LogError("GameplayController is not assigned on LevelCompletePopup!");
        }
    }

    public void OnShowAnimationComplete()
    {
        // This is called by animation event
    }

    private IEnumerator ShowSequence()
    {
        yield return new WaitForSeconds(RankProgressDelay);
        
        yield return AnimateRankProgress();
        
        _popupCanvasGroup.interactable = true;
        _isInteractable = true;
        
        StartCoroutine(BlinkContinueTextRoutine());
    }

    private IEnumerator AnimateRankProgress()
    {
        if (rankProgressUI == null || RankManager.Instance == null)
        {
            yield break;
        }

        var rankManager = RankManager.Instance;
        var config = rankManager.Config;
        if (config == null) yield break;

        // Состояние *до* прохождения этого уровня
        int completedLevelsBefore = _completedLevelNumber - 1;
        if (completedLevelsBefore < 0) completedLevelsBefore = 0;
        
        (int rankIndexBefore, float progressBefore) = config.CalculateRank(completedLevelsBefore);
        
        // Состояние *после* прохождения этого уровня
        int completedLevelsAfter = _completedLevelNumber;
        (int rankIndexAfter, float progressAfter) = config.CalculateRank(completedLevelsAfter);

        bool willRankUp = (rankIndexBefore != rankIndexAfter);
        
        int progressToAnimateFrom = (int)(progressBefore * 100f);
        int progressToAnimateTo = willRankUp ? 100 : (int)(progressAfter * 100f);
        
        rankProgressUI.Initialize(progressToAnimateFrom, rankIndexBefore);
        
        yield return rankProgressUI.AnimateProgress(progressToAnimateTo, willRankUp, rankIndexAfter);
    }

    private IEnumerator BlinkContinueTextRoutine()
    {
        if (tapToContinueCanvasGroup == null) yield break;

        tapToContinueCanvasGroup.gameObject.SetActive(true);
        while (true)
        {
            tapToContinueCanvasGroup.alpha = (Mathf.Sin(Time.time * BlinkSpeed) + 1f) / 2f;
            yield return null;
        }
    }
}