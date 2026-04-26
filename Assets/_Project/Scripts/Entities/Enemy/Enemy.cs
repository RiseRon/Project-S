using System.Threading;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected SO_EnemyData enemyData;

    protected Transform[] waypoints;
    protected int currentWaypointIndex = 0;
    protected float currentHealth;
    protected float currentSpeed;
    protected float lastAttackTime;

    // 상태 확인용 변수
    protected bool isDead = false;
    protected bool isAtEnd = false;
    protected GameObject targetBarrier;

    // 타워가 타겟을 결정할 때 참조할 정보
    public float TotalDistanceTraveled { get; private set; } // 누적 이동 거리
    public bool IsDead => isDead; // 사망 여부 확인용 프로퍼티

    protected virtual void Start()
    {
        // WaypointManager에서 경로 자동 할당
        if (WaypointManager.Waypoints != null && WaypointManager.Waypoints.Length > 0)
        {
            Setup(WaypointManager.Waypoints, 0);
        }
        else
        {
            Debug.LogError("WaypointManager에 길이 설정되지 않았습니다!");
        }
    }

    public virtual void Setup(Transform[] path, float hpGrowthRate)
    {
        waypoints = path;

        // 기본 체력 + (기본 체력 * 상승률 / 100)
        // 예: 기본체력 30, 상승률 10% -> 30 + (30 * 0.1) = 33
        float bonusHealth = enemyData.maxHealth * (hpGrowthRate / 100f);
        currentHealth = enemyData.maxHealth + bonusHealth;

        currentSpeed = enemyData.moveSpeed;

        isDead = false;
        isAtEnd = false;
        TotalDistanceTraveled = 0f;
        targetBarrier = null;

        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }

        //Debug.Log($"{gameObject.name} 생성 - 최종 체력: {currentHealth} (증가량: {hpGrowthRate}%)");
    }

    protected virtual void Update()
    {
        if (isDead || waypoints == null) return;

        if (!isAtEnd)
        {
            Move();
            /*if (Time.frameCount % 60 == 0) // 약 1초(60프레임)마다 한 번씩 출력
            {
                Debug.Log($"[{gameObject.name}] 현재 누적 이동 거리: {TotalDistanceTraveled:F2}");
            }*/
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
            currentSpeed * Time.deltaTime
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
        // 1. 공격 쿨타임 확인
        bool canTimeAttack = Time.time >= lastAttackTime + enemyData.attackInterval;

        // 2. 방벽 생존 확인 (Barrier의 IsDestroyed 프로퍼티 참조)
        // Barrier 클래스에 public bool IsDestroyed { get; private set; }가 있어야 합니다.
        bool isBarrierAlive = Barrier.Instance != null && !Barrier.Instance.IsDestroyed;

        // 도착함 + 쿨타임 참 + 타겟 존재함 + 방벽이 아직 파괴 안됨
        return isAtEnd && canTimeAttack && targetBarrier != null && isBarrierAlive;
    }

    public virtual void Attack()
    {
        lastAttackTime = Time.time;

        // GetComponent도 매번 하면 느리므로, Barrier 컴포넌트를 직접 참조하는 게 더 좋습니다.
        if (Barrier.Instance != null)
        {
            Barrier.Instance.TakeDamage(enemyData.attackDamage);
            Debug.Log($"{enemyData.enemyName}이(가) 방벽을 공격!");
        }
    }

    protected virtual void ReachEnd()
    {
        isAtEnd = true;

        // [수정] 씬 전체를 뒤지는 대신, 미리 등록된 Instance를 바로 가져옴 (성능 소모 0)
        if (Barrier.Instance != null)
        {
            targetBarrier = Barrier.Instance.gameObject;
            Debug.Log($"{enemyData.enemyName}이 방벽에 도달했습니다.");
        }
        else
        {
            Debug.LogError("씬에 Barrier가 존재하지 않습니다!");
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        if (isDead) return; // 이미 죽은 상태라면 중복 실행 방지
        isDead = true;

        // [회수 로직] Destroy 대신 PoolManager에 반납
        // enemyData.enemyID는 인스펙터나 데이터 테이블에서 설정된 int 값입니다.
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(enemyData.enemyID, gameObject);
        }
        else
        {
            // 만약 매니저가 없다면 (테스트용) 삭제
            Destroy(gameObject);
        }
    }

    public void ApplySlow(float slowRate)
    {
        currentSpeed = enemyData.moveSpeed * (1.0f - (slowRate / 100.0f));
    }

    public void ResetSlow()
    {
        currentSpeed = enemyData.moveSpeed;
    }
}