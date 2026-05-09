using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SlimeSummonManager : MonoBehaviour
{
    [Header("UI 설정")]
    public Transform uiPanelTransform;
    public GameObject cardPrefab;

    [Header("로드된 데이터 (Runtime 자동 로드)")]
    public List<SO_SlimeSpawnData> spawnDataList = new List<SO_SlimeSpawnData>();
    public List<SO_SlimeData> slimeDataList = new List<SO_SlimeData>();

    private void Awake()
    {
        LoadAllResources();
    }

    private void LoadAllResources()
    {
        spawnDataList.Clear();
        // 경로 확인: Resources/SlimeSpawnData/
        var spawns = Resources.LoadAll<SO_SlimeSpawnData>("SlimeSpawnData");
        spawnDataList.AddRange(spawns);

        slimeDataList.Clear();
        // 경로 확인: Resources/SlimeData/
        var slimes = Resources.LoadAll<SO_SlimeData>("SlimeData");
        slimeDataList.AddRange(slimes);

        Debug.Log($"<color=cyan>[System]</color> 데이터 로드 완료: 소환({spawnDataList.Count}개), 상세({slimeDataList.Count}개)");
    }

    public void OnClickSpawnButton()
    {
        if (uiPanelTransform.childCount >= 5)
        {
            Debug.Log("<color=red>인벤토리가 꽉 찼습니다!</color>");
            return;
        }

        int pickedID = GetWeightedRandomID();
        SO_SlimeData finalData = slimeDataList.Find(x => x.id == pickedID);

        if (finalData != null)
        {
            SpawnSlimeCard(finalData);
        }
        else
        {
            Debug.LogError($"ID {pickedID}에 해당하는 상세 데이터를 찾을 수 없습니다!");
        }
    }

    private int GetWeightedRandomID()
    {
        if (spawnDataList.Count == 0) return -1;

        int totalWeight = spawnDataList.Sum(x => x.weight);
        int pivot = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var data in spawnDataList)
        {
            currentSum += data.weight;
            if (pivot < currentSum) return data.id;
        }
        return -1;
    }

    private void SpawnSlimeCard(SO_SlimeData data)
    {
        // 1. PoolManager에서 해당 ID의 카드를 꺼내옵니다.
        // UI 카드이므로 위치는 Vector3.zero, 회전은 Quaternion.identity로 일단 설정합니다.
        GameObject newCard = PoolManager.Instance.SpawnFromPool(911, Vector3.zero, Quaternion.identity);

        if (newCard != null)
        {
            // 2. 부모를 UI 패널로 설정하고 스케일을 초기화합니다.
            newCard.transform.SetParent(uiPanelTransform);
            newCard.transform.localScale = Vector3.one; // UI 스케일 깨짐 방지

            // 3. 오브젝트 이름 설정
            newCard.name = $"Card_{data.slimeName}_{data.id}";

            // 4. 슬라임 데이터 주입
            SlimeCard cardScript = newCard.GetComponent<SlimeCard>();
            if (cardScript != null)
            {
                cardScript.Setup(data);
                Debug.Log($"<color=lime><b>[소환 완료]</b></color> " +
                      $"이름: <color=white>{data.slimeName}</color> | " +
                      $"ID: <color=yellow>{data.id}</color> | " +
                      $"등급: <color=cyan>{data.rank}</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[오류]</color> {newCard.name}에 SlimeCard 스크립트가 없습니다!");
            }
        }
        else
        {
            // PoolManager.cs에서 ID가 없으면 null을 리턴하도록 설계되어 있습니다.
            Debug.LogError($"<color=red>[소환 실패]</color> PoolManager에 ID {data.id}가 등록되어 있는지 확인하세요.");
        }
    }
}