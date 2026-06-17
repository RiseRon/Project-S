using UnityEngine;
using System;
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

    private bool isWaveActive = false;  // 현재 웨이브가 동작 중인지 여부
    public float CurrentWaitTime { get; private set; } // 현재 남은 시간
    public float TotalWaitTime { get; private set; }  // 전체 설정된 대기 시간
    public bool IsWaitingNextWave { get; private set; } // 대기 중인지 여부

    public event Action OnStageVictoryDetected;
    public event Action OnWaveChanged;

    private int currentWaveIndex = 0;   // 현재 진행 중인 웨이브 번호 (리스트 인덱스)
    private int activeEnemies = 0; // 현재 필드에 살아있는 적 수
    private int enemiesToSpawn = 0; // 이번 웨이브에서 더 소환해야 할 적 수
    private Coroutine waveCoroutine;  // 실행 중인 코루틴을 제어하기 위함
    public int CurrentWave => currentWaveIndex + 1;

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
    }
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // GameManager가 WaveManager의 승리 알림 함수를 안전하게 바라보도록 강제 역연결
            this.OnStageVictoryDetected -= GameManager.Instance.HandleStageWin; // (HandleStageWin도 public 변경 필요)
            this.OnStageVictoryDetected += GameManager.Instance.HandleStageWin;

            Debug.Log("<color=lime>[WaveManager]</color> 안전하게 GameManager의 승리 이벤트 채널에 스스로를 등록했습니다!");
        }
    }

    /// <summary>
    /// Resources/WaveData 폴더에 있는 모든 SO_WaveData를 로드하고 정렬합니다.
    /// </summary>
    // 외부(StageManager)에서 스테이지 ID를 받아 로드하도록 변경
    public void LoadWaveDataFromResources(int stageID)
    {
        currentWaveIndex = 0;
        stageWaves.Clear();
        activeEnemies = 0;
        enemiesToSpawn = 0;
        // 1. Resources/WaveData 폴더의 모든 에셋 로드
        SO_WaveData[] loadedArray = Resources.LoadAll<SO_WaveData>("WaveData");

        if (loadedArray == null || loadedArray.Length == 0)
        {
            Debug.LogError("[WaveManager] 데이터 에셋이 없습니다!");
            return;
        }

        // 2. 전달받은 stageID인 데이터만 필터링하고 waveID 순으로 정렬
        stageWaves = loadedArray
            .Where(w => w.stageID == stageID) // <--- 매개변수로 받은 ID 사용
            .OrderBy(w => w.waveID)
            .ToList();

        if (stageWaves.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] stageID {stageID}에 해당하는 데이터가 없습니다.");
        }
        else
        {
            Debug.Log($"[WaveManager] {stageID}번 스테이지의 {stageWaves.Count}개 웨이브 로드 완료.");
        }
    }

    /// <summary>
    /// 다음 웨이브를 실행하는 공용 함수
    /// </summary>
    public void StartNextWave()
    {
        if (stageWaves == null || stageWaves.Count == 0)
        {
            Debug.LogError("[WaveManager] 로드된 웨이브 데이터가 없어 시작할 수 없습니다.");
            return;
        }
        // 이미 진행 중이거나 모든 웨이브가 끝났으면 중단
        if (isWaveActive || currentWaveIndex >= stageWaves.Count)
        {
            if (currentWaveIndex >= stageWaves.Count)
                Debug.Log("모든 웨이브가 완료되었습니다!");
            return;
        }
        if (isWaveActive) return;
        enemiesToSpawn = 0;
        SO_WaveData data = stageWaves[currentWaveIndex];
#if UNITY_EDITOR
        waveRemainingTime = data.waveTime;
#endif
        for (int i = 0;  i < data.enemyList.Count; i++)
        {
            enemiesToSpawn += data.enemyList[i].spawnCount ;  // 이번 웨이브 총 소환 수 설정
        }
        OnWaveChanged?.Invoke();

        // 현재 순서의 데이터를 전달하여 루틴 시작
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(WaveRoutine(data));
    }

    private IEnumerator WaveRoutine(SO_WaveData data)
    {
        isWaveActive = true;
        IsWaitingNextWave = true;

        // 웨이브 시작 전 대기 (waitingTime) 설정
        TotalWaitTime = data.waitingTime;
        CurrentWaitTime = TotalWaitTime;

        Debug.Log($"[웨이브 {data.waveID}] 시작 전 대기 중...");

        // 시간을 깎으며 대기
        while (CurrentWaitTime > 0)
        {
            CurrentWaitTime -= Time.deltaTime;
            yield return null;
        }
        CurrentWaitTime = 0;
        IsWaitingNextWave = false; // 전투 시작

        Debug.Log($"[웨이브 {data.waveID}] 전투 개시!");

        // 2. 적 소환 (Enemy List 순회)
        foreach (var group in data.enemyList)
        {
            for (int i = 0; i < group.spawnCount; i++)
            {
                // 💡 [데이터 전달] SpawnManager에 ID, 체력 계수, 그리고 현재 웨이브의 경로 인덱스를 온전히 전달합니다.
                SpawnEnemy(group.enemyID, (float)data.hpGrowthRate, group.pathIndex);

                // 마리당 소환 간격 대기
                yield return new WaitForSeconds(group.spawnCycle);
            }
            if (group != data.enemyList.Last())
            {
                Debug.Log($"[웨이브 {data.waveID}] 다음 그룹 소환까지 대기 ({data.nextGroupCycle}초)");
                yield return new WaitForSeconds(data.nextGroupCycle);
            }
        }

        bool isLastWave = (currentWaveIndex == stageWaves.Count - 1);

        // 적을 다 잡지 못했더라도 waveTime 제한 시간이 지나면 다음 루틴으로 유예 대기
        float timer = 0;
        while (timer < data.waveTime)
        {
            timer += Time.deltaTime;

            // 만약 시간 종료 전에 적을 다 소탕했다면 조기 탈출
            if (activeEnemies <= 0 && enemiesToSpawn <= 0)
            {
                Debug.Log("시간 종료 전 모든 적 처치 완료!");
                break;
            }

            yield return null;
        }

        if (isLastWave)
        {
            Debug.Log("<color=red>[WaveManager]</color> 마지막 웨이브의 스폰 단계가 끝났습니다! 남은 적 소탕 시작.");
            isWaveActive = false;

            // 혹시 스폰 제한 시간이 끝났을 때 이미 필드에 적이 전멸해있다면 즉시 승리 처리
            if (activeEnemies <= 0)
            {
                PublishVictory();
            }
        }
        else
        {
            // [일반 웨이브일 때] 다음 웨이브 단계로 전환
            EndWave();
        }
    }

    /// <summary>
    /// SpawnManager를 통해 실제로 적을 씬에 등장시키는 함수
    /// </summary>
    private void SpawnEnemy(int id, float hpGrowthRate, int pathIndex)
    {
        if (SpawnManager.Instance != null)
        {
            // 💡 [수정] SpawnManager에게 id, hpGrowthRate와 함께 SO에서 추출한 pathIndex도 같이 넘겨줍니다.
            SpawnManager.Instance.Spawn(id, hpGrowthRate, pathIndex);

            activeEnemies++; // 생존 적 수 증가
            enemiesToSpawn--; // 남은 소환 횟수 감소
        }
        else
        {
            Debug.LogError("SpawnManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 웨이브 종료 시 보상 지급 및 다음 준비
    /// </summary>
    private void EndWave()
    {
        if (!isWaveActive) return;

        Debug.Log($"[웨이브 {stageWaves[currentWaveIndex].waveID}] 종료.");

        if (currentWaveIndex < stageWaves.Count)
        {
            GiveRewards(stageWaves[currentWaveIndex]);
        }

        isWaveActive = false;
        currentWaveIndex++;
        OnWaveChanged?.Invoke();

        // 코루틴 정리
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        StartNextWave();
    }
    public void AddActiveEnemy(int amount) { activeEnemies += amount; }
    public void OnEnemyDefeated()
    {
        activeEnemies--;

        if (activeEnemies < 0) activeEnemies = 0;

        // 모든 적을 다 소환했고, 필드에 적이 하나도 없다면
        if (activeEnemies <= 0 && enemiesToSpawn <= 0 && isWaveActive)
        {
            EndWave();
        }
        // 마지막 웨이브 소탕전 중 최종 적이 죽었을 때
        if (stageWaves.Count > 0 && currentWaveIndex >= stageWaves.Count && activeEnemies <= 0)
        {
            PublishVictory();
        }
    }
    private void PublishVictory()
    {
        isWaveActive = false;
        Debug.Log("<color=lime>[WaveManager]</color> 승리 조건 달성! 전 세상에 알림을 보냅니다.");

        // 이 이벤트를 라디오 주파수처럼 쏘아 올립니다. 듣고 있는 자(구독자)들이 반응합니다.
        OnStageVictoryDetected?.Invoke();
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

        // 💡 배리어 체력 기반 추가 보상 계산
        int finalAmount = data.rewardAmount;

        if (Barrier.Instance != null)
        {
            // 현재 배리어 체력 비율 계산 (0% ~ 100%)
            // ※ Barrier 클래스에 CurrentHP와 MaxHP(혹은 InitHP)가 있다고 가정합니다.
            // ※ 만약 프로퍼티 이름이 다르다면 프로젝트에 맞게 수정해주세요 (예: currentHealth 등).
            float hpPercent = (Barrier.Instance.CurrentHealth / Barrier.Instance.MaxHealth) * 100f;

            // 체력이 80% 이상일 때만 보너스 연산 진행
            if (hpPercent >= 80f)
            {
                // 80%를 초과한 수치 계산 (예: 85%면 5%, 100%면 20%)
                float excessPercent = hpPercent - 80f;

                // 최대 20%로 제한 (Clamping)
                float bonusRatePercent = Mathf.Clamp(excessPercent, 0f, 20f);

                // 소수점 버림 처리하여 최종 정수 보너스 퍼센트 확정
                int bonusRate = Mathf.FloorToInt(bonusRatePercent);

                if (bonusRate > 0)
                {
                    // 원래 보상에 (1 + 보너스 비율)을 곱해줍니다. 
                    // 예: bonusRate가 20이면 1 + 0.2 = 1.2배
                    finalAmount = Mathf.RoundToInt(data.rewardAmount * (1f + (bonusRate / 100f)));
                    Debug.Log($"<color=lime>[보상 보너스]</color> 배리어 체력 {hpPercent:F1}% (80% 이상)! 보상 {bonusRate}% 증가 (최종: {finalAmount})");
                }
            }
        }

        // 계산된 finalAmount로 재화 지급
        if (rewardType == CurrencyType.FragmentCoin || rewardType == CurrencyType.CompleteCoin)
        {
            CurrencyManager.Instance.AddCurrency(rewardType, finalAmount);
        }

        // 2. 슬롯 이동 횟수 회복
        if (data.slotMoveRecovery > 0)
        {
            Debug.Log($"슬롯 이동 횟수 {data.slotMoveRecovery}회 회복!");
            PlacementController.Instance.remainingMoves += data.slotMoveRecovery;
        }
    }
#if UNITY_EDITOR
    public int ActiveEnemyCount => activeEnemies;
    private float waveRemainingTime = 0f; // 웨이브 남은 시간 측정용
    public float WaveRemainingTime => waveRemainingTime;

    private void Update()
    {
        // 전투 중일 때만 시간을 감소시킴 (IsWaitingNextWave가 아닐 때)
        if (isWaveActive && !IsWaitingNextWave && waveRemainingTime > 0)
        {
            waveRemainingTime -= Time.deltaTime;
            if (waveRemainingTime < 0) waveRemainingTime = 0;
        }
    }
    public void ForceSkipToNextWave()
    {
        // 1. 현재 실행 중인 웨이브 코루틴 중단 (소환/대기 등)
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // 2. 웨이브 상태 플래그 초기화
        isWaveActive = false;
        IsWaitingNextWave = false;

        // 3. 인덱스 증가
        currentWaveIndex++;

        // 4. 다음 웨이브 실행 (더 이상 데이터가 없으면 종료 로그)
        if (currentWaveIndex < stageWaves.Count)
        {
            Debug.Log($"<color=cyan>[WaveManager]</color> 웨이브 {currentWaveIndex + 1}로 강제 이동합니다.");
            StartNextWave();
        }
        else
        {
            Debug.Log("<color=red>[WaveManager]</color> 마지막 웨이브입니다. 더 이상 스킵할 수 없습니다.");
            if (stageWaves.Count > 0 && currentWaveIndex >= stageWaves.Count && activeEnemies <= 0)
            {
                PublishVictory();
            }
        }
    }
#endif
}