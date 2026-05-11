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

    // Enemy에서 호출할 때 screenPos까지 받도록 구성
    public void Setup(CurrencyType type, int amount, int id, Vector2 screenPos)
    {
        poolID = id;
        timer = 0f;

        // 1. 위치 설정
        transform.position = screenPos;

        // 2. 아이콘 설정 (삼항 연산자 오류 수정)
        if (currencyIcon != null)
        {
            currencyIcon.sprite = (type == CurrencyType.FragmentCoin) ? fragmentIcon : completeIcon;
        }

        // 3. 텍스트 설정
        if (amountText != null)
        {
            amountText.text = $"+{amount}";
        }
    }

    private void Update()
    {
        // 위로 이동 (Overlay이므로 RectTransform 기준 이동)
        transform.Translate(Vector2.up * floatingSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnToPool(poolID, gameObject);
        }
    }
}