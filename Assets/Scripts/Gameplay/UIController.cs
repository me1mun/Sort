using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groupIndicatorsContainer;
    [SerializeField] private UIGroupIndicator groupIndicatorPrefab;
    
    private readonly List<UIGroupIndicator> _uiIndicators = new List<UIGroupIndicator>();
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void InitializeUIForLevel(LevelData level)
    {
        foreach (Transform child in groupIndicatorsContainer)
        {
            Destroy(child.gameObject);
        }
        _uiIndicators.Clear();
        
        for (int i = 0; i < level.requiredGroups.Count; i++)
        {
            UIGroupIndicator indicator = Instantiate(groupIndicatorPrefab, groupIndicatorsContainer);
            _uiIndicators.Add(indicator);
        }
    }
    
    // Новый метод: анимирует и "заполняет" индикатор по его номеру (индексу)
    public void FillIndicator(int index)
    {
        if (index >= 0 && index < _uiIndicators.Count)
        {
            _uiIndicators[index].SetFilled();
        }
    }

    // Новый метод: возвращает позицию индикатора по его номеру (индексу)
    public Vector3 GetIndicatorWorldPosition(int index)
    {
        if (index >= 0 && index < _uiIndicators.Count)
        {
            Vector3 screenPos = _uiIndicators[index].transform.position;
            return _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
        }
        
        return Vector3.zero; // Резервный вариант
    }
}