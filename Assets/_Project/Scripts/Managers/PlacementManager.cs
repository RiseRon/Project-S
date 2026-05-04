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
        // 조건 4: 슬롯 위에서 드롭 + 비어있음 + 이동 횟수 남음
        if (currentOverSlot != null && currentOverSlot.IsEmpty && remainingMoves > 0)
        {
            draggingSlime.transform.position = currentOverSlot.transform.position;
            currentOverSlot.placedSlime = draggingSlime;

            // 이동 횟수 차감
            remainingMoves--;
            Debug.Log($"이동 성공! 남은 횟수: {remainingMoves}");
        }
        else
        {
            // 배치 불가 시 원래 위치로 복구
            draggingSlime.transform.position = originalPos;
            if (originalSlot != null) originalSlot.placedSlime = draggingSlime;
        }

        draggingSlime = null;
        currentOverSlot = null;
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
