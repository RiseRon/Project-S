using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnGroup
{
    public int enemyID;      // 적 데이터 테이블의 ID (Basic, Tank 등)
    public int spawnCount;      // 소환 마릿수
    public float spawnInterval; // 같은 종류의 적 사이의 소환 간격
    public float hpGrowth;      // 해당 웨이브의 체력 상승치
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData")]
public class SO_WaveData : ScriptableObject
{
    [Header("Wave Info")]
    public string waveID;
    public float waveTime;       // 웨이브 제한 시간
    public float waitTime;       // 웨이브 시작 전 대기 시간
    public float nextGroupCycle; // 한 종류의 적 소환 후 다음 종류까지의 대기 시간

    [Header("Wave Rewards")]
    public string rewardID;      // 보상 재화 ID (예: Coin_1)
    public int rewardQuantity;   // 보상 수량
    public int slotMoveRecovery; // 슬롯 이동 횟수 회복량

    [Header("Enemy List")]
    public List<EnemySpawnGroup> enemyList = new List<EnemySpawnGroup>();
}