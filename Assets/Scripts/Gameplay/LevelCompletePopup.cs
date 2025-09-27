using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class LevelCompletePopup : MonoBehaviour, IPointerClickHandler
{
    [Header("Components")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private ParticleSystem confettiParticles;
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private CanvasGroup tapToContinueCanvasGroup;
    [SerializeField] private GameplayController gameplayController;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float blinkSpeed = 2.5f;
    
    private bool _isInteractable = false;

    public void Show(int completedLevelNumber)
    {
        levelCompleteText.text = completedLevelNumber.ToString();
        gameObject.SetActive(true);
        popupCanvasGroup.interactable = true;
        StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        popupCanvasGroup.alpha = 0f;
        _isInteractable = false;
        StopAllCoroutines();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        
        // Делаем попап неинтерактивным, но не скрываем его
        _isInteractable = false;
        popupCanvasGroup.interactable = false;

        // Останавливаем мигание текста
        StopCoroutine(BlinkContinueTextRoutine());
        tapToContinueCanvasGroup.gameObject.SetActive(false);
        
        // Запускаем переход на следующий уровень
        gameplayController.LoadNextLevel();
    }

    private IEnumerator ShowRoutine()
    {
        popupCanvasGroup.alpha = 0f;
        tapToContinueCanvasGroup.alpha = 0f;
        _isInteractable = false;

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            popupCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            yield return null;
        }
        popupCanvasGroup.alpha = 1f;

        if (confettiParticles != null)
        {
            confettiParticles.Play();
        }
        
        _isInteractable = true;
        StartCoroutine(BlinkContinueTextRoutine());
    }

    private IEnumerator BlinkContinueTextRoutine()
    {
        tapToContinueCanvasGroup.gameObject.SetActive(true);
        while (true)
        {
            tapToContinueCanvasGroup.alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            yield return null;
        }
    }
}