using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class SO_EnemyData : ScriptableObject
{
    [Header("ID & 정보")]
    public int id;
    public string enemyName;

    [Header("능력치")]
    public int enemyHP; // 최대 체력
    public int damage; // 공격력
    public int moveSpeed; // 이동 속도
    public float attackSpeed; // 공격속도 (s)

    [Header("특수 능력 및 효과 부여")]
    public int splitID; // 분열 유닛 ID
    public int splitSpwanCount; // 분열 유닛 마릿수
    public bool canMoveStun; // 스턴시 이동 가능 여부
    public bool canAtkStun; // 스턴시 공격 가능 여부
    public int StunGrace; // 스턴 후 스턴 무적시간(s)

    [Header("보상")]
    public int dropID;      // 일반 재화
    public int amount;       // 특수 재화

    [Header("Movement")]
    public float rotationSpeed = 10f;
}