using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MergeRecipe
{
    public int baseID;       // 0열: 베이스 슬라임 ID
    public int materialID;   // 3열: 재료 슬라임 ID
    public int resultID;     // 6열: 결과 슬라임 ID
}

[CreateAssetMenu(fileName = "MergeTable", menuName = "ScriptableObjects/MergeTable")]
public class SO_MergeTableData : ScriptableObject
{
    // CSV에서 읽어온 모든 조합식을 담아둘 리스트
    public List<MergeRecipe> recipes = new List<MergeRecipe>();
}