#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class MergeTableImporter : MonoBehaviour
{
    [MenuItem("Tools/CSV Data Import/Merge Table")]
    public static void ImportMergeTable()
    {
        // 올려주신 임포터들과 동일하게 Path.Combine 구조로 변경
        string csvPath = Path.Combine(Application.dataPath, "_Project", "Data", "Tables", "MergeTable.csv");
        string saveFolderPath = Path.Combine("Assets", "_Project", "Data", "Resources", "MergeData");
        string saveFilePath = Path.Combine(saveFolderPath, "MergeTable.asset");

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[MergeTableImporter] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);

        // 기존 에셋 로드 또는 새 인스턴스 생성
        SO_MergeTableData tableData = AssetDatabase.LoadAssetAtPath<SO_MergeTableData>(saveFilePath);
        if (tableData == null)
        {
            tableData = ScriptableObject.CreateInstance<SO_MergeTableData>();
            AssetDatabase.CreateAsset(tableData, saveFilePath);
        }

        tableData.recipes.Clear();

        string[] lines = File.ReadAllLines(csvPath);

        // 5행(인덱스 4)부터 데이터 파싱 시작
        for (int i = 4; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] data = line.Split(',');

            if (data.Length <= 6) continue;

            int bID = ParseInt(data[0]);
            int mID = ParseInt(data[3]);
            int rID = ParseInt(data[6]);

            if (bID != 0 && mID != 0 && rID != 0)
            {
                tableData.recipes.Add(new MergeRecipe
                {
                    baseID = bID,
                    materialID = mID,
                    resultID = rID
                });
            }
        }

        EditorUtility.SetDirty(tableData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MergeTableImporter] 총 {tableData.recipes.Count}개의 조합식 임포트 완료!");
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value, out int result)) return result;
        return 0;
    }
}
#endif