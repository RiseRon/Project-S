using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float dragDistanceThreshold = 10f; // 마우스가 이 픽셀만큼 움직여야 드래그로 판정
    [SerializeField] private float clickTimeThreshold = 0.3f;   // 누른 지 0.3초 안에 떼어야 클릭으로 판정
    [SerializeField] private LayerMask slimeLayer;              // 슬라임만 감지할 레이어 마스크

    // 🌟 [이벤트 선언] 다른 매니저들이 이 소식을 구독(Subscribe)해서 듣게 됩니다.
    public event Action<Slime> OnSlimeClicked;       // 슬라임을 "클릭" 했을 때 (스탯창 띄우기용)
    public event Action<Slime> OnSlimeDragStart;     // 슬라임을 "드래그 시작" 했을 때 (슬롯 상태 판별용)
    public event Action OnSlimeDragging;             // 슬라임을 "드래그 중" 일 때 (마우스 따라다니기용)
    public event Action OnSlimeDragEnd;              // 마우스를 "놓았을" 때 (배치/머지 실행용)

    private Camera mainCam;
    private Vector2 mouseDownPosition;
    private float mouseDownTime;
    private bool isPointerDown = false;
    private bool isCurrentlyDragging = false;

    private Slime targetSlime; // 현재 마우스 아래에 있는 슬라임

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
    }

    private void Update()
    {
        // UI(스탯창, 버튼 등) 위를 클릭했으면 필드 클릭은 완전히 무시합니다. (UI 관통 방지)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleInput();
    }

    private void HandleInput()
    {
        // 1. 마우스 누름 (Down)
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            mouseDownTime = Time.time;
            isPointerDown = true;
            isCurrentlyDragging = false;

            targetSlime = GetSlimeUnderMouse();
        }

        // 2. 마우스 누르고 움직이는 중 (Hold & Move)
        if (Input.GetMouseButton(0) && isPointerDown)
        {
            // 아직 드래그 판정이 안 났다면, 마우스가 얼만큼 움직였는지 거리 검사
            if (!isCurrentlyDragging)
            {
                float distance = Vector2.Distance(mouseDownPosition, Input.mousePosition);
                if (distance > dragDistanceThreshold) // 일정 거리 이상 움직이면 드래그로 확정!
                {
                    isCurrentlyDragging = true;
                    if (targetSlime != null)
                    {
                        OnSlimeDragStart?.Invoke(targetSlime); // 드래그 시작 알람 띠링!
                    }
                }
            }

            // 드래그 판정이 났다면 매 프레임 드래그 중이라고 알림
            if (isCurrentlyDragging && targetSlime != null)
            {
                OnSlimeDragging?.Invoke();
            }
        }

        // 3. 마우스를 뗌 (Up) -> 클릭인지 드래그 종료인지 최종 판별
        if (Input.GetMouseButtonUp(0) && isPointerDown)
        {
            isPointerDown = false;

            if (isCurrentlyDragging)
            {
                // 드래그를 하다가 뗐으므로 "드래그 종료"
                if (targetSlime != null)
                {
                    OnSlimeDragEnd?.Invoke();
                }
            }
            else
            {
                // 움직이지 않고 짧은 시간 안에 뗐으므로 "클릭"으로 인정!
                float timePassed = Time.time - mouseDownTime;
                if (timePassed <= clickTimeThreshold)
                {
                    OnSlimeClicked?.Invoke(targetSlime); // 대상이 없으면 null이 넘어가서 빈땅 클릭으로 인식됨
                }
            }

            // 상태 초기화
            isCurrentlyDragging = false;
            targetSlime = null;
        }
    }

    // 마우스 좌표에서 레이저를 쏴서 슬라임이 있는지 확인
    private Slime GetSlimeUnderMouse()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, slimeLayer))
        {
            return hit.collider.GetComponent<Slime>();
        }
        return null;
    }
}