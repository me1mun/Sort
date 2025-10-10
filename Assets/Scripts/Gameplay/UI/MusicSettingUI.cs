public class MusicSettingUI : ToggleSettingUI
{
    protected override bool GetCurrentState() => SettingsManagerInstance.IsMusicOn;
    protected override void SetState(bool newState) => SettingsManagerInstance.SetMusicOn(newState);

    protected override void SubscribeToEvents()
    {
        if(SettingsManagerInstance != null)
            SettingsManagerInstance.OnMusicSettingChanged += (isOn) => UpdateVisuals();
    }

    protected override void UnsubscribeFromEvents()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManagerInstance.OnMusicSettingChanged -= (isOn) => UpdateVisuals();
        }
    }
}