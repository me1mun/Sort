using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridController : MonoBehaviour
{
    public event Action OnVictory;
    public event Action OnFinalVictory;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1.2f;
    [SerializeField] private int maxGridHeight = 5;

    [Header("Animation & Feel")]
    [SerializeField] private float collectAnimationDelay = 0.05f;
    [SerializeField] private float dragSmoothingSpeed = 20f;
    
    [Header("Component References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private ParticleSystem victoryParticles;

    private PropPool _propPool;
    private UIController _uiController;
    private GridManager _gridManager;
    private Queue<ItemSpawnInfo> _spawnQueue;
    private Vector2 _gridOffset;
    private PropView _draggedProp;
    private bool _isBusy = false;
    private int _groupsCollectedCount = 0;
    private bool _isLevelCompleted = false;
    private int _totalGroupsInLevel = 0;
    
    private int _width;
    private int _height;

    private void OnEnable() => SubscribeToInput();
    private void OnDisable() => UnsubscribeFromInput();

    public void Initialize(LevelData levelData, PropPool pool, UIController uiController)
    {
        _propPool = pool;
        _uiController = uiController;
        
        _height = Mathf.Min(levelData.requiredGroups.Count, maxGridHeight);
        _width = levelData.requiredGroups.Any() ? levelData.requiredGroups.Max(g => g.items.Count) : 0;
        
        _gridManager = new GridManager(_width, _height);
        _isLevelCompleted = false;
        _groupsCollectedCount = 0;
        _totalGroupsInLevel = levelData.requiredGroups.Count;
        
        CalculateGridOffset();
        PrepareSpawnQueue(levelData);
        
        StartCoroutine(PopulateInitialField());
    }
    
    #region Input Handling

    private void SubscribeToInput()
    {
        if (inputManager == null) return;
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
        
        Vector3 targetPosition = new Vector3(worldPos.x, worldPos.y, _draggedProp.transform.position.z);
        _draggedProp.transform.position = Vector3.Lerp(_draggedProp.transform.position, targetPosition, Time.deltaTime * dragSmoothingSpeed);
    }

    private void OnDragEnd(Vector2 worldPos)
    {
        if (_draggedProp == null) return;
        
        _draggedProp.Select(false);
        Vector2Int dropPosition = GetGridPositionFromWorld(worldPos);
        HandleDropAt(dropPosition);
        _draggedProp = null;
    }

    private void HandleDropAt(Vector2Int dropPosition)
    {
        Vector2Int startPosition = _draggedProp.GridPosition;

        bool isValidDrop = _gridManager.IsWithinBounds(dropPosition.x, dropPosition.y) && dropPosition != startPosition;

        if (isValidDrop)
        {
            PropView targetProp = _gridManager.GetPropAt(dropPosition.x, dropPosition.y);
            StartCoroutine(SwapPropsRoutine(_draggedProp, targetProp));
        }
        else
        {
            StartCoroutine(_draggedProp.AnimateMove(GetWorldPosition(startPosition.x, startPosition.y)));
        }
    }

    #endregion

    #region Game Flow Coroutines

    private IEnumerator PopulateInitialField()
    {
        _isBusy = true;
        var spawnCoroutines = new List<Coroutine>();
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_spawnQueue.Count > 0)
                {
                    var prop = CreatePropAt(x, y, _spawnQueue.Dequeue());
                    spawnCoroutines.Add(StartCoroutine(prop.AnimateMove(GetWorldPosition(x,y))));
                }
            }
        }
        
        foreach (var coro in spawnCoroutines) yield return coro;
        
        _isBusy = false;
    }

    private IEnumerator SwapPropsRoutine(PropView propA, PropView propB)
    {
        _isBusy = true;
        
        if (propB == null)
        {
            StartCoroutine(propA.AnimateMove(GetWorldPosition(propA.GridPosition.x, propA.GridPosition.y)));
            _isBusy = false;
            yield break;
        }

        AudioManager.Instance.Play("Swap");
        
        Vector2Int posA = propA.GridPosition;
        Vector2Int posB = propB.GridPosition;
        
        _gridManager.SwapProps(posA, posB);
        
        var moveA = StartCoroutine(propA.AnimateMove(GetWorldPosition(posB.x, posB.y)));
        var moveB = StartCoroutine(propB.AnimateMove(GetWorldPosition(posA.x, posA.y)));
        yield return moveA;
        yield return moveB;
        
        yield return StartCoroutine(CheckMatchesAndRefill());
    }

    private IEnumerator CheckMatchesAndRefill()
    {
        _isBusy = true;
        List<int> completedRows = _gridManager.GetCompletedRows();

        if (completedRows.Any())
        {
            yield return ClearRowsRoutine(completedRows);
            yield return CollapseAndRefillRoutine();
            yield return StartCoroutine(CheckMatchesAndRefill()); 
        }
        else
        {
            if (!_isLevelCompleted && _spawnQueue.Count == 0 && _gridManager.IsGridEmpty())
            {
                _isLevelCompleted = true; 
                OnFinalVictory?.Invoke();
            }
            _isBusy = false;
        }
    }

    private IEnumerator ClearRowsRoutine(List<int> rows)
    {
        var flyingCoroutines = new List<Coroutine>();
        
        foreach (int y in rows)
        {
            _groupsCollectedCount++;
            
            if (_groupsCollectedCount == _totalGroupsInLevel)
            {
                OnVictory?.Invoke();
                if (victoryParticles != null) victoryParticles.Play();
            }
            
            PropView firstProp = _gridManager.GetPropAt(0, y);
            AudioManager.Instance.Play("Score");
            int targetIndicatorIndex = _groupsCollectedCount - 1;
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
                    yield return new WaitForSeconds(collectAnimationDelay);
                }
            }
        }

        foreach (var coroutine in flyingCoroutines) yield return coroutine;
    }

    private IEnumerator CollapseAndRefillRoutine()
    {
        var moveCoroutines = new List<Coroutine>();
        
        // --- Этап 1: Смещение существующих предметов ---
        for (int x = 0; x < _width; x++)
        {
            int emptySpaces = 0;
            for (int y = 0; y < _height; y++)
            {
                if (_gridManager.GetPropAt(x, y) == null)
                {
                    emptySpaces++;
                }
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
        
        foreach(var coro in moveCoroutines) yield return coro;
        moveCoroutines.Clear();

        // --- Этап 2: Пополнение новыми предметами ---
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_gridManager.GetPropAt(x, y) == null && _spawnQueue.Any())
                {
                    var newProp = CreatePropAt(x, y, _spawnQueue.Dequeue(), true);
                    moveCoroutines.Add(StartCoroutine(newProp.AnimateMove(GetWorldPosition(x,y))));
                }
            }
        }
        
        foreach(var coro in moveCoroutines) yield return coro;
    }
    
    #endregion
    
    #region Helper Methods
    
    private void PrepareSpawnQueue(LevelData levelData)
    {
        if (!levelData.requiredGroups.Any())
        {
            _spawnQueue = new Queue<ItemSpawnInfo>();
            return;
        }

        var allItemsForLevel = new List<ItemSpawnInfo>();
        foreach (var group in levelData.requiredGroups)
        {
            if (!group.items.Any()) continue;
            for(int i = 0; i < _width; i++)
            {
                allItemsForLevel.Add(new ItemSpawnInfo(group, group.items[i % group.items.Count]));
            }
        }

        if (levelData.requiredGroups.Count < 2)
        {
            _spawnQueue = new Queue<ItemSpawnInfo>(allItemsForLevel.OrderBy(x => Random.value));
            return;
        }

        var itemsForInitialGrid = new List<ItemSpawnInfo>();
        var shuffledGroups = levelData.requiredGroups.OrderBy(x => Random.value).ToList();
        var guaranteedGroups = shuffledGroups.Take(2).ToList();
        var otherGroups = shuffledGroups.Skip(2).ToList();

        foreach (var group in guaranteedGroups)
        {
            if (!group.items.Any()) continue;
            for (int i = 0; i < _width; i++)
            {
                 itemsForInitialGrid.Add(new ItemSpawnInfo(group, group.items[i % group.items.Count]));
            }
        }

        foreach (var group in otherGroups)
        {
            if (group.items.Any())
            {
                var item = group.items[Random.Range(0, group.items.Count)];
                itemsForInitialGrid.Add(new ItemSpawnInfo(group, item));
            }
        }
        
        var initialGridCounts = itemsForInitialGrid.GroupBy(i => i.Item).ToDictionary(g => g.Key, g => g.Count());
        var totalCounts = allItemsForLevel.GroupBy(i => i.Item).ToDictionary(g => g.Key, g => g.Count());
        var remainingQueueItems = new List<ItemSpawnInfo>();

        foreach (var pair in totalCounts)
        {
            int inInitial = initialGridCounts.ContainsKey(pair.Key) ? initialGridCounts[pair.Key] : 0;
            int remaining = pair.Value - inInitial;
            for (int i = 0; i < remaining; i++)
            {
                remainingQueueItems.Add(allItemsForLevel.First(itemInfo => itemInfo.Item == pair.Key));
            }
        }

        _spawnQueue = new Queue<ItemSpawnInfo>(itemsForInitialGrid.OrderBy(x => Random.value).Concat(remainingQueueItems.OrderBy(x => Random.value)));
    }
    
    private void CalculateGridOffset()
    {
        float totalGridWidth = (_width - 1) * cellSize;
        float totalGridHeight = (_height - 1) * cellSize;
        _gridOffset = new Vector2(-totalGridWidth / 2f, -totalGridHeight / 2f);
    }

    private PropView CreatePropAt(int x, int y, ItemSpawnInfo itemInfo, bool spawnAtTop = false)
    {
        Vector3 startPosition = spawnAtTop ? GetWorldPosition(x, _height) : GetWorldPosition(x,y);
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
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int y = Mathf.RoundToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }
    
    private PropView GetPropAtWorldPosition(Vector2 worldPos)
    {
        Vector2Int gridPos = GetGridPositionFromWorld(worldPos);
        return _gridManager.GetPropAt(gridPos.x, gridPos.y);
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return (Vector3)_gridOffset + new Vector3(x * cellSize, y * cellSize, 0) + transform.position;
    }
    
    private class ItemSpawnInfo
    {
        public GroupData Group;
        public ItemData Item;
        public ItemSpawnInfo(GroupData g, ItemData i) { Group = g; Item = i; }
    }
    
    #endregion
}