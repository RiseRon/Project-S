using UnityEngine;

public class Barrier : MonoBehaviour
{
    // 어디서든 방벽 상태를 확인할 수 있도록 싱글톤 처리
    public static Barrier Instance { get; private set; }

    [Header("Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
        Instance = this;
        currentHealth = maxHealth;
        IsDestroyed = false;
    }

    // 데미지를 입는 함수
    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return; // 이미 파괴되었다면 무시

        currentHealth -= damage;
        Debug.Log($"방벽 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            DestroyBarrier();
        }
    }

    // 방벽 파괴 처리
    private void DestroyBarrier()
    {
        IsDestroyed = true;
        currentHealth = 0;
        Debug.Log("방벽이 완전히 파괴되었습니다! 적들이 공격을 중지합니다.");

        // 필요 시 여기서 파괴 애니메이션이나 이펙트 실행
    }
}