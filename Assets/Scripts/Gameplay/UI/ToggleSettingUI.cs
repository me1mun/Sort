using UnityEngine;
using UnityEngine.UI;

public abstract class ToggleSettingUI : BaseSettingUI
{
    [Header("UI References")]
    [SerializeField] protected UIButton actionButton;
    [SerializeField] protected Image stateImage;

    // Несериализуемые цвета
    private readonly Color onColor = Color.white;
    private readonly Color offColor = new Color(0f, 0f, 0f, 0.5f);

    public override void Initialize(SettingsManager settingsManager)
    {
        base.Initialize(settingsManager);

        if (actionButton != null)
            actionButton.OnClick.AddListener(OnAction);

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.OnClick.RemoveListener(OnAction);

        UnsubscribeFromEvents();
    }

    public override void UpdateVisuals()
    {
        if (stateImage != null)
        {
            bool isOn = GetCurrentState();
            stateImage.color = isOn ? onColor : offColor;
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
