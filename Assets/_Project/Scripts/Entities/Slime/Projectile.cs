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

        progress = 0f;
        lifeTimer = 0f;

        if (targetEnemy != null)
        {
            // 🎯 [1차 예측] 현재 거리 기준으로 총알이 닿을 예상 시간 계산
            float dist1 = Vector3.Distance(startPosition, targetEnemy.transform.position);
            float time1 = dist1 / data.projectileSpeed;

            // 웨이포인트 경로를 따라 1차 미래 위치를 가져옵니다.
            Vector3 predictedPos1 = targetEnemy.GetPredictedPosition(time1);

            // 🎯🎯 [2차 정밀 예측] 예측된 1차 미래 위치까지의 거리를 다시 재서 시간을 100% 보정
            float dist2 = Vector3.Distance(startPosition, predictedPos1);
            float time2 = dist2 / data.projectileSpeed;

            // 코너 꺾임까지 완벽하게 반영된 최종 타겟 위치
            targetPosition = targetEnemy.GetPredictedPosition(time2);

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
        // 1. 발사 후 3초 경과 시 강제 회수 (허공으로 빗나간 총알 삭제)
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= 3.0f)
        {
            PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
            return;
        }

        // 2. 궤도에 따른 이동 분기
        if (data.trajectoryType == TrajectoryType.Straight)
        {
            MoveStraight();
        }
        else
        {
            MoveParabolic();
        }
    }

    private void MoveStraight()
    {
        // [수정] 직선 단일 운동: 목표 좌표에 도달해도 멈추지 않고, 바라보는 방향으로 계속 뚫고 날아갑니다.
        transform.position += transform.forward * data.projectileSpeed * Time.deltaTime;

        // [수정] 범위/장판(Area/Floor) 공격일 때만! 날아간 거리를 재서 바닥에서 터지도록 처리합니다.
        // 단일(Single) 총알은 이 코드를 무시하고 적을 만날때까지 직진합니다.
        if (data.projectileType != ProjectileType.Single)
        {
            float totalDistance = Vector3.Distance(startPosition, targetPosition);
            float currentDistance = Vector3.Distance(startPosition, transform.position);

            // 목표 지점 거리만큼 날아갔다면 폭발(도착)
            if (currentDistance >= totalDistance)
            {
                ExecuteHitLogic();
            }
        }
    }

    private void MoveParabolic()
    {
        // 포물선 연산 로직 (기존과 동일)
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > 0)
        {
            progress += (data.projectileSpeed / distance) * Time.deltaTime;
        }

        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
        float parabola = 1.0f - 4.0f * (progress - 0.5f) * (progress - 0.5f);
        currentPos.y += parabola * data.arcHeight;
        transform.position = currentPos;

        // 포물선은 목표 지점(바닥)에 도달하면 무조건 폭발/도착 처리
        if (progress >= 1.0f)
        {
            ExecuteHitLogic();
        }
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
            Debug.Log("스턴적용");
        }
    }
}