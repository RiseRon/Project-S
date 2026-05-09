using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyDataImporter
{
    [MenuItem("Tools/CSV Data Import/Enemy Data (Full)")]
    public static void ImportEnemyData()
    {
        // 1. 경로 설정
        string csvPath = Path.Combine(Application.dataPath, "_Project", "Data","Tables", "EnemyDataTable.csv");
        string saveFolderPath = Path.Combine("Assets", "_Project", "Data", "Resources", "EnemyData");

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[Importer] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        if (!Directory.Exists(saveFolderPath)) Directory.CreateDirectory(saveFolderPath);

        // 2. CSV 데이터 읽기
        string[] lines = File.ReadAllLines(csvPath);

        // [주의] 제공하신 CSV는 헤더가 4줄입니다. (설명, 한글명, 영문명, 타입)
        // 실제 데이터는 5번째 줄(인덱스 4)부터 시작합니다.
        for (int i = 4; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 쉼표(,)로 분리
            string[] data = lines[i].Split(',');

            // CSV 컬럼 순서 매칭
            // 0:ID, 1:Name, 2:K_Name, 3:Damage, 4:MoveSpeed, 5:EnemyHP, 6:AttackSpeed, 
            // 7:SplitID, 8:SplitSpawnCount, 9:CanMoveStun, 
            // 10:CanAtkStun, 11:StunGrace, 12:DropID, 13:Amount

            int id = int.Parse(data[0]);
            string assetPath = $"{saveFolderPath}/{id}_{data[1]}.asset";

            // 3. ScriptableObject 파일 로드 또는 생성
            SO_EnemyData asset = AssetDatabase.LoadAssetAtPath<SO_EnemyData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SO_EnemyData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // 4. 데이터 할당 (SO_EnemyData 필드와 매칭)

            // ID & 정보
            asset.id = id;
            asset.enemyName = data[1]; // 한글 이름을 사용하려면 인덱스 2번

            // 능력치
            asset.damage = int.Parse(data[3]);
            asset.moveSpeed = int.Parse(data[4]);
            asset.enemyHP = int.Parse(data[5]);
            asset.attackSpeed = float.Parse(data[6]);

            // 특수 능력 (CSV의 0/1 값을 bool처럼 처리하거나 int 그대로 대입)
            asset.splitID = int.Parse(data[7]);
            asset.splitSpwanCount = int.Parse(data[8]);
            asset.canMoveStun = (int.Parse(data[9]) == 1);
            asset.canMoveStun = (int.Parse(data[10]) == 1);
            asset.StunGrace = int.Parse(data[11]);

            // 보상
            asset.dropID = int.Parse(data[12]);
            asset.amount = int.Parse(data[13]);

            // 에디터 변경사항 기록
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Importer] {lines.Length - 4}개의 적 데이터 임포트 완료!");
    }
}