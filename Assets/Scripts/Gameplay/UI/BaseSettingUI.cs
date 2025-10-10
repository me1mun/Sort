using UnityEngine;

public abstract class BaseSettingUI : MonoBehaviour
{
    protected SettingsManager SettingsManagerInstance { get; private set; }

    public virtual void Initialize(SettingsManager settingsManager)
    {
        SettingsManagerInstance = settingsManager;
    }

    public abstract void UpdateVisuals();
}