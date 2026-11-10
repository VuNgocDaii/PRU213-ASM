using UnityEngine;

// Quái mặt đất (thường, nhanh, trâu, boss): đi lần lượt qua các waypoint, hết đường thì đánh thành.
public class GroundEnemy : EnemyController
{
    private EnemyRoute route;
    private int waypointIndex;

    public override bool IsFlying => false;

    public void SetRoute(EnemyRoute newRoute) { route = newRoute; }

    protected override void OnSpawned() { waypointIndex = 1; }   // waypoint 0 là chỗ xuất hiện

    protected override void Move()
    {
        bool hasNextWaypoint = route != null && waypointIndex < route.Waypoints.Length;
        Vector3 destination = hasNextWaypoint ? route.Waypoints[waypointIndex].position : Context.CastleTarget.position;

        if (!MoveTowards(destination)) return;

        if (hasNextWaypoint) waypointIndex++;
        else ReachCastle();
    }
}
