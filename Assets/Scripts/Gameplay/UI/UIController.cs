using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    private const float GroupNameAnimDuration = 2f;
    private const float GroupNameAnimFastDuration = 0.7f;
    
    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private RectTransform indicatorsContainer;

    [Header("Collected Group Name Animation")]
    [SerializeField] private CanvasGroup collectedGroupCanvasGroup;
    [SerializeField] private TextMeshProUGUI collectedGroupText;
    [SerializeField] private AnimationCurve alphaCurve;
    
    private List<UIGroupIndicator> _indicators;
    private Coroutine _groupNameQueueProcessor;
    private Queue<string> _groupNameQueue = new Queue<string>();

    private void Awake()
    {
        if (indicatorsContainer != null)
        {
            _indicators = indicatorsContainer.GetComponentsInChildren<UIGroupIndicator>(true).ToList();
        }
        else
        {
            _indicators = new List<UIGroupIndicator>();
            Debug.LogError("Indicators Container is not assigned in UIController!");
        }
    }

    public void InitializeUIForLevel(LevelData levelData)
    {
        int requiredCount = levelData?.requiredGroups?.Count ?? 0;
        if (_indicators == null) return;
        for (int i = 0; i < _indicators.Count; i++)
        {
            bool isActive = i < requiredCount;
            _indicators[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                _indicators[i].SetEmpty();
            }
        }
        if(collectedGroupCanvasGroup != null)
        {
            collectedGroupCanvasGroup.alpha = 0;
            _groupNameQueue.Clear();
        }
    }

    // Этот метод отвечает за текст "УРОВЕНЬ 5"
    public void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            string localizedLevelString = LocalizationManager.Instance.Get("ui.level");
            levelText.text = $"{localizedLevelString} {level}";
        }
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

    // Этот метод отвечает за запуск анимации имени собранной группы (например, "Фрукты")
    public void ShowCollectedGroupName(string groupKey)
    {
        if (collectedGroupText == null || collectedGroupCanvasGroup == null) return;
        
        _groupNameQueue.Enqueue(groupKey);
        
        if (_groupNameQueueProcessor == null)
        {
            _groupNameQueueProcessor = StartCoroutine(ProcessGroupNameQueue());
        }
    }

    private IEnumerator ProcessGroupNameQueue()
    {
        while (_groupNameQueue.Count > 0)
        {
            string currentGroupKey = _groupNameQueue.Dequeue();
            string localizedName = LocalizationManager.Instance.Get($"groups.{currentGroupKey}");
            collectedGroupText.text = localizedName;

            float elapsed = 0f;
            float duration = GroupNameAnimDuration;

            while (elapsed < duration)
            {
                if (_groupNameQueue.Count > 0)
                {
                    duration = GroupNameAnimFastDuration;
                }

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = alphaCurve.Evaluate(progress);
                collectedGroupCanvasGroup.alpha = alpha;
                
                yield return null;
            }
        }
        _groupNameQueueProcessor = null;
    }
}