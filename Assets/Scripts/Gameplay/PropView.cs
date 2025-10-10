using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PropView : MonoBehaviour
{
    private const float SpawnDuration = 0.2f;
    private const float MoveDuration = 0.15f;
    private const float SelectDuration = 0.1f;
    private const float SelectedScaleMultiplier = 1.3f;
    private readonly AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private const float CollectAnticipationDuration = 0.12f;
    private const float CollectAnticipationDistance = 0.5f;
    private const float CollectAnticipationScale = 1.3f;
    private const float CollectFlightDuration = 0.4f;
    private const float CollectFinalScale = 0.1f;
    private const float CollectFlightArc = 2.0f;

    private const float HintInterval = 3f;
    private const float HintShakeDuration = 1.0f;
    private const float HintShakeMagnitude = 10f;
    private const float HintShakeFrequency = 15f;

    [Header("Component References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject spriteHolder;
    [SerializeField] private Transform visualToShake;
    
    public GroupData AssignedGroup { get; private set; }
    public ItemData AssignedItem { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public bool IsAnimating { get; private set; }
    
    private Vector3 _initialScale;
    private Coroutine _scaleCoroutine;
    private Coroutine _hintCoroutine;
    
    private const int FlyToUISortingOrder = 10;
    private static readonly int DefaultSortingOrder = 1;
    private static readonly int DraggedSortingOrder = 10;

    private void Awake()
    {
        _initialScale = spriteHolder.transform.localScale;
    }

    public void Initialize(GroupData group, ItemData item, Vector2Int gridPos)
    {
        AssignedGroup = group;
        AssignedItem = item;
        GridPosition = gridPos;
        spriteRenderer.sprite = item.icon;
        name = $"Prop_{gridPos.x}_{gridPos.y} ({group.groupKey})";
    }

    public void OnSpawn()
    {
        spriteHolder.transform.localScale = Vector3.zero;
        ResetSortingOrder();
        StartCoroutine(AnimateScale(_initialScale, SpawnDuration));
    }

    public void Select(bool isSelected)
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        
        Vector3 targetScale = isSelected ? _initialScale * SelectedScaleMultiplier : _initialScale;
        _scaleCoroutine = StartCoroutine(AnimateScale(targetScale, SelectDuration));
    }

    public Coroutine Collect(Vector3 targetWorldPos, Action onArrival)
    {
        StopHintAnimation();
        return StartCoroutine(AnimateFlyAndCollect(targetWorldPos, onArrival));
    }

    public IEnumerator AnimateMove(Vector3 targetPosition)
    {
        yield return Animate(
            MoveDuration, 
            t => transform.position = Vector3.Lerp(transform.position, targetPosition, t)
        );
        transform.position = targetPosition;
    }
    
    public void BringToFront()
    {
        spriteRenderer.sortingOrder = DraggedSortingOrder;
    }

    public void ResetSortingOrder()
    {
        spriteRenderer.sortingOrder = DefaultSortingOrder;
    }
    
    public void StartHintAnimation()
    {
        StopHintAnimation();
        _hintCoroutine = StartCoroutine(HintAnimationRoutine());
    }

    public void StopHintAnimation()
    {
        if (_hintCoroutine != null)
        {
            StopCoroutine(_hintCoroutine);
            _hintCoroutine = null;
        }
        if (visualToShake != null) visualToShake.localRotation = Quaternion.identity;
    }

    private IEnumerator HintAnimationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(HintInterval * 0.25f);
            yield return ShakeRoutine(HintShakeDuration, HintShakeMagnitude, HintShakeFrequency);
            yield return new WaitForSeconds(HintInterval * 0.75f);
        }
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude, float frequency)
    {
        if (visualToShake == null) yield break;
        Quaternion originalRotation = visualToShake.localRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float damper = Mathf.Sin(progress * Mathf.PI);
            float z = Mathf.Sin(elapsed * frequency) * magnitude * damper;
            visualToShake.localRotation = originalRotation * Quaternion.Euler(0, 0, z);
            yield return null;
        }
        visualToShake.localRotation = originalRotation;
    }

    private IEnumerator AnimateScale(Vector3 targetScale, float duration)
    {
        yield return Animate(
            duration, 
            t => spriteHolder.transform.localScale = Vector3.Lerp(spriteHolder.transform.localScale, targetScale, t)
        );
        spriteHolder.transform.localScale = targetScale;
    }

    private IEnumerator AnimateFlyAndCollect(Vector3 targetWorldPos, Action onArrival)
    {
        IsAnimating = true;
        spriteRenderer.sortingOrder = FlyToUISortingOrder;
        Vector3 startPos = transform.position;
        Vector3 startScale = spriteHolder.transform.localScale;
        Vector3 anticipationPos = startPos + (startPos - targetWorldPos).normalized * CollectAnticipationDistance;
        Vector3 anticipationScale = startScale * CollectAnticipationScale;
        
        yield return Animate(CollectAnticipationDuration, t =>
        {
            transform.position = Vector3.Lerp(startPos, anticipationPos, t);
            spriteHolder.transform.localScale = Vector3.Lerp(startScale, anticipationScale, t);
        });

        Vector3 controlPoint = (anticipationPos + targetWorldPos) / 2f + Vector3.up * CollectFlightArc;
        Vector3 finalScale = Vector3.one * CollectFinalScale;

        yield return Animate(CollectFlightDuration, t =>
        {
            transform.position = GetPointOnQuadraticBezier(anticipationPos, controlPoint, targetWorldPos, t);
            spriteHolder.transform.localScale = Vector3.Lerp(anticipationScale, finalScale, t);
        });
        
        onArrival?.Invoke();
        IsAnimating = false;
    }

    private IEnumerator Animate(float duration, Action<float> updateAction)
    {
        IsAnimating = true;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = _animationCurve.Evaluate(time / duration);
            updateAction(progress);
            yield return null;
        }
        updateAction(1);
        IsAnimating = false;
    }
    
    private static Vector3 GetPointOnQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0 + 2f * oneMinusT * t * p1 + t * t * p2;
    }
}