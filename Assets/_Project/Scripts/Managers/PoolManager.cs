using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public int id;
        public GameObject prefab;
        public int size;
    }

    // ID 범위별로 그룹을 나누어 관리 (인스펙터에서 보기 편해집니다)
    [Header("--- 1xx: Slimes ---")]
    [SerializeField] private List<Pool> slimePools;

    [Header("--- 2xx: Enemies ---")]
    [SerializeField] private List<Pool> enemyPools;

    [Header("--- 9xx: Others ---")]
    [SerializeField] private List<Pool> otherPools;

    // 내부적으로는 하나의 딕셔너리로 통합하여 검색 속도를 유지합니다.
    private Dictionary<int, Queue<GameObject>> poolDictionary;
    // 동적 생성을 위해 원본 풀 정보도 저장
    private Dictionary<int, Pool> sourcePools;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializePool();
    }

    private void InitializePool()
    {
        poolDictionary = new Dictionary<int, Queue<GameObject>>();
        sourcePools = new Dictionary<int, Pool>();

        // 모든 리스트를 하나의 통합 리스트처럼 처리하여 초기화
        AddPoolsToDictionary(slimePools);
        AddPoolsToDictionary(enemyPools);
        AddPoolsToDictionary(otherPools);
    }

    private void AddPoolsToDictionary(List<Pool> poolList)
    {
        foreach (Pool pool in poolList)
        {
            if (poolDictionary.ContainsKey(pool.id))
            {
                Debug.LogWarning($"중복된 ID 발견: {pool.id}");
                continue;
            }

            sourcePools.Add(pool.id, pool);
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                objectPool.Enqueue(CreateNewObject(pool.prefab));
            }

            poolDictionary.Add(pool.id, objectPool);
        }
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    public GameObject SpawnFromPool(int id, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(id))
        {
            Debug.LogWarning($"ID {id}의 풀이 존재하지 않습니다.");
            return null;
        }

        GameObject objectToSpawn;

        if (poolDictionary[id].Count == 0)
        {
            // sourcePools에서 원본 프리팹을 찾아 추가 생성
            objectToSpawn = CreateNewObject(sourcePools[id].prefab);
        }
        else
        {
            objectToSpawn = poolDictionary[id].Dequeue();
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    public void ReturnToPool(int id, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(id))
        {
            // 해당 ID의 풀이 없으면 그냥 파괴
            Destroy(obj);
            return;
        }

        // 1. 오브젝트 비활성화
        obj.SetActive(false);

        // 2. [추가] 부모를 다시 PoolManager(this)로 설정하여 Hierarchy 정리
        obj.transform.SetParent(this.transform);

        // 3. 큐에 다시 추가하여 재사용 대기
        poolDictionary[id].Enqueue(obj);
    }
}