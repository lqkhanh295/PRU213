using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Cán bộ NPC 3D - theo sau Kim Đồng khi nhận tín hiệu huýt sáo.
/// Dừng và ẩn khi global alert level >= 1.
/// NavMeshAgent 3D điều hướng trên terrain.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EscortTarget : MonoBehaviour
{
    public enum EscortState { Waiting, Following, Hiding }

    [Header("Movement")]
    [SerializeField] private float followSpeed    = 2.5f;
    [SerializeField] private float followDistance = 2.5f;

    private NavMeshAgent agent;
    private Animator     animator;
    private EscortState  state = EscortState.Waiting;

    private void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = followSpeed;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnAlertLevelChanged += OnAlertLevelChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnAlertLevelChanged -= OnAlertLevelChanged;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (state == EscortState.Following)
            FollowPlayer();
        else
            if (agent.isOnNavMesh) agent.ResetPath();
    }

    private void FollowPlayer()
    {
        if (PlayerController.Instance == null) return;
        float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        if (dist > followDistance)
            agent.SetDestination(PlayerController.Instance.transform.position);
        else
            agent.ResetPath();
    }

    public void ReceiveSignal()
    {
        if (state == EscortState.Hiding) return;
        SetState(EscortState.Following);
        Debug.Log("[Escort] Nhận tín hiệu - bắt đầu theo Kim Đồng.");
    }

    private void OnAlertLevelChanged(int level)
    {
        if (level >= 1) SetState(EscortState.Hiding);
        else if (state == EscortState.Hiding) SetState(EscortState.Waiting);
    }

    private void SetState(EscortState newState)
    {
        state = newState;
        animator?.SetTrigger(newState.ToString());
    }

    public bool HasArrived(Transform destination) =>
        Vector3.Distance(transform.position, destination.position) < 1.2f;
}
