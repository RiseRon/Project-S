using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // 어디서든 접근 가능한 싱글톤 인스턴스
    public static SpawnManager Instance { get; private set; }

    [Header("로드된 데이터")]
    public List<SO_EnemyData> enemyDataList = new List<SO_EnemyData>();

    private void Awake()
    {
        // 싱글톤 초기화 로직
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 중복된 매니저 파괴
            Destroy(gameObject);
        }
        LoadAllResources();
    }
    private void LoadAllResources()
    {
        enemyDataList.Clear();
        // Resources/SlimeSummonData 폴더에서 로드
        var enemys = Resources.LoadAll<SO_EnemyData>("EnemyData");
        enemyDataList.AddRange(enemys);
    }

    /// <summary>
    /// WaveManager로부터 호출받아 실제 적을 생성하고 초기화하는 함수
    /// </summary>
    /// <param name="id">소환할 적의 고유 ID (int)</param>
    /// <param name="hpBonus">해당 웨이브의 체력 증가율 (%)</param>
    public void Spawn(int id, float hpBonus, int pathIndex)
    {
        // 1. 💡 [핵심] 웨이브 데이터가 지정한 인덱스의 경로 세트를 WaypointManager로부터 가져옵니다.
        Transform[] assignedPath = WaypointManager.GetPath(pathIndex);

        // 해당 경로 데이터가 유효한지 검사합니다.
        if (assignedPath == null || assignedPath.Length == 0)
        {
            Debug.LogError($"[SpawnManager] {pathIndex}번 경로에 웨이포인트 데이터가 없거나 유효하지 않습니다! 스폰을 중단합니다.");
            return;
        }

        // 적 스탯 SO 데이터를 ID 기반으로 검색
        SO_EnemyData finalData = enemyDataList.Find(x => x.id == id);
        if (finalData == null)
        {
            Debug.LogError($"ID {id}에 해당하는 SO_EnemyData를 찾을 수 없습니다!");
            return;
        }

        // 2. 💡 배정받은 경로의 첫 번째 지점(Index 0)을 스폰 위치로 설정합니다.
        Vector3 spawnPosition = assignedPath[0].position;
        Quaternion spawnRotation = Quaternion.identity;

        // PoolManager에서 해당 ID를 가진 적을 꺼내옴
        GameObject enemyObj = PoolManager.Instance.SpawnFromPool(id, spawnPosition, spawnRotation);

        if (enemyObj != null)
        {
            // 적 오브젝트에서 Enemy 컴포넌트를 가져옴
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();

            if (enemyScript != null)
            {
                // 인덱스를 첫 번째로 강제 지정
                enemyScript.SetWaypointIndex(0);

                // 3. 💡 [핵심] 전체 배열이 아니라, 배정받은 독립된 경로(assignedPath)만 적에게 쥐어줍니다.
                enemyScript.Setup(assignedPath, hpBonus, finalData);

                // 스폰 직후 첫 웨이포인트 위치에 완벽히 정렬
                enemyObj.transform.position = assignedPath[0].position;

                Debug.Log($"[SpawnManager] ID {id} 소환 완료 (경로: ways{pathIndex + 1}, 위치: 해당 경로의 0번 포인트)");
            }
            else
            {
                Debug.LogError($"ID {id} 프리팹에 Enemy 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"ID {id}의 적을 소환할 수 없습니다. (풀링 에러)");
        }
    }
}