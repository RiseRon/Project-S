using UnityEngine;

public enum SlotState
{
    None,  // 평상시
    Placeable,  // 배치가능
    Mergeable,  // 합성가능
    Invalid  // 배치불가능
}

public class Slot : MonoBehaviour
{
    public Slime placedSlime;
    public SlotState CurrentState { get; private set; } = SlotState.None;

    [Header("물리 검증용 레이어")]
    [SerializeField] private LayerMask slimeLayer;

    private void Awake()
    {
        slimeLayer = 1 << LayerMask.NameToLayer("Slime");
    }

    /// <summary>
    /// [오류 해결] PlacementManager에서 슬롯을 명시적으로 비울 때 호출하는 함수입니다.
    /// </summary>
    public void ClearSlot()
    {
        placedSlime = null;
    }

    public bool IsDataEmpty
    {
        get
        {
            if (placedSlime == null) return true;
            if (!placedSlime.gameObject.activeInHierarchy)
            {
                placedSlime = null; // 유령 데이터 제거
                return true;
            }
            return false;
        }
    }

    public bool IsEmptyOrOccupiedBy(Slime slime)
    {
        if (IsDataEmpty) return true;
        return placedSlime == slime;
    }

    public void AssignSlime(Slime slime)
    {
        if (slime == null) return;
        placedSlime = slime;
    }

    /// <summary> 드래그 시작 시 컨트롤러가 이 슬롯의 상태를 판별하여 주입합니다. </summary>
    public void SetState(SlotState newState)
    {
        CurrentState = newState;

        // 나중에 이 부분에 상태(newState)에 따라 하이라이트 색상이나 밝기를 바꾸는 시각 효과 코드를 넣으시면 됩니다!
        // 예: if (newState == SlotState.Placeable) ChangeColor(Green);
    }
}