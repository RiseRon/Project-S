using UnityEngine;
using UnityEditor;
using System.IO;

public class SlimeSummonImporter
{
    // 메뉴 이름을 Full로 설정하여 전체 갱신임을 명시
    [MenuItem("Tools/CSV Data Import/Slime Summon Data (Full)")]
    public static void ImportSlimeSpawnDataFull()
    {
        // 1. 경로 설정 (Path.Combine을 사용하여 OS간 호환성 확보)
        string csvPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "SlimeSummonTable.csv");
        string relativeSavePath = "Assets/_Project/Data/Resources/SlimeSummonData";
        string absoluteSavePath = Path.Combine(Application.dataPath, "_Project", "Data", "Resources", "SlimeSummonData");

        // CSV 파일 존재 여부 확인
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[Importer] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 저장할 폴더가 없으면 생성
        if (!Directory.Exists(absoluteSavePath))
        {
            Directory.CreateDirectory(absoluteSavePath);
            AssetDatabase.Refresh();
        }

        // 2. CSV 모든 라인 읽기
        string[] lines = File.ReadAllLines(csvPath);
        int importCount = 0;

        // 상단 헤더 4줄 건너뜀 (기본정보, 한글명, 영문명, 자료형)
        for (int i = 4; i < lines.Length; i++)
        {
            // 빈 줄이거나 콤마만 있는 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith(",,")) continue;

            string[] data = lines[i].Split(',');

            // 데이터 파싱 (GroupID, ID, Weight 순서)
            if (!int.TryParse(data[0], out int groupID)) continue;
            int id = int.Parse(data[1]);
            int weight = int.Parse(data[2]);

            // 3. 에셋 생성 또는 로드 (Full 방식: 있으면 덮어쓰고 없으면 생성)
            string assetPath = $"{relativeSavePath}/{data[0]}_{data[1]}_Summon.asset";
            SO_SlimeSummonData asset = AssetDatabase.LoadAssetAtPath<SO_SlimeSummonData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SO_SlimeSummonData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // 값 세팅
            asset.groupID = groupID;
            asset.id = id;
            asset.weight = weight;

            // 변경사항 기록
            EditorUtility.SetDirty(asset);
            importCount++;
        }

        // 4. 데이터베이스 저장 및 정리
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Importer] Slime Summon Data (Full) 완료: {importCount}개의 에셋이 갱신되었습니다.");
    }
}