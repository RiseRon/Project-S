using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    public static MergeSystem Instance { get; private set; }

    [Header("이펙트 설정")]
    [Tooltip("인덱스 번호가 슬라임의 Rank와 일치하도록 세팅하세요. (예: Rank 2 = 2레벨 합성 이펙트 ID)")]
    // [변경] 단일 변수 대신 배열을 사용하여 레벨별 이펙트 ID를 무한히 담을 수 있게 합니다.
    [SerializeField] private string[] mergeEffectNames;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary> 
    /// 두 슬라임이 머지 가능한지 사전 판별합니다. (PlacementController가 드래그 시작 시 사용)
    /// </summary>
    public bool CanMerge(Slime a, Slime b)
    {
        if (a == null || b == null) return false;

        // 임시로 a.Data.id 로 적어두었습니다. Slime 스크립트에서 SO_SlimeData를 반환하는 변수명을 넣어주세요.
        int resultId = DataManager.Instance.GetMergeResult(a.SlimeID, b.SlimeID);
        return resultId != -1;
    }

    /// <summary> 
    /// 머지를 실행하고, 성공 시 새로 태어난 결과물 슬라임을 반환합니다. 
    /// </summary>
    public Slime ExecuteMerge(Slime draggingSlime, Slime targetSlime)
    {
        int resultId = DataManager.Instance.GetMergeResult(draggingSlime.SlimeID, targetSlime.SlimeID);

        if (resultId == -1)
        {
            Debug.LogError("머지 불가능한 조합이 실행되었습니다!");
            return null;
        }

        // [안전한 트랜잭션] 기존 유닛을 삭제하기 전에, 새 유닛 소환부터 시도합니다.
        Vector3 spawnPos = targetSlime.transform.position;
        GameObject newSlimeObj = PoolManager.Instance.SpawnFromPool(resultId, spawnPos, Quaternion.identity);

        if (newSlimeObj == null)
        {
            Debug.LogError("결과물 슬라임 소환에 실패하여 머지를 취소합니다. (유닛 증발 방지)");
            return null;
        }

        // 1. 새 슬라임 세팅
        Slime newSlime = newSlimeObj.GetComponent<Slime>();
        SO_SlimeData newData = DataManager.Instance.GetSlimeData(resultId);
        newSlime.SetData(newData);

        // 2. 소환에 완벽히 성공했으므로, 안심하고 기존 재료 2마리를 풀로 반납합니다.
        PoolManager.Instance.ReturnToPool(draggingSlime.SlimeID, draggingSlime.gameObject);
        PoolManager.Instance.ReturnToPool(targetSlime.SlimeID, targetSlime.gameObject);

        // 3. 시각적 피드백
        PlayMergeEffect(spawnPos, newData.rank);

        // 완성된 결과물을 사령탑(PlacementController)에게 넘겨줍니다.
        return newSlime;
    }

    private void PlayMergeEffect(Vector3 position, int rank)
    {
        // EffectPoolManager를 통해 문자열(이름)로 이펙트를 소환합니다.
        if (EffectPoolManager.Instance != null)
        {
            if (rank >= 0 && rank < mergeEffectNames.Length)
            {
                string effectName = mergeEffectNames[rank];

                // 등록된 이펙트 이름이 비어있지 않을 때만 소환
                if (!string.IsNullOrEmpty(effectName))
                {
                    Vector3 effectPos = position - (Vector3.up * 1f);
                    EffectPoolManager.Instance.SpawnEffect(effectName, effectPos, Quaternion.identity);
                }
            }
        }

        /* 합성 사운드 위치
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("SFX_Merge_Success");
        }
        */
    }
}