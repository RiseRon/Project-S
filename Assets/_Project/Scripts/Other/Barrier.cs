using UnityEngine;
using System;
using System.Diagnostics.Contracts;

public class Barrier : MonoBehaviour
{
    public static Barrier Instance { get; private set; }

    [Header("Status")]
    private float maxHealth;
    private float currentHealth;
    public event Action OnHealthChanged;
    public event Action OnBarrierDestroyed;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDestroyed { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // GameManager가 싱글톤으로 항상 살아있으므로 안전하게 접근 가능합니다.
        if (GameManager.Instance != null)
        {
            // 중복 구독 방지를 위해 한번 안전하게 빼준 뒤 연결(+=)합니다.
            this.OnBarrierDestroyed -= GameManager.Instance.HandlePlayerDefeat;
            this.OnBarrierDestroyed += GameManager.Instance.HandlePlayerDefeat;

            Debug.Log("<color=lime>[Barrier]</color> 안전하게 GameManager의 패배 이벤트 채널에 스스로를 등록했습니다!");
        }
    }

    // 스테이지 매니저가 호출할 초기화 함수
    public void InitBarrier(int hp)
    {
        maxHealth = hp;
        currentHealth = maxHealth;
        IsDestroyed = false;

        Debug.Log($"<color=green>[Barrier]</color> 방벽 초기화 완료. HP: {maxHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyBarrier();
            SoundManager.Instance.PlaySFX("SFX_Barrier_Destroyed");
        }

        OnHealthChanged?.Invoke();
    }

    private void DestroyBarrier()
    {
        PlayExplosionEffect();
        IsDestroyed = true;
        currentHealth = 0;
        Debug.Log("<color=red>방벽 파괴!</color>");
        OnBarrierDestroyed?.Invoke();
    }
    private void PlayExplosionEffect()
    {
        Debug.Log("💥 배리어가 파괴되는 화려한 연출 출력 중...!");
        // 예: PoolManager.Instance.SpawnFromPool(파괴이펙트ID, transform.position, Quaternion.identity);
    }
#if UNITY_EDITOR
    public void SetBarrierDie()
    {
        IsDestroyed = !IsDestroyed;
        Debug.Log($"<color=red>[Cheat]</color> 배리어 무적 {(IsDestroyed? "활성화" : "비활성화")}");
    }
#endif
}