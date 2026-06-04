using UnityEngine;
using System.Collections.Generic;

public class Slime : MonoBehaviour
{
    [SerializeField] protected SO_SlimeData slimeData;

    public int SlimeID => slimeData != null ? slimeData.id : -1;

    private float lastAttackTime;
    private Enemy targetEnemy;

    // [추가] 현재 드래그(배치) 중인지 확인하는 상태 변수
    public bool isDragging = false;

    // PlacementManager에서 호출하여 데이터를 주입함
    public void SetData(SO_SlimeData data)
    {
        slimeData = data;
        Debug.Log($"<color=green>[Slime]</color> {data.slimeName} 데이터 주입 완료.");
    }

    protected virtual void Update()
    {
        if (slimeData == null) return;

        // 드래그 중일 때는 타겟팅과 공격을 모두 중단
        if (isDragging)
        {
            targetEnemy = null;
            return;
        }

        Targeting();

        LookAtTarget();

        if (CanAttack())
        {
            Attack();
        }
    }

    protected virtual void Targeting()
    {
        float maxDistance = -1f;
        targetEnemy = null;

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsDead) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToEnemy <= slimeData.attackRange)
            {
                if (enemy.TotalDistanceTraveled > maxDistance)
                {
                    maxDistance = enemy.TotalDistanceTraveled;
                    targetEnemy = enemy;
                }
            }
        }
    }

    private void LookAtTarget()
    {
        // 타겟이 없거나 이미 죽었다면 회전하지 않음
        if (targetEnemy == null || targetEnemy.IsDead) return;

        // 1. 타겟을 향한 방향 벡터 계산
        Vector3 direction = targetEnemy.transform.position - transform.position;

        // 2. y축(높이) 차이를 0으로 만들어 위아래로 기울어지는 것을 방지
        direction.y = 0f;

        // 3. 방향 벡터가 0이 아닐 때만 회전 처리
        if (direction != Vector3.zero)
        {
            // Slerp(부드러운 회전)를 제거하고 목표 회전값을 즉시 덮어씌웁니다.
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    protected virtual bool CanAttack()
    {
        // 타겟이 있고, 쿨타임이 지났으며, 드래그 중이 아닐 때만 공격 가능
        return targetEnemy != null && Time.time >= lastAttackTime + slimeData.attackSpeed && !isDragging;
    }

    public virtual void Attack()
    {
        lastAttackTime = Time.time;

        if (PoolManager.Instance != null)
        {
            GameObject projGO = PoolManager.Instance.SpawnFromPool(slimeData.projectilePrefabID, transform.position, Quaternion.identity);

            if (projGO != null && projGO.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Setup(targetEnemy, slimeData);
            }
        }
    }

    // ======================================================================================
    // [Gizmos 시각화] 런타임 및 에디터(비런타임) 모두 작동
    // ======================================================================================
    private void OnDrawGizmos()
    {
        // 데이터가 연결되지 않았으면 그리지 않음 (에러 방지)
        if (slimeData == null) return;

        // 1. 공격 사거리 원 그리기 (바닥 평면)
        // 불 속성은 빨간색, 물 속성은 파란색 등 기획에 맞게 색상 변경 가능
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // 노란색, 반투명

        // 디펜스 게임이므로 y축을 기준으로 바닥에 원을 그리기 위해 Matrix 활용
        Matrix4x4 oldMatrix = Gizmos.matrix;
        // 슬라임 위치에서 y축 스케일만 아주 작게 만들어 바닥에 붙은 원처럼 보이게 함
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0.01f, 1f));

        // 와이어 구체를 그리지만 y스케일 때문에 원으로 보임
        Gizmos.DrawWireSphere(Vector3.zero, slimeData.attackRange);

        // 2. 사거리 내부 채우기 (옵션 - 시인성 확보)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.1f); // 아주 투명한 노란색
        Gizmos.DrawSphere(Vector3.zero, slimeData.attackRange);

        // Gizmos 매트릭스 복구
        Gizmos.matrix = oldMatrix;

        // 3. (옵션) 타겟팅 시각화 - 런타임 전용
        if (Application.isPlaying && targetEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, targetEnemy.transform.position + Vector3.up * 0.5f);
        }
    }
}