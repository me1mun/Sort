using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
        if (SceneManager.GetActiveScene().name != "Gameplay")
        {
            SceneManager.LoadScene("Gameplay");
        }
    }
}