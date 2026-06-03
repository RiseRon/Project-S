using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy targetEnemy;
    private SO_SlimeData data;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float progress; // 이동 진행도 (0~1)

    public void Setup(Enemy target, SO_SlimeData slimeData)
    {
        targetEnemy = target;
        data = slimeData;
        startPosition = transform.position;
        progress = 0;

        if (targetEnemy != null)
        {
            targetPosition = targetEnemy.transform.position;

            // [버그 픽스] 포물선/장판형이 공중에 깔리지 않도록 타겟을 '발 밑(바닥)'으로 고정
            if (data.trajectoryType == TrajectoryType.Parabolic || data.projectileType == ProjectileType.Floor)
            {
                targetPosition.y = 0.2f;
            }
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > 0)
        {
            progress += (data.projectileSpeed / distance) * Time.deltaTime;
        }

        if (data.trajectoryType == TrajectoryType.Straight) MoveStraight();
        else MoveParabolic();

        // 🎯 목표 지점 도달 시 타격 로직 실행
        if (progress >= 1.0f)
        {
            ExecuteHitLogic();
        }
    }

    private void MoveStraight()
    {
        // 유도탄: 적이 살아있으면 목표 지점을 계속 추적 (바닥 꽂힘 방지)
        if (targetEnemy != null && !targetEnemy.IsDead)
        {
            targetPosition = targetEnemy.transform.position;
        }
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }

    private void MoveParabolic()
    {
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
        float parabola = 1.0f - 4.0f * (progress - 0.5f) * (progress - 0.5f);
        currentPos.y += parabola * data.arcHeight;
        transform.position = currentPos;
    }

    /// <summary> 단일/범위/장판을 모두 관장하는 핵심 히트 로직 </summary>
    private void ExecuteHitLogic()
    {
        if (data.projectileType == ProjectileType.Single)
        {
            if (targetEnemy != null && !targetEnemy.IsDead)
            {
                targetEnemy.TakeDamage(data.damage);
                ApplyElementEffects(targetEnemy);
            }
        }
        else if (data.projectileType == ProjectileType.Area)
        {
            // 폭발 반경(attackRange) 내의 모든 적에게 범위 딜 및 효과 적용
            Collider[] hits = Physics.OverlapSphere(targetPosition, data.attackRange, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    enemy.TakeDamage(data.damage);
                    ApplyElementEffects(enemy);
                }
            }
        }
        else if (data.projectileType == ProjectileType.Floor)
        {
            SpawnArea();
        }

        // 투사체 반납 (하드코딩 제거, ID 연동)
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SpawnArea()
    {
        if (PoolManager.Instance != null)
        {
            GameObject areaObj = PoolManager.Instance.SpawnFromPool(data.areaPrefabID, targetPosition, Quaternion.identity);
            if (areaObj.TryGetComponent<AreaEffect>(out var area))
            {
                area.Init(data.areaPrefabID, data);
            }
        }
    }

    private void ApplyElementEffects(Enemy target)
    {
        if (data.elementType == SlimeElementType.Ice)
        {
            target.RequestStun(data.stunDuration);
        }
    }
}