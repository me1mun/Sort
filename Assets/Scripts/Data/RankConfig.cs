using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RankConfig", menuName = "Game/Rank Config")]
public class RankConfig : ScriptableObject
{
    [Header("Rank Progression")]
    [Tooltip("All ranks in order from lowest to highest")]
    public List<RankData> ranks = new List<RankData>();
    
    public int GetTotalRanks() => ranks?.Count ?? 0;
    
    public RankData GetRank(int rankIndex)
    {
        if (ranks == null || rankIndex < 0 || rankIndex >= ranks.Count)
            return null;
        return ranks[rankIndex];
    }
    
    public bool IsMaxRank(int rankIndex) => rankIndex >= GetTotalRanks() - 1;

    public (int rankIndex, float progress) CalculateRank(int completedLevelsCount)
    {
        if (ranks == null || ranks.Count == 0)
        {
            return (0, 0f);
        }

        int currentRankIndex = 0;
        int levelsRequiredForCurrentRank = 0;
        int totalLevelsToReachThisRank = 0;

        for (int i = 0; i < ranks.Count; i++)
        {
            currentRankIndex = i;
            var rankData = ranks[i];
            if (rankData == null) continue;

            levelsRequiredForCurrentRank = rankData.levelsRequired;
            if (levelsRequiredForCurrentRank <= 0)
            {
                levelsRequiredForCurrentRank = 1;
            }

            bool isMaxRank = IsMaxRank(i);

            if (isMaxRank || completedLevelsCount < totalLevelsToReachThisRank + levelsRequiredForCurrentRank)
            {
                int levelsInThisRank = completedLevelsCount - totalLevelsToReachThisRank;
                float progress = (float)levelsInThisRank / levelsRequiredForCurrentRank;
                return (currentRankIndex, Mathf.Clamp01(progress));
            }

            totalLevelsToReachThisRank += levelsRequiredForCurrentRank;
        }
        
        return (GetTotalRanks() - 1, 1f);
    }
}