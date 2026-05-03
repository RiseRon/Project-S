using UnityEngine;
using System.Collections.Generic;

public class Slime : MonoBehaviour
{
    [SerializeField] protected SO_SlimeData slimeData;  

    private float lastAttackTime;
    private Enemy targetEnemy; 

    protected virtual void Update()
    {
        Targeting();

        if (CanAttack())
        {
            Attack();
        }
    }

    // "경로상 가장 앞선 적" 식별 (Enemy의 TotalDistanceTraveled 참조)
    protected virtual void Targeting()
    {
        float maxDistance = -1f;
        targetEnemy = null;

        // 공격 범위 내의 모든 적 탐색 (성능 최적화가 필요할 경우 OverlapSphere 사용 가능)
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsDead) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToEnemy <= slimeData.attackRange)
            {
                // Enemy.cs에 구현된 누적 이동 거리를 비교하여 가장 멀리 간 적 선택
                if (enemy.TotalDistanceTraveled > maxDistance)
                {
                    maxDistance = enemy.TotalDistanceTraveled;
                    targetEnemy = enemy;
                }
            }
        }
    }

    protected virtual bool CanAttack()
    {
        return targetEnemy != null && Time.time >= lastAttackTime + slimeData.attackSpeed;
    }

    public virtual void Attack()
    {
        lastAttackTime = Time.time;

        if (PoolManager.Instance != null)
        {
            GameObject projGO = PoolManager.Instance.SpawnFromPool(slimeData.projectilePrefabID, transform.position, Quaternion.identity);

            if (projGO != null && projGO.TryGetComponent<Projectile>(out var projectile))
            {
                // 투사체에 슬라임의 속성 데이터(데미지, 효과 등) 전달
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