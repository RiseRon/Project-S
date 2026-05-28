using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 오브젝트 풀링을 관리하는 매니저
/// 메모리 렉 방지와 스테이지 전환 최적화를 담당합니다.
/// </summary>
public class PoolManager : MonoBehaviour
{
    // 어디서든 접근 가능하도록 싱글톤 패턴 적용
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public int id;          // 데이터 테이블과 매칭될 고유 ID
        public GameObject prefab; // 생성할 원본 프리팹
        public int size;        // 미리 생성해둘 기본 수량
    }

    [Header("--- Pool Groups (Inspector 설정용) ---")]
    [SerializeField] private List<Pool> slimePools;
    [SerializeField] private List<Pool> enemyPools;
    [SerializeField] private List<Pool> otherPools;

    // 실제 비활성 오브젝트들을 보관하는 큐 (검색 속도를 위해 Dictionary 사용)
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    // 원본 풀 정보를 보관 (모자랄 때 추가 생성하기 위함)
    private Dictionary<int, Pool> sourcePools = new Dictionary<int, Pool>();

    // 현재 맵에 나와 있는(활성화된) 객체들의 리스트 (스테이지 이동 시 일괄 회수용)
    private List<GameObject> activeObjects = new List<GameObject>();

    private void Awake()
    {
        // --- 싱글톤 및 DontDestroyOnLoad 설정 ---
        // 씬이 바뀌어도 파괴되지 않고 게임 종료시까지 유지됩니다.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 인스펙터에 등록된 리스트들을 관리용 딕셔너리로 통합합니다.
    /// </summary>
    private void InitializeManager()
    {
        RegisterPoolInfo(slimePools);
        RegisterPoolInfo(enemyPools);
        RegisterPoolInfo(otherPools);
    }

    /// <summary>
    /// 각 리스트의 정보를 읽어 딕셔너리에 등록 (실제 생성은 여기서 하지 않음 - 메모리 절약)
    /// </summary>
    private void RegisterPoolInfo(List<Pool> poolList)
    {
        foreach (Pool pool in poolList)
        {
            if (!sourcePools.ContainsKey(pool.id))
            {
                sourcePools.Add(pool.id, pool);
                poolDictionary.Add(pool.id, new Queue<GameObject>());
            }
        }
    }

    //-----------------------------------------------------------------------
    // [유저 로딩용] 비동기 풀 채우기 (렉 방지용 코루틴)
    //-----------------------------------------------------------------------
    /// <summary>
    /// 로딩 씬에서 호출됩니다. 한 프레임에 모두 생성하지 않고 나누어 생성하여 부드러운 로딩을 보장합니다.
    /// </summary>
    public IEnumerator Co_PreWarmPools()
    {
        Debug.Log("<color=cyan>[PoolManager]</color> 로딩 씬 비동기 풀링 시작...");
        foreach (var pair in sourcePools)
        {
            Pool pool = pair.Value;
            int currentCount = poolDictionary[pool.id].Count;

            // 목표 수량(size)만큼 채워질 때까지 생성
            for (int i = currentCount; i < pool.size; i++)
            {
                poolDictionary[pool.id].Enqueue(CreateNewObject(pool.prefab));

                // 최적화: 5개 생성할 때마다 다음 프레임으로 작업을 넘김 (메인 프레임 끊김 방지)
                if (i % 5 == 0) yield return null;
            }
        }
        Debug.Log("<color=cyan>[PoolManager]</color> 비동기 풀링 완료.");
    }

    //-----------------------------------------------------------------------
    // [개발자 디버그용] 즉시 풀 채우기 (작업 효율용)
    //-----------------------------------------------------------------------
    /// <summary>
    /// 스테이지 씬에서 직접 실행 시 호출됩니다. 프레임 드랍을 무시하고 즉시 모든 오브젝트를 생성합니다.
    /// </summary>
    public void ImmediatePreWarm()
    {
        Debug.Log("<color=orange>[PoolManager]</color> 디버그 모드: 즉시 풀링 실행");
        foreach (var pair in sourcePools)
        {
            Pool pool = pair.Value;
            int currentCount = poolDictionary[pool.id].Count;

            for (int i = currentCount; i < pool.size; i++)
            {
                poolDictionary[pool.id].Enqueue(CreateNewObject(pool.prefab));
            }
        }
    }

    /// <summary>
    /// 새로운 오브젝트를 생성하여 비활성화 상태로 부모(매니저) 아래에 둡니다.
    /// </summary>
    private GameObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    //-----------------------------------------------------------------------
    // [런타임 공용 함수] Spawn / Return / Clear
    //-----------------------------------------------------------------------

    /// <summary>
    /// 지정된 ID의 풀에서 오브젝트 하나를 꺼내 활성화합니다.
    /// </summary>
    public GameObject SpawnFromPool(int id, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(id))
        {
            Debug.LogError($"[PoolManager] Projectile Prefab ID {id}에 해당하는 풀이 없습니다!");
            return null;
        }

        GameObject objectToSpawn;

        // 큐가 비어있다면 추가로 하나 더 생성 (동적 확장)
        if (poolDictionary[id].Count <= 0)
        {
            objectToSpawn = CreateNewObject(sourcePools[id].prefab);
        }
        else
        {
            objectToSpawn = poolDictionary[id].Dequeue();
        }

        // 유니티 최적화: 위치와 회전값을 동시에 설정하여 오버헤드 감소
        objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        objectToSpawn.SetActive(true);

        // 추적 리스트에 추가 (나중에 한꺼번에 끄기 위함)
        activeObjects.Add(objectToSpawn);
        return objectToSpawn;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀로 반납합니다. (비활성화 및 부모 재설정)
    /// </summary>
    public void ReturnToPool(int id, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(id))
        {
            Destroy(obj); // 풀 정보가 없으면 파괴
            return;
        }

        activeObjects.Remove(obj); // 추적 리스트에서 제거
        obj.SetActive(false);
        obj.transform.SetParent(this.transform); // Hierarchy 정리를 위해 매니저 자식으로 회수
        poolDictionary[id].Enqueue(obj);
    }

    /// <summary>
    /// [최적화 핵심] 현재 필드에 활성화된 모든 오브젝트를 강제로 비활성화하고 풀로 회수합니다.
    /// 스테이지 전환(로딩 중) 시 호출하여 메모리 누수와 잔상을 방지합니다.
    /// </summary>
    public void ClearAllActiveObjects()
    {
        // 역순 순회하여 안전하게 비활성화
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] == null) continue;

            activeObjects[i].SetActive(false);
            activeObjects[i].transform.SetParent(this.transform);
        }
        activeObjects.Clear(); // 리스트 초기화
        Debug.Log("<color=yellow>[PoolManager]</color> 모든 활성 객체 회수 및 필드 정리 완료.");
    }
}