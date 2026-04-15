using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static Transform[] Waypoints;

    private void Awake()
    {
        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        int childCount = transform.childCount;
        Waypoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Waypoints[i] = transform.GetChild(i);
        }
        Debug.Log($"경로 설정 완료! 총 {Waypoints.Length}개의 웨이포인트.");
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
