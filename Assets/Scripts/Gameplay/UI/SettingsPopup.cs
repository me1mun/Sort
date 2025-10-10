using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private UIButton closeButton;

    private BaseSettingUI[] _settings;

    private void Awake()
    {
        _settings = GetComponentsInChildren<BaseSettingUI>(true);
        foreach (var setting in _settings)
        {
            setting.Initialize(SettingsManager.Instance);
        }
        
        if (closeButton != null)
        {
            closeButton.OnClick.AddListener(Close);
        }
        //gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.OnClick.RemoveListener(Close);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        foreach (var setting in _settings)
        {
            setting.UpdateVisuals();
        }
    }

    public void Close()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }
        gameObject.SetActive(false);
    }
}