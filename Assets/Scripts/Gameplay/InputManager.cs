using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    private Camera _mainCamera;
    private InputAction _touchPressAction;
    private InputAction _touchPositionAction;
    
    private bool _isDragging = false;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        _touchPressAction = new InputAction("TouchPress", binding: "<Pointer>/press");
        _touchPositionAction = new InputAction("TouchPosition", binding: "<Pointer>/position");
    }

    private void OnEnable()
    {
        _touchPressAction.Enable();
        _touchPositionAction.Enable();
    }

    private void OnDisable()
    {
        _touchPressAction.Disable();
        _touchPositionAction.Disable();
    }
    
    private void Update()
    {
        if (_mainCamera == null) return;

        bool isPressed = _touchPressAction.IsPressed();

        if (isPressed && !_isDragging)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            StartDrag();
            _isDragging = true;
        }
        else if (!isPressed && _isDragging)
        {
            EndDrag();
            _isDragging = false;
        }

        if (_isDragging)
        {
            OnDrag?.Invoke(GetWorldPosition());
        }
    }

    private void StartDrag()
    {
        OnDragStart?.Invoke(GetWorldPosition());
    }

    private void EndDrag()
    {
        OnDragEnd?.Invoke(GetWorldPosition());
    }

    private Vector2 GetWorldPosition()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("InputManager: Main Camera not found!");
                return Vector2.zero;
            }
        }
        return _mainCamera.ScreenToWorldPoint(_touchPositionAction.ReadValue<Vector2>());
    }
}