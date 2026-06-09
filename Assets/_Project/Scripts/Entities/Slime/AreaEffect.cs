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
                // [수정] Enemy가 직접 관리하도록 '요청'
                enemy.AddSlow(gameObject.GetInstanceID(), data.slowRate);

                if (!enemyTimers.ContainsKey(enemy))
                {
                    // 현재 밟은 시간 + 쿨타임(1초) 뒤에 첫 데미지 예약
                    enemyTimers.Add(enemy, Time.time + data.dotDamageInterval);
                }
            }
        }
    }

    // 2. 적이 머무를 때: 쿨타임 체크 후 도트 딜 적용
    private void OnTriggerStay(Collider other)
    {
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
        // [수정] 이 장판이 걸었던 슬로우만 '해제 요청'
        enemy.RemoveSlow(gameObject.GetInstanceID());

        if (enemyTimers.ContainsKey(enemy))
        {
            enemyTimers.Remove(enemy);
        }
    }

    private void ClearAllEffects()
    {
        // 장판이 수명이 다해 사라질 때 내부에 갇혀있던 적들의 슬로우 일괄 해제
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