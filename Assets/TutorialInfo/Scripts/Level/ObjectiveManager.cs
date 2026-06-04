using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý mục tiêu từng màn 3D - phi tuyến tính.
/// Mỗi màn có ít nhất 2 con đường hoàn thành.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public enum ObjectiveType
    {
        DeliverLetter,
        EscortOfficial,
        ReachCheckpoint,
        ScoutArea
    }

    [System.Serializable]
    public class Objective
    {
        public string        description;
        public ObjectiveType type;
        public bool          isCompleted;
        public Transform     targetLocation;
        public UnityEvent    onComplete;
    }

    [Header("Objectives")]
    [SerializeField] private List<Objective> objectives = new();
    private int currentObjectiveIndex = 0;

    public UnityEvent onAllObjectivesComplete;

    public Objective CurrentObjective =>
        currentObjectiveIndex < objectives.Count ? objectives[currentObjectiveIndex] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (objectives.Count > 0)
            Debug.Log($"[Objective] Mục tiêu: {CurrentObjective?.description}");
    }

    public void CompleteCurrentObjective()
    {
        if (CurrentObjective == null) return;
        CurrentObjective.isCompleted = true;
        CurrentObjective.onComplete?.Invoke();
        Debug.Log($"[Objective] Hoàn thành: {CurrentObjective.description}");

        currentObjectiveIndex++;
        if (currentObjectiveIndex >= objectives.Count)
        {
            onAllObjectivesComplete?.Invoke();
            GameManager.Instance?.SetGameState(GameManager.GameState.LevelComplete);
        }
        else
        {
            Debug.Log($"[Objective] Tiếp theo: {CurrentObjective?.description}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (CurrentObjective?.type == ObjectiveType.ReachCheckpoint)
            CompleteCurrentObjective();
    }

    public float GetProgress() =>
        objectives.Count == 0 ? 0f : (float)currentObjectiveIndex / objectives.Count;

    /// <summary>Thêm objective lúc runtime (gọi từ SceneBootstrapper).</summary>
    public void AddObjective(string description, ObjectiveType type, Transform target = null)
    {
        objectives.Add(new Objective
        {
            description    = description,
            type           = type,
            targetLocation = target,
            isCompleted    = false
        });
    }
}
