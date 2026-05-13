using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;

    private void Start()
    {
        // 매니저의 이벤트에 내 함수(UpdateWaveDisplay)를 등록
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += UpdateWaveDisplay;
            UpdateWaveDisplay(); // 첫 시작 시 초기화
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독 해제
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged -= UpdateWaveDisplay;
        }
    }

    // 이벤트가 터질 때만 실행되는 함수
    private void UpdateWaveDisplay()
    {
        if (waveText == null) return;

        waveText.text = $"웨이브 {WaveManager.Instance.CurrentWave}";
    }
}