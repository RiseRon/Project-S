using TMPro;
using UnityEngine;

public class SlotMoveUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI slotMoveCountText;
    
    void Start()
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnSlimeMoved += UpdateSLotMoveDisplay;
            UpdateSLotMoveDisplay(); // 첫 시작 시 초기화
        }
    }
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독 해제
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnSlimeMoved -= UpdateSLotMoveDisplay;
        }
    }
    public void UpdateSLotMoveDisplay()
    {
        if (slotMoveCountText == null) return;

        slotMoveCountText.text = $"{PlacementManager.Instance.remainingMoves}";
    }
}
