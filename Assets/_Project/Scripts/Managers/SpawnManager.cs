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
    public void Spawn(int id, float hpBonus)
    {
        // 웨이포인트 데이터가 유효한지 먼저 확인합니다.
        if (WaypointManager.Waypoints == null || WaypointManager.Waypoints.Length == 0)
        {
            Debug.LogError("WaypointManager에 웨이포인트 데이터가 없습니다! 스폰을 중단합니다.");
            return;
        }
        SO_EnemyData finalData = enemyDataList.Find(x => x.id == id);
        if (finalData == null)
        {
            Debug.LogError($"ID {id}에 해당하는 SO_EnemyData를 찾을 수 없습니다!");
            return;
        }

        // 스폰 위치를 웨이포인트의 첫 번째 지점(Index 0)으로 설정합니다.
        Vector3 spawnPosition = WaypointManager.Waypoints[0].position;
        // 첫 번째 웨이포인트가 바라보는 방향 또는 기본 회전값 설정
        Quaternion spawnRotation = Quaternion.identity;

        // 1. PoolManager에서 해당 ID를 가진 적을 꺼내옴
        // 위치는 위에서 설정한 첫 번째 웨이포인트 좌표를 사용합니다.
        GameObject enemyObj = PoolManager.Instance.SpawnFromPool(id, spawnPosition, spawnRotation);

        if (enemyObj != null)
        {
            // 2. 적 오브젝트에서 Enemy 컴포넌트를 가져옴
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();

            if (enemyScript != null)
            {

                enemyScript.SetWaypointIndex(0);

                // 3. 적의 경로와 스탯(HP 증가율)을 설정하여 초기화
                // 이미 WaypointManager에 저장된 배열 전체를 넘겨줍니다.
                Transform[] path = WaypointManager.Waypoints;

                // Enemy 클래스의 Setup 함수를 호출하여 적을 작동시킵니다.
                enemyScript.Setup(path, hpBonus, finalData);
                enemyObj.transform.position = WaypointManager.Waypoints[0].position;

                Debug.Log($"[SpawnManager] ID {id} 소환 완료 (위치: 첫 번째 웨이포인트)");
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