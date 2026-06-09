using UnityEngine;
using System;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LayerMask slotLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float yOffset = 0.5f;
    public int remainingMoves = 5;

    private Slime draggingSlime;
    private Slot originalSlot;
    private Vector3 originalPos;
    private Slot currentOverSlot;

    private Camera mainCam;
    private Slot[] allSlots; // 맵에 있는 모든 슬롯을 미리 담아둘 배열

    // 인벤토리 연동용 변수
    private bool isDraggingFromInventory = false;
    private SO_SlimeData pendingSlimeData;
    private SlimeCard currentDraggingCard;

    public event Action OnSlimeMoved;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;

        // 시작할 때 맵에 있는 모든 슬롯을 찾아둡니다. (사전 검사용)
        allSlots = FindObjectsByType<Slot>(FindObjectsSortMode.None);
    }

    private void Start()
    {
        // Start는 모든 스크립트의 Awake가 끝난 후 실행되므로, 순서가 꼬이지 않습니다.
        if (InputController.Instance != null)
        {
            InputController.Instance.OnSlimeDragStart += HandleDragStart;
            InputController.Instance.OnSlimeDragging += HandleDragging;
            InputController.Instance.OnSlimeDragEnd += HandleDragEnd;
        }
        else
        {
            Debug.LogError("InputController를 찾을 수 없습니다! 하이어라키를 확인해주세요.");
        }
    }

    private void OnDestroy()
    {
        // 씬이 넘어가거나 오브젝트가 파괴될 때 안전하게 구독을 해제합니다.
        if (InputController.Instance != null)
        {
            InputController.Instance.OnSlimeDragStart -= HandleDragStart;
            InputController.Instance.OnSlimeDragging -= HandleDragging;
            InputController.Instance.OnSlimeDragEnd -= HandleDragEnd;
        }
    }

    // ==========================================
    // 1. 드래그 시작 (필드 & 인벤토리)
    // ==========================================

    private void HandleDragStart(Slime slime)
    {
        isDraggingFromInventory = false;
        draggingSlime = slime;
        originalPos = slime.transform.position;
        originalSlot = FindSlotUnderSlime(originalPos);

        // 집어드는 즉시 슬롯 데이터 비우기 (복제 버그 방지)
        if (originalSlot != null) originalSlot.ClearSlot();

        //  드래그 상태로 만들고 콜라이더 끄기
        draggingSlime.isDragging = true;
        if (draggingSlime.GetComponent<Collider>() != null)
            draggingSlime.GetComponent<Collider>().enabled = false;

        //  [핵심] 집어드는 순간 맵의 모든 슬롯 상태를 미리 계산합니다!
        PreCalculateAllSlotStates();
    }

    public void StartSummonDrag(SO_SlimeData data, SlimeCard card)
    {
        isDraggingFromInventory = true;
        pendingSlimeData = data;
        currentDraggingCard = card;

        GameObject slimeObj = PoolManager.Instance.SpawnFromPool(data.id, Vector3.zero, Quaternion.identity);
        if (slimeObj != null)
        {
            draggingSlime = slimeObj.GetComponent<Slime>();
            draggingSlime.SetData(data);
            originalSlot = null; // UI에서 뽑았으니 원래 슬롯은 없음

            //  드래그 상태로 만들고 콜라이더 끄기 (자신이 레이저를 막는 것 방지)
            draggingSlime.isDragging = true;
            if (draggingSlime.GetComponent<Collider>() != null)
                draggingSlime.GetComponent<Collider>().enabled = false;

            //  [핵심] 인벤토리에서 뽑을 때도 모든 슬롯 상태를 미리 계산합니다!
            PreCalculateAllSlotStates();
        }
    }

    // ==========================================
    // 2. 사전 판정 로직 (Pre-Calculation)
    // ==========================================

    private void PreCalculateAllSlotStates()
    {
        //  드래그를 시작할 때마다 현재 맵에 있는 슬롯들을 새롭게 스캔합니다!
        allSlots = FindObjectsByType<Slot>(FindObjectsSortMode.None);

        foreach (Slot slot in allSlots)
        {
            // 1순위: 빈 자리이거나 내가 원래 있던 자리면 [배치 가능]
            if (slot.IsEmptyOrOccupiedBy(draggingSlime) || slot == originalSlot)
            {
                slot.SetState(SlotState.Placeable);
            }
            // 2순위: 빈 자리가 아닌데, 나랑 머지가 가능한 유닛이 있다면 [머지 가능]
            else if (MergeSystem.Instance != null && MergeSystem.Instance.CanMerge(draggingSlime, slot.placedSlime))
            {
                slot.SetState(SlotState.Mergeable);
            }
            // 3순위: 남의 자리고 머지도 안 되면 [불가능]
            else
            {
                slot.SetState(SlotState.Invalid);
            }
        }
    }

    // ==========================================
    // 3. 드래그 중 이동 및 자석(Snap) 로직
    // ==========================================

    public void HandleDragging()
    {
        if (draggingSlime == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        currentOverSlot = null; // 매 프레임 초기화

        // 슬롯 탐색 (RaycastAll 관통)
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, slotLayer);
        foreach (RaycastHit hit in hits)
        {
            Slot slot = hit.collider.GetComponent<Slot>();
            if (slot != null)
            {
                // 🌟 [핵심] 슬롯의 상태가 Placeable이거나 Mergeable일 때만 자석처럼 붙습니다!
                if (slot.CurrentState == SlotState.Placeable || slot.CurrentState == SlotState.Mergeable)
                {
                    currentOverSlot = slot;
                    draggingSlime.transform.position = currentOverSlot.transform.position + new Vector3(0, yOffset, 0);
                    return; // 자석처럼 붙었으니 바닥 연산은 생략하고 함수 종료
                }
            }
        }

        //  마우스 밑에 유효한 슬롯이 없을 때의 바닥 이동 처리
        if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
        {
            // 물리적인 바닥(Ground Layer 콜라이더)이 있을 때
            draggingSlime.transform.position = groundHit.point + new Vector3(0, yOffset, 0);
        }
        else
        {
            // 물리 바닥이 세팅되지 않았어도 무조건 마우스를 따라다니도록 '가상 바닥(Plane)' 연산 적용!
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float entry))
            {
                draggingSlime.transform.position = ray.GetPoint(entry) + new Vector3(0, yOffset, 0);
            }
        }
    }

    // ==========================================
    // 4. 드래그 종료 (판정 확정)
    // ==========================================

    public void HandleDragEnd()
    {
        if (draggingSlime == null) return;

        // 꺼두었던 콜라이더를 켜고, 공격 정지 상태를 풀어줍니다.
        // (이걸 안 하면 마우스가 슬라임을 영원히 인식하지 못하고, 공격도 안 합니다)
        draggingSlime.isDragging = false;
        if (draggingSlime.GetComponent<Collider>() != null)
        {
            draggingSlime.GetComponent<Collider>().enabled = true;
        }

        // 드래그가 끝났으므로 마우스를 놓은 위치(currentOverSlot)의 미리 판별된 '상태'만 확인하면 됩니다.
        SlotState finalState = currentOverSlot != null ? currentOverSlot.CurrentState : SlotState.None;

        if (finalState == SlotState.Placeable)
        {
            // [단순 배치 확정]
            if (!isDraggingFromInventory && remainingMoves <= 0)
            {
                Debug.Log("이동 횟수 부족!");
                ReturnToOriginalPosition();
            }
            else
            {
                currentOverSlot.AssignSlime(draggingSlime);
                if (!isDraggingFromInventory && originalSlot != currentOverSlot)
                {
                    remainingMoves--;
                    OnSlimeMoved?.Invoke();
                }
                else if (isDraggingFromInventory && currentDraggingCard != null)
                {
                    PoolManager.Instance.ReturnToPool(911, currentDraggingCard.gameObject);
                }
            }
        }
        else if (finalState == SlotState.Mergeable)
        {
            // [머지 확정] MergeSystem에게 교환을 요청해서 새 슬라임을 받아옵니다.
            Slime newSlime = MergeSystem.Instance.ExecuteMerge(draggingSlime, currentOverSlot.placedSlime);

            if (newSlime != null)
            {
                // 사령탑이 직접 슬롯을 통제합니다! (기존 데이터 비우고 새 슬라임 등록)
                currentOverSlot.ClearSlot();
                currentOverSlot.AssignSlime(newSlime);
            }

            // 인벤토리 카드 처리 (기존 코드 유지)
            if (isDraggingFromInventory && currentDraggingCard != null)
            {
                PoolManager.Instance.ReturnToPool(911, currentDraggingCard.gameObject);
            }
        }
        else
        {
            // [배치 실패 - 맨땅이거나 불가능한 자리]
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

        // 배치가 끝났으니 모든 슬롯의 상태(및 시각 효과)를 다시 평상시(None)로 돌려놓습니다.
        ResetAllSlotStates();

        draggingSlime = null;
        currentOverSlot = null;
        originalSlot = null;
    }

    private void ReturnToOriginalPosition()
    {
        draggingSlime.transform.position = originalPos;
        if (originalSlot != null) originalSlot.AssignSlime(draggingSlime);
    }

    private void ResetAllSlotStates()
    {
        foreach (Slot slot in allSlots)
        {
            slot.SetState(SlotState.None);
        }
    }

    public Slot FindSlotUnderSlime(Vector3 position)
    {
        Collider[] hitSlots = Physics.OverlapSphere(position, 0.4f, slotLayer);
        if (hitSlots.Length > 0) return hitSlots[0].GetComponent<Slot>();
        return null;
    }
}