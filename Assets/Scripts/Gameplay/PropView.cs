using System;
using System.Collections;
using UnityEngine;

public class PropView : MonoBehaviour
{
    [Header("Animation Settings")]
    private float _moveDuration = 0.1f;
    private float _scaleDuration = 0.15f;
    private float _selectedScaleMultiplier = 1.2f;
    private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Component References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject spriteHolder;
    [SerializeField] private int _flyToUISortingOrder = 10;
    
    public GroupData AssignedGroup { get; private set; }
    public ItemData AssignedItem { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public bool IsAnimating { get; private set; }
    
    private Vector3 _initialScale;

    private void Awake()
    {
        _initialScale = spriteHolder.transform.localScale;
    }

    // Вызывается, когда объект достают из пула
    public void OnSpawn()
    {
        spriteHolder.transform.localScale = Vector3.zero;
        spriteRenderer.sortingOrder = 1;
        StartCoroutine(AnimateScale(_initialScale));
    }

    public void Initialize(GroupData group, ItemData item, Vector2Int gridPos)
    {
        AssignedGroup = group;
        AssignedItem = item;
        spriteRenderer.sprite = item.icon;
        GridPosition = gridPos;
        name = $"Prop_{gridPos.x}_{gridPos.y} ({group.groupKey})";
    }

    public void Select(bool isSelected)
    {
        StopAllCoroutines(); // Прерываем другие анимации для мгновенной реакции
        StartCoroutine(AnimateScale(isSelected ? _initialScale * _selectedScaleMultiplier : _initialScale));
    }
    
    // Публичный метод для запуска анимации сбора
    public void Collect(Vector3 targetWorldPos, Action onArrival)
    {
        StartCoroutine(AnimateFlyAndCollect(targetWorldPos, onArrival));
    }

    public IEnumerator AnimateMove(Vector3 targetPosition)
    {
        IsAnimating = true;
        Vector3 startPosition = transform.position;
        float time = 0f;

        while (time < _moveDuration)
        {
            time += Time.deltaTime;
            float t = time / _moveDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, _moveCurve.Evaluate(t));
            yield return null;
        }

        transform.position = targetPosition;
        IsAnimating = false;
    }
    
    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        IsAnimating = true;
        Vector3 startScale = spriteHolder.transform.localScale;
        float time = 0f;

        while (time < _scaleDuration)
        {
            time += Time.deltaTime;
            spriteHolder.transform.localScale = Vector3.Lerp(startScale, targetScale, _moveCurve.Evaluate(time / _scaleDuration));
            yield return null;
        }

        spriteHolder.transform.localScale = targetScale;
        IsAnimating = false;
    }
    
    // Та самая анимация полета по дуге
    public IEnumerator AnimateFlyAndCollect(Vector3 targetWorldPos, Action onArrival)
    {
        IsAnimating = true;
        spriteRenderer.sortingOrder = _flyToUISortingOrder;
        Transform spriteTransform = spriteHolder.transform;
        Vector3 startPos = transform.position;
        Vector3 startScale = spriteTransform.localScale;
        
        // --- Фаза 1: Небольшой "отскок" назад и увеличение ---
        float reverseDuration = 0.12f;
        Vector3 reverseDirection = (startPos - targetWorldPos).normalized;
        Vector3 reversePos = startPos + reverseDirection * 0.5f;
        Vector3 reverseScale = startScale * 1.3f;

        for (float t = 0; t < reverseDuration; t += Time.deltaTime)
        {
            float progress = t / reverseDuration;
            transform.position = Vector3.Lerp(startPos, reversePos, progress);
            spriteTransform.localScale = Vector3.Lerp(startScale, reverseScale, progress);
            yield return null;
        }

        // --- Фаза 2: Полет по дуге и уменьшение ---
        float flightDuration = 0.4f;
        Vector3 finalScale = Vector3.one * 0.1f;
        
        Vector3 controlPoint = (reversePos + targetWorldPos) / 2f + new Vector3(0, 2f, 0); // Контрольная точка для кривой
        
        for (float t = 0; t < flightDuration; t += Time.deltaTime)
        {
            float progress = t / flightDuration;
            float oneMinusProgress = 1 - progress;
            // Кривая Безье для дуги
            transform.position = oneMinusProgress * oneMinusProgress * reversePos + 
                                 2 * oneMinusProgress * progress * controlPoint + 
                                 progress * progress * targetWorldPos;
                                 
            // Одновременное уменьшение размера
            spriteTransform.localScale = Vector3.Lerp(reverseScale, finalScale, progress);
            yield return null;
        }
        
        // --- Фаза 3: Прибытие ---
        onArrival?.Invoke();
        IsAnimating = false;
    }
}