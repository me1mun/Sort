using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    private const float GroupNameAnimDuration = 1.5f;

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private RectTransform indicatorsContainer;

    [Header("Collected Group Name Animation")]
    [SerializeField] private CanvasGroup collectedGroupCanvasGroup;
    [SerializeField] private TextMeshProUGUI collectedGroupText;
    [SerializeField] private AnimationCurve alphaCurve;

    private List<UIGroupIndicator> _indicators;
    private Coroutine _groupNameAnimation;

    private void Awake()
    {
        _indicators = indicatorsContainer.GetComponentsInChildren<UIGroupIndicator>(true).ToList();
    }

    public void InitializeUIForLevel(LevelData levelData)
    {
        int requiredCount = levelData?.requiredGroups?.Count ?? 0;
        for (int i = 0; i < _indicators.Count; i++)
        {
            bool isActive = i < requiredCount;
            _indicators[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                _indicators[i].SetEmpty();
            }
        }
        collectedGroupCanvasGroup.alpha = 0;
    }

    public void UpdateLevelText(int level)
    {
        string localizedLevelString = LocalizationManager.Instance.Get("ui.level");
        levelText.text = $"{localizedLevelString} {level}";
    }

    public Vector3 GetIndicatorScreenPosition(int index)
    {
        if (index >= 0 && index < _indicators.Count)
        {
            return _indicators[index].transform.position;
        }
        return Vector3.zero;
    }

    public void NotifyIndicatorPropArrival(int index)
    {
        if (index >= 0 && index < _indicators.Count)
        {
            _indicators[index].SetFilled();
        }
    }

    public void ShowCollectedGroupName(string groupKey)
    {
        if (_groupNameAnimation != null)
        {
            StopCoroutine(_groupNameAnimation);
        }
        _groupNameAnimation = StartCoroutine(AnimateGroupName(groupKey));
    }

    private IEnumerator AnimateGroupName(string groupKey)
    {
        string localizedName = LocalizationManager.Instance.Get($"groups.{groupKey}");
        collectedGroupText.text = localizedName;

        float elapsed = 0f;
        while (elapsed < GroupNameAnimDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / GroupNameAnimDuration);
            collectedGroupCanvasGroup.alpha = alphaCurve.Evaluate(progress);
            yield return null;
        }
        _groupNameAnimation = null;
    }
}