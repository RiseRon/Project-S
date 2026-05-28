using UnityEngine;
using System;

public class Barrier : MonoBehaviour
{
    public static Barrier Instance { get; private set; }

    [Header("Status")]
    public float maxHealth;
    public float currentHealth;
    public event Action OnHealthChanged;
    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // [수정] 스테이지 매니저가 호출할 초기화 함수
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
        OnHealthChanged?.Invoke();

        if (currentHealth <= 0)
        {
            DestroyBarrier();
        }
    }

    private void DestroyBarrier()
    {
        IsDestroyed = true;
        currentHealth = 0;
        Debug.Log("<color=red>방벽 파괴!</color>");
    }
}