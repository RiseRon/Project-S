using System.Collections.Generic;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    private int areaID;
    private SO_SlimeData data;

    // 적별 '다음 도트 딜이 들어갈 시간'을 기록
    private Dictionary<Enemy, float> enemyTimers = new Dictionary<Enemy, float>();

    public void Init(int id, SO_SlimeData slimeData)
    {
        areaID = id;
        data = slimeData;

        CancelInvoke();
        Invoke(nameof(Deactivate), data.areaDuration);
    }

    private void Deactivate()
    {
        ClearAllEffects();
        if (PoolManager.Instance != null)
            PoolManager.Instance.ReturnToPool(areaID, gameObject);
    }

    // 1. 적이 들어올 때: 슬로우 요청 및 타이머 시작
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                // [수정] 슬로우 수치가 0보다 클 때만 슬로우 적용
                if (data.slowRate > 0)
                {
                    enemy.AddSlow(gameObject.GetInstanceID(), data.slowRate);
                }

                // [핵심 수정] 도트 데미지가 있을 때만 타이머에 등록!
                if (data.dotDamage > 0)
                {
                    if (!enemyTimers.ContainsKey(enemy))
                    {
                        // 현재 밟은 시간 + 쿨타임 뒤에 첫 데미지 예약
                        enemyTimers.Add(enemy, Time.time + data.dotDamageInterval);
                    }
                }
            }
        }
    }

    // 2. 적이 머무를 때: 쿨타임 체크 후 도트 딜 적용
    private void OnTriggerStay(Collider other)
    {
        // [핵심 수정] 도트 데미지가 아예 없으면 Stay 연산을 즉시 중단합니다. (최적화 및 버그 방지)
        if (data.dotDamage <= 0) return;

        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                if (enemyTimers.ContainsKey(enemy) && Time.time >= enemyTimers[enemy])
                {
                    enemy.TakeDamage(data.dotDamage);
                    // 다음 데미지 시간 갱신
                    enemyTimers[enemy] = Time.time + data.dotDamageInterval;
                }
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
    }

    private void ClearAllEffects()
    {
        // 장판이 사라질 때 안에 있던 모든 적의 효과 해제
        List<Enemy> currentEnemies = new List<Enemy>(enemyTimers.Keys);
        foreach (var enemy in enemyTimers.Keys)
        {
            if (enemy != null)
            {
                enemy.RemoveSlow(gameObject.GetInstanceID());
            }
        }
        enemyTimers.Clear();
    }
}