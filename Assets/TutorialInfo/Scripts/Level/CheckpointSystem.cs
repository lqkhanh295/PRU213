using UnityEngine;

/// <summary>
/// Checkpoint 3D - lưu vị trí an toàn khi player chạm vào.
/// </summary>
public class CheckpointSystem : MonoBehaviour
{
    public static CheckpointSystem Instance { get; private set; }

    [SerializeField] private Transform[] checkpoints;
    private int     activeCheckpointIndex = 0;
    private Vector3 savedPlayerPos;
    private Vector3 savedEscortPos;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Default spawn position (matches SceneBootstrapper player start)
        savedPlayerPos = new Vector3(-45, 0.9f, -45);
        if (checkpoints != null && checkpoints.Length > 0 && checkpoints[0] != null) 
            savedPlayerPos = checkpoints[0].position;
    }

    public void ActivateCheckpoint(int index)
    {
        if (checkpoints == null || index >= checkpoints.Length || checkpoints[index] == null) return;
        activeCheckpointIndex = index;
        savedPlayerPos        = checkpoints[index].position;

        EscortTarget escort = FindAnyObjectByType<EscortTarget>();
        if (escort) savedEscortPos = escort.transform.position;

        Debug.Log($"[Checkpoint] Lưu trạm {index}");
    }

    public Vector3 GetRespawnPosition()  => savedPlayerPos;
    public Vector3 GetEscortRespawnPos() => savedEscortPos;
}

/// <summary>
/// Trigger 3D gắn vào từng cờ/trạm trong scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private int              checkpointIndex;
    [SerializeField] private MeshRenderer     flagRenderer;
    [SerializeField] private Material         activatedMaterial;
    private bool activated = false;

    private void Awake()
    {
        // Đảm bảo collider là trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || activated) return;
        activated = true;
        CheckpointSystem.Instance?.ActivateCheckpoint(checkpointIndex);
        if (flagRenderer && activatedMaterial)
            flagRenderer.material = activatedMaterial;
    }
}
