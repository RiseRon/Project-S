using UnityEngine;
using System;
using System.Collections.Generic;

// 재화 종류 정의 (CSV의 ID와 일치시킴)
public enum CurrencyType
{
    None = 0,
    FragmentCoin = 301,  // 마석 파편
    CompleteCoin = 302   // 완전한 마석
}

// 테이블 데이터를 담기 위한 클래스
[Serializable]
public class CurrencyData
{
    public int id;
    public string kName;
    public int maxCoin;
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Current Balances (Read Only)")]
    [SerializeField] private int fragmentCoin;
    [SerializeField] private int completeCoin;

    // 테이블 정보를 저장할 사전 (ID, 데이터)
    private Dictionary<int, CurrencyData> currencyTable = new Dictionary<int, CurrencyData>();

    // UI 알림용 이벤트 (타입, 변경된 수량)
    public Action<CurrencyType, int> OnCurrencyChanged;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 1. 데이터 테이블 로드 (최대 수량 등)
        LoadCurrencyTable();
    }

    /// <summary>
    /// Resources/Tables/재화테이블.csv 파일을 읽어 재화 정보를 설정합니다.
    /// </summary>
    private void LoadCurrencyTable()
    {
        // 파일 경로: Resources/Tables/CurrencyTable.csv (확장자 제외)
        TextAsset csvFile = Resources.Load<TextAsset>("Tables/CurrencyTable");

        if (csvFile == null)
        {
            Debug.LogError("재화 테이블 CSV를 찾을 수 없습니다! 경로를 확인하세요.");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // 데이터 시작 라인(보통 헤더 4줄 이후)부터 순회
        for (int i = 4; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 4) continue;

            if (int.TryParse(row[0], out int id))
            {
                CurrencyData data = new CurrencyData
                {
                    id = id,
                    kName = row[2],
                    maxCoin = int.TryParse(row[3], out int max) ? max : 99999 // 파싱 실패시 기본값
                };
                currencyTable[id] = data;
            }
        }
        Debug.Log($"[CurrencyManager] {currencyTable.Count}종의 재화 설정 로드 완료.");
    }

    /// <summary>
    /// 재화를 추가합니다. 테이블에 정의된 최대치를 넘지 않습니다.
    /// </summary>
    public void AddCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        int id = (int)type;
        if (!currencyTable.ContainsKey(id))
        {
            Debug.LogWarning($"등록되지 않은 재화 타입입니다: {type}");
            return;
        }

        // 테이블에서 최대 보유량 가져오기 (하드코딩 없음)
        int maxLimit = currencyTable[id].maxCoin;

        if (type == CurrencyType.FragmentCoin)
            fragmentCoin = Mathf.Min(fragmentCoin + amount, maxLimit);
        else if (type == CurrencyType.CompleteCoin)
            completeCoin = Mathf.Min(completeCoin + amount, maxLimit);

        OnCurrencyChanged?.Invoke(type, GetAmount(type));
    }

    /// <summary>
    /// 재화를 소비합니다. 잔액이 부족하면 false를 반환합니다.
    /// </summary>
    public bool ConsumeCurrency(CurrencyType type, int amount)
    {
        int current = GetAmount(type);
        if (current < amount) return false;

        if (type == CurrencyType.FragmentCoin) fragmentCoin -= amount;
        else if (type == CurrencyType.CompleteCoin) completeCoin -= amount;

        OnCurrencyChanged?.Invoke(type, GetAmount(type));
        return true;
    }

    public int GetAmount(CurrencyType type)
    {
        return (type == CurrencyType.FragmentCoin) ? fragmentCoin : completeCoin;
    }

    /// <summary>
    /// 모든 재화를 0으로 초기화합니다. (치트 및 데이터 리셋용)
    /// </summary>
    public void ResetCurrencies()
    {
        fragmentCoin = 0;
        completeCoin = 0;

        // UI에 알림 전송
        OnCurrencyChanged?.Invoke(CurrencyType.FragmentCoin, 0);
        OnCurrencyChanged?.Invoke(CurrencyType.CompleteCoin, 0);
    }
}