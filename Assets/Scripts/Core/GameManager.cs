using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<Vector2> OnScreenSizeChanged;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Gameplay")
        {
            SceneManager.LoadScene("Gameplay");
        }
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            InvokeScreenSizeEvent();
        }
    }

    private void InvokeScreenSizeEvent()
    {
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        OnScreenSizeChanged?.Invoke(new Vector2(_lastScreenWidth, _lastScreenHeight));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InvokeScreenSizeEvent();
    }
}