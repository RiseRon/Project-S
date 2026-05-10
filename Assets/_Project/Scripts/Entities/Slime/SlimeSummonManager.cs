using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SlimeSummonManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform uiPanelTransform;
    public GameObject cardPrefab;

    [Header("로드된 데이터")]
    public List<SO_SlimeSummonData> summonDataList = new List<SO_SlimeSummonData>();
    public List<SO_SlimeData> slimeDataList = new List<SO_SlimeData>();

    private void Awake()
    {
        LoadAllResources();
    }

    private void LoadAllResources()
    {
        summonDataList.Clear();
        // Resources/SlimeSummonData 폴더에서 로드
        var summons = Resources.LoadAll<SO_SlimeSummonData>("SlimeSummonData");
        summonDataList.AddRange(summons);

        slimeDataList.Clear();
        // Resources/SlimeData 폴더에서 로드
        var slimes = Resources.LoadAll<SO_SlimeData>("SlimeData");
        slimeDataList.AddRange(slimes);

        Debug.Log($"<color=cyan>[System]</color> 로드 완료: 소환({summonDataList.Count}개), 상세({slimeDataList.Count}개)");
    }

    // --- [ 일반 랜덤 소환 ] ---
    // 버튼 이벤트 연결 시 이 함수명을 확인하세요.
    public void OnClickSummonButton()
    {
        if (uiPanelTransform.childCount >= 5)
        {
            Debug.Log("<color=yellow>인벤토리가 가득 찼습니다.</color>");
            return;
        }

        int pickedID = GetWeightedRandomID();
        ExecuteSummon(pickedID, "일반 소환");
    }

    // --- [ 확정 소환 ] ---
    public void SummonGuaranteedSlime(int targetID)
    {
        if (uiPanelTransform.childCount >= 5)
        {
            Debug.Log("<color=yellow>인벤토리가 가득 찼습니다.</color>");
            return;
        }

        ExecuteSummon(targetID, "★확정 소환★");
    }

    private void ExecuteSummon(int targetID, string summonType)
    {
        SO_SlimeData finalData = slimeDataList.Find(x => x.id == targetID);

        if (finalData != null)
        {
            CreateSummonCard(finalData, summonType);
        }
        else
        {
            Debug.LogError($"[오류] ID {targetID}에 해당하는 슬라임 데이터가 없습니다.");
        }
    }

    private int GetWeightedRandomID()
    {
        if (summonDataList.Count == 0) return -1;

        int totalWeight = summonDataList.Sum(x => x.weight);
        int pivot = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var data in summonDataList)
        {
            currentSum += data.weight;
            if (pivot < currentSum) return data.id;
        }
        return -1;
    }

    private void CreateSummonCard(SO_SlimeData data, string summonType)
    {
        // PoolManager의 ID 기반 풀링 시스템 활용
        // PoolManager.cs의 SpawnFromPool 함수 호출 (Spawn을 Summon으로 바꾸지 않은 외부 스크립트 함수임에 유의)
        GameObject newCard = PoolManager.Instance.SpawnFromPool(911, Vector3.zero, Quaternion.identity);

        if (newCard != null)
        {
            newCard.transform.SetParent(uiPanelTransform);
            newCard.transform.localScale = Vector3.one;
            newCard.name = $"Card_{data.slimeName}_{data.id}";

            SlimeCard cardScript = newCard.GetComponent<SlimeCard>();
            if (cardScript != null) cardScript.Setup(data);

            Debug.Log($"<color=lime><b>[{summonType} 완료]</b></color> " +
                      $"이름: <color=white>{data.slimeName}</color> (ID: {data.id})");
        }
    }
}