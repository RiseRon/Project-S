using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private CurrencyType targetType;
    [SerializeField] private TextMeshProUGUI amountText;

    [Header("Popup Settings")]
    [SerializeField] private int rewardPopupID = 912; // PoolManager에 등록된 팝업 ID
    [SerializeField] private Transform popupSpawnPoint; // 재화 UI 밑의 생성 위치(Empty Object)

    private GameObject activePopup; // 현재 화면에 떠 있는 팝업 관리
    private int lastAmount; // 이전 잔액을 저장할 변수

    private void Start()
    {
        // 초기 잔액 저장
        if (CurrencyManager.Instance != null)
        {
            lastAmount = CurrencyManager.Instance.GetAmount(targetType);
        }
        RefreshUI();
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }
    }
    private void RefreshUI()
    {
        if (CurrencyManager.Instance != null && amountText != null)
        {
            int amount = CurrencyManager.Instance.GetAmount(targetType);
            amountText.text = amount.ToString("N0");
        }
    }
    private void HandleCurrencyChanged(CurrencyType type, int newAmount)
    {
        if (type == targetType && amountText != null)
        {
            // 1. 얻은 재화 양 계산 (현재 잔액 - 이전 잔액)
            int diff = newAmount - lastAmount;

            // 2. 텍스트 갱신
            amountText.text = newAmount.ToString("N0");

            // 3. 재화가 늘어났을 때만 팝업 표시
            if (diff > 0)
            {
                ShowCombinedPopup(type, diff);
            }

            // 4. 다음 계산을 위해 현재 잔액을 이전 잔액으로 업데이트
            lastAmount = newAmount;
        }
    }

    private void ShowCombinedPopup(CurrencyType type, int addedAmount)
    {
        if (PoolManager.Instance == null) return;

        int totalDisplayAmount = addedAmount;

        if (activePopup != null && activePopup.activeSelf)
        {
            RewardPopup oldPopup = activePopup.GetComponent<RewardPopup>();
            if (oldPopup != null)
                totalDisplayAmount += oldPopup.GetCurrentAmount();

            PoolManager.Instance.ReturnToPool(rewardPopupID, activePopup);
        }

        activePopup = PoolManager.Instance.SpawnFromPool(rewardPopupID, popupSpawnPoint.position, Quaternion.identity);

        if (activePopup != null)
        {
            activePopup.transform.SetParent(this.transform.parent);
            activePopup.transform.localScale = Vector3.one;

            RewardPopup newPopup = activePopup.GetComponent<RewardPopup>();
            if (newPopup != null)
            {
                // 마지막 인자로 true를 넣어 "위로 올라가지 마라"고 명령함
                newPopup.Setup(type, totalDisplayAmount, rewardPopupID, popupSpawnPoint.position, true);
            }
        }
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }
}