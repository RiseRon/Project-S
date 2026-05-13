using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveTimerUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject visualRoot; // 대기 시간 아닐 때 숨길 부모 오브젝트

    private void Update()
    {
        if (WaveManager.Instance == null) return;

        bool isWaiting = WaveManager.Instance.IsWaitingNextWave;
        float remain = WaveManager.Instance.CurrentWaitTime;
        float total = WaveManager.Instance.TotalWaitTime;

        // 매니저가 대기 상태라고 하면 UI를 보여줌
        if (isWaiting)
        {
            if (!visualRoot.activeSelf) visualRoot.SetActive(true);

            // 슬라이더 업데이트 (시간이 거꾸로 줄어드는 연출)
            // 만약 게이지가 차오르는 게 좋다면: 1 - (remain / total)
            timerSlider.value = remain / total;

            if (timerText != null)
                timerText.text = $"{remain:F1}s";
        }
        else
        {
            // 대기 상태가 아니면(전투 중이면) 끈다
            if (visualRoot.activeSelf) visualRoot.SetActive(false);
        }
    }
}