using UnityEngine;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance {  get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ExecuteMerge(Slime draggingSlime, Slot targetSlot)
    {
        // 목표 슬롯에 있던 기존 슬라임 정보 가져오기
        Slime targetSlime = targetSlot.placedSlime;

        // 안전 검사
        if (draggingSlime == null || targetSlime == null) return;

        // 1. 다음 등급의 슬라임 ID 계산 (현재 ID + 10)
        int nextSlimeID = GetNextRankPrefabID(targetSlime);

        // 2. 기존 슬라임 2마리를 오브젝트 풀로 반납
        PoolManager.Instance.ReturnToPool(draggingSlime.SlimeID, draggingSlime.gameObject);
        PoolManager.Instance.ReturnToPool(targetSlime.SlimeID, targetSlime.gameObject);

        // 3. Resources.Load를 사용하여 다음 등급 슬라임 프리팹 동적 로드
        // 주의: 슬라임 프리팹들이 'Resources/Prefabs/Slimes/' 경로 안에 있어야 하며,
        // 이름이 "Slime_101", "Slime_111"과 같은 규칙으로 저장되어 있어야 합니다.
        string prefabPath = $"Prefabs/Slimes/Slime_{nextSlimeID}";
        GameObject nextSlimePrefab = Resources.Load<GameObject>(prefabPath);

        /*
        =========================================================================================
        [ DataManager 추가 시의 장점 및 변경 방향 ]
        
        현재 머지를 할 때마다 'Resources.Load'를 호출하고 있습니다. 
        이 방식은 디스크(저장소)를 뒤져서 파일을 찾아오는 과정이 포함되어 있어, 
        게임 후반부에 다수의 유저가 동시에 여러 마리를 머지하면 순간적으로 렉(프레임 드랍)이 걸릴 수 있습니다.

        * 추후 DataManager를 도입하면 이렇게 바뀝니다:
          1. 게임 시작(Awake) 시점에 'Resources.LoadAll()'을 통해 모든 슬라임 프리팹을 한 번만 불러옵니다.
          2. 불러온 프리팹들을 DataManager 안의 Dictionary<int, GameObject>에 담아둡니다(캐싱).
          3. 머지할 때는 아래 코드로 변경됩니다.
             (변경 전) GameObject nextSlimePrefab = Resources.Load<GameObject>(prefabPath);
             (변경 후) GameObject nextSlimePrefab = DataManager.Instance.GetSlimePrefab(nextSlimeID);
          4. 이미 메모리에 올라가 있는 데이터를 즉시 꺼내오므로 렉이 전혀 발생하지 않게 됩니다!
        =========================================================================================
        */

        if (nextSlimePrefab != null)
        {
            // 4. 새로운 등급의 슬라임 스폰
            // (추후 이 Instantiate 부분도 PoolManager의 동적 확장 기능과 연동하면 더 완벽해집니다)
            GameObject newSlimeObj = Instantiate(nextSlimePrefab, targetSlot.transform.position, Quaternion.identity);
            Slime newSlime = newSlimeObj.GetComponent<Slime>();

            // 5. 슬롯 정보 갱신
            targetSlot.placedSlime = newSlime;

            // 6. 합성 성공 이펙트 및 사운드 호출
            PlayMergeEffect(targetSlot.transform.position);

            Debug.Log($"머지 성공! 상위 등급 슬라임(ID: {nextSlimeID})이 소환되었습니다.");
        }
        else
        {
            // 프리팹을 찾지 못한 경우의 에러 처리
            Debug.LogError($"[Merge Error] Resources 경로({prefabPath})에서 ID {nextSlimeID}의 슬라임을 찾을 수 없습니다!");
        }
    }

    // 다음 등급 슬라임의 ID를 판별하는 함수
    private int GetNextRankPrefabID(Slime baseSlime)
    {
        // 규칙: 현재 ID + 10 (예: 101 -> 111)
        return baseSlime.SlimeID + 10;
    }

    // 머지 이펙트 재생
    private void PlayMergeEffect(Vector3 position)
    {
        // PoolManager를 통해 파티클 이펙트 소환
        Debug.Log("머지 파티클 이펙트 재생");
    }
}
