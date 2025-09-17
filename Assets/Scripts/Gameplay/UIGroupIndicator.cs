using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIGroupIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite filledSprite;
    
    [Header("Animation Settings")]
    private float baseBounceScale = 1.15f;
    private float bounceIncrement = 0.05f;
    
    [Header("Audio Settings")]
    private string collectSoundName = "Collect";
    private float baseCollectPitch = 0.8f;
    private float pitchStep = 0.1f;
    
    private Vector3 _originalScale;
    private Coroutine _activeBounceCoroutine;
    private int _arrivalCount = 0;

    private void Awake()
    {
        _originalScale = transform.localScale;
        SetEmpty();
    }

    // Этот метод теперь вызывается для КАЖДОГО прилетевшего предмета
    public void SetFilled()
    {
        // Меняем спрайт на "заполненный", если он еще не такой.
        if (iconImage.sprite != filledSprite)
        {
            iconImage.sprite = filledSprite;
        }
        
        // Увеличиваем счетчик прилетов для этой группы
        _arrivalCount++;

        // --- НОВАЯ ЛОГИКА ЗВУКА И АНИМАЦИИ ---
        // 1. Рассчитываем высоту тона. Каждый следующий предмет звучит выше.
        float currentPitch = baseCollectPitch + ((_arrivalCount - 1) * pitchStep);
        
        // 2. Проигрываем звук с рассчитанной высотой тона.
        AudioManager.Instance.PlayWithPitch(collectSoundName, currentPitch);

        // 3. Рассчитываем силу "баунса".
        float currentBounceMultiplier = baseBounceScale + ((_arrivalCount - 1) * bounceIncrement);
        Vector3 peakScale = _originalScale * currentBounceMultiplier;
        
        // 4. Запускаем анимацию "баунса" с новой силой.
        if (_activeBounceCoroutine != null)
        {
            StopCoroutine(_activeBounceCoroutine);
        }
        _activeBounceCoroutine = StartCoroutine(AnimateBounceCoroutine(peakScale));
    }

    // Сбрасывает индикатор в начальное состояние для нового уровня
    public void SetEmpty()
    {
        iconImage.sprite = emptySprite;
        transform.localScale = _originalScale;
        _arrivalCount = 0; // Сбрасываем счетчик
    }

    private IEnumerator AnimateBounceCoroutine(Vector3 peakScale)
    {
        float duration = 0.2f;
        float halfDuration = duration / 2f;
        float timer = 0f;
        Vector3 currentScale = transform.localScale;

        // Фаза 1: Увеличение
        while (timer < halfDuration)
        {
            transform.localScale = Vector3.Lerp(currentScale, peakScale, timer / halfDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Фаза 2: Возвращение
        timer = 0f;
        while (timer < halfDuration)
        {
            transform.localScale = Vector3.Lerp(peakScale, _originalScale, timer / halfDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = _originalScale;
        _activeBounceCoroutine = null;
    }
}