using UnityEngine;

public class OpenUI : MonoBehaviour
{
    [Header("켤 대상 UI (Panel, Popup 등)")]
    [SerializeField] private GameObject targetUI;

    /// <summary>
    /// 버튼 OnClick 이벤트에 연결할 함수
    /// </summary>
    public void ShowUI()
    {
        if (targetUI != null)
        {
            // 대상 UI를 활성화
            targetUI.SetActive(true);

            // (선택) 켰을 때 가장 앞으로 오게 하고 싶다면 아래 주석 해제
            // targetUI.transform.SetAsLastSibling();

            Debug.Log($"<color=green>[UI]</color> {targetUI.name} 오브젝트가 활성화되었습니다.");
        }
        else
        {
            Debug.LogWarning("Target UI가 지정되지 않았습니다!");
        }
    }
}