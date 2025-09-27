using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    private BaseSettingUI[] _settings;

    private void Awake()
    {
        _settings = GetComponentsInChildren<BaseSettingUI>(true);
        foreach (var setting in _settings)
        {
            setting.Initialize(DataManager.Instance);
        }
        
        closeButton.onClick.AddListener(Close);
        //gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        //gameObject.SetActive(true);
        foreach (var setting in _settings)
        {
            setting.UpdateVisuals();
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}