using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
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
        popupRoot.SetActive(false);
    }

    public void Open()
    {
        popupRoot.SetActive(true);
        foreach (var setting in _settings)
        {
            setting.UpdateVisuals();
        }
    }

    public void Close()
    {
        popupRoot.SetActive(false);
    }
}