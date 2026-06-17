using UnityEngine;
using System.Collections.Generic;

public class WaypointManager : MonoBehaviour
{
    // 💡 이제 여러 개의 경로 리스트를 리스트로 관리합니다.
    // 다른 스크립트에서 WaypointManager.GetPath(0) 형태로 꺼내 쓸 수 있습니다.
    private static List<List<Transform>> allPaths = new List<List<Transform>>();

    private void Awake()
    {
        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        allPaths.Clear();

        // 1단계 자식들 (ways1, ways2 등 각 경로 그룹들)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform pathGroup = transform.GetChild(i);
            List<Transform> singlePath = new List<Transform>();

            // 2단계 자식들 (해당 그룹 안의 실제 웨이포인트들)
            for (int j = 0; j < pathGroup.childCount; j++)
            {
                singlePath.Add(pathGroup.GetChild(j));
            }

            // 완성된 하나의 길을 전체 경로 리스트에 추가
            allPaths.Add(singlePath);
        }

        Debug.Log($"경로 설정 완료! 총 {allPaths.Count}개의 독립된 길이 수집되었습니다.");
    }

    /// <summary> 💡 외부(SpawnManager 등)에서 특정 번호의 길을 요구할 때 주는 함수 </summary>
    public static Transform[] GetPath(int pathIndex)
    {
        if (allPaths == null || pathIndex < 0 || pathIndex >= allPaths.Count)
        {
            Debug.LogError($"[WaypointManager] {pathIndex}번 경로는 존재하지 않습니다!");
            return null;
        }
        return allPaths[pathIndex].ToArray();
    }

    /// <summary> 💡 등록된 총 경로(길)의 개수를 반환 </summary>
    public static int PathCount => allPaths.Count;

    // 에디터에서 선이 섞이지 않고 각 길마다 따로 예쁘게 그려지도록 기즈모 수정
    private void OnDrawGizmos()
    {
        Color[] pathColors = new Color[] { Color.green, Color.cyan, Color.yellow, Color.magenta };

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform pathGroup = transform.GetChild(i);
            if (pathGroup.childCount < 2) continue;

            // 길마다 다른 색으로 그려서 식별하기 편하게 만듭니다.
            Gizmos.color = pathColors[i % pathColors.Length];

            for (int j = 0; j < pathGroup.childCount - 1; j++)
            {
                if (pathGroup.GetChild(j) != null && pathGroup.GetChild(j + 1) != null)
                {
                    Gizmos.DrawLine(pathGroup.GetChild(j).position, pathGroup.GetChild(j + 1).position);
                }
            }
        }
    }
}