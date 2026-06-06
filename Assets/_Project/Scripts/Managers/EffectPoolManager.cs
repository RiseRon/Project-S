using System.Collections.Generic;
using UnityEngine;

public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance { get; private set; }

    // 이펙트 이름별로 풀(Queue)을 나누어 관리하는 장부
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    // 원본 프리팹을 빠르게 찾기 위한 딕셔너리
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

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

        // 풀에 재사용 가능한(꺼져있는) 오브젝트가 있는지 확인
        if (objectPool.Count > 0 && !objectPool.Peek().activeSelf)
        {
            effectInstance = objectPool.Dequeue();
        }
        else
        {
            // 풀이 비어있거나 다 사용 중이라면 새로 생성해서 공급 (자동 확장 구조)
            GameObject prefab = prefabDictionary[effectName];
            effectInstance = Instantiate(prefab, this.transform);

            // ★ 안전장치: 혹시 프리팹에 자동 반환 스크립트가 없다면 컴포넌트로 강제 부착
            if (effectInstance.GetComponent<AutoReturnEffect>() == null)
            {
                effectInstance.AddComponent<AutoReturnEffect>();
            }
        }

        // 위치와 회전값을 맞추고 활성화
        effectInstance.transform.position = position;
        effectInstance.transform.rotation = rotation;
        effectInstance.SetActive(true);

        // 사용한 오브젝트는 다시 큐의 맨 뒤로 넣어 돌려막기 구조 완성
        objectPool.Enqueue(effectInstance);

        return effectInstance;
    }
    public void ReturnEffect(string effectName, GameObject effectInstance)
    {
        if (effectInstance == null) return;

        // 이미 꺼져있다면 중복 처리 방지
        if (!effectInstance.activeSelf) return;

        // 이펙트를 강제로 비활성화 (꺼지는 순간 AutoReturnEffect의 OnDisable이 켜지며 Invoke도 취소됨)
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
}