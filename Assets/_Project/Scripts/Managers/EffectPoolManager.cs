using System.Collections.Generic;
using UnityEngine;

public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance { get; private set; }

    // 이펙트 이름별로 풀(Queue)을 나누어 관리하는 장부
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    // 원본 프리팹을 빠르게 찾기 위한 딕셔너리
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();
    // 현재 필드에 활성화(소환)되어 있는 이펙트들을 실시간으로 관리하는 장부
    private HashSet<GameObject> activeEffects = new HashSet<GameObject>();
    [SerializeField] private int prewarmCount = 10;

    private void Awake()
    {
        // 씬이 전환되면(메인메뉴로 나가면) 자동으로 파괴되게 합니다.
        if (Instance == null)
        {
            Instance = this;
            InitializeEffectPrefabs(); // 인게임 씬이 켜질 때 에셋 자동 로드 및 풀 준비
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1. Resources/Effects 폴더 내의 모든 이펙트 프리팹을 자동으로 읽어와 장부에 등록
    private void InitializeEffectPrefabs()
    {
        GameObject[] targetPrefabs = Resources.LoadAll<GameObject>("Effects");

        foreach (var prefab in targetPrefabs)
        {
            prefabDictionary[prefab.name] = prefab;

            Queue<GameObject> newPool = new Queue<GameObject>();
            poolDictionary[prefab.name] = newPool;

            // 💡 [핵심 추가] 각 이펙트 종류마다 설정된 개수(5개)만큼 루프를 돌며 선배치
            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject effectInstance = Instantiate(prefab, this.transform);

                // 안전장치: 자동 반환 스크립트 누락 방지
                if (effectInstance.GetComponent<AutoReturnEffect>() == null)
                {
                    effectInstance.AddComponent<AutoReturnEffect>();
                }

                // ★ 중요: 미리 만들어두는 것이므로 꺼진 상태(비활성화)로 풀에 저장합니다.
                effectInstance.SetActive(false);
                newPool.Enqueue(effectInstance);
            }
        }

        Debug.Log($"[EffectPool] 총 {targetPrefabs.Length}종류의 이펙트 등록 및 각 {prewarmCount}개씩 사전 생성(Prewarm) 완료.");
    }

    // 2. 외부에서 이펙트를 띄우고 싶을 때 호출하는 핵심 함수
    // 예시: EffectPoolManager.Instance.SpawnEffect("Hit_Normal", hitPoint.position, Quaternion.identity);
    public GameObject SpawnEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (!prefabDictionary.ContainsKey(effectName))
        {
            Debug.LogWarning($"[EffectPool] 등록되지 않은 이펙트 이름입니다: {effectName}");
            return null;
        }

        Queue<GameObject> objectPool = poolDictionary[effectName];
        GameObject effectInstance = null;

        // 💡 [핵심 수정] 현재 풀에 들어있는 오브젝트의 총개수를 미리 기억합니다.
        int currentPoolCount = objectPool.Count;

        // 💡 딱 풀에 있는 개수만큼만 루프를 돌며 검사합니다. (무한 루프 및 억지 생성 방지)
        for (int i = 0; i < currentPoolCount; i++)
        {
            GameObject peekObj = objectPool.Peek();

            // 맨 앞에 있는 녀석이 꺼져있다면(재사용 가능하다면) 즉시 꺼내서 사용!
            if (peekObj != null && !peekObj.activeSelf)
            {
                effectInstance = objectPool.Dequeue();
                break; // 찾았으니 루프를 탈출합니다.
            }
            else
            {
                // 이미 켜져서 사용 중인 이펙트라면 뒤로 줄을 다시 세우고 다음 녀석을 검사합니다.
                objectPool.Enqueue(objectPool.Dequeue());
            }
        }

        // 💡 [결과 확인] 위에서 한 바퀴 다 돌았는데도 꺼진 걸 못 찾아서 effectInstance가 null이라는 것은
        // "진짜로 풀에 있는 모든 이펙트가 필드에서 활발히 사용 중"이라는 뜻입니다. 이때만 새로 생성합니다!
        if (effectInstance == null)
        {
            GameObject prefab = prefabDictionary[effectName];
            effectInstance = Instantiate(prefab, this.transform);

            if (effectInstance.GetComponent<AutoReturnEffect>() == null)
            {
                effectInstance.AddComponent<AutoReturnEffect>();
            }

            // 💡 새로 생성된 오브젝트도 규칙에 맞게 일단 큐에 한 번 넣어줍니다.
            objectPool.Enqueue(effectInstance);
        }

        // 위치와 회전값을 맞추고 활성화
        effectInstance.transform.position = position;
        effectInstance.transform.rotation = rotation;
        effectInstance.SetActive(true);

        // 💡 활성 리스트에 등록하여 실시간 추적 관리
        activeEffects.Add(effectInstance);

        return effectInstance;
    }
    public void ReturnEffect(string effectName, GameObject effectInstance)
    {
        if (effectInstance == null) return;

        // 이미 꺼져있다면 중복 처리 방지
        if (!effectInstance.activeSelf) return;

        // 자연 반환 시 활성화 목록에서 안전하게 제외합니다.
        if (activeEffects.Contains(effectInstance))
        {
            activeEffects.Remove(effectInstance);
        }

        effectInstance.transform.SetParent(this.transform);
        effectInstance.SetActive(false);

        // 장부에 안전하게 다시 줄 세우기 (혹시 모를 중복 Enqueue 방지 검사)
        if (poolDictionary.ContainsKey(effectName))
        {
            Queue<GameObject> objectPool = poolDictionary[effectName];
            if (!objectPool.Contains(effectInstance))
            {
                objectPool.Enqueue(effectInstance);
            }
        }
    }
    public void ClearAllActiveEffects()
    {
        Debug.Log("<color=cyan>[EffectPoolManager]</color> 활성 이펙트 목록을 기반으로 정밀 청소를 시작합니다.");

        if (activeEffects.Count == 0) return;

        // 💡 [전면 수정] 루프 실행 중 원본 수정 에러를 방지하기 위해 임시 복사 리스트 생성
        List<GameObject> toReturnList = new List<GameObject>(activeEffects);

        foreach (var effectInstance in toReturnList)
        {
            if (effectInstance == null) continue;

            // 현재 맵에 켜져 있는 이펙트만 부모를 되돌리고 비활성화
            if (effectInstance.activeSelf)
            {
                effectInstance.transform.SetParent(this.transform);
                effectInstance.SetActive(false);
            }
        }

        // 💡 청소가 끝났으므로 활성 장부를 완벽하게 비워줍니다.
        activeEffects.Clear();

        Debug.Log("<color=cyan>[EffectPoolManager]</color> 필드 위 모든 이펙트 추적 및 회수 완료.");
    }
}