using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Data")]
    private List<SO_StageData> allStageData = new List<SO_StageData>();
    private SO_StageData currentStageData;
    private GameObject spawnedMap;

    [Header("Status")]
    public bool IsStageActive { get; private set; }

    private void Awake()
    {
        // 1. 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. 임포터로 생성된 모든 스테이지 데이터 로드 (Resources 폴더 기준)
        LoadAllStageData();
    }

    private void LoadAllStageData()
    {
        // "Resources/StageData" 폴더 내의 모든 SO_StageData를 가져와 ID 순으로 정렬
        allStageData = Resources.LoadAll<SO_StageData>("StageData")
                                .OrderBy(s => s.stageID)
                                .ToList();

        Debug.Log($"[StageManager] {allStageData.Count}개의 스테이지 데이터를 로드했습니다.");
    }

    private void Start()
    {
        InitStage(allStageData[0].stageID);
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
            spawnedMap = Instantiate(mapPrefab);
        }
        else
        {
            Debug.LogWarning($"[StageManager] 맵 프리팹을 찾을 수 없습니다: {currentStageData.mapPrefabPath}");
        }

        // 2. 인게임 설정값 적용
        ApplyStageSettings();

        // 3. 웨이브 매니저 시작
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.LoadWaveDataFromResources(stageID);
            WaveManager.Instance.StartNextWave(); 
        }

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
        PlayStageBGM();
    }
    private void PlayStageBGM()
    {
        if (string.IsNullOrEmpty(currentStageData.bgmPath))
        {
            Debug.LogWarning("[StageManager] BGM 경로가 비어있습니다.");
            return;
        }

        // Resources 폴더에서 오디오 클립 로드
        AudioClip stageBgm = Resources.Load<AudioClip>(currentStageData.bgmPath);

        /*if (stageBgm != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(stageBgm);
        }
        else
        {
            Debug.LogWarning($"[StageManager] BGM 로드 실패 또는 SoundManager 없음: {currentStageData.bgmPath}");
        }*/
    }

    /// <summary>
    /// 현재 스테이지 정보를 외부에서 참조할 때 사용
    /// </summary>
    public SO_StageData GetCurrentStageData() => currentStageData;
}