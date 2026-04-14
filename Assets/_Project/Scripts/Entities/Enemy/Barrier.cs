using UnityEngine;

public class Barrier : MonoBehaviour
{
    public static Barrier Instance { get; private set; }

    private float currentHealth;
    private float maxHealth;
    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
        // 씬 싱글톤 설정
        if (Instance == null) Instance = this;
    }

    // [핵심] 스테이지 매니저가 호출할 초기화 함수
    public void Setup(float hp)
    {
        maxHealth = hp;
        currentHealth = maxHealth;
        IsDestroyed = false;
        Debug.Log($"방벽 설정 완료! 최대 체력: {maxHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHealth -= damage;
        Debug.Log($"방벽 피해 입음! 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDestroyed = true;
        Debug.Log("방벽이 파괴되었습니다!");

        // 스테이지 매니저에게 패배 알림
        // StageManager.Instance.OnStageFailed();
    }
}