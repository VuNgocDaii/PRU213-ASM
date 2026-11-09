using UnityEngine;

// WaveSpawner selects an active route. Waypoints are ordered from the map edge to the castle.
public class EnemyRoute : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    public Transform[] Waypoints => waypoints;
    public Vector3 SpawnPosition => waypoints != null && waypoints.Length > 0 && waypoints[0] != null ? waypoints[0].position : transform.position;
    private void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = new Color(1, 0.45f, 0.2f);
        for (int i = 1; i < waypoints.Length; i++)
            if (waypoints[i - 1] != null && waypoints[i] != null) Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
    }
}
