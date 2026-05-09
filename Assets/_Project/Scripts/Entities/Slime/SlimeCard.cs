using UnityEngine;

public class SlimeCard : MonoBehaviour
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
}