using UnityEngine;

public enum SlimeElementType { Fire, Water, Electric, Ice, Poison, Earth }
public enum ProjectileType { Single, Area, Floor } // 투사체 타입 (단일, 범위, 장판)
public enum TrajectoryType { Straight, Parabolic } // 궤도 타입 (직선, 포물선)
[CreateAssetMenu(fileName = "NewSlimeData", menuName = "ScriptableObjects/SlimeData")]
public class SO_SlimeData : ScriptableObject
{
    [Header("기본 정보")]
    public int id;              
    public string slimeName;
    public SlimeElementType elementType;
    public int rank;

    [Header("전투 스탯")]
    public float damage;
    public float attackRange;
    public float attackSpeed;

    [Header("투사체 설정")]
    public int projectilePrefabID;
    public ProjectileType projectileType;
    public TrajectoryType trajectoryType;

    [Header("투사체 세부 설정")]
    public float projectileSpeed;
    public float arcHeight;

    [Header("특수 효과")]
    public float stunChance;
    public float stunDuration;
    public float slowRate;
    public float dotDamage;
    public float dotDamageInterval;

    [Header("장판 설정")]
    public int areaPrefabID;
    public float areaDuration;
}