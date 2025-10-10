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
    [SerializeField] private AnimationCurve bounceCurve;
    
    private const float BounceDuration = 0.3f;
    private const float BaseBounceScale = 1.15f;
    private const float BounceIncrement = 0.05f;
    
    private const string CollectSoundName = "Collect";
    private const float BaseCollectPitch = 0.8f;
    private const float PitchStep = 0.1f;
    
    private Vector3 _originalScale;
    private Coroutine _activeBounceCoroutine;
    private int _arrivalCount = 0;

    private void Awake()
    {
        _originalScale = transform.localScale;
        SetEmpty();
    }

    public void SetFilled()
    {
        if (iconImage.sprite != filledSprite)
        {
            iconImage.sprite = filledSprite;
        }
        
        _arrivalCount++;
        
        float currentPitch = BaseCollectPitch + ((_arrivalCount - 1) * PitchStep);
        AudioManager.Instance.PlayWithPitch(CollectSoundName, currentPitch);

        if (_activeBounceCoroutine != null)
        {
            StopCoroutine(_activeBounceCoroutine);
        }
        _activeBounceCoroutine = StartCoroutine(AnimateBounceCoroutine());
    }

    public void SetEmpty()
    {
        iconImage.sprite = emptySprite;
        transform.localScale = _originalScale;
        _arrivalCount = 0;
    }

    private IEnumerator AnimateBounceCoroutine()
    {
        float timer = 0f;
        float currentBounceMultiplier = BaseBounceScale + ((_arrivalCount - 1) * BounceIncrement);
        
        while (timer < BounceDuration)
        {
            float progress = timer / BounceDuration;
            float curveValue = bounceCurve.Evaluate(progress);
            
            float scaleMultiplier = 1f + (currentBounceMultiplier - 1f) * curveValue;
            transform.localScale = _originalScale * scaleMultiplier;

            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = _originalScale;
        _activeBounceCoroutine = null;
    }
}