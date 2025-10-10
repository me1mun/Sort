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

    [Header("Animation Settings")]
    private const float charsPerSecond = 60f;
    private const float blinkSpeed = 2.5f;

    private bool _isInteractable;
    private Animator _animator;
    private CanvasGroup _popupCanvasGroup;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _popupCanvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(int completedLevelNumber)
    {
        gameObject.SetActive(true);

        if (levelNumberText != null)
        {
            levelNumberText.text = completedLevelNumber.ToString();
        }

        if (victoryText != null)
        {
            victoryText.text = LocalizationManager.Instance.Get("ui.levelComplete");
        }
        
        // Делаем попап интерактивным сразу при появлении
        _popupCanvasGroup.interactable = true;
        _isInteractable = true;

        _animator.SetTrigger("Show");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Проверка не дает нажать на попап дважды
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
        StartCoroutine(BlinkContinueTextRoutine());
    }


    private IEnumerator BlinkContinueTextRoutine()
    {
        if (tapToContinueCanvasGroup == null) yield break;

        tapToContinueCanvasGroup.gameObject.SetActive(true);
        while (true)
        {
            tapToContinueCanvasGroup.alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            yield return null;
        }
    }
}