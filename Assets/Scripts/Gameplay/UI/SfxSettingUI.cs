public class SfxSettingUI : ToggleSettingUI
{
    protected override bool GetCurrentState() => DataManagerInstance.Settings.isSfxOn;

    protected override void SetState(bool newState) => DataManagerInstance.Settings.isSfxOn = newState;

    protected override void ApplySetting(bool isOn) => AudioManager.Instance.SetSoundVolume(isOn ? 1.0f : 0.0f);
}