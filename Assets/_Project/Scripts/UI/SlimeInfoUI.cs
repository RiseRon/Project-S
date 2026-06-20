using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용한다고 가정합니다. (일반 Text면 Text로 변경)

public class SlimeInfoUI : MonoBehaviour
{
    [Header("UI 전체 패널 (이것을 켜고 끕니다)")]
    [SerializeField] private GameObject infoPanel;

    [Header("텍스트 컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI attackTypeText;

    [Header("랭크(별) 표시 컴포넌트")]
    [Tooltip("Horizontal Layout Group 안에 있는 별 이미지 게임오브젝트를 1성, 2성, 3성 순서대로 넣어주세요.")]
    [SerializeField] private GameObject[] rankStars;

    [Header("특수 능력 패널 (조건에 따라 켜집니다)")]
    // 각 패널(GameObject) 아래에 아이콘 이미지와 텍스트가 자식으로 있다고 가정합니다.
    [SerializeField] private GameObject slowPanel;
    [SerializeField] private TextMeshProUGUI slowText;

    [SerializeField] private GameObject stunPanel;
    [SerializeField] private TextMeshProUGUI stunText;

    [SerializeField] private GameObject poisonPanel;
    [SerializeField] private TextMeshProUGUI poisonText;

    // 현재 스탯창이 띄워진 타겟 슬라임
    private Slime currentTargetSlime;

    private void Start()
    {
        // 시작할 때 UI는 보이지 않게 꺼둡니다.
        infoPanel.SetActive(false);

        // 통신병의 '클릭' 알람을 구독합니다.
        if (InputController.Instance != null)
            InputController.Instance.OnSlimeClicked += HandleSlimeClicked;
    }

    private void OnDestroy()
    {
        if (InputController.Instance != null)
            InputController.Instance.OnSlimeClicked -= HandleSlimeClicked;
    }

    // ==========================================
    // 1. 스탯창 열기 / 닫기 로직
    // ==========================================
    private void HandleSlimeClicked(Slime clickedSlime)
    {
        if (clickedSlime == null)
        {
            CloseUI();
            return;
        }

        currentTargetSlime = clickedSlime;
        UpdateUI(clickedSlime.Data);
        infoPanel.SetActive(true);
    }

    // 슬라임 데이터를 바탕으로 텍스트를 최신화합니다.
    private void UpdateUI(SO_SlimeData data)
    {
        if (data == null) return;

        // 1. 공통 스탯 텍스트 갱신
        damageText.text = data.damage.ToString("F1");
        rangeText.text = data.attackRange.ToString("F1");
        attackSpeedText.text = data.attackSpeed.ToString("F1");

        // 공격 타입 표기 변환 (Enum -> String)
        string typeStr = "";
        switch (data.projectileType)
        {
            case ProjectileType.Single: typeStr = "단일"; break;
            case ProjectileType.Area: typeStr = "범위"; break;
            case ProjectileType.Floor: typeStr = "장판"; break;
        }
        attackTypeText.text = typeStr;

        for (int i = 0; i < rankStars.Length; i++)
        {
            if (rankStars[i] != null)
            {
                // 인덱스(0,1,2)가 슬라임의 랭크(1,2,3)보다 작으면 켜고, 아니면 끕니다.
                // 예: rank가 2면 i=0, i=1일 때만 켜짐 (별 2개)
                rankStars[i].SetActive(i < data.rank);
            }
        }

        // 일단 모든 특수 패널을 깨끗하게 끕니다.
        if (slowPanel != null) slowPanel.SetActive(false);
        if (stunPanel != null) stunPanel.SetActive(false);
        if (poisonPanel != null) poisonPanel.SetActive(false);

        // 슬라임이 가진 데이터(속성)를 감지하여 해당하는 UI만 켜고 값을 채웁니다.
        if (data.slowRate > 0)
        {
            slowPanel.SetActive(true);
            // 0.5 같은 소수를 50으로 만들고 뒤에 %를 붙입니다. ("F0"은 소수점 제거)
            slowText.text = $"{(data.slowRate).ToString("F0")}%";
        }

        if (data.stunChance > 0)
        {
            stunPanel.SetActive(true);
            stunText.text = $"{(data.stunChance).ToString("F0")}%";
        }

        if (data.dotDamageInterval > 0)
        {
            poisonPanel.SetActive(true);
            poisonText.text = $"{data.dotDamageInterval}초";
        }
    }

    public void CloseUI()
    {
        infoPanel.SetActive(false);
        currentTargetSlime = null;
    }

    // ==========================================
    // 2. 제거(판매) 버튼 로직
    // ==========================================
    public void OnRemoveButtonClicked()
    {
        if (currentTargetSlime == null) return;
        if(PlacementController.Instance != null)
        {
            Slot slot = PlacementController.Instance.FindSlotUnderSlime(currentTargetSlime.transform.position);
            if (slot != null) slot.ClearSlot();
        }
        if(PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(currentTargetSlime.SlimeID, currentTargetSlime.gameObject);
        }
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_Slime_Pop");
        }
        CloseUI();
    }
}