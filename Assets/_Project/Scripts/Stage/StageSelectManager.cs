using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    // 현재 선택된 스테이지 정보를 임시로 저장할 변수
    private int selectedStageID = -1;

    /// <summary>
    /// 1. 개별 스테이지 버튼을 눌렀을 때 호출할 함수
    /// 유니티 인스펙터 OnClick()에서 이 함수를 선택하고 Stage ID(정수)를 입력하세요.
    /// </summary>
    public void OnStageSelect(int stageID)
    {
        selectedStageID = stageID;
        Debug.Log($"<color=yellow>[StageSelect]</color> 스테이지 {selectedStageID - 500}번이 선택되었습니다. (대기 중)");
    }

    /// <summary>
    /// 2. 최종 '시작' 버튼을 눌렀을 때 호출할 함수
    /// </summary>
    public void OnStartButtonClicked()
    {
        // 안전장치: 스테이지를 선택하지 않고 시작을 누른 경우
        if (selectedStageID == -1)
        {
            Debug.LogWarning("[StageSelectManager] 먼저 스테이지를 선택해야 합니다!");
            return;
        }

        // 파괴되지 않는 StageManager에게 선택된 ID 주입
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetNextStage(selectedStageID);
        }
        else
        {
            Debug.LogError("[StageSelectManager] StageManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"<color=cyan>[StageSelect]</color> 게임 시작! 스테이지 {selectedStageID - 500}로 이동합니다.");

        // 로딩 씬 오픈
        LoadingSceneManager.LoadScene();
    }

    /// <summary>
    /// 3. 뒤로가기 버튼을 눌렀을 때 메인 메뉴 씬으로 돌아가는 함수
    /// </summary>
    public void OnBackButtonClicked()
    {
        Debug.Log("<color=white>[StageSelect]</color> 뒤로가기 버튼 클릭. 메인 메뉴로 돌아갑니다.");
        SceneManager.LoadScene("Scene_Main");
    }
}