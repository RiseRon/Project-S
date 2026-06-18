using System.Collections.Generic;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    private int areaID;
    private SO_SlimeData data;

    // 적별 '다음 도트 딜이 들어갈 시간'을 기록
    private Dictionary<Enemy, float> enemyTimers = new Dictionary<Enemy, float>();
    // 장판 내부에 들어와 있는 모든 적을 예외 없이 추적하는 리스트
    private List<Enemy> trackedEnemies = new List<Enemy>();
    public void Init(int id, SO_SlimeData slimeData)
    {
        areaID = id;
        data = slimeData;

        // 풀에서 꺼내질 때 이전 장판 데이터가 남아있지 않도록 클리어
        enemyTimers.Clear();
        trackedEnemies.Clear();

        CancelInvoke();
        Invoke(nameof(Deactivate), data.areaDuration);
    }

    private void Deactivate()
    {
        ClearAllEffects();
        if (PoolManager.Instance != null)
            PoolManager.Instance.ReturnToPool(areaID, gameObject);
    }

    // 1. 적이 들어올 때
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            if (!trackedEnemies.Contains(enemy))
            {
                trackedEnemies.Add(enemy);
            }

            if (data.slowRate > 0)
            {
                enemy.AddSlow(gameObject.GetInstanceID(), data.slowRate);
            }

            // [수정] Interval이 0보다 크면 도트 딜이 있는 장판(독)으로 간주합니다!
            if (data.dotDamageInterval > 0 && data.damage > 0)
            {
                if (!enemyTimers.ContainsKey(enemy))
                {
                    enemyTimers.Add(enemy, Time.time + data.dotDamageInterval);
                }
            }
        }
    }

    // 2. 적이 머무를 때
    private void OnTriggerStay(Collider other)
    {
        // [수정] Interval이 없거나 데미지가 없으면(순수 슬로우 물장판) 즉시 패스!
        if (data.dotDamageInterval <= 0 || data.damage <= 0) return;

        if (other.CompareTag("Enemy") && other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            if (enemyTimers.ContainsKey(enemy) && Time.time >= enemyTimers[enemy])
            {
                // [수정] dotDamage 대신 기본 damage 변수를 사용합니다.
                enemy.TakeDamage(data.damage);
                enemyTimers[enemy] = Time.time + data.dotDamageInterval;
            }
        }
    }

    // 3. 적이 나갈 때: 슬로우 해제 및 타이머 취소
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
        // [수정] 슬로우를 걸었을 때만 해제 요청
        if (data.slowRate > 0)
        {
            enemy.RemoveSlow(gameObject.GetInstanceID());
        }

        if (enemyTimers.ContainsKey(enemy))
        {
            enemyTimers.Remove(enemy);
        }
        if (trackedEnemies.Contains(enemy))
        {
            trackedEnemies.Remove(enemy);
        }
    }

    private void ClearAllEffects()
    {
        if (data == null) return;
        // 장판이 사라질 때 안에 있던 모든 적의 효과 해제
        if (data.slowRate > 0)
        {
            foreach (var enemy in trackedEnemies)
            {
                // 적이 도중에 죽지 않고 씬에 살아있을 때만 해제 요청 (널 에러 방지)
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.RemoveSlow(gameObject.GetInstanceID());
                }
            }
        }
        trackedEnemies.Clear();
        enemyTimers.Clear();
    }
}