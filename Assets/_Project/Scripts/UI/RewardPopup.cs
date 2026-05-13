using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RewardPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image currencyIcon;

    [Header("Icon Assets")]
    [SerializeField] private Sprite fragmentIcon; // 인스펙터에서 '마석 파편' 이미지 할당
    [SerializeField] private Sprite completeIcon; // 인스펙터에서 '완전한 마석' 이미지 할당

    [Header("Settings")]
    [SerializeField] private float floatingSpeed = 25f;
    [SerializeField] private float lifeTime = 1.0f;

    private float timer;
    private int poolID;
    private int currentAmount;
    private bool isStatic = false; // 위로 올라가는지 여부 제어

    public void Setup(CurrencyType type, int amount, int id, Vector2 screenPos, bool isStatic = false)
    {
        poolID = id;
        timer = 0f;
        currentAmount = amount;
        this.isStatic = isStatic; // 설정값 저장

        transform.position = screenPos;

        if (currencyIcon != null)
            currencyIcon.sprite = (type == CurrencyType.FragmentCoin) ? fragmentIcon : completeIcon;

        if (amountText != null)
            amountText.text = $"+{amount}";
    }

    // 현재 팝업이 들고 있는 금액을 가져오는 함수
    public int GetCurrentAmount() => currentAmount;

    private void Update()
    {
        // isStatic이 false일 때만 위로 이동 (적 처치 시 등)
        if (!isStatic)
        {
            transform.Translate(Vector2.up * floatingSpeed * Time.deltaTime);
        }

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnToPool(poolID, gameObject);
        }
    }
}