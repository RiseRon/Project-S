using UnityEngine;

[CreateAssetMenu(fileName = "StageData_", menuName = "ScriptableObjects/StageData")]
public class SO_StageData : ScriptableObject
{
    [Header("기본 정보")]
    public int stageID;          // StageID
    public int stage;            // 스테이지 단계

    [Header("리소스 경로")]
    public string mapPrefabPath; // MapPrefabPath (Resources 폴더 기준) 
    public string bgmPath;       // BGMPath 

    [Header("세부 설정")]
    public int startCoin;        // 초기 재화
    public int barrierHP;        // 방벽 내구력
    public int slotMove;         // 슬롯 이동 횟수
}