using System;
using UnityEngine;

public class GridInputHandler : IDisposable
{
    public event Action<PropView, PropView> OnSwapRequested;

    private readonly InputManager _inputManager;
    private readonly GridManager _gridManager;
    private readonly Func<bool> _isGridBusy;
    private readonly float _dragSmoothingSpeed = 20f;
    
    private PropView _draggedProp;

    public GridInputHandler(InputManager inputManager, GridManager gridManager, Func<bool> isGridBusy)
    {
        _inputManager = inputManager;
        _gridManager = gridManager;
        _isGridBusy = isGridBusy;
        SubscribeToInput();
    }

    private void OnDragStart(Vector2 worldPos)
    {
        if (_isGridBusy()) return;

        PropView prop = _gridManager.GetPropAtWorldPosition(worldPos);
        if (prop != null && !prop.IsAnimating)
        {
            _draggedProp = prop;
            _draggedProp.Select(true);
            _draggedProp.BringToFront();
            AudioManager.Instance.Play("Tap");
        }
    }

    private void OnDrag(Vector2 worldPos)
    {
        if (_draggedProp == null) return;
        
        Vector3 targetPosition = new Vector3(worldPos.x, worldPos.y, _draggedProp.transform.position.z);
        _draggedProp.transform.position = Vector3.Lerp(_draggedProp.transform.position, targetPosition, Time.deltaTime * _dragSmoothingSpeed);
    }

    private void OnDragEnd(Vector2 worldPos)
    {
        if (_draggedProp == null) return;

        _draggedProp.ResetSortingOrder();
        _draggedProp.Select(false);
        
        Vector2Int startPosition = _draggedProp.GridPosition;
        Vector2Int dropPosition = _gridManager.GetGridPositionFromWorld(worldPos);
        PropView targetProp = _gridManager.GetPropAt(dropPosition.x, dropPosition.y);

        bool isValidDrop = _gridManager.IsWithinBounds(dropPosition.x, dropPosition.y) && dropPosition != startPosition && targetProp != null;

        if (isValidDrop)
        {
            OnSwapRequested?.Invoke(_draggedProp, targetProp);
        }
        else
        {
            Vector3 originalWorldPos = _gridManager.GetWorldPosition(startPosition.x, startPosition.y);
            _draggedProp.StartCoroutine(_draggedProp.AnimateMove(originalWorldPos));
        }
        
        _draggedProp = null;
    }
    
    private void SubscribeToInput()
    {
        if (_inputManager == null) return;
        _inputManager.OnDragStart += OnDragStart;
        _inputManager.OnDrag += OnDrag;
        _inputManager.OnDragEnd += OnDragEnd;
    }

    public void Dispose()
    {
        if (_inputManager == null) return;
        _inputManager.OnDragStart -= OnDragStart;
        _inputManager.OnDrag -= OnDrag;
        _inputManager.OnDragEnd -= OnDragEnd;
    }
}