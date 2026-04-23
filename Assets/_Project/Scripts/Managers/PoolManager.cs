using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 어디서든 쉽게 접근할 수 있도록 싱글톤 인스턴스 설정
    public static PoolManager Instance { get; private set; }

    // 인스펙터에서 적의 종류별로 풀 설정을 하기 위한 직렬화 클래스
    [System.Serializable]
    public class Pool
    {
        public int id;               // 적 데이터 시트와 매칭될 고유 번호 (int ID)
        public GameObject prefab;    // 생성할 적의 프리팹
        public int size;             // 게임 시작 시 미리 생성해둘 오브젝트 개수
    }

    [SerializeField] private List<Pool> pools; // 여러 종류의 풀 정보를 담는 리스트

    // ID를 키(Key)로 사용하여 해당 종류의 오브젝트들을 보관하는 딕셔너리
    // Queue를 사용하여 먼저 들어온 오브젝트가 먼저 나가는 선입선출 구조로 관리
    private Dictionary<int, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 중복된 매니저 파괴
            Destroy(gameObject);
        }
        // 게임 시작 시 설정된 모든 풀을 생성 및 초기화
        InitializePool();
    }

    // 설정된 리스트를 바탕으로 실제 메모리에 오브젝트를 미리 만드는 과정
    private void InitializePool()
    {
        poolDictionary = new Dictionary<int, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            // 각 ID별로 오브젝트를 보관할 새로운 큐 생성
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // 지정된 초기 개수(size)만큼 미리 생성하여 비활성화 후 큐에 삽입
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab);
                objectPool.Enqueue(obj);
            }

            // 딕셔너리에 ID와 생성된 큐를 등록
            poolDictionary.Add(pool.id, objectPool);
        }
    }

    // 새로운 게임 오브젝트를 생성하고 PoolManager의 자식으로 등록한 뒤 비활성화하는 함수
    private GameObject CreateNewObject(GameObject prefab)
    {
        // 프리팹 생성 및 PoolManager(this.transform)를 부모로 설정하여 하이어라키 정리
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false); // 꺼진 상태로 대기
        return obj;
    }

    // 풀에서 오브젝트를 꺼내어 배치하는 핵심 함수
    public GameObject SpawnFromPool(int id, Vector3 position, Quaternion rotation)
    {
        // 딕셔너리에 해당 ID가 등록되어 있지 않은 경우 예외 처리
        if (!poolDictionary.ContainsKey(id))
        {
            Debug.LogWarning($"Pool with ID {id} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn;

        // [동적 확장 로직] 현재 대기 중인 오브젝트가 하나도 없다면
        if (poolDictionary[id].Count == 0)
        {
            // 리스트에서 해당 ID의 프리팹을 찾아 즉석에서 하나 더 생성
            GameObject prefab = pools.Find(x => x.id == id).prefab;
            objectToSpawn = CreateNewObject(prefab);
            Debug.Log($"ID {id}의 풀이 부족하여 추가 생성되었습니다.");
        }
        else
        {
            // 대기 중인 오브젝트 중 가장 오래된 것을 하나 꺼냄
            objectToSpawn = poolDictionary[id].Dequeue();
        }

        // 꺼낸 오브젝트의 위치와 회전값을 설정하고 활성화
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    // 적이 죽거나 화면에서 사라질 때 호출하는 함수 (Destroy 대신 사용)
    public void ReturnToPool(int id, GameObject obj)
    {
        // 해당 ID의 딕셔너리가 존재하면 다시 큐에 넣고 비활성화
        if (poolDictionary.ContainsKey(id))
        {
            obj.SetActive(false);
            poolDictionary[id].Enqueue(obj);
        }
        else
        {
            // 등록되지 않은 ID의 오브젝트가 반납될 경우 그냥 파괴
            Destroy(obj);
        }
    }
}