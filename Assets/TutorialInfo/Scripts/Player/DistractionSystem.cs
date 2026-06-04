using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hệ thống đánh lạc hướng - New Input System.
/// Q = Huýt sáo | E = Ném sỏi | R = Thả động vật
/// </summary>
public class DistractionSystem : MonoBehaviour
{
    [Header("Whistle (Q)")]
    [SerializeField] private float whistleCooldown = 5f;

    [Header("Rock Throw (E)")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float rockThrowForce  = 15f;
    [SerializeField] private float rockSoundRadius = 6f;
    [SerializeField] private float rockCooldown    = 3f;
    [SerializeField] private int   maxRocks        = 5;

    [Header("Animal Release (R)")]
    [SerializeField] private float animalRadius   = 10f;
    [SerializeField] private float animalDuration = 6f;
    [SerializeField] private float animalCooldown = 15f;
    [SerializeField] private int   maxAnimals     = 2;

    private float whistleTimer = 0f;
    private float rockTimer    = 0f;
    private float animalTimer  = 0f;
    private int   rocksLeft;
    private int   animalsLeft;

    private Camera mainCam;

    private void Start()
    {
        mainCam     = Camera.main;
        rocksLeft   = maxRocks;
        animalsLeft = maxAnimals;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Keyboard.current == null) return;

        if (whistleTimer > 0f) whistleTimer -= Time.deltaTime;
        if (rockTimer    > 0f) rockTimer    -= Time.deltaTime;
        if (animalTimer  > 0f) animalTimer  -= Time.deltaTime;

        if (Keyboard.current.qKey.wasPressedThisFrame && whistleTimer <= 0f) UseWhistle();
        if (Keyboard.current.eKey.wasPressedThisFrame && rockTimer    <= 0f) ThrowRock();
        if (Keyboard.current.rKey.wasPressedThisFrame && animalTimer  <= 0f) ReleaseAnimal();
    }

    private void UseWhistle()
    {
        whistleTimer = whistleCooldown;
        AudioManager.Instance?.PlayWhistle();
        FindAnyObjectByType<EscortTarget>()?.ReceiveSignal();
        Debug.Log("[Distraction] Huýt sáo tiếng chim rừng!");
    }

    private void ThrowRock()
    {
        if (rocksLeft <= 0) { Debug.Log("[Distraction] Hết sỏi!"); return; }
        if (mainCam == null) { mainCam = Camera.main; if (mainCam == null) return; }

        // Raycast từ camera qua vị trí chuột xuống mặt phẳng Y=0
        Vector2 mouseScreenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Ray ray = mainCam.ScreenPointToRay(mouseScreenPos);
        Vector3 target = transform.position + transform.forward * 5f; // fallback
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            target = hit.point;

        rocksLeft--;
        rockTimer = rockCooldown;
        AudioManager.Instance?.PlayRockThrow();

        if (rockPrefab != null)
        {
            GameObject rock = Instantiate(rockPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Vector3 dir     = (target - rock.transform.position).normalized;
            Rigidbody rockRb = rock.GetComponent<Rigidbody>();
            if (rockRb) rockRb.AddForce(dir * rockThrowForce, ForceMode.Impulse);
            SoundEmitter emitter = rock.AddComponent<SoundEmitter>();
            emitter.Init(rockSoundRadius, target);
        }
        else
        {
            // Tạo hòn sỏi 3D
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "ThrownRock";
            rock.transform.position = transform.position + Vector3.up * 0.5f;
            rock.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            Renderer rend = rock.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = MaterialLibrary.Rock();
            }

            Rigidbody rockRb = rock.AddComponent<Rigidbody>();
            rockRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Vector3 dir = (target - rock.transform.position).normalized;
            // Cho bay vồng lên một tí cho tự nhiên
            dir.y += 0.25f;
            dir.Normalize();

            rockRb.AddForce(dir * rockThrowForce, ForceMode.Impulse);

            SoundEmitter emitter = rock.AddComponent<SoundEmitter>();
            emitter.Init(rockSoundRadius, target);
        }
    }

    private void ReleaseAnimal()
    {
        if (animalsLeft <= 0) { Debug.Log("[Distraction] Hết động vật!"); return; }
        animalsLeft--;
        animalTimer = animalCooldown;
        StartCoroutine(AnimalDistractionRoutine());
    }

    private IEnumerator AnimalDistractionRoutine()
    {
        Debug.Log("[Distraction] Thả động vật nhỏ - gây náo loạn!");
        EmitSoundWave(transform.position, animalRadius);
        yield return new WaitForSeconds(animalDuration * 0.5f);
        Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
        EmitSoundWave(transform.position + offset, animalRadius * 0.6f);
        yield return new WaitForSeconds(animalDuration * 0.5f);
        Debug.Log("[Distraction] Náo loạn kết thúc.");
    }

    private void EmitSoundWave(Vector3 origin, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        foreach (Collider hit in hits)
            hit.GetComponent<EnemyAI>()?.OnSoundHeard(origin, radius);
    }

    public (int rocks, int animals) GetAmmo() => (rocksLeft, animalsLeft);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, animalRadius);
    }
}

public class SoundEmitter : MonoBehaviour
{
    private float   radius;
    private Vector3 targetPos;

    public void Init(float r, Vector3 target) { radius = r; targetPos = target; }

    private void OnCollisionEnter(Collision col)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider hit in hits)
            hit.GetComponent<EnemyAI>()?.OnSoundHeard(transform.position, radius);
        Destroy(gameObject);
    }
}
