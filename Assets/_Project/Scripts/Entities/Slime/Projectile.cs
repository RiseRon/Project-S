using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy targetEnemy;
    private SO_SlimeData data;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    
    private float progress; // 포물선 이동 진행도 (0~1)
    private float lifeTimer; // 소멸 타이머

    public void Setup(Enemy target, SO_SlimeData slimeData)
    {
        targetEnemy = target;
        data = slimeData;
        startPosition = transform.position;

        progress = 0;
        lifeTimer = 0f;

        if (targetEnemy != null)
        {
            // [기획 반영] 실시간 유도(Homing) 삭제
            // 발사하는 순간에 적이 있던 위치를 목표(targetPosition)로 영구 고정합니다.
            targetPosition = targetEnemy.transform.position;

            // 포물선/장판형이 공중에 생성되지 않도록 목표 지점을 바닥으로 고정
            if (data.trajectoryType == TrajectoryType.Parabolic || data.projectileType == ProjectileType.Floor)
            {
                targetPosition.y = 0.2f;
            }

            // 시각적으로 날아가는 방향을 바라보게 회전
            Vector3 direction = (targetPosition - startPosition).normalized;
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }
        }
    }

    void Update()
    {
        // 1. 발사 후 3초 경과 시 강제 회수
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= 3.0f)
        {
            PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
            return;
        }

        // 2. 이동 진행도 연산 (속도 기반)
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > 0)
        {
            progress += (data.projectileSpeed / distance) * Time.deltaTime;
        }

        // 3. 궤도에 따른 이동
        if (data.trajectoryType == TrajectoryType.Straight)
        {
            MoveStraight();
        }
        else
        {
            MoveParabolic();
        }

        // 4. 발사 당시 목표 지점 도달 시 폭발/회수 로직 처리
        if (progress >= 1.0f)
        {
            ExecuteHitLogic();
        }
    }

    private void MoveStraight()
    {
        // 목표 지점(발사 당시 위치)을 향해 Lerp로 정확히 이동
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }

    private void MoveParabolic()
    {
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
        float parabola = 1.0f - 4.0f * (progress - 0.5f) * (progress - 0.5f);
        currentPos.y += parabola * data.arcHeight;
        transform.position = currentPos;
    }

    // [핵심] 단일 투사체가 날아가다가 적과 물리적으로 부딪혔을 때 처리
    private void OnTriggerEnter(Collider other)
    {
        if (data.projectileType == ProjectileType.Single && other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
            {
                // 부딪힌 즉시 데미지 및 효과 적용
                enemy.TakeDamage(data.damage);
                ApplyElementEffects(enemy);

                // 명중했으므로 더 날아가지 않고 즉시 풀로 반납
                PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
            }
        }
    }

    /// <summary> 단일/범위/장판을 모두 관장하는 핵심 히트 로직 </summary>
    private void ExecuteHitLogic()
    {
        // 단일(Single) 타입은 날아가다 맞지 않고(OnTriggerEnter 미발동) 
        // 목표 지점에 도착해버렸다면 데미지 없이 그냥 사라집니다. (논타겟팅 공격 빗나감)

        if (data.projectileType == ProjectileType.Area)
        {
            // 범위 공격: 폭발 반경(attackRange) 내의 모든 적 타격
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
            // 장판 공격: 바닥에 장판 생성
            SpawnArea();
        }

        // 할 일을 마친 투사체 풀로 반납
        PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
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