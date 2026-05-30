using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int currentStageID;
    private float currentPlayTime = 0f;
    private bool isTimerRunning = false;
    public static float TotalPlayTime { get; private set; }
    public static int LastPlayedStageID { get; private set; }
    public static bool IsGameWin { get; private set; }
    public static int KilledEnemyCount { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 씬이 바뀔 때마다 실행될 함수를 유니티 엔진에 등록합니다.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        UnsubscribeEvent();
        SubscribeEvent();
    }

    private void OnDestroy()
    {
        // GameManager가 혹시라도 파괴될 때 이벤트 해제 (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnsubscribeEvent();
    }

    // [★핵심 해결책] 씬이 로드될 때마다 자동으로 실행되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 리절트 씬이나 로비 씬이 아니라 '스테이지' 씬일 때만 작동하도록 방어선 구축
        if (scene.name == "Scene_Stage")
        {
            StartTimer();

            // 기존에 물려있던 배리어가 있다면 안전하게 연결을 끊고, 새 배리어를 구독합니다.
            UnsubscribeEvent();
            SubscribeEvent();
        }
    }

    private void SubscribeEvent()
    {
        // 씬 내부에서 Barrier 컴포넌트를 직접 찾아 안전하게 대입 및 구독합니다. (Awake 순서 꼬임 해결)
        Barrier barrier = FindFirstObjectByType<Barrier>();
        if (barrier != null)
        {
            barrier.OnBarrierDestroyed += HandlePlayerDefeat;
            Debug.Log("<color=cyan>[GameManager]</color> 새로운 배리어 구독 완료!");
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnStageVictoryDetected += HandleStageWin;
            Debug.Log("<color=cyan>[GameManager]</color> 새로운 웨이브 구독 완료!");
        }
    }

    private void UnsubscribeEvent()
    {
        // 기존 배리어 인스턴스가 남아있다면 이벤트를 끊어줍니다.
        if (Barrier.Instance != null)
        {
            Barrier.Instance.OnBarrierDestroyed -= HandlePlayerDefeat;
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnStageVictoryDetected -= HandleStageWin;
        }
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            currentPlayTime += Time.deltaTime;
        }
    }

    public void StartTimer()
    {
        currentPlayTime = 0f;
        isTimerRunning = true;
        KilledEnemyCount = 0;
    }
    public static void AddKilledEnemyCount()
    {
        KilledEnemyCount++;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void HandlePlayerDefeat()
    {
        // 스테이지 데이터 백업
        if (StageManager.Instance != null && StageManager.Instance.GetCurrentStageData() != null)
        {
            currentStageID = StageManager.Instance.GetCurrentStageData().stageID;
        }
        Debug.Log($"<color=orange>[GameManager]</color> ☠ 배리어 파괴 감지! {currentStageID - 500} 스테이지 패배 정산을 시작합니다.");

        LastPlayedStageID = currentStageID;
        IsGameWin = false;
        TotalPlayTime = currentPlayTime;
        StopTimer();
        // 패배 처리가 되었으니 안전하게 이벤트를 끊어줍니다.
        UnsubscribeEvent();

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ClearAllActiveObjects();
        }

        SceneManager.LoadScene("Scene_Result");
    }
    public void HandleStageWin()
    {
        // 스테이지 데이터 백업
        if (StageManager.Instance != null && StageManager.Instance.GetCurrentStageData() != null)
        {
            currentStageID = StageManager.Instance.GetCurrentStageData().stageID;
        }
        Debug.Log($"<color=orange>[GameManager]</color> 승리 신호 감지! {currentStageID - 500} 스테이지 승리 정산을 시작합니다.");

        LastPlayedStageID = currentStageID;
        IsGameWin = true;
        TotalPlayTime = currentPlayTime;
        StopTimer();
        // 승리 처리가 되었으니 안전하게 이벤트를 끊어줍니다.
        UnsubscribeEvent();

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ClearAllActiveObjects();
        }

        SceneManager.LoadScene("Scene_Result");
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}