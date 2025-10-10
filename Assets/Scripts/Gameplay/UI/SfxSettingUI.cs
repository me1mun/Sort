public class SfxSettingUI : ToggleSettingUI
{
    protected override bool GetCurrentState() => SettingsManagerInstance.IsSfxOn;
    protected override void SetState(bool newState) => SettingsManagerInstance.SetSfxOn(newState);
    
    protected override void SubscribeToEvents()
    {
        if(SettingsManagerInstance != null)
            SettingsManagerInstance.OnSfxSettingChanged += (isOn) => UpdateVisuals();
    }

    protected override void UnsubscribeFromEvents()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManagerInstance.OnSfxSettingChanged -= (isOn) => UpdateVisuals();
        }
    }

    protected override void OnAction()
    {
        base.OnAction();

        if (GetCurrentState())
        {
            AudioManager.Instance.Play("Tap");
        }
    }
}