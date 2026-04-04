using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyData enemyData;

    protected Transform[] waypoints;
    protected int currentWaypointIndex = 0;
    protected float currentHealth;
    protected float lastAttackTime;

    // 상태 확인용 bool 변수들
    protected bool isDead = false;
    protected bool hasTarget = false;

    public virtual void Setup(Transform[] path)
    {
        waypoints = path;
        currentHealth = enemyData.maxHealth;
        isDead = false;

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    protected virtual void Update()
    {
        if (isDead || waypoints == null) return;

        Move();

        if (CanAttack())
        {
            Attack();
        }
    }

    protected virtual void Move()
    {
        if (currentWaypointIndex >= waypoints.Length) return;

        Transform target = waypoints[currentWaypointIndex];

        // 이동 로직
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            enemyData.moveSpeed * Time.deltaTime
        );

        // 회전 로직 (지상 적 고정)
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemyData.rotationSpeed * Time.deltaTime);
        }

        // 웨이포인트 도착 체크
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                ReachEnd();
            }
        }
    }

    protected virtual bool CanAttack()
    {
        // 공격 주기 확인 및 타겟 소유 여부 확인
        return Time.time >= lastAttackTime + enemyData.attackInterval && hasTarget;
    }

    public abstract void Attack();

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        // 보상 지급 로직 호출 등
        Destroy(gameObject);
    }

    protected virtual void ReachEnd()
    {
        // 본진 공격 후 소멸
        Destroy(gameObject);
    }
}