using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarrierHPUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI barrierHPText;
    [SerializeField] private Slider barrierHPSlider;

    void Start()
    {
        if (Barrier.Instance != null)
        {
            Barrier.Instance.OnHealthChanged += UpdateBarrierHPDisplay;
        }
        if (barrierHPSlider != null)
        {
            barrierHPSlider.minValue = 0f;
            barrierHPSlider.maxValue = Barrier.Instance.MaxHealth;
        }
        UpdateBarrierHPDisplay();
    }
    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독 해제
        if (Barrier.Instance != null)
        {
            Barrier.Instance.OnHealthChanged -= UpdateBarrierHPDisplay;
        }
    }
    public void UpdateBarrierHPDisplay()
    {
        if (barrierHPText == null) return;
        if (barrierHPSlider == null) return;

        barrierHPText.text = $"{Barrier.Instance.CurrentHealth}";
        barrierHPSlider.value = Barrier.Instance.CurrentHealth;
    }
}
