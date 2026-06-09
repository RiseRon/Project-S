using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    private static int currentStageID = -1;

    [Header("Stage Data")]
    private List<SO_StageData> allStageData = new List<SO_StageData>();
    private SO_StageData currentStageData;
    public GameObject spawnedMap;

    [Header("--- Debug Settings ---")]
    [SerializeField] private int testStageID = 501;   // 직접 실행 시 로드할 테스트 스테이지 ID

    [Header("Status")]
    public bool IsStageActive { get; private set; }

    private void Awake()
    {
        // 1. 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 2. 임포터로 생성된 모든 스테이지 데이터 로드 (Resources 폴더 기준)
            LoadAllStageData();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    private void OnEnable()
    {
        // 이벤트 구독 시 기존 구독 해제 후 등록 (중복 방지 안전장치)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void Start()
    {
        // [핵심] 개발자용 직결 시작 로직
        // 로딩 씬을 거치지 않고 스테이지 씬에서 바로 'Play'를 눌렀을 경우를 감지합니다.
        CheckDirectEntry();
    }
    /// <summary>
    /// 로딩 씬을 거치지 않고 현재 스테이지에서 바로 시작했는지 확인합니다.
    /// </summary>
    private void CheckDirectEntry()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 현재 씬 이름이 정확히 "Scene_Stage" 이고, ID가 설정되지 않았다면 직접 시작한 것으로 간주
        if (currentSceneName == "Scene_Stage" && currentStageID == -1)
        {
            Debug.Log($"<color=orange>[StageManager]</color> 스테이지 직결 시작 감지: {currentSceneName}");

            // 1. 테스트용 ID 설정
            currentStageID = testStageID;

            // 2. 풀 매니저가 있다면 즉시 풀링 실행 (기다리지 않고 바로 생성)
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.ImmediatePreWarm();
            }

            // 3. 스테이지 즉시 초기화
            InitStage(currentStageID);
        }
    }

    // [중요] 스테이지 선택 시 ID를 먼저 저장하고 씬을 넘깁니다.
    public void SetNextStage(int id)
    {
        currentStageID = id;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. 로딩 씬이나 메뉴 씬이 아닐 때만 작동
        if (scene.name.StartsWith("Scene_Stage"))
        {
            if (currentStageID != -1 && spawnedMap == null)
            {
                InitStage(currentStageID);
            }
        }
        else if (scene.name != "Scene_Loading")
        {
            currentStageID = -1;
            spawnedMap = null;
            IsStageActive = false;
        }
    }

    private void LoadAllStageData()
    {
        // "Resources/StageData" 폴더 내의 모든 SO_StageData를 가져와 ID 순으로 정렬
        allStageData = Resources.LoadAll<SO_StageData>("StageData")
                                .OrderBy(s => s.stageID)
                                .ToList();
        if (allStageData.Count == 0)
        {
            Debug.LogError("[StageManager] Resources/StageData 폴더에 SO_StageData가 없습니다!");
        }
        else
        {
            Debug.Log($"[StageManager] {allStageData.Count}개의 스테이지 데이터를 로드했습니다.");
        }
    }
    /// <summary>
    /// 스테이지를 초기화하고 맵을 생성합니다.
    /// </summary>
    public void InitStage(int stageID)
    {
        currentStageData = allStageData.Find(s => s.stageID == stageID);

        if (currentStageData == null)
        {
            Debug.LogError($"[StageManager] StageID {stageID} 데이터를 찾을 수 없습니다.");
            return;
        }

        // 기존에 생성된 맵이 있다면 파괴
        if (spawnedMap != null) Destroy(spawnedMap);

        // 1. 맵 생성 (Resources/MapPrefabs 경로에 프리팹이 있어야 함)
        GameObject mapPrefab = Resources.Load<GameObject>(currentStageData.mapPrefabPath);
        if (mapPrefab != null)
        {
            spawnedMap = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"[StageManager] 맵 프리팹을 찾을 수 없습니다: {currentStageData.mapPrefabPath}");
        }

        // 2. 인게임 설정값 적용
        ApplyStageSettings();

        IsStageActive = true;
        Debug.Log($"<color=cyan>[StageManager]</color> 스테이지 {currentStageData.stage} 시작!");
    }

    private void ApplyStageSettings()
    {
        // SO_StageData에 정의한 세부 설정 적용
        Debug.Log($"[StageManager] 초기 재화: {currentStageData.startCoin}, 성벽 HP: {currentStageData.barrierHP}");

        if(CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.FragmentCoin, currentStageData.startCoin);
        }
        else
        {
            Debug.LogWarning("[StageManager] 씬에 CurrencyManager가 존재하지 않습니다.");
        }
        if(WaveManager.Instance != null)
        {
            WaveManager.Instance.LoadWaveDataFromResources(currentStageData.stageID);
            WaveManager.Instance.StartNextWave();
        }
        else
        {
            Debug.LogWarning("[StageManager] 씬에 WaveManager가 존재하지 않습니다.");
        }

        // 방벽 체력 설정
        if (Barrier.Instance != null)
        {
            // SO_StageData에 작성하신 barrierHP를 주입합니다.
            Barrier.Instance.InitBarrier(currentStageData.barrierHP);
        }
        else
        {
            Debug.LogWarning("[StageManager] 씬에 Barrier가 존재하지 않습니다.");
        }
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.remainingMoves = currentStageData.slotMove;
        }
        else
        {
            Debug.LogWarning("[StageManager] 씬에 Barrier가 존재하지 않습니다.");
        }
        PlayStageBGM();
    }
    private void PlayStageBGM()
    {
        if (currentStageData == null || string.IsNullOrEmpty(currentStageData.bgmPath))
        {
            Debug.LogWarning("[StageManager] BGM 이름이 비어있거나 스테이지 데이터가 없습니다.");
            return;
        }

        // =========================================================================
        // [★안전 가드 추가] 싱글톤 인스턴스가 존재할 때만 사운드 제어 명령을 하도록 예외 처리
        // =========================================================================
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.PlayBGM(currentStageData.bgmPath);
        }
        else
        {
            Debug.LogWarning("[StageManager] 사운드를 재생하려 했으나 씬에 SoundManager가 아직 생성되지 않았습니다.");
        }
    }

    /// <summary>
    /// 현재 스테이지 정보를 외부에서 참조할 때 사용
    /// </summary>
    public SO_StageData GetCurrentStageData() => currentStageData;
}