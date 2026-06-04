using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bộ điều phối AI kẻ địch 3D - FSM (State Pattern).
/// NavMeshAgent cho pathfinding trên terrain 3D.
/// Trạng thái: Patrol → Investigate → Alert
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyVision))]
[RequireComponent(typeof(EnemySoundDetection))]
public class EnemyAI : MonoBehaviour
{
    public EnemyVision         Vision         { get; private set; }
    public EnemySoundDetection SoundDetection { get; private set; }
    public DetectionGaugeUI    DetectionGauge { get; private set; }
    public NavMeshAgent        Agent          { get; private set; }

    [Header("Speed")]
    public float PatrolSpeed      = 2f;
    public float InvestigateSpeed = 3f;
    public float ChaseSpeed       = 5.5f;

    [Header("Waypoints")]
    public Transform[] Waypoints;
    public float WaypointWaitTime = 2f;

    [Header("Capture")]
    public float CaptureDistance = 1.2f;

    private IEnemyState currentState;
    public IEnemyState CurrentState => currentState;

    private void Awake()
    {
        Agent          = GetComponent<NavMeshAgent>();
        Vision         = GetComponent<EnemyVision>();
        SoundDetection = GetComponent<EnemySoundDetection>();
        DetectionGauge = GetComponentInChildren<DetectionGaugeUI>();
    }

    private void Start() => TransitionTo(new PatrolState());

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        currentState?.UpdateState(this);
    }

    public void TransitionTo(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public void SetAnimationState(string stateName)
    {
        Animator anim = GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController != null) anim.SetTrigger(stateName);
    }

    private Coroutine lookCoroutine;
    public void StartLookAround()
    {
        if (lookCoroutine != null) StopCoroutine(lookCoroutine);
        lookCoroutine = StartCoroutine(LookAroundRoutine());
    }

    private System.Collections.IEnumerator LookAroundRoutine()
    {
        float elapsed = 0f;
        float total   = 3f;
        float startY  = transform.eulerAngles.y;
        while (elapsed < total)
        {
            float angle = Mathf.Sin(elapsed * Mathf.PI) * 60f;
            transform.rotation = Quaternion.Euler(0f, startY + angle, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Gọi từ DistractionSystem / SoundEmitter
    public void OnSoundHeard(Vector3 soundOrigin, float radius)
    {
        if (currentState is AlertState) return;
        float dist = Vector3.Distance(transform.position, soundOrigin);
        if (dist <= radius)
            TransitionTo(new InvestigateState(soundOrigin));
    }

    private void OnDrawGizmosSelected()
    {
        if (Waypoints == null) return;
        Gizmos.color = Color.magenta;
        foreach (Transform wp in Waypoints)
            if (wp) Gizmos.DrawSphere(wp.position, 0.25f);
    }
}
