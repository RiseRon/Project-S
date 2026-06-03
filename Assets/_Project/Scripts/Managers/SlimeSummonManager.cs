using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SlimeSummonManager : MonoBehaviour
{
    [System.Serializable]
    public class SummonTableData // CSV 데이터를 담을 구조체
    {
        public int id;
        public int summonType;
        public int costType;
        public int cost;
    }

    [Header("UI 연결")]
    public Transform uiPanelTransform;
    public GameObject cardPrefab;

    [Header("로드된 데이터")]
    public List<SO_SlimeSummonData> summonDataList = new List<SO_SlimeSummonData>();
    public List<SO_SlimeData> slimeDataList = new List<SO_SlimeData>();

    private Dictionary<int, SummonTableData> summonTable = new Dictionary<int, SummonTableData>();

    private void Awake()
    {
        LoadAllResources();
        LoadSummonDataTable();
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
    private void LoadSummonDataTable()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("Tables/SummonTable");
        if (csvFile == null) { Debug.LogError("소환 테이블을 찾을 수 없습니다."); return; }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 데이터 시작 라인(5행부터이므로 index 4)
        for (int i = 4; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');

            // 1. 유효한 데이터 줄인지 확인 (열 개수가 부족하거나 첫 번째 열이 비어있으면 스킵)
            if (row.Length < 5 || string.IsNullOrWhiteSpace(row[0])) continue;

            try
            {
                // 2. 숫자로 변환하기 전 공백 제거(Trim) 처리
                SummonTableData data = new SummonTableData
                {
                    id = int.Parse(row[0].Trim()),
                    summonType = int.Parse(row[2].Trim()),
                    costType = int.Parse(row[3].Trim()),
                    cost = int.Parse(row[4].Trim())
                };
                summonTable[data.id] = data;
            }
            catch (System.FormatException e)
            {
                // 어떤 줄에서 에러가 났는지 로그 출력 (디버깅용)
                Debug.LogWarning($"[CSV 파싱 에러] {i + 1}행에서 오류 발생: {lines[i]} / {e.Message}");
                continue;
            }
        }
        Debug.Log($"<color=green>[System]</color> 소환 테이블 {summonTable.Count}개 로드 완료.");
    }

    // --- [ 일반 랜덤 소환 ] ---
    // 버튼 이벤트 연결 시 이 함수명을 확인하세요.
    public void OnClickSummonButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }
        if (uiPanelTransform.childCount >= 5)
        {
            Debug.Log("<color=yellow>인벤토리가 가득 찼습니다.</color>");
            return;
        }

        SummonTableData data = summonTable[401];
        CurrencyType type = (CurrencyType)data.costType;

        // 1. CurrencyManager를 통해 재화 소모 시도
        if (CurrencyManager.Instance.ConsumeCurrency(type, data.cost))
        {
            int pickedID = GetWeightedRandomID();
            ExecuteSummon(pickedID, "일반 소환");
        }
        else
        {
            Debug.LogWarning($"<color=red>[소환 실패]</color> 재화가 부족합니다. 필요: {type} {data.cost}개");
        }
    }

    // --- [ 확정 소환 ] ---
    public void SummonGuaranteedSlime(int targetID)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_UI_Click");
        }
        if (uiPanelTransform.childCount >= 5)
        {
            Debug.Log("<color=yellow>인벤토리가 가득 찼습니다.</color>");
            return;
        }
        SummonTableData data = summonTable[402];
        CurrencyType type = (CurrencyType)data.costType;

        // 1. CurrencyManager를 통해 재화 소모 시도
        if (CurrencyManager.Instance.ConsumeCurrency(type, data.cost))
        {
            ExecuteSummon(targetID, "★확정 소환★");
        }
        else
        {
            SummonTableData data2 = summonTable[403];
            CurrencyType type2 = (CurrencyType)data2.costType;
            if (CurrencyManager.Instance.ConsumeCurrency(type2, data2.cost))
            {
                ExecuteSummon(targetID, "★확정 소환★");
            }
            else
            {

                Debug.LogWarning($"<color=red>[소환 실패]</color> 재화가 부족합니다. 필요: {type} {data.cost}개 OR {type2} {data2.cost}개");
            }
        }
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