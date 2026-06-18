using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Enemy를 상속받으며, 부모의 ScriptableObject인 enemyData.SkillID를 추적하여
/// CSV 데이터 테이블에서 딱 맞는 스킬 제원만 동적으로 로드해 사용하는 지능형 보스 클래스
/// </summary>
public class Boss : Enemy
{
    // CSV 스킬 테이블의 규격을 구조화한 데이터셋
    private struct SkillSpecification
    {
        public int id;
        public string name;
        public float coolTime;
        public float duration;
        public string targetType;
        public float amount;
        public float range;
        public float interval;
    }

    // CSV에서 추출되어 최종 바인딩된 단 하나의 스킬 데이터
    private SkillSpecification activeSkillData;

    // 실시간 패턴 제어 연산용 변수 장부
    private float cooldownTimer = 0f;
    private float currentShieldAmount = 0f;
    private GameObject activeShieldEffect = null;
    private bool isSkillLoaded = false;

    /// <summary>
    /// 오브젝트 풀링에 의해 씬에 스폰 및 활성화될 때 실행됩니다.
    /// </summary>
    private void OnEnable()
    {
        // OnEnable에서는 상태 초기화만 진행하고 CSV는 읽지 않습니다.
        currentShieldAmount = 0f;
        activeShieldEffect = null;
        cooldownTimer = 0f;
        isSkillLoaded = false;
    }

    // 부모 클래스가 데이터를 정상적으로 다 들고 배치가 완료된 직후인 Start 시점에 로드하도록 안전 정박
    protected override void Start()
    {
        base.Start();
        if (enemyData != null)
        {
            LoadTargetSkillFromCSV(enemyData.SkillID);
        }
        else
        {
            Debug.LogError($"[Boss] enemyData(SO)가 비어있습니다. 프리팹 인스펙터를 확인해 주세요.");
        }
    }

    /// <summary>
    /// Resources 폴더의 CSV 데이터 테이블 전체를 긁어와서
    /// 현재 보스의 enemyData.SkillID와 정확히 일치하는 행(Row)만 핀포인트로 가져옵니다.
    /// </summary>
    private void LoadTargetSkillFromCSV(int targetSkillID)
    {
        // 만약 SO에 SkillID가 설정되어 있지 않다면 스킬 연산 패스
        if (targetSkillID <= 0)
        {
            Debug.LogWarning($"[Boss] {gameObject.name}의 SkillID가 {targetSkillID} 이므로 스킬 패턴을 비활성화합니다.");
            return;
        }

        TextAsset csvFile = Resources.Load<TextAsset>("Tables/BossSkillTable");
        if (csvFile == null)
        {
            Debug.LogError("[Boss] 'Resources/Table/BossSkillTable' 파일을 로드할 수 없습니다.");
            return;
        }

        // 줄바꿈 필터링 분할
        string[] rows = csvFile.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < rows.Length; i++)
        {
            string[] cols = rows[i].Split(',');

            // 첫 번째 열(ID) 파싱 비교
            if (cols.Length > 0 && int.TryParse(cols[0].Trim(), out int csvID))
            {
                // CSV 행의 ID가 기획 SO 문서(`enemyData.SkillID`)와 매칭되는 순간!
                if (csvID == targetSkillID)
                {
                    activeSkillData = new SkillSpecification();
                    activeSkillData.id = csvID;
                    if (cols.Length > 1) activeSkillData.name = cols[1].Trim();
                    if (cols.Length > 2) float.TryParse(cols[2].Trim(), out activeSkillData.coolTime);
                    if (cols.Length > 3) float.TryParse(cols[3].Trim(), out activeSkillData.duration);
                    if (cols.Length > 4) activeSkillData.targetType = cols[4].Trim();
                    if (cols.Length > 5) float.TryParse(cols[5].Trim(), out activeSkillData.amount);
                    if (cols.Length > 6) float.TryParse(cols[6].Trim(), out activeSkillData.range);
                    if (cols.Length > 7) float.TryParse(cols[7].Trim(), out activeSkillData.interval);

                    isSkillLoaded = true;
                    Debug.Log($"<color=lime>[Boss Skill Link]</color> SO 스킬 ID [{targetSkillID}] 검출 및 CSV 연동 완료! 패턴명: {activeSkillData.name}");

                    if (BossUIManager.Instance != null && enemyData != null)
                    {
                        BossUIManager.Instance.TurnOnBossUI(enemyData.enemyName, enemyData.enemyHP, activeSkillData.coolTime, cooldownTimer);
                    }

                    return; // 최적화를 위해 데이터를 매칭시킨 순간 루프 완전 종료
                }
            }
        }

