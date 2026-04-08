using UnityEngine;
using UnityEngine.UIElements.Experimental;

// 슬라임의 속성 및 공격 방식을 열거형으로 정의
public enum SlimeAttribute { Fire, Water, Electric, Ice, Posion, Earth }
public enum ProjectileType { Single, Area, Floor }
public enum TrajectoryType { Straight, Parabolic }
[CreateAssetMenu(fileName = "NewSlimeData", menuName = "ScriptableObjects/SlimeData")]
public class SO_SlimeData : ScriptableObject
{
    [Header("기본 정보")]
    public string slimeID;
    public string slimeName;
    public SlimeAttribute attribute;
    public int MasteryLevel; // 1: Noema, 2: Rare, 3Epic

    [Header("전투 스탯")]
    public float attackDamage;
    public float attackRange;
    public float attackInterval; // 공격 주기(초)

    [Header("투사체 설정")]
    public GameObject projectilePrefab;
    public ProjectileType projectileType;
    public TrajectoryType trajectoryType;

    [Header("투사체 세부 설정")]
    public float projectileSpeed; // 탄환 속도
    public float arcHeight; // 포물선 높이

    [Header("특수 효과")]
    [Range(0, 100)] public float slowRate; // 슬로우 비율
    [Range(0, 100)] public float stunChance; //스턴 확률
    public float dotDamage; // 지속 초당 데미지
    public float effectDuration; //효과 지속 시간
}
