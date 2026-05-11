using UnityEngine;

public class InventoryCheat : MonoBehaviour
{
    [Header("인벤토리 설정")]
    [SerializeField] private Transform inventoryPanel; // 인벤토리 슬롯들이 담기는 부모 패널
    [SerializeField] private int itemSlotPoolID = 911; // 인벤토리 슬롯 프리팹의 풀 ID (예: 911)

#if UNITY_EDITOR
    private void Update()
    {
        // F4 키를 누르면 인벤토리 비우기
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ClearInventory();
        }
    }

    private void ClearInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("Inventory Panel이 할당되지 않았습니다!");
            return;
        }

        if (PoolManager.Instance == null) return;

        // 자식 오브젝트들을 역순으로 순회하며 풀에 반납
        // (정방향 순회 시 자식이 삭제되면 인덱스 꼬임이 발생할 수 있어 역순 권장)
        for (int i = inventoryPanel.childCount - 1; i >= 0; i--)
        {
            GameObject itemObj = inventoryPanel.GetChild(i).gameObject;

            // PoolManager를 통해 풀로 되돌림
            PoolManager.Instance.ReturnToPool(itemSlotPoolID, itemObj);
        }

        Debug.Log($"<color=yellow>[Cheat]</color> 인벤토리 패널의 자식 {inventoryPanel.childCount}개를 모두 풀로 반납했습니다.");
    }
#endif
}