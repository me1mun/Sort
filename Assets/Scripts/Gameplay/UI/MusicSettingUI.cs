public class MusicSettingUI : ToggleSettingUI
{
    protected override bool GetCurrentState() => DataManagerInstance.Settings.isMusicOn;

    protected override void SetState(bool newState) => DataManagerInstance.Settings.isMusicOn = newState;

    protected override void ApplySetting(bool isOn) => AudioManager.Instance.SetMusicVolume(isOn ? 1.0f : 0.0f);
}