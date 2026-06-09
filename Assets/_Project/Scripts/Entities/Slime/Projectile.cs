using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy targetEnemy;
    private SO_SlimeData data;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float progress;  // 포물선 이동 진행도 (0 ~ 1)
    private float lifeTimer; // 무한 루프(허공에 멈춤) 방지용 소멸 타이머

    public void Setup(Enemy target, SO_SlimeData slimeData)
    {
        targetEnemy = target;
        data = slimeData;
        startPosition = transform.position;

        progress = 0f;
        lifeTimer = 0f;

        if (targetEnemy != null)
        {
            // 1차 예측: 현재 거리 기준으로 투사체가 닿을 예상 시간 계산
            float dist1 = Vector3.Distance(startPosition, targetEnemy.transform.position);
            float time1 = dist1 / data.projectileSpeed;
            Vector3 predictedPos1 = targetEnemy.GetPredictedPosition(time1);

            // 2차 정밀 예측: 1차 예측 위치까지의 거리를 다시 재서 시간을 100% 보정 (코너 꺾임 완벽 반영)
            float dist2 = Vector3.Distance(startPosition, predictedPos1);
            float time2 = dist2 / data.projectileSpeed;
            targetPosition = targetEnemy.GetPredictedPosition(time2);
        }
        else
        {
            targetPosition = transform.position;
        }
    }

    private void Update()
    {
        // 안전장치: 투사체가 모종의 이유로 도착 판정을 받지 못하면 5초 뒤 자동 소멸
        lifeTimer += Time.deltaTime;
        if (lifeTimer > 5f)
        {
            PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
            return;
        }

        // 궤도 설정에 따른 이동 처리
        if (data.trajectoryType == TrajectoryType.Straight)
        {
            MoveStraight();
        }
        else if (data.trajectoryType == TrajectoryType.Parabolic)
        {
            MoveParabolic();
        }
    }

    private void MoveStraight()
    {
        // 타겟 위치를 향해 직선으로 등속도 이동
        Vector3 dir = (targetPosition - transform.position).normalized;
        transform.position += dir * data.projectileSpeed * Time.deltaTime;

        // 도착 판정: 거리가 0.2f 이하로 좁혀지면 도착한 것으로 간주
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            Arrive();
        }
    }

    private void MoveParabolic()
    {
        // 시작점과 끝점 사이를 곡선(포물선)으로 이동
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance <= 0f) return;

        progress += (Time.deltaTime * data.projectileSpeed) / distance;

        // Vector3.Lerp로 기본 직선 보간 후, Y축에 곡선(arcHeight) 추가
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
        currentPos.y += Mathf.Sin(progress * Mathf.PI) * data.arcHeight;

        transform.position = currentPos;

        // 도착 판정: 진행도가 100%가 되면 도착
        if (progress >= 1f)
        {
            Arrive();
        }
    }

    // 단일(Single) 공격용: 투사체가 적과 직접 충돌했을 때 발동
    private void OnTriggerEnter(Collider other)
    {
        if (data.projectileType != ProjectileType.Single) return;

        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(data.damage);
                ApplyElementEffects(enemy);

                PoolManager.Instance.ReturnToPool(data.projectilePrefabID, gameObject);
            }
        }
    }

    // 범위(Area) 및 장판(Floor) 공격용: 투사체가 목표 지점에 도달했을 때 발동
    private void Arrive()
    {
        if (data.projectileType == ProjectileType.Area)
        {
            // [수정] 폭발 반경(2.0f)을 사거리와 분리하여 임시로 하드코딩
            float explosionRadius = 2.0f;

            // [수정] 레이어 마스크 검사를 제거하고, 충돌한 모든 객체 중 태그와 컴포넌트로 적만 골라냅니다.
            Collider[] hits = Physics.OverlapSphere(targetPosition, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy") && hit.TryGetComponent<Enemy>(out var enemy) && !enemy.IsDead)
                {
                    enemy.TakeDamage(data.damage);
                    ApplyElementEffects(enemy);
                }
            }
        }
        else if (data.projectileType == ProjectileType.Floor)
        {
            // 바닥에 장판 생성
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

    private void ApplyElementEffects(Enemy enemy)
    {
        // 속성에 따른 추가 상태 이상(기절 등) 부여
        if (data.stunChance > 0f && Random.value < data.stunChance)
        {
            enemy.RequestStun(data.stunDuration);
        }
    }
}