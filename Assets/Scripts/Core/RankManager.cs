using System;
using UnityEngine;

public class RankManager : MonoBehaviour
{
    public static RankManager Instance { get; private set; }
    
    [SerializeField] private RankConfig rankConfig;
    
    public RankConfig Config => rankConfig;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (rankConfig == null)
        {
            Debug.LogError("RankManager: RankConfig is not assigned!");
        }
    }
    
    public (int rankIndex, float progress) GetCurrentRankData()
    {
        if (rankConfig == null || DataManager.Instance == null)
        {
            return (0, 0f);
        }
        
        int completedLevels = DataManager.Instance.Progress.CompletedLevelsCount;
        return rankConfig.CalculateRank(completedLevels);
    }

    public int GetCurrentRankIndex()
    {
        return GetCurrentRankData().rankIndex;
    }
    
    public float GetCurrentProgress()
    {
        return GetCurrentRankData().progress;
    }
    
    public RankData GetCurrentRank()
    {
        return rankConfig?.GetRank(GetCurrentRankIndex());
    }
    
    public RankData GetRank(int rankIndex)
    {
        return rankConfig?.GetRank(rankIndex);
    }
    
    public string GetCurrentRankName()
    {
        return GetRankName(GetCurrentRankIndex());
    }
    
    public string GetRankName(int rankIndex)
    {
        var rank = GetRank(rankIndex);
        if (rank == null) return "Unknown";
        
        if (LocalizationManager.Instance == null) return rank.rankKey;
        return LocalizationManager.Instance.Get($"ranks.{rank.rankKey}");
    }
}