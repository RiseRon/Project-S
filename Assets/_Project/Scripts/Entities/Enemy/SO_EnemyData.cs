using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class SO_EnemyData : ScriptableObject
{
    [Header("ID & 정보")]
    public int enemyID;
    public string enemyName;

    [Header("능력치")]
    public float maxHealth; // 최대 체력
    public float attackDamage; // 공격력
    public float moveSpeed; // 이동 속도
    public float attackInterval; // 공격 주기 (초)

    [Header("보상")]
    public int normalReward;      // 일반 재화
    public int specialReward;       // 특수 재화

    [Header("Movement")]
    public float rotationSpeed = 10f;
}