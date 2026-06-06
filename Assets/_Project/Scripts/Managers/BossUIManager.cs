using UnityEngine;

public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance { get; private set; }

    [Header("--- 보스별 실제 UI 패널 (빈 오브젝트) ---")]
    [SerializeField] private BossHPUI midBossUI;  // UI_MidBoss_HPBar
    [SerializeField] private BossHPUI finalBossUI; // UI_FinalBoss_HPBar

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 보스가 소환될 때 호출되어 해당하는 보스의 알맹이 패널만 켜주는 함수
    /// </summary>
    public void TurnOnBossUI(string enemyName, float maxHp, float maxCool, float currentCool)
    {
        if (enemyName == "MidBoss" && midBossUI != null)
        {
            midBossUI.gameObject.SetActive(true);
            midBossUI.hpSlider.maxValue = maxHp;
            midBossUI.hpSlider.value = maxHp;
            midBossUI.coolTimeSlider.maxValue = maxCool;
            midBossUI.coolTimeSlider.value = currentCool;
            UpdateCoolTimeText(midBossUI, currentCool, maxCool);
        }
        else if (enemyName == "FinalBoss" && finalBossUI != null)
        {
            finalBossUI.gameObject.SetActive(true);
            finalBossUI.hpSlider.maxValue = maxHp;
            finalBossUI.hpSlider.value = maxHp;
            finalBossUI.coolTimeSlider.maxValue = maxCool;
            finalBossUI.coolTimeSlider.value = currentCool;
            UpdateCoolTimeText(finalBossUI, currentCool, maxCool);
        }
    }

    // 보스 이름을 받아 매칭되는 슬라이더의 HP를 정확히 깎습니다.
    public void UpdateHP(string enemyName, float currentHp)
    {
        if (enemyName == "MidBoss" && midBossUI != null && midBossUI.gameObject.activeSelf)
        {
            midBossUI.hpSlider.value = currentHp;
        }
        else if (enemyName == "FinalBoss" && finalBossUI != null && finalBossUI.gameObject.activeSelf)
        {
            finalBossUI.hpSlider.value = currentHp;
        }
    }

    // 보스 이름을 받아 매칭되는 슬라이더의 쿨타임과 텍스트를 정확히 갱신합니다.
    public void UpdateCoolTime(string enemyName, float currentCoolTime)
    {
        if (enemyName == "MidBoss" && midBossUI != null && midBossUI.gameObject.activeSelf)
        {
            midBossUI.coolTimeSlider.value = currentCoolTime;
            UpdateCoolTimeText(midBossUI, currentCoolTime, midBossUI.coolTimeSlider.maxValue);
        }
        else if (enemyName == "FinalBoss" && finalBossUI != null && finalBossUI.gameObject.activeSelf)
        {
            finalBossUI.coolTimeSlider.value = currentCoolTime;
            UpdateCoolTimeText(finalBossUI, currentCoolTime, finalBossUI.coolTimeSlider.maxValue);
        }
    }

    private void UpdateCoolTimeText(BossHPUI container, float currentCool, float maxCool)
    {
        if (container == null || container.coolTimeText == null) return;

        float remainingTime = maxCool - currentCool;
        if (remainingTime < 0f) remainingTime = 0f;

        container.coolTimeText.text = $"{remainingTime:F0}s";
    }

    /// <summary>
    /// 특정 보스가 죽었을 때 해당 보스의 패널만 정확히 끕니다.
    /// </summary>
    public void TurnOffBossUI(string enemyName)
    {
        if (enemyName == "MidBoss" && midBossUI != null)
        {
            midBossUI.gameObject.SetActive(false);
        }
        else if (enemyName == "FinalBoss" && finalBossUI != null)
        {
            finalBossUI.gameObject.SetActive(false);
        }
    }
}