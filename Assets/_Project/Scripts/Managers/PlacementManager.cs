using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LayerMask slotLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Stage State")]
    public int remainingMoves = 5;

    private Slime draggingSlime;
    private Slot currentOverSlot;
    private Vector3 originalPos;
    private Slot originalSlot;

    private Camera mainCam;

    private bool isDraggingFromInventory = false;
    private SO_SlimeData pendingSlimeData;
    private SlimeCard currentDraggingCard;
    public event Action OnSlimeMoved;

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

        // [관통형 수정] Raycast -> RaycastAll 변경
        // 슬라임이나 다른 장애물이 마우스를 가리고 있어도, 마우스 밑을 관통하여 모든 물체를 검사합니다.
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, slotLayer);

        foreach (RaycastHit hit in hits)
        {
            Slot slot = hit.collider.GetComponent<Slot>();
            if (slot != null)
            {
                // [머지 로직 수정] 슬롯을 발견하면, 빈 슬롯이든 꽉 찬 슬롯이든 일단 잡습니다.
                // 배치를 할지, 머지를 할지, 튕겨낼지는 EndDrag에서 판단합니다.
                currentOverSlot = slot;
                draggingSlime.transform.position = currentOverSlot.transform.position;
                return; // 가장 먼저 찾은 슬롯(가장 가까운)에 스냅하고 함수 종료
            }
        }

        // 슬롯을 못 찾았거나 맨땅인 경우 마우스 포인터를 정직하게 따라다닙니다.
        draggingSlime.transform.position = mouseGroundPos;
    }

    private void EndDrag()
    {
        if (draggingSlime != null) draggingSlime.isDragging = false;

        // 최종 데이터 배치가 가능한 '빈 자리' 인지 검증
        bool targetSlotAvailable = currentOverSlot != null &&
            (currentOverSlot.IsEmptyOrOccupiedBy(draggingSlime) || currentOverSlot == originalSlot);

        // 1. 단순 배치 로직 (빈 자리)
        if (targetSlotAvailable)
        {
            bool isFromInventory = (originalSlot == null);

            if (!isFromInventory && remainingMoves <= 0)
            {
                Debug.Log("슬롯 이동 횟수가 없습니다.");
                ReturnToOriginalPosition();
                ResetDragState();
                return;
            }

            if (originalSlot != null && originalSlot != currentOverSlot)
            {
                originalSlot.ClearSlot();
            }

            currentOverSlot.AssignSlime(draggingSlime);

            if (!isFromInventory)
            {
                if (originalSlot != currentOverSlot) 
                {
                    remainingMoves--;
                    Debug.Log($"남은 슬롯 이동 횟수: {remainingMoves}");
                    OnSlimeMoved?.Invoke();
                }
            }
            else if (currentDraggingCard != null)
            {
                PoolManager.Instance.ReturnToPool(911, currentDraggingCard.gameObject);
            }
        }
        // 2. 머지 로직 (빈 자리가 아닌 꽉 찬 자리이고, 조합식이 맞을 때)
        else if (!isDraggingFromInventory && currentOverSlot != null && currentOverSlot.CanMerge(draggingSlime))
        {
            // 머지 성공
            MergeManager.Instance.ExecuteMerge(draggingSlime, currentOverSlot);

            if (originalSlot != null)
            {
                originalSlot.ClearSlot();
            }
        }
        // 3. 실패 및 튕겨내기 (맨땅, 머지 실패, 남의 자리 등)
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