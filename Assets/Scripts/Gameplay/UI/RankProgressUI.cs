using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RankProgressUI : MonoBehaviour
{
    private const float FillDuration = 1.1f;
    private const float AnimationConst = 0.4f;
    private const float TextFadeOutDelay = 1.0f;

    private AnimationCurve FillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField] private AnimationCurve RankUpScaleDownCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f), 
        new Keyframe(0.33f, 1.25f, 5f, 0f),
        new Keyframe(1f, 0f, 0f, 0f)
    );
    
    [SerializeField] private AnimationCurve RankUpScaleUpCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.66f, 1.25f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );
    
    private static readonly AnimationCurve RankNameFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    [SerializeField] private Image rankIcon;
    [SerializeField] private TextMeshProUGUI rankNameText;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private TextMeshProUGUI progressText;

    private CanvasGroup _rankNameCanvasGroup;
    private CanvasGroup _progressTextCanvasGroup;
    
    private int _previousProgress;
    private int _currentRankIndex;
    private Vector3 _originalIconScale;
    
    private Material _rankIconMaterial;
    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    
    private void Awake()
    {
        if (iconContainer != null)
        {
            _originalIconScale = iconContainer.localScale;
        }

        if (rankIcon == null)
        {
            Debug.LogError($"{nameof(RankProgressUI)}: {nameof(rankIcon)} не назначен!");
            return;
        }

        _rankIconMaterial = Instantiate(rankIcon.material);
        rankIcon.material = _rankIconMaterial;

        _rankNameCanvasGroup = rankNameText.GetComponent<CanvasGroup>();
        if (_rankNameCanvasGroup == null)
        {
            _rankNameCanvasGroup = rankNameText.gameObject.AddComponent<CanvasGroup>();
        }

        _progressTextCanvasGroup = progressText.GetComponent<CanvasGroup>();
        if (_progressTextCanvasGroup == null)
        {
            _progressTextCanvasGroup = progressText.gameObject.AddComponent<CanvasGroup>();
        }
        _progressTextCanvasGroup.alpha = 0f;
    }
    
    public void Initialize(int previousProgress, int currentRankIndex)
    {
        _previousProgress = previousProgress;
        _currentRankIndex = currentRankIndex;
        
        UpdateRankVisuals(currentRankIndex);
        SetFillImmediate(_previousProgress / 100f);
        SetRankName(currentRankIndex);
        SetProgressText(_previousProgress);
        
        _rankNameCanvasGroup.alpha = 1f;
    }
    
    public IEnumerator AnimateProgress(int targetProgress, bool willRankUp, int newRankIndex)
    {
        float startFill = _previousProgress / 100f;
        float targetFill = willRankUp ? 1f : targetProgress / 100f;
        
        yield return AnimateFillAndText(startFill, targetFill, targetProgress);
        
        _previousProgress = targetProgress;

        if (willRankUp)
        {
            yield return AnimateRankUp(newRankIndex);
        }

        yield return new WaitForSeconds(TextFadeOutDelay);
        yield return AnimateProgressTextFade(1f, 0f, AnimationConst);
    }
    
    private void UpdateRankVisuals(int rankIndex)
    {
        if (RankManager.Instance == null)
        {
            Debug.LogError($"{nameof(RankProgressUI)}: {nameof(RankManager.Instance)} is null");
            return;
        }
        
        var rank = RankManager.Instance.GetRank(rankIndex);
        if (rank == null)
        {
            Debug.LogError($"{nameof(RankProgressUI)}: Invalid rank index {rankIndex}");
            return;
        }
        
        if (rankIcon != null)
        {
            rankIcon.sprite = rank.icon;
        }
    }
    
    private void SetRankName(int rankIndex)
    {
        if (rankNameText != null && RankManager.Instance != null)
        {
            rankNameText.text = RankManager.Instance.GetRankName(rankIndex);
        }
    }

    private void SetProgressText(int progress)
    {
        if (progressText != null)
        {
            progressText.text = $"{progress}%";
            _progressTextCanvasGroup.alpha = 1f;
        }
    }

    private void SetFillImmediate(float fillAmount)
    {
        if (_rankIconMaterial != null)
        {
            _rankIconMaterial.SetFloat(ProgressProperty, Mathf.Clamp01(fillAmount));
        }
    }
    
    private IEnumerator AnimateFillAndText(float startFill, float targetFill, int targetProgressInt)
    {
        float elapsed = 0f;
        int startProgressInt = Mathf.RoundToInt(startFill * 100f);
        
        while (elapsed < FillDuration)
        {
            elapsed += Time.deltaTime;
            float t = FillCurve.Evaluate(elapsed / FillDuration);
            float currentFill = Mathf.Lerp(startFill, targetFill, t);
            int currentProgressInt = Mathf.RoundToInt(Mathf.Lerp(startProgressInt, targetProgressInt, t));
            
            SetFillImmediate(currentFill);
            SetProgressText(currentProgressInt);
            
            yield return null;
        }
        
        SetFillImmediate(targetFill);
        SetProgressText(targetProgressInt);
    }

    private IEnumerator AnimateProgressTextFade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _progressTextCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        _progressTextCanvasGroup.alpha = endAlpha;
    }
    
    public IEnumerator AnimateRankUp(int newRankIndex)
    {
        if (iconContainer == null) yield break;

        bool scaleDownFinished = false;
        bool nameFadeOutFinished = false;

        StartCoroutine(AnimateIconScaleInternal(_originalIconScale, Vector3.zero, AnimationConst, RankUpScaleDownCurve, () => scaleDownFinished = true));
        StartCoroutine(AnimateRankNameFadeInternal(1f, 0f, AnimationConst, () => nameFadeOutFinished = true));

        yield return new WaitUntil(() => scaleDownFinished && nameFadeOutFinished);
        
        UpdateRankVisuals(newRankIndex);
        SetRankName(newRankIndex);
        _currentRankIndex = newRankIndex;
        SetFillImmediate(0f); 
        SetProgressText(0); 

        bool scaleUpFinished = false;
        bool nameFadeInFinished = false;

        StartCoroutine(AnimateIconScaleInternal(Vector3.zero, _originalIconScale, AnimationConst, RankUpScaleUpCurve, () => scaleUpFinished = true));
        StartCoroutine(AnimateRankNameFadeInternal(0f, 1f, AnimationConst, () => nameFadeInFinished = true));

        yield return new WaitUntil(() => scaleUpFinished && nameFadeInFinished);
    }
    
    private IEnumerator AnimateIconScaleInternal(Vector3 from, Vector3 to, float duration, AnimationCurve curve, Action onComplete = null)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            iconContainer.localScale = _originalIconScale * t;
            
            yield return null;
        }
        
        iconContainer.localScale = to;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateRankNameFadeInternal(float fromAlpha, float toAlpha, float duration, Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = RankNameFadeCurve.Evaluate(elapsed / duration);
            _rankNameCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }
        _rankNameCanvasGroup.alpha = toAlpha;
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (_rankIconMaterial != null)
        {
            Destroy(_rankIconMaterial);
            _rankIconMaterial = null;
        }
    }
}