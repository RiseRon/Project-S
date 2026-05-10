using UnityEngine;

public class Slot : MonoBehaviour
{
    // 현재 이 슬롯에 배치된 슬라임 (없으면 null)
    public Slime placedSlime;

    // 슬롯이 비어있는지 확인하는 프로퍼티
    public bool IsEmpty => placedSlime == null;

    // 머지 가능 여부를 확인하는 가상 함수 (추후 확장)
    public bool CanMerge(Slime draggingslime)
    {
        if (IsEmpty ) return false;
        
        // 여기에 등급이나 종류 비교 로직 추가예정
        return false;
    }
}
