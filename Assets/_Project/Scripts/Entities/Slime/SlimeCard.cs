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
            if (info.slimeID == id)
            {
                return info.cardSprite;
            }
        }
        return null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // PlacementManager에 카드 자신(this)을 함께 넘겨줍니다.
        PlacementManager.Instance.StartSummonDrag(slimeData, this);

        // 드래그 시작 시 카드는 숨깁니다.
        gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 3D 슬라임 이동은 PlacementManager에서 처리하므로 비워둡니다.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 배치가 끝나면 PlacementManager에서 처리하도록 유도하거나 
        // 로직에 따라 여기서 추가 처리를 합니다.
    }

    // 배치 실패 시 다시 카드를 보여주는 함수
    public void OnPlacementFailed()
    {
        gameObject.SetActive(true);
    }
}