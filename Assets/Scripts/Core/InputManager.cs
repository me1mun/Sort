using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Убираем static Instance. Теперь это обычный компонент.
    
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    private Camera _mainCamera;
    private InputAction _touchPressAction;
    private InputAction _touchPositionAction;

    private void Awake()
    {
        // Вся логика синглтона удалена.
        // Просто находим камеру и создаем обработчики ввода.
        _mainCamera = Camera.main;
        
        _touchPressAction = new InputAction("TouchPress", binding: "<Pointer>/press");
        _touchPositionAction = new InputAction("TouchPosition", binding: "<Pointer>/position");

        _touchPressAction.started += ctx => StartDrag();
        _touchPressAction.canceled += ctx => EndDrag();
    }

    private void OnEnable()
    {
        _touchPressAction.Enable();
        _touchPositionAction.Enable();
    }

    private void OnDisable()
    {
        // OnDisable теперь тоже простой, без лишних проверок.
        _touchPressAction.Disable();
        _touchPositionAction.Disable();
    }
    
    private void Update()
    {
        if (_touchPressAction.IsPressed() && _mainCamera != null)
        {
            OnDrag?.Invoke(GetWorldPosition());
        }
    }

    private void StartDrag()
    {
        if (_mainCamera == null) return;
        OnDragStart?.Invoke(GetWorldPosition());
    }

    private void EndDrag()
    {
        if (_mainCamera == null) return;
        OnDragEnd?.Invoke(GetWorldPosition());
    }

    private Vector2 GetWorldPosition()
    {
        if (_mainCamera == null)
        {
            // Если камера не найдена, пытаемся найти ее еще раз.
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