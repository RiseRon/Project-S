using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    // static 변수는 관례상 대문자로 시작하거나 규칙에 따라 표기
    public static Transform[] WaypointPoints;

    private void Awake()
    {
        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        int childCount = transform.childCount;
        WaypointPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            WaypointPoints[i] = transform.GetChild(i);
        }
    }

    private void OnDrawGizmos()
    {
        if (transform.childCount < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }
    }
}
