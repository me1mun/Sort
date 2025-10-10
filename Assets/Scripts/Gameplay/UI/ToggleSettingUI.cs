using UnityEngine;
using UnityEngine.UI;

public abstract class ToggleSettingUI : BaseSettingUI
{
    [Header("UI References")]
    [SerializeField] protected UIButton actionButton;
    [SerializeField] protected Image stateImage;
    [SerializeField] protected Sprite onSprite;
    [SerializeField] protected Sprite offSprite;

    public override void Initialize(SettingsManager settingsManager)
    {
        base.Initialize(settingsManager);
        if (actionButton != null)
        {
            actionButton.OnClick.AddListener(OnAction);
        }
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.OnClick.RemoveListener(OnAction);
        }
        UnsubscribeFromEvents();
    }

    public override void UpdateVisuals()
    {
        if (stateImage != null)
        {
            stateImage.sprite = GetCurrentState() ? onSprite : offSprite;
        }
    }

    protected virtual void OnAction()
    {
        bool newState = !GetCurrentState();
        SetState(newState);
    }
    
    protected abstract bool GetCurrentState();
    protected abstract void SetState(bool newState);
    protected abstract void SubscribeToEvents();
    protected abstract void UnsubscribeFromEvents();
}