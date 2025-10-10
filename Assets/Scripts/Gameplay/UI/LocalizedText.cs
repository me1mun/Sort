using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _localizationKey;

    private void OnEnable()
    {
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    private void UpdateText()
    {
        var tmpComponent = GetComponent<TextMeshProUGUI>();
        string translatedText = LocalizationManager.Instance.Get(_localizationKey);
        
        if (tmpComponent != null) tmpComponent.text = translatedText;
    }
}