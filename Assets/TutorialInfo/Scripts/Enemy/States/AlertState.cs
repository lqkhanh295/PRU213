using UnityEngine;

/// <summary>
/// Trạng thái Báo động 3D - địch truy đuổi player trực tiếp.
/// Detection gauge đỏ. Nhạc dồn dập.
/// Mất dấu LOST_SIGHT_PATIENCE giây → quay về Investigate vị trí cuối.
/// </summary>
public class AlertState : IEnemyState
{
    private float   lostSightTimer = 0f;
    private Vector3 lastKnownPosition;
    private const float LOST_SIGHT_PATIENCE = 4f;

    public void EnterState(EnemyAI enemy)
    {
        enemy.SetAnimationState("Alert");
        enemy.Agent.speed            = enemy.ChaseSpeed;
        enemy.Agent.stoppingDistance = enemy.CaptureDistance * 0.5f;
        enemy.DetectionGauge?.SetLevel(DetectionLevel.Detected);
        AudioManager.Instance?.PlayAlertMusic();
        GameManager.Instance?.SetGlobalAlertLevel(2);
        lostSightTimer    = 0f;
        lastKnownPosition = PlayerController.Instance
                            ? PlayerController.Instance.transform.position
                            : enemy.transform.position;
        Debug.Log("[Enemy] BÁO ĐỘNG - phát hiện Kim Đồng!");
    }

    public void UpdateState(EnemyAI enemy)
    {
        if (PlayerController.Instance == null) return;

        if (enemy.Vision.CanSeePlayer())
        {
            lostSightTimer    = 0f;
            lastKnownPosition = PlayerController.Instance.transform.position;
            enemy.Agent.SetDestination(lastKnownPosition);

            float dist = Vector3.Distance(enemy.transform.position, lastKnownPosition);
            if (dist < enemy.CaptureDistance)
                PlayerController.Instance.Die();
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= LOST_SIGHT_PATIENCE)
                enemy.TransitionTo(new InvestigateState(lastKnownPosition));
        }
    }

    public void ExitState(EnemyAI enemy)
    {
        enemy.DetectionGauge?.SetLevel(DetectionLevel.Normal);
        GameManager.Instance?.LowerAlertLevel();
    }
}
