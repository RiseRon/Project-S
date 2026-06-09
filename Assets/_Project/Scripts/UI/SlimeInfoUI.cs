using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용한다고 가정합니다. (일반 Text면 Text로 변경)

public class SlimeInfoUI : MonoBehaviour
{
    [Header("UI 전체 패널 (이것을 켜고 끕니다)")]
    [SerializeField] private GameObject infoPanel;

    [Header("텍스트 컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI rangeText;

    // 현재 스탯창이 띄워진 타겟 슬라임
    private Slime currentTargetSlime;

    private void Start()
    {
        // 시작할 때 UI는 보이지 않게 꺼둡니다.
        infoPanel.SetActive(false);

        // 통신병의 '클릭' 알람을 구독합니다.
        if (InputController.Instance != null)
        {
            InputController.Instance.OnSlimeClicked += HandleSlimeClicked;
        }
    }

    private void OnDestroy()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnSlimeClicked -= HandleSlimeClicked;
        }
    }

    // ==========================================
    // 1. 스탯창 열기 / 닫기 로직
    // ==========================================
    private void HandleSlimeClicked(Slime clickedSlime)
    {
        // 빈 땅을 클릭했다면? (null이 넘어옴) -> 창을 닫습니다!
        if (clickedSlime == null)
        {
            infoPanel.SetActive(false);
            currentTargetSlime = null;
            return;
        }

        // 슬라임을 클릭했다면? -> 타겟을 갱신하고 UI를 엽니다!
        currentTargetSlime = clickedSlime;
        UpdateUI(clickedSlime.Data);
        infoPanel.SetActive(true);
    }

    // 슬라임 데이터를 바탕으로 텍스트를 최신화합니다.
    private void UpdateUI(SO_SlimeData data)
    {
        if (data == null) return;

        //nameText.text = data.slimeName;
        //levelText.text = $"Lv.{data.rank}";
        damageText.text = data.damage.ToString("F1"); // 소수점 1자리까지 표시
        attackSpeedText.text = data.attackSpeed.ToString("F1");
        rangeText.text = data.attackRange.ToString("F1");
    }

    // ==========================================
    // 2. 제거(판매) 버튼 로직
    // ==========================================
    public void OnRemoveButtonClicked()
    {
        if (currentTargetSlime == null) return;

        // 1. 사령탑에게 현재 슬라임이 밟고 있는 '슬롯'을 찾아달라고 합니다.
        Slot slot = PlacementController.Instance.FindSlotUnderSlime(currentTargetSlime.transform.position);

        // 2. 슬롯을 발견했다면, 상태를 완벽하게 비웁니다! (이걸 안하면 다음 슬라임 배치 불가)
        if (slot != null)
        {
            slot.ClearSlot();
        }

        // 3. 슬라임을 풀(Pool)로 돌려보내 화면에서 지웁니다.
        PoolManager.Instance.ReturnToPool(currentTargetSlime.SlimeID, currentTargetSlime.gameObject);

        // 4. 삭제가 끝났으니 스탯창을 닫습니다.
        infoPanel.SetActive(false);
        currentTargetSlime = null;
    }
}