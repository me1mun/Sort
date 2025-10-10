using System;
using UnityEngine;

public abstract class AdUnit : ScriptableObject
{
    [SerializeField]
    private string adUnitName;
    public string AdUnitName => adUnitName;

    public abstract void LoadAd();
    public abstract void ShowAd(Action onAdCompleted);
    public abstract bool IsAdLoaded();
}