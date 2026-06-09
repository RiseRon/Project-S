using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("데이터 연결")]
    // 에디터에서 SO_MergeTableData 에셋을 직접 드래그해서 넣어주기 위한 변수입니다.
    [SerializeField] private SO_MergeTableData mergeTableData;

    private Dictionary<int, SO_SlimeData> slimeDataCache = new Dictionary<int, SO_SlimeData>();
    // (베이스 ID, 재료 ID) -> 결과물 ID 를 저장하는 캐시
    private Dictionary<(int, int), int> recipeCache = new Dictionary<(int, int), int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        LoadAllData();
    }

    private void LoadAllData()
    {
        // 1. 슬라임 데이터 로드 및 캐싱
        SO_SlimeData[] allData = Resources.LoadAll<SO_SlimeData>("SlimeData");
        foreach (var data in allData)
        {
            slimeDataCache[data.id] = data;
        }
        Debug.Log($"[DataManager] 슬라임 데이터 {slimeDataCache.Count}개 로드 완료.");

        // 2. 머지 레시피 딕셔너리로 변환 (보내주신 SO 구조 활용!)
        if (mergeTableData != null)
        {
            foreach (MergeRecipe recipe in mergeTableData.recipes)
            {
                // (101, 101) 조합을 키로, 결과값 111을 값으로 저장합니다.
                recipeCache[(recipe.baseID, recipe.materialID)] = recipe.resultID;
            }
            Debug.Log($"[DataManager] 머지 레시피 {recipeCache.Count}개 세팅 완료.");
        }
        else
        {
            Debug.LogError("[DataManager] 인스펙터에 MergeTableData가 연결되지 않았습니다!");
        }
    }

    /// <summary> 특정 ID의 슬라임 데이터를 반환합니다. </summary>
    public SO_SlimeData GetSlimeData(int id)
    {
        if (slimeDataCache.TryGetValue(id, out SO_SlimeData data)) return data;

        Debug.LogError($"[DataManager] ID {id}의 데이터를 찾을 수 없습니다.");
        return null;
    }

    /// <summary> 두 슬라임의 조합 결과를 반환합니다. (불가능하면 -1) </summary>
    public int GetMergeResult(int idA, int idB)
    {
        // CSV 구조상 순서가 A+B로 등록되어 있을 수도, B+A로 등록되어 있을 수도 있으므로 양방향 검사
        if (recipeCache.TryGetValue((idA, idB), out int resultId)) return resultId;
        if (recipeCache.TryGetValue((idB, idA), out resultId)) return resultId;

        return -1;
    }
}