using UnityEngine;

public class LevelCompletePopup : MonoBehaviour
{
    [SerializeField] private GameplayController gameplayController;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    
    // Этот метод вызывается кнопкой "Продолжить"
    public void OnContinueClicked()
    {
        // Просто просим GameplayController начать загрузку следующего уровня
        if (gameplayController != null)
        {
            gameplayController.LoadNextLevel();
        }
        else
        {
            Debug.LogError("GameplayController не назначен в инспекторе LevelCompletePopup!");
        }
    }
}