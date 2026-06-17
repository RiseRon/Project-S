#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectCheat : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TriggerUnlockCheat();
        }
    }
    private void TriggerUnlockCheat()
    {
        // 1. 현재 씬에 존재하는 StageSelectManager를 직접 검색하여 찾아옵니다.
        StageSelectManager manager = FindFirstObjectByType<StageSelectManager>();

        if (manager == null)
        {
            Debug.LogError("[StageSelectCheat] 씬에서 StageSelectManager를 찾을 수 없습니다!");
            return;
        }

        Debug.Log($"<color=red>[CHEAT]</color> '{KeyCode.F1}' 입력 감지: 모든 스테이지를 강제 개방합니다.");

        // 💡 팁: 가장 확실하게 UI를 강제 변경하기 위해 Manager가 가지고 있는 자식 오브젝트들을 직접 제어합니다.
        List<Button> stageButton = manager.GetStageButtons();
        foreach (Button btn in stageButton)
        {
            if (btn != null)
            {
                btn.interactable = true; // 2, 3스테이지 버튼 포함 모든 버튼 활성화
            }
        }

        List<GameObject> lockIcon = manager.GetLockIcon();
        foreach (GameObject icon in lockIcon)
        {
            icon.gameObject.SetActive(false); // 자물쇠 전부 비활성화
        }
    }
}
#endif