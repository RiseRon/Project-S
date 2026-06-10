using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlimeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [System.Serializable]
    public struct CardVisualInfo
    {
        public int slimeID;       // 데이터 테이블의 슬라임 ID
        public Sprite cardSprite; // 해당 슬라임에 사용할 카드 이미지
    }

    [Header("--- UI Components ---")]
    [SerializeField] private Image cardImage; // 카드의 외관을 바꿀 이미지 컴포넌트

    [Header("--- Card Visual Settings ---")]
    [Tooltip("슬라임 ID에 맵핑될 이미지 리스트를 등록해주세요.")]
    [SerializeField] private List<CardVisualInfo> visualList; // 인스펙터 매칭용 리스트

    // 이 카드가 들고 있을 슬라임 데이터
    public SO_SlimeData slimeData;

    // 데이터를 셋업하는 함수
    public void Setup(SO_SlimeData data)
    {
        slimeData = data;

        if (cardImage != null && slimeData != null)
        {
            Sprite targetSprite = GetSpriteBySlimeID(slimeData.id);

            if (targetSprite != null)
            {
                cardImage.sprite = targetSprite;
            }
            else
            {
                Debug.LogWarning($"[SlimeCard] ID {slimeData.id}에 해당하는 스프라이트가 visualList에 등록되지 않았습니다.");
            }
        }
    }

    private Sprite GetSpriteBySlimeID(int id)
    {
        foreach (var info in visualList)
        {
            if (info.slimeID == id) return info.cardSprite;
        }
        return null;
    }
    private void OnDisable()
    {
        if (cardImage != null)
        {
            cardImage.color = new Color(1f, 1f, 1f, 1f);
        }
        cardImage.sprite = null;
    }

    // ==========================================
    // [드래그 이벤트 연동 구간]
    // ==========================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. 사령탑에게 슬라임 소환을 요청합니다.
        PlacementController.Instance.StartSummonDrag(slimeData, this);

        //  [핵심 수정] SetActive(false) 대신 투명도(Alpha)를 0으로 만들어 이벤트를 유지합니다!
        if (cardImage != null)
        {
            cardImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 2. 마우스를 움직일 때마다 3D 사령탑의 이동 로직을 강제로 실행시킵니다.
        PlacementController.Instance.HandleDragging();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 3. 마우스를 놓았을 때 사령탑에게 배치/머지 확정 명령을 내립니다.
        PlacementController.Instance.HandleDragEnd();
    }

    // 배치 실패 시 다시 카드를 보여주는 함수
    public void OnPlacementFailed()
    {
        //  투명도를 다시 100%로 되돌려 카드가 보이게 합니다.
        if (cardImage != null)
        {
            cardImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }
}