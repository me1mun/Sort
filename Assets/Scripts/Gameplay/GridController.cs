using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridController : MonoBehaviour
{
    private const float CellSize = 1.1f;
    private const int MaxGridHeight = 5;
    private const float CollectAnimationDelay = 0.05f;
    
    public event Action OnVictory;
    public event Action OnFinalVictory;
    public event Action<string> OnGroupCollected;
    public event Action<int> OnPropArrivedAtIndicator;

    public Func<int, Vector3> GetIndicatorWorldPositionProvider;
    public bool IsBusy => _isBusy;

    [Header("Component References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private ParticleSystem victoryParticles;
    [SerializeField] private PropPool propPool;
    
    private Grid _grid;
    private GridInputHandler _inputHandler;
    private GridSpawner _spawner;
    private Camera _camera;
    
    private bool _isBusy = false;
    private int _groupsCollectedCount = 0;
    private bool _isLevelCompleted = false;
    private int _totalGroupsInLevel = 0;

    public void Initialize(LevelData levelData, Camera mainCamera)
    {
        _camera = mainCamera;
        GameplayController.OnScreenSizeChanged += UpdateGridPosition;
        UpdateGridPosition();

        _grid = new Grid(levelData, CellSize, MaxGridHeight, transform.position);
        _spawner = new GridSpawner(levelData, _grid.Width, propPool, _grid);
        _inputHandler = new GridInputHandler(inputManager, _grid, () => _isBusy);
        _isLevelCompleted = false;
        _groupsCollectedCount = 0;
        _totalGroupsInLevel = levelData.requiredGroups.Count;
        _inputHandler.OnSwapRequested += HandleSwapRequest;
        StartCoroutine(PopulateInitialField());
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnSwapRequested -= HandleSwapRequest;
            _inputHandler.Dispose();
        }
        GameplayController.OnScreenSizeChanged -= UpdateGridPosition;
    }
    
    private void UpdateGridPosition()
    {
        if (_camera == null) return;

        float yOffsetPixels = Screen.height * 0.05f;
        Vector3 screenCenterWithOffset = new Vector3(Screen.width / 2f, (Screen.height / 2f) - yOffsetPixels, 0);
        float distance = Mathf.Abs(_camera.transform.position.z);
        screenCenterWithOffset.z = distance;
        
        Vector3 worldOrigin = _camera.ScreenToWorldPoint(screenCenterWithOffset);
        worldOrigin.z = 0f;
        transform.position = worldOrigin;
    }

    public string FindCompletableGroupKey()
    {
        if (_isBusy) return null;

        var allProps = _grid.GetAllProps();
        var completableGroups = allProps
            .GroupBy(p => p.AssignedGroup)
            .Where(g => g.Count() == _grid.Width)
            .ToList();

        if (completableGroups.Any())
        {
            return completableGroups[Random.Range(0, completableGroups.Count)].Key.groupKey;
        }
        
        return null;
    }

    public void AnimateHintForGroup(string groupKey)
    {
        var allProps = _grid.GetAllProps();
        var propsToAnimate = allProps.Where(p => p.AssignedGroup.groupKey == groupKey);

        foreach (var prop in propsToAnimate)
        {
            prop.StartHintAnimation();
        }
    }
    
    public void StopAllHintAnimations()
    {
        var allProps = _grid.GetAllProps();
        foreach (var prop in allProps)
        {
            prop.StopHintAnimation();
        }
    }
    
    private void HandleSwapRequest(PropView propA, PropView propB)
    {
        if (_isBusy) return;
        StartCoroutine(SwapPropsRoutine(propA, propB));
    }

    private IEnumerator PopulateInitialField()
    {
        _isBusy = true;
        var spawnCoroutines = new List<Coroutine>();
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                if (_spawner.HasItemsToSpawn())
                {
                    var prop = _spawner.CreatePropAt(x, y);
                    Vector3 targetPos = _grid.GetWorldPosition(x, y);
                    spawnCoroutines.Add(StartCoroutine(prop.AnimateMove(targetPos)));
                }
            }
        }
        foreach (var coroutine in spawnCoroutines) yield return coroutine;
        _isBusy = false;
    }

    private IEnumerator SwapPropsRoutine(PropView propA, PropView propB)
    {
        _isBusy = true;
        AudioManager.Instance.Play("Swap");
        Vector2Int posA = propA.GridPosition;
        Vector2Int posB = propB.GridPosition;
        _grid.SwapProps(posA, posB);
        var moveA = StartCoroutine(propA.AnimateMove(_grid.GetWorldPosition(posB.x, posB.y)));
        var moveB = StartCoroutine(propB.AnimateMove(_grid.GetWorldPosition(posA.x, posA.y)));
        yield return moveA;
        yield return moveB;
        yield return StartCoroutine(CheckMatchesAndRefill());
    }

    private IEnumerator CheckMatchesAndRefill()
    {
        _isBusy = true;
        bool madeChanges;
        do
        {
            madeChanges = false;
            List<int> completedRows = _grid.GetCompletedRows();
            if (completedRows.Any())
            {
                madeChanges = true;
                yield return ClearRowsRoutine(completedRows);
                yield return CollapseAndRefillRoutine();
            }
        } while (madeChanges);
        if (!_isLevelCompleted && !_spawner.HasItemsToSpawn() && _grid.IsGridEmpty())
        {
            _isLevelCompleted = true; 
            OnFinalVictory?.Invoke();
        }
        _isBusy = false;
    }

    private IEnumerator ClearRowsRoutine(List<int> rows)
    {
        var flyingCoroutines = new List<Coroutine>();
        foreach (int y in rows)
        {
            PropView firstPropInRow = _grid.GetPropAt(0, y);
            if (firstPropInRow != null)
            {
                OnGroupCollected?.Invoke(firstPropInRow.AssignedGroup.groupKey);
            }
            _groupsCollectedCount++;
            int targetIndicatorIndex = _groupsCollectedCount - 1;
            if (_groupsCollectedCount == _totalGroupsInLevel)
            {
                OnVictory?.Invoke();
                victoryParticles?.Play();
            }
            AudioManager.Instance.Play("Score");
            Vector3 targetPos = GetIndicatorWorldPositionProvider?.Invoke(targetIndicatorIndex) ?? Vector3.zero;
            for (int x = 0; x < _grid.Width; x++)
            {
                PropView prop = _grid.GetPropAt(x, y);
                if (prop != null)
                {
                    _grid.ClearCell(x, y);
                    Coroutine flyCoroutine = prop.Collect(targetPos, () => {
                        OnPropArrivedAtIndicator?.Invoke(targetIndicatorIndex);
                        propPool.Return(prop);
                    });
                    flyingCoroutines.Add(flyCoroutine);
                    yield return new WaitForSeconds(CollectAnimationDelay);
                }
            }
        }
        foreach (var coroutine in flyingCoroutines)
        {
            if (coroutine != null) yield return coroutine;
        }
    }

    private IEnumerator CollapseAndRefillRoutine()
    {
        var moveCoroutines = new List<Coroutine>();
        var movedProps = _grid.CollapseColumns();
        foreach (var prop in movedProps)
        {
            Vector3 targetPos = _grid.GetWorldPosition(prop.GridPosition.x, prop.GridPosition.y);
            moveCoroutines.Add(StartCoroutine(prop.AnimateMove(targetPos)));
        }
        foreach(var coro in moveCoroutines) yield return coro;
        moveCoroutines.Clear();
        var newProps = _spawner.RefillGrid();
        foreach (var prop in newProps)
        {
            Vector3 targetPos = _grid.GetWorldPosition(prop.GridPosition.x, prop.GridPosition.y);
            moveCoroutines.Add(StartCoroutine(prop.AnimateMove(targetPos)));
        }
        foreach(var coro in moveCoroutines) yield return coro;
    }
}