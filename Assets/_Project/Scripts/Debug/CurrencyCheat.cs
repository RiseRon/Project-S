using UnityEngine;

/// <summary>
/// 개발 중 재화 테스트를 위한 치트 스크립트입니다.
/// </summary>
public class CurrencyCheat : MonoBehaviour
{
#if UNITY_EDITOR // 에디터에서 실행 중일 때만 작동하도록 제한 (빌드 시 자동 제외)

    [Header("설정")]
    [SerializeField] private int fragmentAddAmount = 1000;
    [SerializeField] private int completeAddAmount = 10;

    private void Update()
    {
        // F1 키: 마석 파편 추가
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.FragmentCoin, fragmentAddAmount);
                Debug.Log($"<color=yellow>[Cheat]</color> 마석 파편 +{fragmentAddAmount} 추가됨");
            }
        }

        // F2 키: 완전한 마석 추가
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.CompleteCoin, completeAddAmount);
                Debug.Log($"<color=yellow>[Cheat]</color> 완전한 마석 +{completeAddAmount} 추가됨");
            }
        }

        // F3 키: 재화 초기화
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CurrencyManager.Instance?.ResetCurrencies();
            Debug.Log("<color=red>[Cheat]</color> 재화 초기화 완료");
        }
    }

#endif
}