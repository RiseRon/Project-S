using System.Collections.Generic;
using UnityEngine;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("Data Caches")]
    // 1. 모든 슬라임 데이터를 미리 로드하여 보관 (성능 최적화)
    private Dictionary<int, SO_SlimeData> slimeDataCache = new Dictionary<int, SO_SlimeData>();

    // 2. 머지 조합식 딕셔너리 (Base ID, Material ID) -> Result ID
    private Dictionary<(int, int), int> recipeCache = new Dictionary<(int, int), int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeData();
    }

    /// <summary>
    /// 게임 시작 시 1회만 실행되어 모든 데이터와 레시피를 메모리에 올립니다.
    /// </summary>
    private void InitializeData()
    {
        // 1. 슬라임 데이터 캐싱 (기존과 동일)
        SO_SlimeData[] allData = Resources.LoadAll<SO_SlimeData>("SlimeData");
        foreach (var data in allData)
        {
            if (!slimeDataCache.ContainsKey(data.id))
            {
                slimeDataCache.Add(data.id, data);
            }
        }

        // 2. [추가된 부분] 방금 임포트한 MergeTable을 Resources에서 불러와서 캐싱
        SO_MergeTableData mergeTable = Resources.Load<SO_MergeTableData>("MergeData/MergeTable");
        if (mergeTable != null)
        {
            foreach (var recipe in mergeTable.recipes)
            {
                // Key: (베이스 ID, 재료 ID), Value: 결과 ID
                if (!recipeCache.ContainsKey((recipe.baseID, recipe.materialID)))
                {
                    recipeCache.Add((recipe.baseID, recipe.materialID), recipe.resultID);
                }
            }
            Debug.Log($"[MergeManager] 레시피 {recipeCache.Count}개 등록 완료.");
        }
        else
        {
            Debug.LogError("[MergeManager] Resources 폴더에 MergeTable.asset을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 캡슐화된 조건: 두 슬라임이 조합 가능한지 '미리' 확인합니다. (Slot.cs에서 호출)
    /// </summary>
    public bool CheckCanMerge(int baseID, int materialID)
    {
        // 순서가 상관없는 조합식 (불+물 이나 물+불 이나 같게 처리)
        if (recipeCache.ContainsKey((baseID, materialID))) return true;
        if (recipeCache.ContainsKey((materialID, baseID))) return true;

        return false;
    }

    /// <summary>
    /// 실제 머지 과정을 실행합니다. (PlacementManager에서 호출)
    /// </summary>
    public void ExecuteMerge(Slime draggingSlime, Slot targetSlot)
    {
        Slime targetSlime = targetSlot.placedSlime;
        if (draggingSlime == null || targetSlime == null) return;

        int baseID = targetSlime.SlimeID;
        int materialID = draggingSlime.SlimeID;
        int resultID = -1;

        // 1. 레시피 확인 (순서 무관 적용)
        if (recipeCache.TryGetValue((baseID, materialID), out int result1)) resultID = result1;
        else if (recipeCache.TryGetValue((materialID, baseID), out int result2)) resultID = result2;

        // 레시피가 없거나 캐싱된 데이터에 결과 슬라임이 없다면 취소 (안전장치)
        if (resultID == -1 || !slimeDataCache.ContainsKey(resultID))
        {
            Debug.LogWarning($"[MergeManager] 유효하지 않은 조합이거나, 결과 ID({resultID})의 데이터가 없습니다!");
            return;
        }

        // 2. 기존 슬라임 2마리 회수 (Destroy -> Pool 반납)
        PoolManager.Instance.ReturnToPool(materialID, draggingSlime.gameObject);
        PoolManager.Instance.ReturnToPool(baseID, targetSlime.gameObject);

        // 3. 새로운 결과 슬라임 소환 (Instantiate -> Pool 소환)
        // (주의: PoolManager의 그룹에 결과 슬라임 ID 프리팹이 미리 등록되어 있어야 합니다)
        GameObject newSlimeObj = PoolManager.Instance.SpawnFromPool(resultID, targetSlot.transform.position, Quaternion.identity);

        if (newSlimeObj != null)
        {
            Slime newSlime = newSlimeObj.GetComponent<Slime>();

            // [수정된 부분] 캐싱된 데이터에서 결과 ID에 맞는 SO를 찾아 새 슬라임에게 주입!
            newSlime.SetData(slimeDataCache[resultID]);

            // 4. 슬롯에 새로운 슬라임 장착 (캡슐화된 함수 사용)
            targetSlot.AssignSlime(newSlime);

            // 5. 이펙트 및 사운드 호출
            PlayMergeEffect(targetSlot.transform.position, resultID);
            Debug.Log($"[MergeManager] 머지 성공! (재료: {baseID} + {materialID} => 결과: {resultID})");
        }
    }

    private void PlayMergeEffect(Vector3 position, int resultID)
    {
        // 추후 이펙트도 PoolManager.Instance.SpawnFromPool(Effect_ID, ...) 로 소환하면 완벽합니다.
        // 사운드: SoundManager.Instance.PlaySFX("MergeSuccess");
    }
}