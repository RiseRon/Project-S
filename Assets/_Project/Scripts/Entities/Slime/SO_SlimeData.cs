using UnityEngine;
using UnityEngine.UIElements.Experimental;

// 슬라임의 속성 및 공격 방식을 열거형으로 정의
public enum SlimeElementType { Fire, Water, Electric, Ice, Posion, Earth }
public enum ProjectileType { Single, Area, Floor } // 투사체 타입 (단일, 범위, 장판)
public enum TrajectoryType { Straight, Parabolic } // 궤도 타입 (직선, 포물선)
[CreateAssetMenu(fileName = "NewSlimeData", menuName = "ScriptableObjects/SlimeData")]
public class SO_SlimeData : ScriptableObject
{
    [Header("기본 정보")]
    public string slimeID;
    public string slimeName;
    public SlimeElementType elementType;
    public int MasteryLevel; // 1: Noemal, 2: Rare, 3Epic

    [Header("전투 스탯")]
    public float attackDamage;
    public float attackRange;
    public float attackInterval; // 공격 주기(초)

    [Header("투사체 설정")]
    public GameObject projectilePrefab;
    public ProjectileType projectileType; // 투사체 타입
    public TrajectoryType trajectoryType; // 궤도 타입

    [Header("투사체 세부 설정")]
    public float projectileSpeed; // 탄환 속도
    public float arcHeight; // 포물선 높이

    [Header("특수 효과")]
    [Range(0, 100)] public float slowRate; // 슬로우 비율
    [Range(0, 100)] public float stunChance; //스턴 확률
    public float dotDamage; // 도트 데미지
    public float damageInterval; // 도트 데미지 주기
    public float effectDuration; //효과 지속 시간

    [Header("장판 설정")]
    public int areaPrefabID; // PoolManager에 등록된 장판 프리팹의 ID
    public float areaDuration; // 장판 유지 시간
}
