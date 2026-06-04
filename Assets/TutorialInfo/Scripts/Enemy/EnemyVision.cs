using UnityEngine;

/// <summary>
/// Tầm nhìn 3D của địch - Cone of Sight dạng hình nón.
/// - Vùng đỏ  (Immediate): phát hiện ngay trong bán kính gần.
/// - Vùng vàng (Suspicious): trong góc nhìn, cần thời gian để xác nhận.
/// Raycast 3D kiểm tra vật cản (tường, cây, đá).
/// Player ẩn trong bụi → tăng thời gian phát hiện.
/// Player đang Disguise → cần thêm thời gian.
/// </summary>
public class EnemyVision : MonoBehaviour
{
    [Header("Cone of Sight")]
    [SerializeField] private float immediateDetectRange = 3f;
    [SerializeField] private float suspiciousRange      = 9f;
    [SerializeField] private float fieldOfViewAngle     = 100f;
    [SerializeField] private float eyeHeight            = 1.6f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Detection Speed")]
    [SerializeField] private float normalDetectTime    = 1.5f;
    [SerializeField] private float hiddenDetectTime    = 5f;
    [SerializeField] private float disguisedDetectTime = 3f;

    private float detectionProgress = 0f;
    private bool  playerInSight     = false;

    private DetectionGaugeUI gauge;

    private void Awake() => gauge = GetComponentInChildren<DetectionGaugeUI>();

    private void Update()
    {
        playerInSight = CheckSight();
        UpdateDetectionProgress();
    }

    private bool CheckSight()
    {
        if (PlayerController.Instance == null) return false;

        Vector3 eyePos     = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos  = PlayerController.Instance.transform.position + Vector3.up * 1f;
        Vector3 dirToPlayer= (playerPos - eyePos).normalized;
        float   dist       = Vector3.Distance(eyePos, playerPos);

        // Vùng phát hiện ngay (đỏ) - không cần góc nhìn
        if (dist <= immediateDetectRange && !PlayerController.Instance.IsHidden)
        {
            bool blocked = Physics.Raycast(eyePos, dirToPlayer, dist, obstacleMask);
            return !blocked;
        }

        // Vùng nghi ngờ (vàng) - trong góc nhìn
        if (dist <= suspiciousRange)
        {
            // Bỏ qua nếu người chơi đang ngụy trang và địch không ở trạng thái Báo động
            EnemyAI ai = GetComponent<EnemyAI>();
            bool isChasing = ai != null && ai.CurrentState is AlertState;
            if (PlayerController.Instance.IsDisguised && !isChasing)
            {
                return false;
            }

            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle < fieldOfViewAngle * 0.5f)
            {
                bool blocked = Physics.Raycast(eyePos, dirToPlayer, dist, obstacleMask);
                return !blocked;
            }
        }

        return false;
    }

    private void UpdateDetectionProgress()
    {
        if (!playerInSight)
        {
            detectionProgress = Mathf.Max(0f, detectionProgress - Time.deltaTime * 0.5f);
            gauge?.SetProgress(detectionProgress / GetRequiredTime());
            return;
        }

        detectionProgress += Time.deltaTime;
        gauge?.SetProgress(detectionProgress / GetRequiredTime());
    }

    private float GetRequiredTime()
    {
        if (PlayerController.Instance.IsHidden)    return hiddenDetectTime;
        if (PlayerController.Instance.IsDisguised) return disguisedDetectTime;
        return normalDetectTime;
    }

    public bool CanSeePlayer() =>
        playerInSight && detectionProgress >= GetRequiredTime();

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        // Vùng vàng
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Vector3 leftDir  = Quaternion.Euler(0, -fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0,  fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(eye, leftDir  * suspiciousRange);
        Gizmos.DrawRay(eye, rightDir * suspiciousRange);

        // Vùng đỏ
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(eye, immediateDetectRange);
    }
}
