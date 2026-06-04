using UnityEngine;

/// <summary>
/// Trạng thái Nghi ngờ 3D - địch rời lộ trình tới vị trí nghe thấy âm thanh.
/// Detection gauge vàng. Nếu thấy player → Alert. Hết thời gian → Patrol.
/// </summary>
public class InvestigateState : IEnemyState
{
    private Vector3 investigateTarget;
    private float   investigateTimer;
    private bool    reachedTarget = false;
    private const float INVESTIGATE_DURATION = 6f;

    public InvestigateState(Vector3 soundPosition)
    {
        investigateTarget = soundPosition;
    }

    public void EnterState(EnemyAI enemy)
    {
        enemy.SetAnimationState("Investigate");
        enemy.Agent.speed            = enemy.InvestigateSpeed;
        enemy.Agent.stoppingDistance = 0.5f;
        investigateTimer             = INVESTIGATE_DURATION;
        reachedTarget                = false;
        enemy.Agent.SetDestination(investigateTarget);
        enemy.DetectionGauge?.SetLevel(DetectionLevel.Suspicious);
        AudioManager.Instance?.PlaySuspenseMusic();
        GameManager.Instance?.RaiseAlertLevel();
        Debug.Log($"[Enemy] Nghi ngờ - đang đến {investigateTarget}");
    }

    public void UpdateState(EnemyAI enemy)
    {
        if (enemy.Vision.CanSeePlayer())
        {
            enemy.TransitionTo(new AlertState());
            return;
        }

        investigateTimer -= Time.deltaTime;
        if (investigateTimer <= 0f)
        {
            enemy.TransitionTo(new PatrolState());
            return;
        }

        if (!reachedTarget && !enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.6f)
        {
            reachedTarget = true;
            enemy.Agent.ResetPath();
            enemy.StartLookAround();
        }
    }

    public void ExitState(EnemyAI enemy)
    {
        enemy.DetectionGauge?.SetLevel(DetectionLevel.Normal);
        GameManager.Instance?.LowerAlertLevel();
    }
}
