using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Initializing, InMenu, Playing, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

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
    }

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        CurrentState = GameState.Initializing;
        yield return null;
        
        SceneManager.LoadScene("Gameplay");
        CurrentState = GameState.Playing;
    }
    
    public void UpdateGameState(GameState newState)
    {
        CurrentState = newState;
    }
}