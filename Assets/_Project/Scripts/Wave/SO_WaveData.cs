using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnGroup
{
    public int enemyID;           // 적 데이터 ID
    public int spawnCount;        // 소환 마릿수
    public float spawnInterval;   // 소환 간격
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData")]
public class SO_WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public int stageID;            // 스테이지 ID
    public int waveID;            // 웨이브 ID
    public int waveTime;          // 시작 전 대기 시간
    public int waitingTime;        // 웨이브 제한 시간
    public int nextGroupCycle;    // 그룹 간 대기 시간

    [Header("적 채력 상승량")]
    public int hpGrowthRate;      // 체력 배율 (csv: 체력_배율)

    [Header("웨이브 보상")]
    public int rewardID;          // 보상 ID
    public int rewardAmount;      // 보상 수량
    public int slotMoveRecovery;  // 슬롯 이동 회복량

    [Header("적 리스트")]
    public List<EnemySpawnGroup> enemyList = new List<EnemySpawnGroup>();
}