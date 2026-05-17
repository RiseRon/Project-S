using UnityEngine;

public class Slot : MonoBehaviour
{
    public Slime placedSlime;

    [Header("물리 검증용 레이어")]
    [SerializeField] private LayerMask slimeLayer;
    private void Awake()
    {
        slimeLayer = 1 << LayerMask.NameToLayer("Slime");
    }

    public bool IsDataEmpty
    {
        get
        {
            if (placedSlime == null) return true;
            if (!placedSlime.gameObject.activeInHierarchy)
            {
                placedSlime = null;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 슬롯이 비어있거나, 지정된 슬라임 본인이 점유 중인 경우 true를 반환합니다.
    /// 드래그 중인 슬라임이 자신이 원래 있던 슬롯을 덮어쓰는 오판을 방지하기 위해 사용합니다.
    /// </summary>
    public bool IsEmptyOrOccupiedBy(Slime slime)
    {
        if (IsDataEmpty) return true;
        return placedSlime == slime;
    }

    /// <summary>
    /// 이 슬롯에 슬라임을 물리적/데이터적으로 완전히 고정시킵니다.
    /// </summary>
    public void AssignSlime(Slime slime)
    {
        if (slime == null) return;

        // 메모리 데이터 등록과 3D 월드 좌표 고정을 동시에 처리하여 오차를 차단합니다.
        placedSlime = slime;
        slime.transform.position = transform.position;
    }

    /// <summary>
    /// 슬롯에 묶여있던 슬라임 관계를 안전하게 지워줍니다.
    /// </summary>
    public void ClearSlot()
    {
        placedSlime = null;
    }

    public bool CanMerge(Slime draggingslime)
    {
        if (IsDataEmpty) return false;
        return false;
    }
}