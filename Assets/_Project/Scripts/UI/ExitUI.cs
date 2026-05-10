using UnityEngine;

public class ExitUI : MonoBehaviour
{
    [Header("설정 (비어있으면 부모를 자동으로 할당)")]
    [SerializeField] private GameObject targetUI;

    private void Awake()
    {
        // 만약 인스펙터에서 타겟을 지정하지 않았다면, 자동으로 바로 위의 부모를 타겟으로 잡습니다.
        if (targetUI == null && transform.parent != null)
        {
            targetUI = transform.parent.gameObject;
        }
    }

    /// <summary>
    /// 버튼 OnClick 이벤트에 연결할 함수
    /// </summary>
    public void CloseUI()
    {
        if (targetUI != null)
        {
            targetUI.SetActive(false);
            Debug.Log($"<color=orange>[UI]</color> {targetUI.name} 오브젝트가 비활성화되었습니다.");
        }
    }
}