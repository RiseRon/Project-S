using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // 현재 선택된 스테이지 정보를 임시로 저장할 변수
    private int selectedStageID = -1;
    private bool isFirstCheck = true;
    [Header("--- Stage UI Lists (순서대로 등록해주세요) ---")]
    [Tooltip("1스테이지, 2스테이지, 3스테이지 순서대로 Button 컴포넌트를 넣어주세요.")]
    [SerializeField] private List<Button> stageButtons = new List<Button>();

    [Tooltip("2스테이지, 3스테이지 버튼에 들어있는 자물쇠 오브젝트들을 순서대로 넣어주세요. (1스테이지는 없으므로 Element 0번에 2스테이지 자물쇠 배치)")]
    [SerializeField] private List<GameObject> lockIcons = new List<GameObject>();

    [Tooltip("1스테이지, 2스테이지, 3스테이지 버튼을 선택됐을 때 들어갈 체크 이미지를 넣어주세요.")]
    [SerializeField] private List<GameObject> checkIcons = new List<GameObject>();
    /// <summary>
    /// 1. 개별 스테이지 버튼을 눌렀을 때 호출할 함수
    /// 유니티 인스펙터 OnClick()에서 이 함수를 선택하고 Stage ID(정수)를 입력하세요.
    /// </summary>
    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("BGM_Title");
        }
        // 씬이 켜질 때 GameManager 메모리를 확인해서 버튼 상호작용(Interactable) 처리
        CheckAndApplyStageLocks();
        // 시작 시에는 아무것도 선택 안 되어 있으므로 체크 아이콘 전체 비활성화
        UpdateCheckIcons(-1);
    }
     /// <summary>
     /// GameManager의 메모리를 기반으로 버튼의 활성화/잠금 상태를 제어합니다.
     /// </summary>
    private void CheckAndApplyStageLocks()
    {
        // 예외 방지: 등록된 버튼이 없다면 리턴
        if (stageButtons == null || stageButtons.Count == 0) return;

        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (stageButtons[i] == null) continue;

            if (i == 0)
            {
                stageButtons[i].interactable = true;
                continue;
            }

            // 처음 켰을 때(isFirstCheck가 true일 때)는 이 if문을 통과하지 않고, 
            // 무조건 아래로 내려가 GameManager 데이터를 제대로 검사합니다.
            if (!isFirstCheck && stageButtons[i].interactable == true)
            {
                int lockIndex = i - 1;
                if (lockIcons != null && lockIndex < lockIcons.Count && lockIcons[lockIndex] != null)
                {
                    lockIcons[lockIndex].SetActive(false);
                }
                continue;
            }

            // 2. 두 번째 버튼(인덱스 1 = 2스테이지)부터는 '직전 스테이지 ID'를 역추적합니다.
            // 인덱스 1일 때 직전 스테이지 ID는 501번(500 + i)이 됩니다.
            int previousStageID = 500 + i;

            bool isPreviousStageCleared = false;
            if (GameManager.Instance != null)
            {
                isPreviousStageCleared = GameManager.Instance.IsStageCleared(previousStageID);
            }

            // 3. 버튼 활성화 상태 적용 (직전 탄을 깼다면 true, 안 깼다면 false)
            stageButtons[i].interactable = isPreviousStageCleared;

            // 4. 자물쇠 아이콘 오브젝트 켜고 끄기 (리스트 범위 초과 방지 안전장치 포함)
            // 1스테이지는 자물쇠가 없으므로 자물쇠 리스트의 i-1 번째 원소와 매칭시킵니다.
            int lockIndexForCheck = i - 1;
            if (lockIcons != null && lockIndexForCheck < lockIcons.Count && lockIcons[lockIndexForCheck] != null)
            {
                lockIcons[lockIndexForCheck].SetActive(!isPreviousStageCleared);
            }
        }

        // 💡전체 루프를 돌며 최초 정산을 끝마쳤으므로, 
        // 플래그를 false로 전환하여 다음 검사(재플레이 후 패배 등)부터 예외처리가 작동되게 합니다.
        isFirstCheck = false;
    }
    public void OnStageSelect(int stageID)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }

        // 💡만약 유효하지 않은 ID가 들어오거나 취소 요청(-1)이 오면 선택을 해제합니다.
        if (stageID <= 500)
        {
            selectedStageID = -1;
            Debug.Log("<color=orange>[StageSelect]</color> 스테이지 선택이 해제되었습니다.");
            return;
        }

        selectedStageID = stageID;

        // 선택된 스테이지에 맞춰 체크 아이콘 활성화 (501번 입력 시 -> 인덱스 0번 활성화)
        int checkIndex = stageID - 501;
        UpdateCheckIcons(checkIndex);

        Debug.Log($"<color=yellow>[StageSelect]</color> 스테이지 {selectedStageID - 500}번이 선택되었습니다. (대기 중)");
    }

    /// <summary>
    /// 2. 최종 '시작' 버튼을 눌렀을 때 호출할 함수
    /// </summary>
    public void OnStartButtonClicked()
    {
        // 스테이지를 선택하지 않았거나 해제된 상태(-1)라면 게임 시작을 원천 차단합니다.
        if (selectedStageID == -1)
        {
            Debug.LogWarning("[StageSelectManager] 현재 선택된 스테이지가 없습니다! 시작할 수 없습니다.");
            return;
        }

        // 파괴되지 않는 StageManager에게 선택된 ID 주입
        if (StageManager.Instance != null)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("SFX_UI_Click");
            }
            StageManager.Instance.SetNextStage(selectedStageID);
        }
        else
        {
            Debug.LogError("[StageSelectManager] StageManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"<color=cyan>[StageSelect]</color> 게임 시작! 스테이지 {selectedStageID - 500}로 이동합니다.");
        LoadingSceneManager.LoadScene();
    }

    /// <summary>
    /// 3. 뒤로가기 버튼을 눌렀을 때 메인 메뉴 씬으로 돌아가는 함수
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }
        Debug.Log("<color=white>[StageSelect]</color> 뒤로가기 버튼 클릭. 메인 메뉴로 돌아갑니다.");
        SceneManager.LoadScene("Scene_Main");
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 버튼을 누르면 OnStageSelect(stageID)가 먼저 실행되므로 여기서는 별도의 처리를 하지 않아도 무방합니다.
    }

    // 버튼이 포커스를 잃거나, 다른 빈 곳을 눌러 '선택 해제'가 되었을 때 버튼 자체에서 감지하는 함수
    public void OnDeselect(BaseEventData eventData)
    {
        // 다른 곳을 누르면 버튼 자체에서 감지하여 선택된 ID를 안전하게 초기화합니다.
        selectedStageID = -1;
        Debug.Log("<color=orange>[StageSelect]</color> 버튼 포커스가 해제되어 스테이지 선택이 초기화되었습니다.");
    }
    private void UpdateCheckIcons(int activeIndex)
    {
        if (checkIcons == null || checkIcons.Count == 0) return;

        for (int i = 0; i < checkIcons.Count; i++)
        {
            if (checkIcons[i] == null) continue;

            // 내가 선택한 인덱스 번호와 일치하면 true(켜기), 틀리면 false(끄기)
            checkIcons[i].SetActive(i == activeIndex);
        }
    }

#if UNITY_EDITOR
    public List<Button> GetStageButtons()
    {
        return stageButtons;
    }
    public List<GameObject> GetLockIcon()
    {
        return lockIcons;
    }
#endif
}