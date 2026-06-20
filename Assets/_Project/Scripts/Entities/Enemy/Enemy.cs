using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected SO_EnemyData enemyData;

    [Header("UI Feedback")]
    [SerializeField] private int rewardPopupID = 912; // PoolManager에 등록된 팝업 프리팹의 ID

    protected Transform[] waypoints;
    protected int currentWaypointIndex = 0;
    protected float currentHealth;
    protected float currentSpeed;
    protected float lastAttackTime;
    protected float savedHpGrowthRate;

    // 상태 확인용 변수
    protected bool isDead = false;
    protected bool isAtEnd = false;
    public bool isSturn = false;
    protected GameObject targetBarrier;

    // 타워가 타겟을 결정할 때 참조할 정보
    public float RemainingDistance { get; protected set; } // 마지막 웨이 포인트까지의 남은 거리
    public bool IsDead => isDead; // 사망 여부 확인용 프로퍼티
    public bool IsSturn => isSturn; // 스턴 여부 확인용 프로퍼티

    // 예측 사격을 위해 투사체가 참조할 적의 속도와 방향
    public float CurrentSpeed => currentSpeed;
    public Vector3 MoveDirection => transform.forward;

    // [추가된 상태이상 통제 모듈 변수]
    private Dictionary<int, float> activeSlows = new Dictionary<int, float>();
    private float stunImmuneEndTime = 0f;

    private Animator animator;

    private Vector3 dieEffectPo = new Vector3(0, 1.5f, 0);

    // 보스 여부 확인(SO_EnemyData의 이름을 기준으로 판별)
    public bool IsBoss => enemyData.enemyName == "MidBoss" || enemyData.enemyName == "FinalBoss";

    protected virtual void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected virtual void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            // 아무것도 지정 안 되어 있으면 기본적으로 0번 길(ways1)이라도 할당
            Transform[] defaultPath = WaypointManager.GetPath(0);
            if (defaultPath != null) Setup(defaultPath, 0, null);
        }
    }
    public void SetWaypointIndex(int index)
    {
        currentWaypointIndex = index;
    }

    public virtual void Setup(Transform[] path, float hpGrowthRate, SO_EnemyData data)
    {
        if (data != null) enemyData = data;

        if (enemyData == null)
        {
            Debug.LogError("EnemyData가 할당되지 않았습니다!");
            return;
        }
        waypoints = path;
        savedHpGrowthRate = hpGrowthRate;

        // 기본 체력 + (기본 체력 * 상승률 / 100)
        // 예: 기본체력 30, 상승률 10% -> 30 + (30 * 0.1) = 33
        float bonusHealth = enemyData.enemyHP * (hpGrowthRate / 100f);
        currentHealth = enemyData.enemyHP + bonusHealth;

        currentSpeed = enemyData.moveSpeed;

        isDead = false;
        isAtEnd = false;
        targetBarrier = null;
        if(animator != null)
        {
            animator.SetBool("IsAtEnd", isAtEnd);
        }

        UpdateRemainingDistance();
    }

    protected virtual void Update()
    {
        if (isDead || waypoints == null) return;

        if (!isAtEnd)
        {
            Move();
            // 💡매 프레임 이동할 때마다 최종 목적지까지의 거리를 업데이트합니다.
            UpdateRemainingDistance();
        }
        else
        {
            if (isSturn == false && CanAttack())
            {
                Attack();
            }
        }
    }
    private void UpdateRemainingDistance()
    {
        if (waypoints == null || waypoints.Length == 0 || isAtEnd)
        {
            RemainingDistance = 0f;
            return;
        }

        // 1. 현재 내가 위치한 곳에서 다음 타겟 웨이포인트까지의 실시간 거리
        float distance = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);

        // 2. 그 다음 웨이포인트들끼리의 일직선 물리적 거리를 전부 계산해서 누적합산
        for (int i = currentWaypointIndex; i < waypoints.Length - 1; i++)
        {
            distance += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }

        RemainingDistance = distance;
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
        bool canTimeAttack = Time.time >= lastAttackTime + enemyData.attackSpeed;

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
            // EffectPoolManager.Instance.SpawnEffect("P_Enemy_Attack", Barrier.Instance.transform.position, Barrier.Instance.transform.rotation);
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            SoundManager.Instance.PlaySFX("SFX_Enemy_Attack");
            Barrier.Instance.TakeDamage(enemyData.damage);
            Debug.Log($"{enemyData.name}이(가) 방벽을 공격!");
        }
    }

    protected virtual void ReachEnd()
    {
        isAtEnd = true;
        RemainingDistance = 0f;
        if (animator != null)
        {
            animator.SetBool("IsAtEnd", isAtEnd);
        }

        // [수정] 씬 전체를 뒤지는 대신, 미리 등록된 Instance를 바로 가져옴 (성능 소모 0)
        if (Barrier.Instance != null)
        {
            targetBarrier = Barrier.Instance.gameObject;
            Debug.Log($"{enemyData.name}이 방벽에 도달했습니다.");
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
        if(EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.SpawnEffect("P_Enemy_Hit", gameObject.transform.position, Quaternion.identity);
        }
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_Enemy_Hit");
        }
    }

    public virtual void Heal(float healAmount)
    {
        // 1. 이미 죽어있는 적이라면 치유 정산 예외 패스
        if (isDead) return;

        // 2. 현재 적의 웨이브 배율이 적용된 '진짜 최대 체력' 실시간 계산
        // (Setup 함수에서 정의된 방식인 [기본 체력 + 상승 배율 분율] 공식을 그대로 복사)
        float bonusHealth = enemyData.enemyHP * (savedHpGrowthRate / 100f);
        float maxHealth = enemyData.enemyHP + bonusHealth;

        // 3. 체력 가산 및 최대 체력 클램핑(오버힐 방지)
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // 4. 피격 이펙트와 구별되는 치유 성공 이펙트 팝업 연동
        // (만약 초록색 힐 이펙트가 따로 있다면 "P_Enemy_Heal" 등으로 이름을 교체하세요)
        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.SpawnEffect("P_Enemy_Heal", gameObject.transform.position, Quaternion.identity);
        }
        Debug.Log($"[{enemyData.enemyName}] 치유 발생! 현재 체력: {currentHealth} / {maxHealth}");
    }

    protected virtual void Die()
    {
        if (isDead) return; // 이미 죽은 상태라면 중복 실행 방지
        isDead = true;

        GameManager.AddKilledEnemyCount();

        GiveDeathReward();

        if(EffectPoolManager.Instance != null)
        {
            dieEffectPo = gameObject.transform.position + dieEffectPo;
            EffectPoolManager.Instance.SpawnEffect("P_Enemy_Die", dieEffectPo, gameObject.transform.rotation);
            Debug.Log("작동");
        }
        if(SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_Enemy_Die");
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyDefeated();
        }

        // [회수 로직] Destroy 대신 PoolManager에 반납
        // enemyData.enemyID는 인스펙터나 데이터 테이블에서 설정된 int 값입니다.
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(enemyData.id, gameObject);
        }
        else
        {
            // 만약 매니저가 없다면 (테스트용) 삭제
            Destroy(gameObject);
        }
    }
    protected virtual void GiveDeathReward()
    {
        if (CurrencyManager.Instance == null || enemyData == null) return;

        CurrencyType rewardType = (CurrencyType)enemyData.dropID;

        if (enemyData.amount > 0)
        {
            // 1. 재화 데이터 추가
            CurrencyManager.Instance.AddCurrency(rewardType, enemyData.amount);

            // 2. UI 팝업 생성 함수 호출
            ShowRewardUI(rewardType, enemyData.amount);
        }
    }

    private void ShowRewardUI(CurrencyType type, int amount)
    {
        // 1. 카메라 체크
        if (Camera.main == null)
        {
            Debug.LogError("씬에 MainCamera 태그가 붙은 카메라가 없습니다!");
            return;
        }

        if (PoolManager.Instance == null) return;

        // 2. 월드 좌표를 화면 좌표로 변환
        Vector3 worldPos = transform.position + Vector3.up * 2f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0) return; // 카메라 뒤쪽이면 무시

        // 3. 풀에서 객체 가져오기
        GameObject popupObj = PoolManager.Instance.SpawnFromPool(rewardPopupID, transform.position, Quaternion.identity);

        if (popupObj == null)
        {
            Debug.LogWarning($"PoolManager에서 ID {rewardPopupID}를 찾을 수 없습니다.");
            return;
        }

        // 4. 캔버스 찾기 (가장 흔한 에러 지점)
        // "Canvas"라는 이름 대신 태그나 타입을 쓰는 것이 더 안전합니다.
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();

        if (mainCanvas != null)
        {
            popupObj.transform.SetParent(mainCanvas.transform);
        }
        else
        {
            Debug.LogError("씬에 Canvas가 존재하지 않습니다!");
            return;
        }

        // 5. 컴포넌트 호출
        RewardPopup popup = popupObj.GetComponent<RewardPopup>();
        if (popup != null)
        {
            popup.Setup(type, amount, rewardPopupID, (Vector2)screenPos);
        }
        else
        {
            Debug.LogError("팝업 프리팹에 RewardPopup 스크립트가 붙어있지 않습니다!");
        }
    }

    // =========================================================
    // [상태이상: 슬로우 시스템 (기존 ApplySlow, ResetSlow 대체)]
    // =========================================================

    /// <summary> 장판이나 투사체가 슬로우를 '요청'할 때 호출 </summary>
    public void AddSlow(int sourceID, float slowRate)
    {
        if (!activeSlows.ContainsKey(sourceID))
        {
            activeSlows.Add(sourceID, slowRate);
            RecalculateSpeed();
        }
    }

    /// <summary> 장판에서 벗어났을 때 슬로우 '제거' 요청 </summary>
    public void RemoveSlow(int sourceID)
    {
        if (activeSlows.ContainsKey(sourceID))
        {
            activeSlows.Remove(sourceID);
            RecalculateSpeed();
        }
    }

    /// <summary> 활성화된 슬로우 중 가장 수치가 높은 1개만 적용 (중복 방지) </summary>
    private void RecalculateSpeed()
    {
        if (isSturn)
        {
            currentSpeed = 0f;
            return;
        }

        float maxSlow = 0f;
        foreach (var slow in activeSlows.Values)
        {
            if (slow > maxSlow) maxSlow = slow;
        }

        currentSpeed = enemyData.moveSpeed * (1.0f - (maxSlow / 100.0f));
    }

    // =========================================================
    // [상태이상: 스턴 시스템]
    // =========================================================

    /// <summary> 투사체가 스턴을 '요청'할 때 호출 </summary>
    public void RequestStun(float stunDuration)
    {
        if (IsBoss) return; // 보스는 스턴 면역
        if (isSturn) return; // 이미 스턴 중이면 무시
        if (Time.time < stunImmuneEndTime) return; // 면역 시간 중이면 무시
        if (isDead || !gameObject.activeInHierarchy)
        {
            return;
        }
        animator.speed = 0f;
        StartCoroutine(StunRoutine(stunDuration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isSturn = true;
        RecalculateSpeed(); // 스턴 시 속도를 0으로 갱신

        yield return new WaitForSeconds(duration);

        isSturn = false;
        // 스턴이 풀리면 데이터의 무적시간(StunGrace)만큼 쿨타임 가동
        stunImmuneEndTime = Time.time + enemyData.StunGrace;

        animator.speed = 1f;
        RecalculateSpeed(); // 원래 속도(혹은 슬로우 걸린 상태)로 복구
    }

    // =========================================================
    // [경로 기반 예측 사격 시스템]
    // =========================================================

    /// <summary> 
    /// 투사체가 지정된 시간(초) 뒤에 이 적이 웨이포인트를 따라 어디에 있을지 물어볼 때 위치를 반환합니다.
    /// </summary>
    public Vector3 GetPredictedPosition(float timeInFuture)
    {
        // 1. 해당 시간 동안 이동할 총 예상 거리
        float distanceToTravel = currentSpeed * timeInFuture;

        // 2. 이미 죽었거나, 스턴 상태라서 이동 거리가 없으면 현재 위치 반환
        if (distanceToTravel <= 0f || isDead || isSturn)
            return transform.position;

        Vector3 currentPos = transform.position;
        int tempWpIndex = currentWaypointIndex;

        // 3. 웨이포인트를 따라가며 가상으로 이동 시뮬레이션
        while (tempWpIndex < waypoints.Length)
        {
            Vector3 targetWpPos = waypoints[tempWpIndex].position;
            float distToTarget = Vector3.Distance(currentPos, targetWpPos);

            // 남은 이동 거리가 다음 웨이포인트까지의 거리보다 짧다면 (이 구간 안에서 멈춤)
            if (distanceToTravel <= distToTarget)
            {
                Vector3 dir = (targetWpPos - currentPos).normalized;
                return currentPos + (dir * distanceToTravel);
            }
            else
            {
                // 다음 웨이포인트를 지나쳐버림 -> 거리를 깎고 다음 웨이포인트로 가상 이동
                distanceToTravel -= distToTarget;
                currentPos = targetWpPos;
                tempWpIndex++;
            }
        }

        // 경로의 끝에 도달했다면 마지막 위치 반환
        return currentPos;
    }
}