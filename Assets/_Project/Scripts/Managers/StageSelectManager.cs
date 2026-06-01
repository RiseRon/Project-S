using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    // 현재 선택된 스테이지 정보를 임시로 저장할 변수
    private int selectedStageID = -1;
    
    [Header("--- Stage UI Lists (순서대로 등록해주세요) ---")]
    [Tooltip("1스테이지, 2스테이지, 3스테이지 순서대로 Button 컴포넌트를 넣어주세요.")]
    [SerializeField] private List<Button> stageButtons = new List<Button>();

    [Tooltip("2스테이지, 3스테이지 버튼에 들어있는 자물쇠 오브젝트들을 순서대로 넣어주세요. (1스테이지는 없으므로 Element 0번에 2스테이지 자물쇠 배치)")]
    [SerializeField] private List<GameObject> lockIcons = new List<GameObject>();
    /// <summary>
    /// 1. 개별 스테이지 버튼을 눌렀을 때 호출할 함수
    /// 유니티 인스펙터 OnClick()에서 이 함수를 선택하고 Stage ID(정수)를 입력하세요.
    /// </summary>
    private void Start()
    {
        // 씬이 켜질 때 GameManager 메모리를 확인해서 버튼 상호작용(Interactable) 처리
        CheckAndApplyStageLocks();
    }/// <summary>
     /// GameManager의 메모리를 기반으로 버튼의 활성화/잠금 상태를 제어합니다.
     /// </summary>
    private void CheckAndApplyStageLocks()
    {
        // 예외 방지: 등록된 버튼이 없다면 리턴
        if (stageButtons == null || stageButtons.Count == 0) return;

        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (stageButtons[i] == null) continue;

            // 1. 첫 번째 버튼(인덱스 0 = 1스테이지)은 무조건 클릭 가능하게 열어둡니다.
            if (i == 0)
            {
                stageButtons[i].interactable = true;
                continue;
            }

            // 2. 두 번째 버튼(인덱스 1 = 2스테이지)부터는 '직전 스테이지 ID'를 역추적합니다.
            // 인덱스 1일 때 직전 스테이지 ID는 501번(500 + i)이 됩니다.
            int previousStageID = 500 + i;

            bool isPreviousStageCleared = false;
            if (GameManager.Instance != null)
            {
                // GameManager 메모리에 501번이 들어있는지 물어봅니다.
                isPreviousStageCleared = GameManager.Instance.IsStageCleared(previousStageID);
            }

            // 3. 버튼 활성화 상태 적용 (직전 탄을 깼다면 true, 안 깼다면 false)
            stageButtons[i].interactable = isPreviousStageCleared;

            // 4. 자물쇠 아이콘 오브젝트 켜고 끄기 (리스트 범위 초과 방지 안전장치 포함)
            // 1스테이지는 자물쇠가 없으므로 자물쇠 리스트의 i-1 번째 원소와 매칭시킵니다.
            int lockIndex = i - 1;
            if (lockIcons != null && lockIndex < lockIcons.Count && lockIcons[lockIndex] != null)
            {
                // 이전 스테이지를 클리어했다면 자물쇠를 비활성화(false), 못깼다면 활성화(true)
                lockIcons[lockIndex].SetActive(!isPreviousStageCleared);
            }
        }
    }
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