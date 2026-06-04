using UnityEngine;

/// <summary>
/// Trạng thái Tuần tra 3D - địch di chuyển theo waypoints trên NavMesh.
/// Chuyển sang Investigate khi nghe thấy âm thanh.
/// Chuyển sang Alert khi thấy player qua EnemyVision.
/// </summary>
public class PatrolState : IEnemyState
{
    private int   currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool  isWaiting = false;

    public void EnterState(EnemyAI enemy)
    {
        enemy.SetAnimationState("Patrol");
        enemy.Agent.speed        = enemy.PatrolSpeed;
        enemy.Agent.stoppingDistance = 0.3f;
        MoveToNextWaypoint(enemy);
    }

    public void UpdateState(EnemyAI enemy)
    {
        if (enemy.Vision.CanSeePlayer())
        {
            enemy.TransitionTo(new AlertState());
            return;
        }

        if (enemy.SoundDetection.HasHeardSound())
        {
            enemy.TransitionTo(new InvestigateState(enemy.SoundDetection.LastSoundPosition));
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) { isWaiting = false; MoveToNextWaypoint(enemy); }
            return;
        }

        if (enemy.Agent.isOnNavMesh && !enemy.Agent.pathPending && enemy.Agent.remainingDistance <= enemy.Agent.stoppingDistance)
        {
            isWaiting = true;
            waitTimer = enemy.WaypointWaitTime;
        }
    }

    public void ExitState(EnemyAI enemy) { }

    private void MoveToNextWaypoint(EnemyAI enemy)
    {
        if (enemy.Waypoints != null && enemy.Waypoints.Length > 0 && enemy.Agent.isOnNavMesh)
        {
            Transform wp = enemy.Waypoints[currentWaypointIndex];
            if (wp) enemy.Agent.SetDestination(wp.position);
        }
        currentWaypointIndex = (currentWaypointIndex + 1) % enemy.Waypoints.Length;
    }
}
