using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected SO_EnemyData enemyData;

    protected Transform[] waypoints;
    protected int currentWaypointIndex = 0;
    protected float currentHealth;
    protected float lastAttackTime;

    // 상태 확인용 변수
    protected bool isDead = false;
    protected bool isAtEnd = false;
    protected GameObject targetBarrier;

    // [추가] 타워가 타겟을 결정할 때 참조할 정보
    public float TotalDistanceTraveled { get; private set; } // 누적 이동 거리
    public bool IsDead => isDead; // 사망 여부 확인용 프로퍼티

    protected virtual void Start()
    {
        // WaypointManager에서 경로 자동 할당
        if (WaypointManager.Waypoints != null && WaypointManager.Waypoints.Length > 0)
        {
            Setup(WaypointManager.Waypoints);
        }
        else
        {
            Debug.LogError("WaypointManager에 길이 설정되지 않았습니다!");
        }
    }

    public virtual void Setup(Transform[] path)
    {
        waypoints = path;
        currentHealth = enemyData.maxHealth;
        isDead = false;
        isAtEnd = false;
        TotalDistanceTraveled = 0f; // 누적 거리 초기화

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    protected virtual void Update()
    {
        if (isDead || waypoints == null) return;

        if (!isAtEnd)
        {
            Move();
            if (Time.frameCount % 60 == 0) // 약 1초(60프레임)마다 한 번씩 출력
            {
                Debug.Log($"[{gameObject.name}] 현재 누적 이동 거리: {TotalDistanceTraveled:F2}");
            }
        }
        else
        {
            if (CanAttack())
            {
                Attack();
            }
        }
    }

    protected virtual void Move()
    {
        if (currentWaypointIndex >= waypoints.Length) return;

        Transform target = waypoints[currentWaypointIndex];

        // [핵심] 이동 전 위치 저장
        Vector3 previousPosition = transform.position;

        // 이동 처리 (슬로우 등이 걸려 속도가 변하면 이동량도 자동으로 변함)
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            enemyData.moveSpeed * Time.deltaTime
        );

        // [추가] 실제로 이동한 물리적 거리를 누적 거리에 더함
        // 스턴 시에는 transform 변화가 없으므로 증가하지 않음
        TotalDistanceTraveled += Vector3.Distance(previousPosition, transform.position);

        // 회전 로직
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, enemyData.rotationSpeed * Time.deltaTime);
        }

        // 도착 체크 (성능을 위해 Distance 대신 sqrMagnitude를 쓰는 경우도 있으나 가독성을 위해 유지)
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
        bool canTimeAttack = Time.time >= lastAttackTime + enemyData.attackInterval;
        return isAtEnd && canTimeAttack && targetBarrier != null;
    }

    public virtual void Attack()
    {
        lastAttackTime = Time.time;

        if (targetBarrier != null)
        {
            // 실제 방벽 스크립트의 데미지 함수 호출 부위
            // targetBarrier.GetComponent<Barrier>().TakeDamage(enemyData.attackDamage);
            Debug.Log($"{enemyData.enemyName}이(가) 방벽을 공격! 피해량: {enemyData.attackDamage}");
        }
    }

    protected virtual void ReachEnd()
    {
        isAtEnd = true;
        targetBarrier = GameObject.FindGameObjectWithTag("Barrier");
        Debug.Log($"{enemyData.enemyName}이 방벽에 도달했습니다.");
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject);
    }
}