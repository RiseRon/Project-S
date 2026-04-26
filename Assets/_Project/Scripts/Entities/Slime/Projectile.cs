using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy targetEnemy;
    private SO_SlimeData data;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float progress = 0; // 이동 진행도 (0~1)

    public void Setup(Enemy target, SO_SlimeData slimeData)
    {
        targetEnemy = target;
        data = slimeData;
        startPosition = transform.position;

        // [기획 반영] 포물선/장판형은 발사 시점의 위치를 고정 타겟으로 함
        // 직선/유도형은 나중에 Update에서 실시간 타겟 위치를 참조함
        if (targetEnemy != null)
        {
            targetPosition = targetEnemy.transform.position;
        }
    }

    void Update()
    {
        // 진행도 계산: 속도에 따라 0에서 1까지 증가
        // 실제 거리 대비 속도를 적용하기 위해 거리를 나눕니다.
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (distance > 0)
        {
            progress += (data.projectileSpeed / distance) * Time.deltaTime;
        }

        if (data.trajectoryType == TrajectoryType.Straight)
        {
            MoveStraight();
        }
        else
        {
            MoveParabolic();
        }

        // 도착 체크
        if (progress >= 1.0f)
        {
            if (data.trajectoryType == TrajectoryType.Straight)
            {
                Hit();
            }
            else
            {
                SpawnArea();
            }
        }
    }

    // 1. 직선 이동 (유도형)
    private void MoveStraight()
    {
        // 타겟이 살아있다면 실시간으로 위치 업데이트 (유도)
        if (targetEnemy != null && !targetEnemy.IsDead)
            targetPosition = targetEnemy.transform.position;

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        // 진행 방향 바라보기
        transform.LookAt(targetPosition);
    }

    // 2. 포물선 이동 (지점 낙하)
    private void MoveParabolic()
    {
        // 선형 보간 위치 계산
        Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, progress);

        // 이차 곡선(Parabola) 공식 적용: y = 4h * x * (1 - x)
        // progress가 0.5일 때 최대 높이(data.arcHeight)에 도달합니다.
        float height = 4 * data.arcHeight * progress * (1 - progress);

        Vector3 finalPos = new Vector3(linearPos.x, linearPos.y + height, linearPos.z);

        // 이동 및 회전 처리
        transform.LookAt(finalPos + (finalPos - transform.position));
        transform.position = finalPos;
    }

    private void Hit()
    {
        // 1. 데미지 입히기
        targetEnemy.TakeDamage(data.attackDamage);

        // 2. 속성별 특수 효과 적용 (얼음-스턴)
        ApplyElementEffects();

        Destroy(gameObject);
    }

    private void SpawnArea()
    {
        if (PoolManager.Instance != null)
        {
            // 1. Pool에서 장판 오브젝트 소환 (현재 투사체 위치)
            GameObject areaObj = PoolManager.Instance.SpawnFromPool(data.areaPrefabID, targetPosition, Quaternion.identity);

            // 2. 장판 스크립트를 가져와서 유지 시간 및 ID 설정
            if (areaObj.TryGetComponent<AreaEffect>(out var area))
            {
                area.Init(data.areaPrefabID, data);
            }
        }

        // 투사체 파괴
        Destroy(gameObject);
    }

    private void ApplyElementEffects()
    {
        // 타겟 Enemy의 상태를 변화시키는 로직 (기획서 데이터 기반)
        if (data.elementType == SlimeElementType.Water)
        {
            // target.ApplySlow(data.slowRate, data.effectDuration);
        }
        else if (data.elementType == SlimeElementType.Ice)
        {
            // float rand = Random.value * 100;
            // if(rand <= data.stunChance) target.ApplyStun(data.effectDuration);
        }
    }
}