using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class BackgroundScroller : MonoBehaviour
{
    private Vector2 scrollSpeed = new Vector2(0.03f, 0.04f);

    private float verticalTiles = 12f;

    private RawImage _image;
    private int _lastScreenWidth = 0;
    private int _lastScreenHeight = 0;

    private void Start()
    {
        _image = GetComponent<RawImage>();
        // Первоначальная настройка тайлинга
        UpdateTiling();
    }

    private void Update()
    {
        // Проверяем, изменилось ли разрешение экрана
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            // Если изменилось, пересчитываем тайлинг
            UpdateTiling();
        }

        // Продолжаем двигать текстуру
        if (_image != null && _image.texture != null)
        {
            Rect currentUVRect = _image.uvRect;
            currentUVRect.x += scrollSpeed.x * Time.deltaTime;
            currentUVRect.y += scrollSpeed.y * Time.deltaTime;
            _image.uvRect = currentUVRect;
        }
    }

    private void UpdateTiling()
    {
        if (_image == null || _image.texture == null) return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        float screenWidth = _image.rectTransform.rect.width;
        float screenHeight = _image.rectTransform.rect.height;

        float screenAspect = screenWidth / screenHeight;

        float tileY = verticalTiles;

        float tileX = verticalTiles * screenAspect;

        Rect newUVRect = _image.uvRect;
        newUVRect.size = new Vector2(tileX, tileY);
        _image.uvRect = newUVRect;
        
        //Debug.Log($"Tiling updated: {tileX} x {tileY}");
    }
}