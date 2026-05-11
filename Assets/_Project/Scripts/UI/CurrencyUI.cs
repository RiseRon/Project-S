using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private CurrencyType targetType; // 인스펙터에서 FragmentCoin 또는 CompleteCoin 선택
    [SerializeField] private TextMeshProUGUI amountText;

    private void Start()
    {
        // 1. 초기 UI 표시
        RefreshUI();

        // 2. 이벤트 구독 (매니저가 있을 때만)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }
    }

    private void OnEnable()
    {
        // 씬이 바뀌거나 오브젝트가 활성화될 때 다시 한번 초기화
        RefreshUI();
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
        // 내가 관리하는 재화 타입이 맞을 때만 텍스트 변경
        if (type == targetType && amountText != null)
        {
            amountText.text = newAmount.ToString("N0");
        }
    }

    private void OnDestroy()
    {
        // 중요: 오브젝트가 파괴될 때 이벤트 구독을 해제해야 에러가 안 남
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }
    }
}