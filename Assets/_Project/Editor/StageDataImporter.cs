using UnityEngine;
using UnityEditor;
using System.IO;

public class StageDataImporter
{
    [MenuItem("Tools/CSV Data Import/Stage Data (Full)")]
    public static void ImportStageData()
    {
        // 1. 경로 설정 (프로젝트 구조에 맞춰 설정)
        string csvPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "StageTable.csv");
        string saveFolderPath = "Assets/_Project/Data/Resources/StageData";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[Importer] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 저장 폴더가 없으면 생성 (Path.Combine 대신 문자열 경로 사용 권장 - 유니티 에셋 데이터베이스용)
        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);

        // 2. CSV 데이터 읽기
        string[] lines = File.ReadAllLines(csvPath);

        // [주의] 제공하신 스테이지 CSV는 헤더가 4줄입니다.
        // 실제 데이터는 5번째 줄(인덱스 4)부터 시작합니다.
        for (int i = 4; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 쉼표(,)로 분리
            string[] data = lines[i].Split(',');

            // CSV 컬럼 순서 매칭
            // 0:StageID, 3:Stage, 6:MapPrefabPath, 7:BGMPath, 5:StartCoin, 10:BarrierHP, 11:SlotMove

            int stageID = int.Parse(data[0]);
            int stage = int.Parse(data[3]);
            string assetPath = $"{saveFolderPath}/{stageID}_Stage{stage}.asset";

            // 3. ScriptableObject 파일 로드 또는 생성
            SO_StageData asset = AssetDatabase.LoadAssetAtPath<SO_StageData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SO_StageData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // 4. 데이터 할당 (수정하신 SO_StageData 필드와 매칭)
            asset.stageID = stageID;
            asset.stage = stage;
            asset.mapPrefabPath = data[6];
            asset.bgmPath = data[7];

            // 세부 설정 필드
            asset.startCoin = int.Parse(data[5]);
            asset.barrierHP = int.Parse(data[10]);
            asset.slotMove = int.Parse(data[11]);

            // 에디터 변경사항 기록
            EditorUtility.SetDirty(asset);
        }

        // 5. 저장 및 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Importer] {lines.Length - 1}개의 스테이지 데이터 임포트 완료!");
    }
}