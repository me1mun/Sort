using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISafeArea : MonoBehaviour
{
    RectTransform panel;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);
    Vector2Int lastScreenSize = new Vector2Int(0, 0);
    ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Проверяем, не изменилась ли ориентация или размер экрана
        if (Screen.safeArea != lastSafeArea
            || Screen.width != lastScreenSize.x
            || Screen.height != lastScreenSize.y
            || Screen.orientation != lastOrientation)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea == lastSafeArea) return;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Нормализуем safe area
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;

        Debug.Log($"[SafeArea] Applied to {gameObject.name}: {safeArea}");
    }
}
