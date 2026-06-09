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

    /// <summary>
    /// 드래그 중인 슬라임과 이 슬롯에 있는 슬라임이 머지(합성) 가능한지 판별합니다.
    /// </summary>
    public bool CanMerge(Slime draggingSlime)
    {
        if (IsDataEmpty) return false;
        if (this.placedSlime == draggingSlime) return false;

        int baseID = this.placedSlime.SlimeID;
        int materialID = draggingSlime.SlimeID;

        if (MergeManager.Instance != null)
        {
            return MergeManager.Instance.CheckCanMerge(baseID, materialID);
        }

        return false;
    }
}