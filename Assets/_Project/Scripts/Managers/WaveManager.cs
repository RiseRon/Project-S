using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    // 어디서든 접근 가능한 싱글톤 인스턴스
    public static WaveManager Instance { get; private set; }

    [Header("Wave Data")]
    // 스테이지에 설정된 웨이브 데이터 리스트 (인스펙터에서 할당)
    [SerializeField] private List<SO_WaveData> stageWaves;

    private int currentWaveIndex = 0; // 현재 진행 중인 웨이브 번호
    private bool isWaveActive = false; // 현재 웨이브가 동작 중인지 여부

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 중복된 매니저 파괴
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 첫 번째 웨이브 실행
        StartNextWave();
    }

    // 다음 웨이브를 실행하는 공용 함수
    public void StartNextWave()
    {
        // 이미 웨이브가 진행 중이거나 모든 웨이브를 끝냈다면 중단
        if (isWaveActive || currentWaveIndex >= stageWaves.Count) return;

        // 현재 인덱스에 맞는 웨이브 데이터로 루틴 시작
        StartCoroutine(WaveRoutine(stageWaves[currentWaveIndex]));
    }

    // 웨이브의 전체 흐름(대기 -> 소환 -> 지속 -> 종료)을 관리하는 코루틴
    private IEnumerator WaveRoutine(SO_WaveData data)
    {
        isWaveActive = true;
        Debug.Log($"[웨이브 {data.waveID}] 준비 중...");

        // 1. 웨이브 시작 전 대기 시간 (WaitTime)
        // 이전 웨이브 종료 후 또는 게임 시작 직후의 여유 시간을 가짐
        yield return new WaitForSeconds(data.waitTime);

        Debug.Log($"[웨이브 {data.waveID}] 소환 시작!");

        // 2. 적 소환 루틴 실행 (실제 소환이 끝날 때까지 기다림)
        yield return StartCoroutine(SpawnRoutine(data));

        // 3. 웨이브 지속 시간 대기 (WaveTime)
        // 소환이 끝난 시점부터 테이블에 정의된 시간만큼 웨이브 유지
        yield return new WaitForSeconds(data.waveTime);

        // 4. 웨이브 종료 처리 및 보상 지급
        EndWave(data);
    }

    // 웨이브 데이터에 정의된 적들을 순서대로 소환하는 코루틴
    private IEnumerator SpawnRoutine(SO_WaveData data)
    {
        // 데이터 테이블에 등록된 적 그룹(EnemyList)을 하나씩 순회
        foreach (var group in data.enemyList)
        {
            // 각 그룹에 설정된 소환 마릿수만큼 반복
            for (int i = 0; i < group.spawnCount; i++)
            {
                // [핵심] 직접 소환하지 않고 SpawnManager에게 요청 (관심사 분리)
                // group.id: 적의 종류(int), group.hpGrowth: 이번 웨이브의 체력 보너스
                SpawnEnemy(group.enemyID, group.hpGrowth);

                // 같은 종류의 적 사이의 생성 간격 적용
                if (group.spawnInterval > 0)
                    yield return new WaitForSeconds(group.spawnInterval);
            }

            // 한 종류의 적 소환이 끝난 후 다음 종류 소환 전까지의 그룹 간 대기 시간 적용
            yield return new WaitForSeconds(data.nextGroupCycle);
        }
    }

    // SpawnManager를 통해 실제로 적을 씬에 등장시키는 함수
    private void SpawnEnemy(int id, float hpBonus)
    {
        // 나중에 생성할 SpawnManager가 씬에 있는지 확인 후 소환 명령 전달
        if (SpawnManager.Instance != null)
        {
            // SpawnManager는 내부적으로 PoolManager를 사용하여 적을 꺼내고 Setup을 호출함
            SpawnManager.Instance.Spawn(id, hpBonus);
        }
        else
        {
            Debug.LogError("SpawnManager를 찾을 수 없습니다! 적 소환에 실패했습니다.");
        }
    }

    // 웨이브가 끝났을 때 호출되는 마무리 함수
    private void EndWave(SO_WaveData data)
    {
        Debug.Log($"[웨이브 {data.waveID}] 종료. 보상을 지급합니다.");

        // 데이터에 설정된 보상(재화, 슬롯 회복 등) 지급
        GiveRewards(data);

        isWaveActive = false;
        currentWaveIndex++; // 다음 웨이브 번호로 증가

        // 일정 시간 후 다음 웨이브가 자동으로 시작되도록 재귀 호출
        StartNextWave();
    }

    // 테이블 수치에 따른 보상 지급 로직
    private void GiveRewards(SO_WaveData data)
    {
        // 재화 보상 (예: 코인)
        if (!string.IsNullOrEmpty(data.rewardID) && data.rewardQuantity > 0)
        {
            Debug.Log($"아이템 보상 획득: {data.rewardID} x{data.rewardQuantity}");
            // CurrencyManager.Instance.AddCurrency(data.rewardID, data.rewardQuantity);
        }

        // 슬롯 이동 횟수 보상
        if (data.slotMoveRecovery > 0)
        {
            Debug.Log($"슬롯 이동 횟수 {data.slotMoveRecovery}회 회복");
            // SlotManager.Instance.RecoverMoveCount(data.slotMoveRecovery);
        }
    }
}