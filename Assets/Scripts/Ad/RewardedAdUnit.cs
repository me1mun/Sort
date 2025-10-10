using System;
using UnityEngine;
using GoogleMobileAds.Api;

[CreateAssetMenu(fileName = "RewardedAdUnit", menuName = "Ad Units/Rewarded Ad Unit")]
public class RewardedAdUnit : AdUnit
{
    [SerializeField]
    private string androidAdUnitId;

    [SerializeField]
    private string iosAdUnitId;

    private RewardedAd rewardedAd;

    public override bool IsAdLoaded()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }

    public override void LoadAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        string idToLoad;

        #if UNITY_EDITOR
            idToLoad = "ca-app-pub-3940256099942544/5224354917";
        #elif UNITY_ANDROID
            idToLoad = androidAdUnitId;
        #elif UNITY_IPHONE
            idToLoad = iosAdUnitId;
        #else
            idToLoad = "unused";
        #endif

        var adRequest = new AdRequest();
        RewardedAd.Load(idToLoad, adRequest, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Rewarded ad failed to load an ad with error: " + error);
                return;
            }
            rewardedAd = ad;
        });
    }

    public override void ShowAd(Action onAdCompleted)
    {
        if (IsAdLoaded())
        {
            rewardedAd.Show((Reward reward) =>
            {
                onAdCompleted?.Invoke();
            });
        }
        else
        {
            Debug.Log("Rewarded ad is not ready yet.");
        }
        LoadAd();
    }
}