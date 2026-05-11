using UnityEngine;
using UnityEngine.EventSystems;

public class SlimeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 이 카드가 들고 있을 슬라임 데이터
    public SO_SlimeData slimeData;

    // 데이터를 셋업하는 함수
    public void Setup(SO_SlimeData data)
    {
        slimeData = data;

        // 여기서 나중에 UI 텍스트(이름, 공격력 등)를 업데이트할 수 있습니다.
        // 예: nameText.text = data.slimeName;
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