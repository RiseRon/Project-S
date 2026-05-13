using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 데이터 정렬(OrderBy)을 위해 필요

public class WaveManager : MonoBehaviour
{
    // 어디서든 접근 가능한 싱글톤 인스턴스
    public static WaveManager Instance { get; private set; }

    [Header("Wave Data Settings")]
    // 자동으로 로드된 웨이브 데이터들이 저장될 리스트
    [SerializeField] private List<SO_WaveData> stageWaves = new List<SO_WaveData>();

    private int currentWaveIndex = 0;   // 현재 진행 중인 웨이브 번호 (리스트 인덱스)
    private bool isWaveActive = false;  // 현재 웨이브가 동작 중인지 여부
    public float CurrentWaitTime { get; private set; } // 현재 남은 시간
    public float TotalWaitTime { get; private set; }  // 전체 설정된 대기 시간
    public bool IsWaitingNextWave { get; private set; } // 대기 중인지 여부

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 게임 시작 시 Resources 폴더에서 데이터를 자동으로 불러옴
        LoadWaveDataFromResources();
    }

    private void Start()
    {
        // 첫 번째 웨이브 실행
        StartNextWave();
    }

    /// <summary>
    /// Resources/WaveData 폴더에 있는 모든 SO_WaveData를 로드하고 정렬합니다.
    /// </summary>
    private void LoadWaveDataFromResources()
    {
        // 1. Resources 폴더 내 해당 경로의 모든 SO_WaveData 타입 에셋 로드
        SO_WaveData[] loadedArray = Resources.LoadAll<SO_WaveData>("WaveData");

        if (loadedArray == null || loadedArray.Length == 0)
        {
            Debug.LogError("[WaveManager] Resources/WaveData 폴더에 데이터 에셋이 없습니다! 경로를 확인하세요.");
            return;
        }

        // 2. 스테이지 ID와 웨이브 ID 순서대로 정렬하여 리스트에 담기 (LINQ 사용)
        stageWaves = loadedArray
            .OrderBy(w => w.stageID)
            .ThenBy(w => w.waveID)
            .ToList();

        Debug.Log($"[WaveManager] {stageWaves.Count}개의 웨이브 데이터를 성공적으로 로드했습니다.");
    }

    /// <summary>
    /// 다음 웨이브를 실행하는 공용 함수
    /// </summary>
    public void StartNextWave()
    {
        // 이미 진행 중이거나 모든 웨이브가 끝났으면 중단
        if (isWaveActive || currentWaveIndex >= stageWaves.Count)
        {
            if (currentWaveIndex >= stageWaves.Count)
                Debug.Log("모든 웨이브가 완료되었습니다!");
            return;
        }

        // 현재 순서의 데이터를 전달하여 루틴 시작
        StartCoroutine(WaveRoutine(stageWaves[currentWaveIndex]));
    }

    private IEnumerator WaveRoutine(SO_WaveData data)
    {
        isWaveActive = true;
        IsWaitingNextWave = true;

        // 웨이브 시작 전 대기 (waitingTime)를 UI로 표현하기 위해 로직 변경
        TotalWaitTime = data.waitingTime;
        CurrentWaitTime = TotalWaitTime;

        Debug.Log($"[웨이브 {data.waveID}] 시작 전 대기 중...");

        // 단순히 yield return new WaitForSeconds 대신 시간을 깎으며 대기
        while (CurrentWaitTime > 0)
        {
            CurrentWaitTime -= Time.deltaTime;
            yield return null; // 매 프레임 대기
        }
        CurrentWaitTime = 0; // 정확히 0으로 맞춤
        IsWaitingNextWave = false; // 대기 종료 (전투 시작)

        Debug.Log($"[웨이브 {data.waveID}] 전투 개시!");

        // 2. 적 소환 (Enemy List 순회)
        foreach (var group in data.enemyList)
        {
            // 각 몬스터 그룹의 마릿수만큼 소환
            for (int i = 0; i < group.spawnCount; i++)
            {
                // Enemy.cs의 HP 계산 방식과 맞추기 위해 CSV 수치(0, 10, 20...)를 그대로 넘김
                SpawnEnemy(group.enemyID, (float)data.hpGrowthRate);

                // 마리당 소환 간격 대기
                yield return new WaitForSeconds(group.spawnInterval);
            }

            // 그룹 간 대기 시간 (nextGroupCycle 사용)
            Debug.Log($"[웨이브 {data.waveID}] 다음 그룹 소환까지 대기 ({data.nextGroupCycle}초)");
            yield return new WaitForSeconds(data.nextGroupCycle);
        }

        // 3. 웨이브 제한 시간 동안 대기 (waveTime 사용)
        // 적을 다 뽑은 후 웨이브가 유지되는 시간입니다.
        yield return new WaitForSeconds(data.waveTime);

        // 4. 웨이브 종료 처리
        EndWave(data);
    }

    /// <summary>
    /// SpawnManager를 통해 실제로 적을 씬에 등장시키는 함수
    /// </summary>
    private void SpawnEnemy(int id, float hpGrowthRate)
    {
        if (SpawnManager.Instance != null)
        {
            // hpGrowthRate는 10, 20 같은 정수값으로 전달됨 (Enemy.cs에서 백분율 계산)
            SpawnManager.Instance.Spawn(id, hpGrowthRate);
        }
        else
        {
            Debug.LogError("SpawnManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 웨이브 종료 시 보상 지급 및 다음 준비
    /// </summary>
    private void EndWave(SO_WaveData data)
    {
        Debug.Log($"[웨이브 {data.waveID}] 종료. 보상을 확인합니다.");

        // 보상 지급 로직 실행
        GiveRewards(data);

        isWaveActive = false;
        currentWaveIndex++; // 인덱스 증가

        // 자동 다음 웨이브 시작
        StartNextWave();
    }

    /// <summary>
    /// SO 데이터에 정의된 보상을 플레이어에게 지급합니다.
    /// </summary>
    private void GiveRewards(SO_WaveData data)
    {
        // 1. 아이템/재화 보상
        if (data.rewardID <= 0 || data.rewardAmount <= 0) return;

        // int ID를 Enum으로 형변환
        CurrencyType rewardType = (CurrencyType)data.rewardID;

        if (rewardType == CurrencyType.FragmentCoin || rewardType == CurrencyType.CompleteCoin)
        {
            CurrencyManager.Instance.AddCurrency(rewardType, data.rewardAmount);
        }

        // 2. 슬롯 이동 횟수 회복
        if (data.slotMoveRecovery > 0)
        {
            Debug.Log($"슬롯 이동 횟수 {data.slotMoveRecovery}회 회복!");
            // 예: PlayerStatus.Instance.RecoverMoveCount(data.slotMoveRecovery);
        }
    }
}