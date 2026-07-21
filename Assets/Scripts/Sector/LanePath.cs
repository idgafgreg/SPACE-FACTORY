using UnityEngine;

/// <summary>
/// Ordered list of world-space waypoints defining an enemy lane.
/// Assign in the Inspector or procedurally at runtime.
/// </summary>
public class LanePath : MonoBehaviour
{
    [Tooltip("Lane identifier matching EnemySpawnEntry.laneId (e.g. 'WestCorridor')")]
    public string laneId;

    [Tooltip("Ordered waypoints from spawn edge to Command Hub")]
    public Transform[] points;

    public int PointCount => points?.Length ?? 0;

    public Vector3 GetPoint(int index)
    {
        if (points == null || points.Length == 0) return transform.position;
        index = Mathf.Clamp(index, 0, points.Length - 1);
        return points[index].position;
    }

    public bool IsLastPoint(int index) => index >= PointCount - 1;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
#endif
}
