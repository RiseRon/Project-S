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
            if (group != data.enemyList.Last())
            {
                // 그룹 간 대기 시간 (nextGroupCycle 사용)
                Debug.Log($"[웨이브 {data.waveID}] 다음 그룹 소환까지 대기 ({data.nextGroupCycle}초)");
                yield return new WaitForSeconds(data.nextGroupCycle);
            }
            
        }
        bool isLastWave = (currentWaveIndex == stageWaves.Count - 1);
        // 적을 다 잡지 못했더라도 이 시간이 지나면 EndWave로 넘어갑니다.
        float timer = 0;
        while (timer < data.waveTime)
        {
            timer += Time.deltaTime;

            // 만약 시간 내에 적을 다 잡았다면 루프 탈출
            if (activeEnemies <= 0 && enemiesToSpawn <= 0)
            {
                Debug.Log("시간 종료 전 모든 적 처치 완료!");
                break;
            }

            yield return null;
        }
        if (isLastWave)
        {
            // [마지막 웨이브일 때]
            // EndWave()를 호출하지 않고 코루틴을 여기서 종료합니다.
            // 대신 화면에 "FINAL WAVE - 모든 적을 처치하세요!" 같은 UI 문구를 띄우기 아주 좋은 타이밍입니다.
            Debug.Log("<color=red>[WaveManager]</color> 마지막 웨이브의 스폰 단계가 끝났습니다! 남은 적 소탕 시작.");
            isWaveActive = false;

            // 만약 소환 시간이 다 되었을 때 이미 필드에 적이 없다면 즉시 승리 처리
            if (activeEnemies <= 0)
            {
                PublishVictory();
            }
        }
        else
        {
            // [일반 웨이브일 때]
            // 기존처럼 정상적으로 웨이브 종료 문구를 띄우고 보상을 주며 다음 웨이브를 엽니다.
            EndWave();
        }
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