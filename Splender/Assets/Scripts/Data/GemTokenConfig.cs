using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GemTokenConfig", menuName = "Config/GemTokenConfig")]


//보석 데이터 및 플레이어 인원수 데이터 설정
public class GemTokenConfig : ScriptableObject
{
    [System.Serializable]
    public struct GemInitialData
    {
        public GemType type;
        public int initialCount;
    }

    [SerializeField] private int playerCount;
    [SerializeField] private GemInitialData[] gemData;

    public int PlayerCount => playerCount;
    public IReadOnlyList<GemInitialData> GemData => gemData;

    // Configs/Players.asset 파일 내 데이터를 통해 보석 타입별 개수값 초기 세팅
    public int GetInitialCount(GemType type)
    {
        foreach (var data in gemData)
            if (data.type == type) return data.initialCount;
        return 0;
    }
}
