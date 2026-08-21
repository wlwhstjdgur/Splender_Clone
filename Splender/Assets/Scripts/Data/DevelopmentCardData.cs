using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DevelopmentCardData
{
    public int id;
    public int level;              // 1, 2, 3
    public GemType bonus;          // 이 카드가 주는 보너스 색상
    public int victoryPoints;      // 0~5

    // 비용은 Gold를 제외한 5색만 사용
    public int costDiamond;
    public int costSapphire;
    public int costEmerald;
    public int costRuby;
    public int costOnyx;

    public Dictionary<GemType, int> GetCostDictionary()
    {
        var dict = new Dictionary<GemType, int>();
        if (costDiamond > 0) dict[GemType.Diamond] = costDiamond;
        if (costSapphire > 0) dict[GemType.Sapphire] = costSapphire;
        if (costEmerald > 0) dict[GemType.Emerald] = costEmerald;
        if (costRuby > 0) dict[GemType.Ruby] = costRuby;
        if (costOnyx > 0) dict[GemType.Onyx] = costOnyx;
        return dict;
    }
}
