using UnityEngine;
using UnityEngine.UI;

public abstract class ToggleSettingUI : BaseSettingUI
{
    [Header("UI References")]
    [SerializeField] protected Button actionButton;
    [SerializeField] protected Image stateImage;
    [SerializeField] protected Sprite onSprite;
    [SerializeField] protected Sprite offSprite;

    public override void Initialize(DataManager dataManager)
    {
        base.Initialize(dataManager);
        actionButton.onClick.AddListener(OnAction);
    }

    public override void UpdateVisuals()
    {
        stateImage.sprite = GetCurrentState() ? onSprite : offSprite;
    }

    private void OnAction()
    {
        bool newState = !GetCurrentState();
        SetState(newState);
        ApplySetting(newState);
        DataManagerInstance.SaveSettings();
        UpdateVisuals();
    }
    
    protected abstract bool GetCurrentState();
    protected abstract void SetState(bool newState);
    protected abstract void ApplySetting(bool isOn);
}