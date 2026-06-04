using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Camera Third-Person Shooter kiểu PUBG:
/// - Mouse xoay camera quanh player (yaw + pitch)
/// - Camera offset sau lưng + hơi sang phải
/// - Camera collision: không xuyên tường
/// - Cursor bị khóa giữa màn hình
/// - Player quay theo hướng camera khi di chuyển
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;              // Kim Đồng
    [SerializeField] private Vector3   shoulderOffset = new Vector3(0.5f, 1.6f, 0f); // Vai phải

    [Header("Distance")]
    [SerializeField] private float distance    = 3.5f;     // Khoảng cách mặc định
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed   = 3f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch         = -25f;  // Góc nhìn xuống
    [SerializeField] private float maxPitch         =  60f;  // Góc nhìn lên

    [Header("Smoothing")]
    [SerializeField] private float rotationSmooth = 10f;
    [SerializeField] private float followSmooth   = 15f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float     collisionRadius = 0.2f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    // Góc hiện tại
    private float yaw   = 0f;
    private float pitch = 15f;

    private float   currentDistance;
    private Vector3 currentPosition;
    private bool    cursorLocked = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentDistance = distance;
    }

    private void Start()
    {
        if (target == null && PlayerController.Instance != null)
            target = PlayerController.Instance.transform;

        if (lockCursorOnStart) LockCursor(true);

        // Góc ban đầu theo hướng nhân vật
        if (target != null) yaw = target.eulerAngles.y;

        // Giới hạn tầm nhìn (Fog of War)
        if (Camera.main != null) Camera.main.farClipPlane = 45f;

        // Enable post-processing
        var urpData = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpData != null) urpData.renderPostProcessing = true;
    }

    private void Update()
    {
        HandleCursorLock();
        if (target == null)
        {
            if (PlayerController.Instance != null) target = PlayerController.Instance.transform;
            return;
        }
        HandleInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        ApplyCamera();
    }

    // ─── Input ────────────────────────────────────────────────────────────────
    private void HandleInput()
    {
        if (!cursorLocked || Mouse.current == null) return;

        // Mouse delta
        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw   += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Scroll zoom
        if (Mouse.current.scroll.y.ReadValue() != 0f)
        {
            float scroll = Mouse.current.scroll.y.ReadValue() * 0.01f;
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    private void HandleCursorLock()
    {
        if (Keyboard.current == null) return;

        // Alt để mở khóa cursor tạm thời
        if (Keyboard.current.leftAltKey.wasPressedThisFrame) LockCursor(!cursorLocked);

        // Khi pause thì mở khóa cursor
        if (GameManager.Instance != null)
        {
            bool shouldLock = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
            if (cursorLocked != shouldLock) LockCursor(shouldLock);
        }
    }

    private void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // ─── Camera Apply ─────────────────────────────────────────────────────────
    private void ApplyCamera()
    {
        // Tính rotation camera
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        // Pivot = đầu nhân vật + shoulder offset
        Vector3 pivotWorld = target.position + target.TransformDirection(shoulderOffset);

        // Vị trí camera = pivot - forward * distance
        Vector3 desiredPos = pivotWorld - targetRot * Vector3.forward * distance;

        // Camera collision: tránh xuyên tường
        float actualDist = CheckCameraCollision(pivotWorld, desiredPos);
        Vector3 finalPos = pivotWorld - targetRot * Vector3.forward * actualDist;

        // Smooth follow
        currentPosition = Vector3.Lerp(currentPosition, finalPos, Time.deltaTime * followSmooth);
        transform.position = currentPosition;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);

        // Thông báo cho PlayerController biết hướng camera để di chuyển đúng
        NotifyPlayerCameraYaw();
    }

    private float CheckCameraCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir  = (to - from).normalized;
        float   dist = Vector3.Distance(from, to);

        if (Physics.SphereCast(from, collisionRadius, dir, out RaycastHit hit, dist, collisionMask))
            return Mathf.Max(hit.distance - collisionRadius, minDistance);

        return distance;
    }

    private void NotifyPlayerCameraYaw()
    {
        // Player sẽ dùng yaw này để biết "forward" theo camera
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetCameraYaw(yaw);
    }

    // ─── Public ───────────────────────────────────────────────────────────────
    public float GetYaw()   => yaw;
    public float GetPitch() => pitch;
    public void  SetTarget(Transform t) => target = t;
}
