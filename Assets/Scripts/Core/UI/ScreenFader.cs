using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    
    [Header("Fade Configurations")]
    [SerializeField] private FadeSettings fadeInSettings;
    [SerializeField] private FadeSettings fadeOutSettings;
    
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            Debug.LogError("ScreenFader Error: No CanvasGroup component found on this object or its children. Please add one.", this.gameObject);
            return;
        }

        canvasGroup.alpha = 0f;
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        if (canvasGroup == null) return;
        StartCoroutine(FadeAndLoadSceneRoutine(sceneIndex));
    }

    private IEnumerator FadeAndLoadSceneRoutine(int sceneIndex)
    {
        yield return StartCoroutine(Fade(1f, fadeOutSettings));
        SceneManager.LoadScene(sceneIndex);
        yield return StartCoroutine(Fade(0f, fadeInSettings));
    }

    private IEnumerator Fade(float targetAlpha, FadeSettings settings)
    {
        //canvasGroup.blocksRaycasts = true;
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < settings.duration)
        {
            float linearProgress = timer / settings.duration;
            float curvedProgress = settings.curve.Evaluate(linearProgress);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curvedProgress);
            
            timer += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        //canvasGroup.blocksRaycasts = (targetAlpha > 0);
    }
}

[System.Serializable]
public class FadeSettings
{
    public float duration = 0.5f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}