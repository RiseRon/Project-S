using System.Collections.Generic;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    private int areaID;
    private SO_SlimeData data;

    private Dictionary<Enemy, float> enemyTimers = new Dictionary<Enemy, float>();

    public void Init(int id, SO_SlimeData slimeData) // 초기화 단계
    {
        areaID = id;
        data = slimeData;

        // 지정된 시간 후 ReturnToPool 호출
        CancelInvoke(); // 소환 직수 이전 기록 초기화
        Invoke(nameof(Deactivate), data.areaDuration); // duration 초가 지난 후 Deactivate 함수 실행
    }

    private void Deactivate() // 반납
    {
        // 반납 전 남아있는 적의 상태 원복
        ClearAllEffects();
        if (PoolManager.Instance != null)
            PoolManager.Instance.ReturnToPool(areaID, gameObject);
    }

    // 1. 적이 들어올 때: 슬로우 적용 및 데미지 타이머 초기화
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                // 즉시 슬로우 적용
                enemy.ApplySlow(data.slowRate);

                // 첫 데미지 시간 설정 (입장 즉시 혹은 1초 뒤)
                if (!enemyTimers.ContainsKey(enemy))
                {
                    enemyTimers.Add(enemy, Time.time + data.damageInterval);
                    // 만약 밟자마자 첫 틱 데미지를 주고 싶다면 여기서 TakeDamage 호출
                }
            }
        }
    }

    // 2. 적이 머무를 때: 1초 간격으로 도트 데미지 체크
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                // 현재 시간이 저장된 다음 데미지 시간보다 크거나 같다면
                if (enemyTimers.ContainsKey(enemy) && Time.time >= enemyTimers[enemy])
                {
                    enemy.TakeDamage(data.dotDamage);

                    // 다음 틱 시간 갱신: 현재 시간 + 1.0초
                    enemyTimers[enemy] = Time.time + data.damageInterval;
                }
            }
        }
    }

    // 3. 적이 나갈 때: 슬로우 해제 및 타이머 제거
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                RemoveEnemyEffect(enemy);
            }
        }
    }

    private void RemoveEnemyEffect(Enemy enemy)
    {
        enemy.ResetSlow(); // 슬로우 해제
        enemyTimers.Remove(enemy);
    }

    private void ClearAllEffects()
    {
        foreach (var enemy in enemyTimers.Keys)
        {
            if (enemy != null) enemy.ResetSlow();
        }
        enemyTimers.Clear();
    }
}