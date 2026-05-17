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
                draggingSlime.isDragging = true;

                if (draggingSlime.GetComponent<Collider>() != null)
                    draggingSlime.GetComponent<Collider>().enabled = false;
            }
            originalSlot = null;
        }
    }

    private void HandleInput()
    {
        // 마우스를 클릭했을 때 (필드 드래그 시작)
        if (Input.GetMouseButtonDown(0) && !isDraggingFromInventory)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            BeginDrag();
        }

        // 마우스를 누르고 움직이는 중 (실시간 드래그)
        if (Input.GetMouseButton(0) && draggingSlime != null)
        {
            OnDragging();
        }

        // 마우스를 놓았을 때 (배치 확정 및 판정)
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
                draggingSlime.isDragging = true;
                originalPos = draggingSlime.transform.position;

                // 구체 범위 검출로 슬라임 발밑의 원래 슬롯을 정밀하게 백업합니다.
                originalSlot = FindSlotUnderSlime(draggingSlime.transform.position);

                // 핵심 수정: 집어들는 즉시 originalSlot.ClearSlot()을 호왈하여 슬롯의 데이터를 비웁니다.
                // 비우지 않으면 originalSlot.placedSlime 참조가 남아 IsDataEmpty 검증이 오염되어 중복 배치가 발생합니다.
                if (originalSlot != null) originalSlot.ClearSlot();

                // 드래그 중인 본인의 몸통 콜라이더가 레이캐스트를 방해하지 않도록 잠시 끕니다.
                if (draggingSlime.GetComponent<Collider>() != null)
                    draggingSlime.GetComponent<Collider>().enabled = false;
            }
        }
    }

    private void OnDragging()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // 슬라임 위치와 무관한 순수 마우스의 3D 바닥 좌표 연산
        Vector3 mouseGroundPos = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
        {
            mouseGroundPos = groundHit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float entry))
            {
                mouseGroundPos = ray.GetPoint(entry);
            }
        }

        // 이전 프레임의 슬롯 조준 찌꺼기 잔상을 매 프레임 초기화합니다.
        currentOverSlot = null;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, slotLayer))
        {
            Slot slot = hit.collider.GetComponent<Slot>();
            if (slot != null)
            {
                // 배치 가능한 슬롯일 때만 currentOverSlot에 등록합니다.
                // 가득 찬 슬롯은 절대 currentOverSlot에 담지 않아 EndDrag에서 배치 시도를 차단합니다.
                if (slot.IsDataEmpty || slot == originalSlot)
                {
                    currentOverSlot = slot;
                    draggingSlime.transform.position = currentOverSlot.transform.position;
                    return;
                }
            }
        }

        // 꽉 찬 슬롯이거나 맨땅인 경우 마우스 포인터를 정직하게 따라다닙니다.
        draggingSlime.transform.position = mouseGroundPos;
    }

    private void EndDrag()
    {
        // 배치가 판단되는 연산 순간이므로 조작 정지 상태로 전환 (콜라이더는 맨 마지막에 켭니다)
        if (draggingSlime != null) draggingSlime.isDragging = false;

        // OnDragging()이 매 프레임 currentOverSlot을 정확히 관리합니다.
        // EndDrag에서 추가 레이캐스트를 하면 슬롯 콜라이더를 잘못 히트하여 중복 배치가 발생하므로 제거합니다.

        // 최종 데이터 철벽 배치 검증
        // ✅ 버그 수정: IsDataEmpty 대신 IsEmptyOrOccupiedBy(draggingSlime)로 검증하여
        //    드래그 중인 슬라임 자신이 슬롯에 등록된 경우(= 제자리)도 빈 슬롯으로 정확히 판단합니다.
        bool targetSlotAvailable = currentOverSlot != null &&
            (currentOverSlot.IsEmptyOrOccupiedBy(draggingSlime) || currentOverSlot == originalSlot);

        if (targetSlotAvailable)
        {
            bool isFromInventory = (originalSlot == null);

            // 필드 이동인데 잔여 횟수가 소진된 경우 강제 튕김 처리
            if (!isFromInventory && remainingMoves <= 0)
            {
                ReturnToOriginalPosition();
                ResetDragState();
                return;
            }

            // 배치가 완벽히 확정되었으므로 원래 딛고 있던 슬롯을 안전하게 청소합니다.
            if (originalSlot != null && originalSlot != currentOverSlot)
            {
                originalSlot.ClearSlot();
            }

            // 슬롯에게 직접 고정 처리를 지시하여 데이터와 물리 좌표를 동시에 완전히 동기화합니다.
            currentOverSlot.AssignSlime(draggingSlime);

            if (!isFromInventory)
            {
                if (originalSlot != currentOverSlot) remainingMoves--;
            }
            else if (currentDraggingCard != null)
            {
                Destroy(currentDraggingCard.gameObject);
            }
        }
        else if (!targetSlotAvailable && currentOverSlot != null && currentOverSlot.CanMerge(draggingSlime))
        {
            // MergeManager.Instance.ExecuteMerge(draggingSlime, currentOverSlot);
        }
        else
        {
            // 꽉 찬 슬롯에 던졌거나 맨땅에 놓아 배치가 실패한 경우 처리
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

        ResetDragState();
    }

    private void ResetDragState()
    {
        // 배치가 완벽히 끝나 월드 상에 데이터가 안전 안착된 직후 콜라이더 스위치를 켭니다.
        if (draggingSlime != null && draggingSlime.GetComponent<Collider>() != null)
            draggingSlime.GetComponent<Collider>().enabled = true;

        draggingSlime = null;
        currentOverSlot = null;
        originalSlot = null;
        isDraggingFromInventory = false;
    }

    private void ReturnToOriginalPosition()
    {
        // 복귀할 때도 원래 백업 슬롯 데이터가 존재한다면 슬롯에게 완벽한 재고정을 지시합니다.
        draggingSlime.transform.position = originalPos;
        if (originalSlot != null) originalSlot.AssignSlime(draggingSlime);
    }

    private Slot FindSlotUnderSlime(Vector3 position)
    {
        // 바늘 같은 레이캐스트 대신, 구체 오버랩 범위를 활용하여 꼬인 배치 상태에서도 슬롯을 완벽 추적합니다.
        Collider[] hitSlots = Physics.OverlapSphere(position, 0.4f, slotLayer);
        if (hitSlots.Length > 0)
        {
            return hitSlots[0].GetComponent<Slot>();
        }
        return null;
    }
}