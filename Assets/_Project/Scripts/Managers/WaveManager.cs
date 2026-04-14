using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Data")]
    [SerializeField] private List<SO_WaveData> stageWaves; // 현재 스테이지의 웨이브 리스트

    private int currentWaveIndex = 0;
    private bool isWaveActive = false;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 첫 번째 웨이브 시작
        StartNextWave();
    }

    // 다음 웨이브를 시작시키는 함수
    public void StartNextWave()
    {
        if (isWaveActive) return;

        if (currentWaveIndex < stageWaves.Count)
        {
            StartCoroutine(WaveRoutine(stageWaves[currentWaveIndex]));
        }
        else
        {
            Debug.Log("모든 웨이브 클리어! 스테이지 종료.");
            // StageManager.Instance.OnStageClear(); 
        }
    }

    private IEnumerator WaveRoutine(SO_WaveData data)
    {
        isWaveActive = true;
        Debug.Log($"[웨이브 {data.waveID}] 시작 전 대기: {data.waitTime}초");

        // 1. 웨이브 시작 전 대기 시간 (WaitTime)
        yield return new WaitForSeconds(data.waitTime);

        Debug.Log($"[웨이브 {data.waveID}] 시작! 제한 시간: {data.waveTime}초");

        // 2. 적 소환 루틴 시작 (비동기로 실행하여 타이머와 별개로 움직임)
        Coroutine spawnCoroutine = StartCoroutine(SpawnRoutine(data));

        // 3. 웨이브 타이머 진행 (지정된 WaveTime 만큼 대기)
        // 이 시간 동안 적은 계속 생성되거나 움직입니다.
        yield return new WaitForSeconds(data.waveTime);

        // 4. 웨이브 시간 종료
        Debug.Log($"[웨이브 {data.waveID}] 시간 종료! 보상 지급 및 다음 웨이브 준비.");

        // (선택) 아직 소환 중인 적이 있다면 소환 중단
        // StopCoroutine(spawnCoroutine);

        // 5. 웨이브 클리어 보상 지급
        GiveRewards(data);

        // 6. 다음 웨이브를 위해 상태 초기화 및 인덱스 증가
        isWaveActive = false;
        currentWaveIndex++;

        // 7. 바로 다음 웨이브 루틴 실행 (자동 진행)
        StartNextWave();
    }

    private IEnumerator SpawnRoutine(SO_WaveData data)
    {
        foreach (var group in data.enemyList)
        {
            for (int i = 0; i < group.spawnCount; i++)
            {
                GameObject enemy = SpawnEnemy(group.enemyID, group.hpGrowth);
                if (enemy != null) activeEnemies.Add(enemy);

                // 개별 적 생성 간격 적용
                if (group.spawnInterval > 0)
                    yield return new WaitForSeconds(group.spawnInterval);
            }
            // 다음 종류 적 소환 전 그룹 간 대기
            yield return new WaitForSeconds(data.nextGroupCycle);
        }
    }

    private void GiveRewards(SO_WaveData data)
    {
        // 재화 보상
        if (!string.IsNullOrEmpty(data.rewardID) && data.rewardQuantity > 0)
        {
            Debug.Log($"보상 획득: {data.rewardID} x{data.rewardQuantity}");
            // CurrencyManager.Instance.AddCurrency(data.rewardID, data.rewardQuantity);
        }

        // 슬롯 이동 횟수 회복
        if (data.slotMoveRecovery > 0)
        {
            Debug.Log($"슬롯 이동 횟수 {data.slotMoveRecovery}회 회복");
            // SlotManager.Instance.RecoverMoveCount(data.slotMoveRecovery);
        }
    }

    private GameObject SpawnEnemy(string id, float hpBonus)
    {
        // 실제 스폰 매니저와 연결할 부분 (예시)
        // GameObject enemy = SpawnManager.Instance.GetEnemy(id);
        // enemy.GetComponent<Enemy>().Setup(hpBonus);
        return null;
    }
}