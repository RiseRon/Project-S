using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultSceneManager : MonoBehaviour
{
    [Header("=== Left Panel (Stats) ===")]
    [SerializeField] private TextMeshProUGUI timerText;          // 타이머 영역에 흐른 시간 표시 (예: 01:23)
    [SerializeField] private TextMeshProUGUI monsterKillCountText; // "죽인 몬스터 수: 000" 표시용

    [Header("=== Right Panel (State) ===")]
    [SerializeField] private GameObject winVisualGroup;   // 승리 시 활성화할 오브젝트 그룹
    [SerializeField] private GameObject defeatVisualGroup; // 패배 시 활성화할 오브젝트 그룹

    void Start()
    {
        // 씬이 켜지자마자 GameManager의 static 데이터들을 UI에 반영
        UpdateResultUI();
    }

    private void UpdateResultUI()
    {
        // 1. 죽인 몬스터 수 반영 (스크린샷 포맷 유지)
        if (monsterKillCountText != null)
        {
            monsterKillCountText.text = $"죽인 몬스터 수: {GameManager.KilledEnemyCount:D3}"; // D3 설정 시 005 형태로 이쁘게 출력됨
        }

        // 2. 타이머 텍스트 포맷팅 (초 단위를 분:초 형태로 변환)
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(GameManager.TotalPlayTime / 60F);
            int seconds = Mathf.FloorToInt(GameManager.TotalPlayTime % 60F);
            timerText.text = $"{minutes:D2}:{seconds:D2}"; // 예: 02:15 형태로 출력
        }

        // 3. 승리 / 패배 비주얼 상태 전환
        bool isWin = GameManager.IsGameWin;

        if (winVisualGroup != null) winVisualGroup.SetActive(isWin);
        if (defeatVisualGroup != null) defeatVisualGroup.SetActive(!isWin);
    }

    /// <summary>
    /// 초록색 [재시작] 버튼 클릭 시 동작
    /// </summary>
    public void OnRestartButtonClicked()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetNextStage(GameManager.LastPlayedStageID);
        }
        else
        {
            Debug.LogError("[StageSelectManager] StageManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"<color=cyan>[StageSelect]</color> 게임 시작! 스테이지 {GameManager.LastPlayedStageID - 500}로 이동합니다.");

        // 로딩 씬 오픈
        LoadingSceneManager.LoadScene();
    }
    public void OnNextStageButtonClicked()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetNextStage(GameManager.LastPlayedStageID + 1);
        }
        else
        {
            Debug.LogError("[StageSelectManager] StageManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"<color=cyan>[StageSelect]</color> 게임 시작! 스테이지 {GameManager.LastPlayedStageID - 500 + 1}로 이동합니다.");

        // 로딩 씬 오픈
        LoadingSceneManager.LoadScene();
    }

    /// <summary>
    /// 빨간색 [나가기] 버튼 클릭 시 동작 (로비나 메인화면 이동)
    /// </summary>
    public void OnExitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene("Scene_StageSelect"); // 프로젝트의 로비/메인 화면 씬 이름 입력
        }
        else
        {
            SceneManager.LoadScene("Scene_Main");
        }
    }
}