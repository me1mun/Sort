using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    private void Awake()
    {
        // При старте сцены экран должен быть полностью черным,
        // чтобы GameplayController мог запустить анимацию появления.
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        fadeImage.gameObject.SetActive(true);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0f, 1f);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        
        Color currentColor = fadeColor;

        while (timer < fadeDuration)
        {
            float progress = timer / fadeDuration;
            currentColor.a = Mathf.Lerp(startAlpha, endAlpha, progress);
            fadeImage.color = currentColor;
            
            timer += Time.deltaTime;
            yield return null;
        }

        currentColor.a = endAlpha;
        fadeImage.color = currentColor;
        
        if (endAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}