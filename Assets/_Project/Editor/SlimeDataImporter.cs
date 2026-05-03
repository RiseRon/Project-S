using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class SlimeDataImporter
{
    [MenuItem("Tools/CSV Data Import/Slime Data (Full)")]
    public static void ImportSlimeData()
    {
        string csvPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "SlimeDataTable.csv");
        string saveFolderPath = Path.Combine("Assets", "_Project", "Data", "SlimeData");

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[Importer] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        if (!Directory.Exists(saveFolderPath)) Directory.CreateDirectory(saveFolderPath);

        string[] lines = File.ReadAllLines(csvPath);

        for (int i = 4; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] data = line.Split(',');

            // CSV 컬럼 순서 매칭 (SlimeDataTable.csv 기준)
            // 0:ID, 1:Name, 2:K_Name, 3:ElementType, 4:Rank, 5:Damage, 6:AttackRange, 7:AttackSpeed
            // 8:ProjectilePrefabID, 9:ProjectileType, 10:TrajectoryType, 11:ProjectileSpeed, 12:ArcHeight
            // 13:StunChance, 14:StunDuration, 15:AreaPrefabID, 16:AreaDuration, 17:SlowRate, 18:DotDamage, 19:DotDamageInterval

            if (data.Length < 20 || !int.TryParse(data[0], out int id)) continue;

            string assetPath = $"{saveFolderPath}/{id}_{data[1]}.asset";

            SO_SlimeData asset = AssetDatabase.LoadAssetAtPath<SO_SlimeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SO_SlimeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            // --- 데이터 할당 (안전한 헬퍼 함수 사용) ---

            // 기본 정보
            asset.id = id;
            asset.slimeName = data[1]; // 한글 이름을 사용하려면 인덱스 2번
            Enum.TryParse(data[3], out asset.elementType); // Enum도 안전하게 파싱 (데이터가 잘못되어도 튕기지 않음)
            asset.rank = ParseInt(data[4], "숙련도", id);

            // 전투 스탯
            asset.damage = ParseFloat(data[5], "공격력", id);
            asset.attackRange = ParseFloat(data[6], "공격 사거리", id);
            asset.attackSpeed = ParseFloat(data[7], "공격 속도", id);

            // 투사체 설정
            asset.projectilePrefabID = ParseInt(data[8], "투사체 프리팹 ID", id);
            Enum.TryParse(data[9], out asset.projectileType);
            Enum.TryParse(data[10], out asset.trajectoryType);

            // 투사체 세부 설정
            asset.projectileSpeed = ParseFloat(data[11], "투사체 속도", id);
            asset.arcHeight = ParseFloat(data[12], "포물선 높이", id);

            // 특수 효과 및 장판
            asset.stunChance = ParseFloat(data[13], "스턴 확률", id);
            asset.stunDuration = ParseFloat(data[14], "스턴 지속 시간", id);
            asset.areaPrefabID = ParseInt(data[15], "장판 프리팹 ID", id);
            asset.areaDuration = ParseFloat(data[16], "장판 유지 시간", id);
            asset.slowRate = ParseFloat(data[17], "슬로우 비율", id);
            asset.dotDamage = ParseFloat(data[18], "지속 데미지", id);
            asset.dotDamageInterval = ParseFloat(data[19], "도트 주기", id);

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Importer] {lines.Length - 4}개의 슬라임 데이터 임포트 완료!");
    }

    // --- 헬퍼 함수 정의 ---

    private static float ParseFloat(string value, string fieldName, int id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning($"[임포트 알림] ID {id}의 '{fieldName}' 데이터가 비어있어 0으로 설정되었습니다.");
            return 0f;
        }

        if (!float.TryParse(value, out float result))
        {
            Debug.LogError($"[임포트 에러] ID {id}의 '{fieldName}'에 잘못된 값('{value}')이 들어있습니다.");
            return 0f;
        }
        return result;
    }

    private static int ParseInt(string value, string fieldName, int id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning($"[임포트 알림] ID {id}의 '{fieldName}' 데이터가 비어있어 0으로 설정되었습니다.");
            return 0;
        }

        if (!int.TryParse(value, out int result))
        {
            Debug.LogError($"[임포트 에러] ID {id}의 '{fieldName}'에 잘못된 값('{value}')이 들어있습니다.");
            return 0;
        }
        return result;
    }
}