        Debug.LogError($"[Boss] CSV 테이블 내부에서 SO 매칭 타겟 스킬 ID [{targetSkillID}]를 발견하지 못했습니다.");
    }

    /// <summary>
    /// [오버라이드] 부모 Enemy의 기본 기동 방식을 무해하게 동기화하며 스킬 쿨타임을 관리합니다.
    /// </summary>
    protected override void Update()
    {
        // 부모의 웨이포인트 추적 주행, 누적 이동 거리, 슬로우 상태이상 처리 보존
        base.Update();

        // 스킬 데이터 바인딩 실패했거나 사망/스턴 상태면 패턴 연산 유예
        if (!isSkillLoaded || isDead || isSturn) return;

        HandleSkillLoop();

        if (BossUIManager.Instance != null && enemyData != null)
        {
            BossUIManager.Instance.UpdateCoolTime(enemyData.enemyName, cooldownTimer);
        }
    }

    /// <summary>
    /// 런타임으로 수집된 스펙 스케줄러(`coolTime`)에 맞춰 주기별 패턴을 동적 스위칭하는 제어 타워
    /// </summary>
    private void HandleSkillLoop()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= activeSkillData.coolTime)
        {

            Debug.Log($"[Boss] 쿨타임 도달! 패턴 작동 시도 코드: {activeSkillData.id}");
            SoundManager.Instance.PlaySFX("SFX_Boss_Skill");
            // [지능형 분기점] 로드된 스킬 ID 분기점에 따라 실시간으로 행동 메커니즘을 결정합니다.
            if (activeSkillData.id == 2001)
            {
                CastShieldSkill(activeSkillData.amount);
            }
            else if (activeSkillData.id == 2002)
            {
                StartCoroutine(CastAreaHealRoutine());
            }

            cooldownTimer = 0f; // 쿨타임 스톱워치 리셋
        }
    }

    // =========================================================================
    // [MECHANISM 1] 2001 - MidBossShield 패턴 실행부
    // =========================================================================
    private void CastShieldSkill(float shieldValue)
    {
        currentShieldAmount = shieldValue;
        Debug.Log($"<color=emerald>[패턴 실행]</color> {activeSkillData.name} 발동! 보호막 충전: +{shieldValue} (총합: {currentShieldAmount})");

        if (activeShieldEffect == null)
        {
            activeShieldEffect = EffectPoolManager.Instance.SpawnEffect("P_MidBoss_Shield", transform.position, transform.rotation);

            if (activeShieldEffect != null)
            {
                activeShieldEffect.transform.SetParent(this.transform);
                activeShieldEffect.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            }
        }
    }

    // =========================================================================
    // [MECHANISM 2] 2002 - BossHeal 패턴 실행부 (도트 광역 힐)
    // =========================================================================
    private IEnumerator CastAreaHealRoutine()
    {
        Debug.Log($"<color=yellow>[패턴 실행]</color> {activeSkillData.name} 전개! 주변 아군 치유를 시작합니다.");

        // 한 번 힐 주기가 켜지면 Interval(1초) 간격으로 정확히 나누어 정산되도록 제어
        float timePassed = 0f;

        // 중복 연산 방지용 장부 (하나의 몬스터가 가진 여러 콜라이더 중복 필터링용)
        HashSet<Enemy> healedEnemies = new HashSet<Enemy>();

        while (timePassed < activeSkillData.duration)
        {
            if (isDead) break;

            // 매 틱마다 새로 감지된 대상을 초기화
            healedEnemies.Clear();

            // 범위 내 충돌체 서칭 (Range: 15)
            Collider[] areaTargets = Physics.OverlapSphere(transform.position, activeSkillData.range);

            foreach (var hit in areaTargets)
            {
                Enemy targetEnemy = hit.GetComponent<Enemy>();

                // 아군이 존재하고, 죽지 않았으며, 결정적으로 '이번 1초(틱) 안에 이미 치료받지 않은' 대상일 때만 진입!
                if (targetEnemy != null && !targetEnemy.IsDead && !healedEnemies.Contains(targetEnemy))
                {
                    // 중복 처리 방지를 위해 장부에 등록
                    healedEnemies.Add(targetEnemy);

                    // 📜 [치유 정산 영역] 부모 체력 변수에 안전하게 힐 가산
                    targetEnemy.Heal(activeSkillData.amount);
                    if (BossUIManager.Instance != null && enemyData != null)
                    {
                        BossUIManager.Instance.UpdateHP(enemyData.enemyName, currentHealth);
                    }
                    Debug.Log($"치유 오라 전달 -> [{hit.name}] 체력 {activeSkillData.amount} 회복");
                }
            }

            // 기획 발동 간격(Interval: 1초)만큼 정확하게 대기 후 시간 누적
            yield return new WaitForSeconds(activeSkillData.interval);
            timePassed += activeSkillData.interval;
        }
        Debug.Log("<color=yellow>[패턴 종료]</color> 광역 치유 서클이 해제되었습니다.");
    }

    // =========================================================================
    // [전전투 통제 레이어: 피격 및 소멸 오버라이드]
    // =========================================================================
    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        // 보호막 차단막이 활성화 상태일 경우 피해 필터링 우선 정산
        if (currentShieldAmount > 0f)
        {
            if (damage < currentShieldAmount)
            {
                currentShieldAmount -= damage;
                damage = 0f;
                Debug.Log($"보호막이 완벽하게 가드했습니다. (남은 실드 내구도: {currentShieldAmount})");
            }
            else
            {
                damage -= currentShieldAmount;
                currentShieldAmount = 0f;
                Debug.Log($"보호막 차단 장벽 완파! 찌꺼기 데미지 [{damage}]가 본체 체력으로 누수됩니다.");

                // 실드 완파 즉시 바인딩 이펙트 풀 회수 청소
                BreakShieldEffect();
            }
        }

        // 실드로 상쇄 불가능한 순수 초과 대미지만 부모 Enemy의 오리지널 HP 차감 및 사망 트리거식으로 연계
        if (damage > 0f)
        {
            base.TakeDamage(damage);
            if (BossUIManager.Instance != null && enemyData != null)
            {
                BossUIManager.Instance.UpdateHP(enemyData.enemyName, currentHealth);
            }
        }
    }

    private void BreakShieldEffect()
    {
        if (activeShieldEffect != null)
        {
            // 2. 물리적으로 즉시 꺼버립니다. (이제 눈에서 사라집니다!)
            activeShieldEffect.SetActive(false);
            
            if (EffectPoolManager.Instance != null)
            {
                // 3. 풀 매니저에게 반환하여 activeEffects 장부에서 지우고 큐에 넣습니다.
                EffectPoolManager.Instance.ReturnEffect("P_MidBoss_Shield", activeShieldEffect);
            }

            activeShieldEffect = null;
        }
    }

    protected override void Die()
    {
        // 사망 예외 필터: 유령 껍데기 잔상 쉴드 방역
        currentShieldAmount = 0f;
        BreakShieldEffect();

        // 시전 중이던 모든 코루틴 패턴(힐 장판 도트 타이머) 전면 강제 종료
        StopAllCoroutines();

        if (BossUIManager.Instance != null && enemyData != null)
        {
            BossUIManager.Instance.TurnOffBossUI(enemyData.enemyName);
        }

        Debug.Log("<color=orange>[Boss 시스템]</color> 스킬 스택 릴리즈 완료. 부모 클래스의 풀 매니저 소멸 연산을 인보크합니다.");
        base.Die();
    }

    private void OnDrawGizmos()
    {
        // 1. 게임 실행 중(런타임)일 때의 처리
        if (Application.isPlaying)
        {
            // 스킬 데이터가 올바르게 로드되었고, 범위가 0보다 클 때만 그립니다.
            if (isSkillLoaded && activeSkillData.range > 0f)
            {
                // 💡 2002번인 힐 스킬일 때만 초록색 선으로 상시 표시
                if (activeSkillData.id == 2002)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // 녹색 (투명도 40%)
                    Gizmos.DrawWireSphere(transform.position, activeSkillData.range);
                }
                else if (activeSkillData.id == 2001)
                {
                    // 필요 시 실드 스킬 범위나 기본 가이드라인으로 활용 (청록색)
                    Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
                    Gizmos.DrawWireSphere(transform.position, activeSkillData.range);
                }
            }
        }
        // 2. 유니티 에디터 편집 상태(게임 미실행)일 때의 예외 처리
        else
        {
            // 게임이 켜지기 전에는 CSV 데이터가 없으므로 기획 테이블의 기본 힐 범위(15)를 
            // 노란색 가이드라인으로 미리 에디터 씬 뷰에 띄워줍니다.
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f); // 노란색
            Gizmos.DrawWireSphere(transform.position, 15f);
        }
    }
}