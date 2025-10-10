using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdService : MonoBehaviour
{
    public static AdService Instance { get; private set; }

    [SerializeField]
    private List<AdUnit> adUnits;

    private Dictionary<string, AdUnit> adUnitMap;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        adUnitMap = adUnits.ToDictionary(unit => unit.AdUnitName, unit => unit);
    }

    void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            foreach (var adUnit in adUnits)
            {
                adUnit.LoadAd();
            }
        });
    }

    public void ShowAd(string adUnitName, Action onAdCompleted)
    {
        if (adUnitMap.TryGetValue(adUnitName, out AdUnit adUnit))
        {
            adUnit.ShowAd(onAdCompleted);
        }
        else
        {
            Debug.LogError($"Ad unit with name '{adUnitName}' not found.");
        }
    }

    public bool IsAdLoaded(string adUnitName)
    {
        if (adUnitMap.TryGetValue(adUnitName, out AdUnit adUnit))
        {
            return adUnit.IsAdLoaded();
        }
        return false;
    }
}