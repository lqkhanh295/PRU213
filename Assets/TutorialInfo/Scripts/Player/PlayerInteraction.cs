using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cơ chế "Giả vờ" (Disguise) - New Input System.
/// Nhấn F để ngụy trang khi địch đến gần.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public enum DisguiseType { None, HerdingBuffalo, PlayingFlute, ChoppingWood }

    [Header("Disguise Settings")]
    [SerializeField] private float disguiseDuration = 4f;
    [SerializeField] private float disguiseCooldown = 8f;

    [Header("Interaction Radius")]
    [SerializeField] private float interactRadius   = 2f;
    [SerializeField] private LayerMask interactLayer;

    private float cooldownTimer = 0f;
    private bool  isDisguising  = false;
    private float disguiseTimer = 0f;
    private DisguiseType activeDisguise = DisguiseType.None;

    private PlayerController player;
    private Animator         animator;

    private void Awake()
    {
        player   = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (isDisguising)
        {
            disguiseTimer -= Time.deltaTime;
            if (disguiseTimer <= 0f) EndDisguise();
            return;
        }

        // New Input System: F key
        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame &&
            cooldownTimer <= 0f)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Use all layers if interactLayer is not set
        Collider[] hits = interactLayer == 0
            ? Physics.OverlapSphere(transform.position, interactRadius)
            : Physics.OverlapSphere(transform.position, interactRadius, interactLayer);

        DisguiseType chosen = DisguiseType.PlayingFlute;
        float closestDist   = float.MaxValue;

        foreach (Collider hit in hits)
        {
            InteractPoint point = hit.GetComponent<InteractPoint>();
            if (point == null) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist) { closestDist = dist; chosen = point.disguiseType; }
        }

        StartDisguise(chosen);
    }

    private Coroutine visualRoutine;

    private void StartDisguise(DisguiseType type)
    {
        activeDisguise = type;
        isDisguising   = true;
        disguiseTimer  = disguiseDuration;
        cooldownTimer  = disguiseCooldown;
        player.SetDisguised(true);

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(type.ToString());

        AudioManager.Instance?.PlayDisguise();
        Debug.Log($"[Disguise] Dền đang giả vờ: {type}");

        if (visualRoutine != null) StopCoroutine(visualRoutine);
        visualRoutine = StartCoroutine(DisguiseVisualRoutine(type));
    }

    private void EndDisguise()
    {
        isDisguising   = false;
        activeDisguise = DisguiseType.None;
        player.SetDisguised(false);
        if (visualRoutine != null) { StopCoroutine(visualRoutine); visualRoutine = null; }
    }

    public void CancelDisguise()
    {
        if (isDisguising)
        {
            EndDisguise();
            Debug.Log("[Disguise] Hủy ngụy trang do di chuyển.");
        }
    }

    private IEnumerator DisguiseVisualRoutine(DisguiseType type)
    {
        float spawnTimer = 0f;
        while (isDisguising)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= 0.25f)
            {
                spawnTimer = 0f;
                if (type == DisguiseType.PlayingFlute)
                {
                    CreateFloatingNote();
                }
                else if (type == DisguiseType.ChoppingWood)
                {
                    CreateWoodChip();
                }
                else if (type == DisguiseType.HerdingBuffalo)
                {
                    CreateGrassLeaf();
                }
            }
            yield return null;
        }
    }

    private void CreateFloatingNote()
    {
        GameObject note = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        note.name = "MusicNote";
        note.transform.position = transform.position + Vector3.up * 1.8f + Random.insideUnitSphere * 0.2f;
        note.transform.localScale = Vector3.one * Random.Range(0.12f, 0.2f);
        note.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.1f, 0.9f, 0.8f));
        Destroy(note.GetComponent<Collider>());
        
        StartCoroutine(FloatAndFade(note, Vector3.up * Random.Range(1.5f, 2.5f), 1.2f));
    }

    private void CreateWoodChip()
    {
        GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chip.name = "WoodChip";
        chip.transform.position = transform.position + Vector3.up * 0.2f + transform.forward * 0.5f;
        chip.transform.localScale = new Vector3(Random.Range(0.06f, 0.12f), Random.Range(0.04f, 0.08f), Random.Range(0.06f, 0.12f));
        chip.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.45f, 0.3f, 0.15f));
        Destroy(chip.GetComponent<Collider>());

        Rigidbody rb = chip.AddComponent<Rigidbody>();
        Vector3 force = (transform.forward + transform.up + Random.insideUnitSphere * 0.5f).normalized * Random.Range(2f, 4f);
        rb.AddForce(force, ForceMode.Impulse);
        Destroy(chip, 1f);
    }

    private void CreateGrassLeaf()
    {
        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "GrassLeaf";
        leaf.transform.position = transform.position + Vector3.up * 1.6f + Random.insideUnitSphere * 0.4f;
        leaf.transform.localScale = new Vector3(Random.Range(0.03f, 0.05f), Random.Range(0.15f, 0.25f), Random.Range(0.03f, 0.05f));
        leaf.transform.rotation = Random.rotation;
        leaf.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.2f, 0.8f, 0.2f));
        Destroy(leaf.GetComponent<Collider>());

        StartCoroutine(FloatAndFade(leaf, (Vector3.down + Random.insideUnitSphere * 0.5f).normalized * Random.Range(0.5f, 1f), 1.5f));
    }

    private IEnumerator FloatAndFade(GameObject obj, Vector3 velocity, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = obj.transform.localScale;
        Renderer rend = obj.GetComponent<Renderer>();
        Color startColor = rend.material.color;

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.position += velocity * Time.deltaTime;
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (rend != null)
            {
                rend.material.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            }
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    public bool IsActivelyDisguised()     => isDisguising;
    public DisguiseType GetDisguiseType() => activeDisguise;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

public class InteractPoint : MonoBehaviour
{
    public PlayerInteraction.DisguiseType disguiseType;
}
