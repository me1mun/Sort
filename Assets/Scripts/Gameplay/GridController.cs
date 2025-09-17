using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridController : MonoBehaviour
{
    public event Action OnLevelCompleted;

    [Header("Grid Settings")]
    [SerializeField] private int _width = 5;
    [SerializeField] private int _height = 7;
    [SerializeField] private float _cellSize = 1.2f;
    [SerializeField] private float _collectAnimationDelay = 0.05f;

    [Header("Component References")]
    [SerializeField] private InputManager inputManager;
    
    private PropPool _propPool;
    private UIController _uiController;
    private GridManager _gridManager;
    private Queue<ItemSpawnInfo> _spawnQueue;
    private Vector2 _gridOffset;
    private PropView _draggedProp;
    private bool _isBusy = false;
    private int _groupsCollectedCount = 0; // <-- Счетчик собранных групп
    private bool _isLevelCompleted = false;

    public void Initialize(LevelData levelData, PropPool pool, UIController uiController)
    {
        _propPool = pool;
        _uiController = uiController;
        _gridManager = new GridManager(_width, _height);
        _isLevelCompleted = false;
        _groupsCollectedCount = 0; // Сбрасываем счетчик при инициализации
        
        CalculateGridOffset();
        PrepareSpawnQueue(levelData);
        
        SubscribeToInput();
        StartCoroutine(GenerateInitialField());
    }

    private IEnumerator ClearRows(List<int> rows)
    {
        List<Coroutine> flyingCoroutines = new List<Coroutine>();
        
        foreach (int y in rows)
        {
            PropView firstProp = _gridManager.GetPropAt(0, y);
            if(firstProp == null) continue;

            GroupData collectedGroup = firstProp.AssignedGroup;
            string translatedGroupName = LocalizationManager.Instance.GetTranslation(collectedGroup.groupKey, TranslationGroup.Groups);
            Debug.Log($"Group collected: {translatedGroupName}");

            AudioManager.Instance.Play("Score");

            int targetIndicatorIndex = _groupsCollectedCount;
            
            Vector3 targetPos = _uiController.GetIndicatorWorldPosition(targetIndicatorIndex);

            for (int x = 0; x < _width; x++)
            {
                PropView prop = _gridManager.GetPropAt(x, y);
                if (prop != null)
                {
                    _gridManager.ClearCell(x, y);

                    Coroutine flyCoroutine = StartCoroutine(prop.AnimateFlyAndCollect(targetPos, () => {
                        _uiController.FillIndicator(targetIndicatorIndex);
                        _propPool.Return(prop);
                    }));
                    flyingCoroutines.Add(flyCoroutine);
                    
                    yield return new WaitForSeconds(_collectAnimationDelay);
                }
            }

            _groupsCollectedCount++;
        }

        foreach (var coroutine in flyingCoroutines)
        {
            yield return coroutine;
        }
    }
    
    private void OnDestroy() => UnsubscribeFromInput();

    private IEnumerator SwapProps(PropView propA, PropView propB)
    {
        _isBusy = true;

        AudioManager.Instance.Play("Swap");
        
        Vector2Int posA = propA.GridPosition;
        Vector2Int posB = propB.GridPosition;
        
        _gridManager.SwapProps(posA, posB);
        
        Coroutine moveA = StartCoroutine(propA.AnimateMove(GetWorldPosition(posB.x, posB.y)));
        Coroutine moveB = StartCoroutine(propB.AnimateMove(GetWorldPosition(posA.x, posA.y)));
        yield return moveA;
        yield return moveB;
        
        yield return StartCoroutine(CheckMatchesAndRefill());
    }
    
    private IEnumerator CheckMatchesAndRefill()
    {
        _isBusy = true;
        List<int> completedRows = _gridManager.GetCompletedRows();

        if (completedRows.Count > 0)
        {
            yield return ClearRows(completedRows);
            yield return CollapseColumns();
            yield return RefillGrid();
            yield return StartCoroutine(CheckMatchesAndRefill()); 
        }
        else
        {
            if (!_isLevelCompleted && _spawnQueue.Count == 0 && _gridManager.IsGridEmpty())
            {
                _isLevelCompleted = true; 
                OnLevelCompleted?.Invoke();
            }
            
            _isBusy = false;
        }
    }

    private IEnumerator CollapseColumns()
    {
        List<Coroutine> moveCoroutines = new List<Coroutine>();
        for (int x = 0; x < _width; x++)
        {
            int emptySpaces = 0;
            for (int y = 0; y < _height; y++)
            {
                if (_gridManager.GetPropAt(x, y) == null) { emptySpaces++; }
                else if (emptySpaces > 0)
                {
                    PropView prop = _gridManager.GetPropAt(x, y);
                    Vector2Int newPos = new Vector2Int(x, y - emptySpaces);
                    
                    _gridManager.SetPropAt(x, y, null);
                    _gridManager.SetPropAt(newPos.x, newPos.y, prop);
                    
                    prop.GridPosition = newPos;
                    moveCoroutines.Add(StartCoroutine(prop.AnimateMove(GetWorldPosition(newPos.x, newPos.y))));
                }
            }
        }
        foreach(var coro in moveCoroutines) { yield return coro; }
        yield return new WaitUntil(()=> _gridManager.IsGridIdle());
    }

    private IEnumerator RefillGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_gridManager.GetPropAt(x, y) == null && _spawnQueue.Count > 0)
                {
                    var newProp = CreatePropAt(x, y, _spawnQueue.Dequeue(), true);
                    StartCoroutine(newProp.AnimateMove(GetWorldPosition(x,y)));
                }
            }
        }
        yield return new WaitUntil(() => _gridManager.IsGridIdle());
    }
    
    private void SubscribeToInput()
    {
        if (inputManager == null) 
        {
            Debug.LogError("InputManager не назначен в инспекторе GridController!");
            return;
        }
        inputManager.OnDragStart += OnDragStart;
        inputManager.OnDrag += OnDrag;
        inputManager.OnDragEnd += OnDragEnd;
    }

    private void UnsubscribeFromInput()
    {
        if (inputManager == null) return;
        inputManager.OnDragStart -= OnDragStart;
        inputManager.OnDrag -= OnDrag;
        inputManager.OnDragEnd -= OnDragEnd;
    }

    #region Unchanged Code
    
    private void CalculateGridOffset()
    {
        float totalGridWidth = (_width - 1) * _cellSize;
        float totalGridHeight = (_height - 1) * _cellSize;
        _gridOffset = new Vector2(-totalGridWidth / 2f, -totalGridHeight / 2f);
    }
    
    private void PrepareSpawnQueue(LevelData levelData)
    {
        // 1. Создаем списки для предметов, которые появятся сразу, и для тех, что в запасе.
        var itemsForInitialGrid = new List<ItemSpawnInfo>();
        var remainingQueueItems = new List<ItemSpawnInfo>();

        if (levelData.requiredGroups.Count < 2)
        {
            Debug.LogWarning("В уровне меньше 2 групп, используется стандартное перемешивание.");
            // Если групп мало, используем старый простой метод
            var allItems = levelData.requiredGroups
                .SelectMany(group => group.items.Select(item => new ItemSpawnInfo(group, item)))
                .OrderBy(x => Random.value)
                .ToList();
            _spawnQueue = new Queue<ItemSpawnInfo>(allItems);
            return;
        }

        // 2. Перемешиваем группы, чтобы выбрать 2 случайные для гарантии на старте.
        var shuffledGroups = levelData.requiredGroups.OrderBy(x => Random.value).ToList();
        
        var guaranteedGroups = shuffledGroups.Take(2).ToList();
        var otherGroups = shuffledGroups.Skip(2).ToList();

        // 3. Добавляем ВСЕ предметы из 2-х гарантированных групп в стартовый пул.
        foreach (var group in guaranteedGroups)
        {
            Debug.Log("start group: " + group.groupKey);
            foreach (var item in group.items)
            {
                itemsForInitialGrid.Add(new ItemSpawnInfo(group, item));
            }
        }

        // 4. Из всех ОСТАЛЬНЫХ групп берем по ОДНОМУ случайному предмету.
        foreach (var group in otherGroups)
        {
            if (group.items.Count > 0)
            {
                var tempItemsInGroup = new List<ItemData>(group.items);
                
                int randomIndex = Random.Range(0, tempItemsInGroup.Count);
                ItemData randomItem = tempItemsInGroup[randomIndex];
                
                // Один случайный предмет — на стартовое поле.
                itemsForInitialGrid.Add(new ItemSpawnInfo(group, randomItem));
                
                // Удаляем его из временного списка.
                tempItemsInGroup.RemoveAt(randomIndex);
                
                // Все оставшиеся предметы этой группы — в очередь запаса.
                foreach (var item in tempItemsInGroup)
                {
                    remainingQueueItems.Add(new ItemSpawnInfo(group, item));
                }
            }
        }

        // 5. Перемешиваем оба списка и объединяем их в финальную очередь спавна.
        itemsForInitialGrid = itemsForInitialGrid.OrderBy(x => Random.value).ToList();
        remainingQueueItems = remainingQueueItems.OrderBy(x => Random.value).ToList();

        _spawnQueue = new Queue<ItemSpawnInfo>(itemsForInitialGrid.Concat(remainingQueueItems));
    }
    
    private IEnumerator GenerateInitialField()
    {
        _isBusy = true;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_spawnQueue.Count > 0)
                {
                    var newProp = CreatePropAt(x, y, _spawnQueue.Dequeue());
                    StartCoroutine(newProp.AnimateMove(GetWorldPosition(x,y)));
                }
            }
             yield return new WaitForSeconds(0.05f); 
        }
        
        yield return new WaitUntil(() => _gridManager.IsGridIdle());
        _isBusy = false;
    }
    
    private void OnDragStart(Vector2 worldPos)
    {
        if (_isBusy) return;
        
        PropView prop = GetPropAtWorldPosition(worldPos);
        if (prop != null && !prop.IsAnimating)
        {
            _draggedProp = prop;
            _draggedProp.Select(true);

            AudioManager.Instance.Play("Tap");
        }
    }

    private void OnDrag(Vector2 worldPos)
    {
        if (_draggedProp == null) return;
        Vector3 draggedPosition = new Vector3(worldPos.x, worldPos.y, -1);
        _draggedProp.transform.position = draggedPosition;
    }

    private void OnDragEnd(Vector2 worldPos)
    {
        if (_draggedProp == null) return;

        _draggedProp.Select(false);
        
        Vector2Int startPos = _draggedProp.GridPosition;
        Vector2Int endPos = GetGridPositionFromWorld(worldPos);
        
        if (_gridManager.IsWithinBounds(endPos.x, endPos.y) && endPos != startPos)
        {
            PropView targetProp = _gridManager.GetPropAt(endPos.x, endPos.y);
            if (targetProp != null && !targetProp.IsAnimating)
            {
                StartCoroutine(SwapProps(_draggedProp, targetProp));
            }
            else
            {
                StartCoroutine(_draggedProp.AnimateMove(GetWorldPosition(startPos.x, startPos.y)));
            }
        }
        else
        {
            StartCoroutine(_draggedProp.AnimateMove(GetWorldPosition(startPos.x, startPos.y)));
        }
        
        _draggedProp = null;
    }
    
    private PropView CreatePropAt(int x, int y, ItemSpawnInfo itemInfo, bool spawnAtTop = false)
    {
        Vector3 startPosition = spawnAtTop ? GetWorldPosition(x, _height) : GetWorldPosition(x, y);
        PropView prop = _propPool.Get();
        prop.transform.position = startPosition;
        prop.Initialize(itemInfo.Group, itemInfo.Item, new Vector2Int(x, y));
        prop.OnSpawn();
        _gridManager.SetPropAt(x, y, prop);
        return prop;
    }

    private Vector2Int GetGridPositionFromWorld(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - ((Vector2)transform.position + _gridOffset);
        int x = Mathf.RoundToInt(localPos.x / _cellSize);
        int y = Mathf.RoundToInt(localPos.y / _cellSize);
        return new Vector2Int(x, y);
    }
    
    private PropView GetPropAtWorldPosition(Vector2 worldPos)
    {
        Vector2Int gridPos = GetGridPositionFromWorld(worldPos);
        return _gridManager.GetPropAt(gridPos.x, gridPos.y);
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return (Vector3)_gridOffset + new Vector3(x * _cellSize, y * _cellSize, 0) + transform.position;
    }
    
    private class ItemSpawnInfo
    {
        public GroupData Group;
        public ItemData Item;
        public ItemSpawnInfo(GroupData g, ItemData i) { Group = g; Item = i; }
    }
    
    #endregion
}