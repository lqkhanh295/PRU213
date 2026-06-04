using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển Kim Đồng - TPS kiểu PUBG.
/// - WASD di chuyển theo hướng camera
/// - Player xoay mặt về hướng đang chạy (smooth)
/// - Shift = chạy
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private float walkSpeed            = 4f;
    [SerializeField] private float runSpeed             = 7f;
    [SerializeField] private float waterSpeedMultiplier = 0.5f;
    [SerializeField] private float rotationSpeed        = 600f;

    [Header("Sound Emission")]
    [SerializeField] private float runSoundRadius = 5f;

    public bool  IsHidden    { get; private set; }
    public bool  IsDisguised { get; private set; }
    public bool  IsRunning   { get; private set; }
    public bool  IsInWater   { get; private set; }
    public float CurrentSoundRadius { get; private set; }

    private Rigidbody rb;
    private Animator  animator;
    private Vector3   moveInput;
    private bool      isAlive    = true;
    private float     cameraYaw  = 0f;   // nhận từ CameraController

    private int hidingZoneCount = 0;
    private int waterZoneCount  = 0;
    private Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private float movementSmoothTime = 0.1f;

    private static readonly int AnimSpeed  = Animator.StringToHash("Speed");
    private static readonly int AnimHidden = Animator.StringToHash("IsHidden");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance          = this;
        rb                = GetComponent<Rigidbody>();
        animator          = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        gameObject.AddComponent<SoundRingVisual>();
    }

    private void Update()
    {
        if (!isAlive) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        ReadInput();

        // Hủy ngụy trang khi di chuyển
        if (IsDisguised && moveInput.sqrMagnitude > 0.01f)
        {
            GetComponent<PlayerInteraction>()?.CancelDisguise();
        }

        EmitSound();
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;
        Move();
        RotateTowardsMoveDirection();
    }

    // ─── Input ────────────────────────────────────────────────────────────────
    private void ReadInput()
    {
        if (Keyboard.current == null) return;

        float h = 0f, v = 0f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  h -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    v += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  v -= 1f;

        IsRunning = Keyboard.current.leftShiftKey.isPressed ||
                    Keyboard.current.rightShiftKey.isPressed;

        // Hướng di chuyển dựa trên camera yaw (TPS)
        Quaternion camRot    = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 camForward   = camRot * Vector3.forward;
        Vector3 camRight     = camRot * Vector3.right;
        moveInput            = (camForward * v + camRight * h).normalized;
    }

    // ─── Movement ─────────────────────────────────────────────────────────────
    private void Move()
    {
        if (IsDisguised)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(AnimSpeed, 0f);
                animator.SetBool(AnimHidden, IsHidden);
            }
            return;
        }

        float speed = IsRunning ? runSpeed : walkSpeed;
        if (IsInWater) speed *= waterSpeedMultiplier;

        Vector3 targetVel = moveInput * speed;
        targetVel.y       = rb.linearVelocity.y;
        
        Vector3 smoothedVel = Vector3.SmoothDamp(rb.linearVelocity, targetVel, ref currentVelocity, movementSmoothTime);
        rb.linearVelocity = smoothedVel;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(AnimSpeed, moveInput.magnitude * (IsRunning ? 1f : 0.5f));
            animator.SetBool(AnimHidden, IsHidden);
        }
    }

    private void RotateTowardsMoveDirection()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(moveInput, Vector3.up);
        rb.rotation = Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime);
    }

    // ─── Sound ────────────────────────────────────────────────────────────────
    private void EmitSound()
    {
        CurrentSoundRadius = IsRunning && moveInput.magnitude > 0f ? runSoundRadius : 0f;
        if (IsRunning && moveInput.magnitude > 0f)
            AudioManager.Instance?.PlayFootstep(IsInWater);
    }

    // ─── Triggers ─────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HidingZone")) { hidingZoneCount++; IsHidden = true; }
        else if (other.CompareTag("WaterZone")) { waterZoneCount++; IsInWater = true; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HidingZone"))
        {
            hidingZoneCount = Mathf.Max(0, hidingZoneCount - 1);
            IsHidden = hidingZoneCount > 0;
        }
        else if (other.CompareTag("WaterZone"))
        {
            waterZoneCount = Mathf.Max(0, waterZoneCount - 1);
            IsInWater = waterZoneCount > 0;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    /// <summary>Nhận góc yaw từ CameraController mỗi frame.</summary>
    public void SetCameraYaw(float yaw) => cameraYaw = yaw;

    public void SetDisguised(bool value) => IsDisguised = value;

    public void Die()
    {
        if (!isAlive) return;
        isAlive           = false;
        rb.linearVelocity = Vector3.zero;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger("Die");
        GameManager.Instance?.PlayerCaught();
    }
}
