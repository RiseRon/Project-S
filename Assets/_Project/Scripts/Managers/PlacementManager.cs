using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LayerMask slotLayer; // 슬롯 레이어
    [SerializeField] private LayerMask groundLayer; // 바닥 레이어

    [Header("Stage State")]
    public int remainingMoves = 10; // 스테이지 전체 이동 횟수

    private Slime draggingSlime; // 현재 드래그 중인 슬라임
    private Slot currentOverSlot; // 마우스가 현재 올라가 있는 슬롯
    private Vector3 originalPos; // 드래그 시작 시점의 위치 (복귀용)
    private Slot originalSlot; // 드래그 시작 시점의 슬롯

    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCam = Camera.main;
    }

    void Update()
    {
        HandleInput();  
    }

    private void HandleInput()
    {
        // 1. 마우스 클릭 시작 (슬라임 선택)
        if (Input.GetMouseButtonDown(0))
        {
            BeginDrag();
        }

        // 2. 드래그 중
        if (Input.GetMouseButton(0) && draggingSlime != null)
        {
            OnDragging();
        }

        // 3. 드롭
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
            // 클릭한 대상이 슬라임인지 확인 (Slime 컴포넌트 필요)
            Slime slime = hit.collider.GetComponent<Slime>();
            if (slime != null)
            {
                draggingSlime = slime;
                originalPos = draggingSlime.transform.position;

                // 기존에 슬롯에 있던 녀석이면 슬롯 정보 저장 및 비우기
                // 드래그 시작 시점에는 아직 횟수를 깎지 않음
                originalSlot = FindSlotUnderSlime(draggingSlime.transform.position);
                if (originalSlot != null) originalSlot.placedSlime = null;
            }
        }
    }

    private void OnDragging()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // 슬롯 레이어를 우선 체크
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, slotLayer))
        {
            currentOverSlot = hit.collider.GetComponent<Slot>();

            // 조건 3: 슬롯 위이고 비어있으면 중앙 고정, 아니면 마우스 따라가기
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
            // 바닥 높이에 맞춰 이동 (쿼터뷰 고려)
            draggingSlime.transform.position = hit.point;
        }
    }

    private void EndDrag()
    {
        // 1. 빈 슬롯에 배치하는 경우
        if (currentOverSlot != null && currentOverSlot.IsEmpty)
        {
            // 인벤토리에서 온 것인지(기존 슬롯이 없는지) 확인
            bool isFromInventory = (originalSlot == null);

            // [예외 처리] 맵에서 맵으로 이동하는데 이동 횟수가 없는 경우 배치 취소
            if (!isFromInventory && remainingMoves <= 0)
            {
                ReturnToOriginalPosition();
                return;
            }

            // [배치 실행] 슬라임을 목표 슬롯으로 이동 및 정보 갱신
            draggingSlime.transform.position = currentOverSlot.transform.position;
            currentOverSlot.placedSlime = draggingSlime;

            // [횟수 차감] 맵에서 맵으로 이동한 경우에만 차감
            if (!isFromInventory)
            {
                remainingMoves--;
                Debug.Log($"슬롯 간 이동 완료! 남은 이동 횟수: {remainingMoves}");
            }
            else
            {
                Debug.Log("인벤토리에서 최초 배치 완료! (이동 횟수 차감 안 됨)");
            }
        }
        // 2. 머지(Merge)가 가능한 슬롯에 놓은 경우
        else if (currentOverSlot != null && currentOverSlot.CanMerge(draggingSlime))
        {
            // 여기에 머지 로직 실행 (아래 매니저 관련 설명 참고)
            // ex) MergeManager.Instance.ExecuteMerge(draggingSlime, currentOverSlot);
        }
        // 3. 배치할 수 없는 곳(허공, 병합 불가능한 꽉 찬 슬롯 등)에 놓은 경우
        else
        {
            ReturnToOriginalPosition();
        }

        // 상태 초기화
        draggingSlime = null;
        currentOverSlot = null;
    }

    // 복구 로직
    private void ReturnToOriginalPosition()
    {
        draggingSlime.transform.position = originalPos;
        if (originalSlot != null)
        {
            originalSlot.placedSlime = draggingSlime;
        }
        else
        {
            // 만약 인벤토리에서 꺼내다가 취소한 거라면 인벤토리 UI로 다시 돌려보내는 로직 필요
            Debug.Log("인벤토리로 슬라임 복귀");
        }
    }

    private Slot FindSlotUnderSlime(Vector3 position)
    {
        // 슬라임 위치에서 아래로 Ray를 쏴서 슬롯을 찾는 보조 함수
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, slotLayer))
        {
            return hit.collider.GetComponent<Slot>();
        }
        return null;
    }
}
