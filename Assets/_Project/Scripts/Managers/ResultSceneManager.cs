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

    [Header("=== Win Panel Sub Groups (Is Last Stage?)===")]
    [SerializeField] private GameObject normalWinButtonGroup;
    [SerializeField] private GameObject finalWinButtonGroup;

    private const int MAX_STAGE_ID = 503;
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

        if (isWin)
        {
            // 마지막으로 플레이한 스테이지 ID가 MAX_STAGE_ID와 같거나 큰지 검사합니다.
            bool isFinalStage = GameManager.LastPlayedStageID >= MAX_STAGE_ID;

            if (isFinalStage)
            {
                // 마지막 스테이지를 이겼다면: 다음 스테이지 버튼 숨기고, 패배 시 버튼 구성을 켭니다.
                if (normalWinButtonGroup != null) normalWinButtonGroup.SetActive(false);
                if (finalWinButtonGroup != null) finalWinButtonGroup.SetActive(true);

                Debug.Log($"<color=lime>[Result]</color> 최종 스테이지({GameManager.LastPlayedStageID - 500}탄) 클리어! 패배형 버튼 레이아웃을 표시합니다.");
            }
            else
            {
                // 일반 스테이지를 이겼다면: 원래대로 다음 스테이지 이동 버튼이 있는 그룹을 켭니다.
                if (normalWinButtonGroup != null) normalWinButtonGroup.SetActive(true);
                if (finalWinButtonGroup != null) finalWinButtonGroup.SetActive(false);
            }
        }
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
        // 안전장치: 혹시라도 마지막 스테이지에서 이 함수가 예외적으로 눌리는 것을 방지
        if (GameManager.LastPlayedStageID >= MAX_STAGE_ID)
        {
            Debug.LogWarning("[ResultSceneManager] 마지막 스테이지이므로 다음 스테이지로 진할 수 없습니다.");
            return;
        }

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