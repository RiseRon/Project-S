using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LayerMask slotLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Stage State")]
    public int remainingMoves = 10;

    private Slime draggingSlime;
    private Slot currentOverSlot;
    private Vector3 originalPos;
    private Slot originalSlot;

    private Camera mainCam;

    // 인벤토리 드래그 상태 관리
    private bool isDraggingFromInventory = false;
    private SO_SlimeData pendingSlimeData;
    private SlimeCard currentDraggingCard;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
    }

    void Update()
    {
        HandleInput();
    }

    // SlimeCard UI에서 드래그 시작 시 호출
    public void StartSummonDrag(SO_SlimeData data, SlimeCard card)
    {
        isDraggingFromInventory = true;
        pendingSlimeData = data;
        currentDraggingCard = card;

        GameObject slimeObj = PoolManager.Instance.SpawnFromPool(data.id, Vector3.zero, Quaternion.identity);

        if (slimeObj != null)
        {
            draggingSlime = slimeObj.GetComponent<Slime>();
            if (draggingSlime != null)
            {
                draggingSlime.SetData(data);
                draggingSlime.isDragging = true; // 공격 중지 상태 활성화
            }
            originalSlot = null;
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && !isDraggingFromInventory)
        {
            BeginDrag();
        }

        if (Input.GetMouseButton(0) && draggingSlime != null)
        {
            OnDragging();
        }

        if (Input.GetMouseButtonUp(0) && draggingSlime != null)
        {
            EndDrag();
        }
    }

    private void BeginDrag()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Slime slime = hit.collider.GetComponent<Slime>();
            if (slime != null)
            {
                isDraggingFromInventory = false;
                draggingSlime = slime;
                draggingSlime.isDragging = true; // 드래그 중 공격 중지
                originalPos = draggingSlime.transform.position;

                originalSlot = FindSlotUnderSlime(draggingSlime.transform.position);
                if (originalSlot != null) originalSlot.placedSlime = null;
            }
        }
    }

    private void OnDragging()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, slotLayer))
        {
            currentOverSlot = hit.collider.GetComponent<Slot>();

            if (currentOverSlot != null && currentOverSlot.IsEmpty)
            {
                draggingSlime.transform.position = currentOverSlot.transform.position;
            }
            else
            {
                MoveSlimeOnGround(ray);
            }
        }
        else
        {
            currentOverSlot = null;
            MoveSlimeOnGround(ray);
        }
    }

    private void MoveSlimeOnGround(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            draggingSlime.transform.position = hit.point;
        }
    }

    private void EndDrag()
    {
        if (draggingSlime != null) draggingSlime.isDragging = false; // 드래그 종료로 공격 재개

        if (currentOverSlot != null && currentOverSlot.IsEmpty)
        {
            bool isFromInventory = (originalSlot == null);

            if (!isFromInventory && remainingMoves <= 0)
            {
                ReturnToOriginalPosition();
                return;
            }

            draggingSlime.transform.position = currentOverSlot.transform.position;
            currentOverSlot.placedSlime = draggingSlime;

            if (!isFromInventory)
            {
                remainingMoves--;
            }
            else if (currentDraggingCard != null)
            {
                Destroy(currentDraggingCard.gameObject); // 배치 성공 시 카드 제거
            }
        }
        else if (currentOverSlot != null && currentOverSlot.CanMerge(draggingSlime))
        {
            // MergeManager.Instance.ExecuteMerge(draggingSlime, currentOverSlot);
        }
        else
        {
            if (isDraggingFromInventory)
            {
                PoolManager.Instance.ReturnToPool(pendingSlimeData.id, draggingSlime.gameObject);
                if (currentDraggingCard != null) currentDraggingCard.OnPlacementFailed();
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }

        draggingSlime = null;
        currentOverSlot = null;
        isDraggingFromInventory = false;
    }

    private void ReturnToOriginalPosition()
    {
        draggingSlime.transform.position = originalPos;
        if (originalSlot != null) originalSlot.placedSlime = draggingSlime;
    }

    private Slot FindSlotUnderSlime(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, slotLayer))
        {
            return hit.collider.GetComponent<Slot>();
        }
        return null;
    }
}