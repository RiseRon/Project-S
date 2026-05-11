using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class WaveDataImporter
{
    [MenuItem("Tools/CSV Data Import/Wave Data (Full)")]
    public static void ImportCSV()
    {
        string waveCSVPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "WaveTable.csv");
        string monsterCSVPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "WaveSpawnTable.csv");
        string savePath = Path.Combine("Assets", "_Project", "Data", "Resources", "WaveData");

        if (!File.Exists(waveCSVPath) || !File.Exists(monsterCSVPath))
        {
            Debug.LogError($"[Importer] CSV 파일을 찾을 수 없습니다.");
            return;
        }

        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        // 1. 몬스터 데이터를 먼저 안전하게 가공
        Dictionary<int, List<EnemySpawnGroup>> monsterMap = PreProcessMonsterData(monsterCSVPath);

        // 2. 웨이브 데이터 읽기
        string[] waveLines = File.ReadAllLines(waveCSVPath);

        for (int i = 4; i < waveLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(waveLines[i])) continue; // 완전히 비어있는 줄 건너뛰기

            string[] waveData = waveLines[i].Split(',');

            // 필수 데이터(StageID, WaveID)가 숫자인지 확인 (빈 쉼표 줄 방지)
            if (!int.TryParse(waveData[0], out int stageID)) continue;
            if (!int.TryParse(waveData[1], out int waveID)) continue;

            SO_WaveData so = CreateOrGetSO(savePath, stageID, waveID);

            // 데이터 할당 (TryParse를 사용하여 에러 방지)
            so.stageID = stageID;
            so.waveID = waveID;
            int.TryParse(waveData[2], out so.rewardID);
            int.TryParse(waveData[3], out so.rewardAmount);
            int.TryParse(waveData[4], out so.slotMoveRecovery);
            int.TryParse(waveData[5], out so.waveTime);
            int.TryParse(waveData[6], out so.waitingTime);
            int.TryParse(waveData[7], out so.nextGroupCycle);
            int.TryParse(waveData[8], out so.hpGrowthRate);

            // 몬스터 리스트 할당
            if (monsterMap.ContainsKey(waveID))
                so.enemyList = monsterMap[waveID];
            else
                so.enemyList = new List<EnemySpawnGroup>();

            EditorUtility.SetDirty(so);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>웨이브 데이터 임포트 완료!</color>");
    }

    private static Dictionary<int, List<EnemySpawnGroup>> PreProcessMonsterData(string path)
    {
        Dictionary<int, List<EnemySpawnGroup>> map = new Dictionary<int, List<EnemySpawnGroup>>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 4; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split(',');

            // WaveID(1번 인덱스)가 숫자인지 확인 (데이터가 없는 빈 칸/쉼표 줄 건너뛰기)
            if (!int.TryParse(data[1], out int waveID)) continue;
            if (!int.TryParse(data[4], out int count)) continue;

            if (count <= 0) continue;

            if (!map.ContainsKey(waveID))
                map[waveID] = new List<EnemySpawnGroup>();

            float.TryParse(data[2], out float interval);
            int.TryParse(data[3], out int enemyID);

            map[waveID].Add(new EnemySpawnGroup
            {
                spawnInterval = interval,
                enemyID = enemyID,
                spawnCount = count
            });
        }
        return map;
    }

    private static SO_WaveData CreateOrGetSO(string path, int stageID, int waveID)
    {
        string fullPath = $"{path}/{stageID}_{waveID}_Wave.asset";
        SO_WaveData so = AssetDatabase.LoadAssetAtPath<SO_WaveData>(fullPath);

        if (so == null)
        {
            so = ScriptableObject.CreateInstance<SO_WaveData>();
            AssetDatabase.CreateAsset(so, fullPath);
        }
        return so;
    }
}