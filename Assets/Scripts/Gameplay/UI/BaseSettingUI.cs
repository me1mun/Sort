using UnityEngine;

public abstract class BaseSettingUI : MonoBehaviour
{
    protected DataManager DataManagerInstance { get; private set; }

    public virtual void Initialize(DataManager dataManager)
    {
        DataManagerInstance = dataManager;
    }

    public abstract void UpdateVisuals();
}