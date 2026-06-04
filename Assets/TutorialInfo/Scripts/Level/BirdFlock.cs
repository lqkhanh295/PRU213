using System.Collections;
using UnityEngine;

/// <summary>
/// Bầy chim bay lên khi Kim Đồng chạy qua gần.
/// Cơ chế "Yếu tố động" trong GDD - làm lộ vị trí nếu chạy ẩu.
/// </summary>
public class BirdFlock : MonoBehaviour
{
    [Header("Flock Settings")]
    [SerializeField] private int   birdCount      = 5;
    [SerializeField] private float triggerRadius  = 3.5f;
    [SerializeField] private float flyHeight      = 8f;
#pragma warning disable 0414
    [SerializeField] private float flySpeed       = 6f;
#pragma warning restore 0414
    [SerializeField] private float resetDelay     = 8f;
    [SerializeField] private float noiseSoundRadius = 7f;

    private GameObject[] birds;
    private Vector3[]    groundPositions;
    private bool         hasFled    = false;
    private bool         resetting  = false;

    private void Start() => SpawnBirds();

    private void SpawnBirds()
    {
        birds           = new GameObject[birdCount];
        groundPositions = new Vector3[birdCount];

        for (int i = 0; i < birdCount; i++)
        {
            GameObject bird = new GameObject($"Bird_{i}");
            bird.transform.SetParent(transform);

            // Thân chim: hình cầu nhỏ
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(bird.transform);
            body.transform.localScale    = new Vector3(0.15f, 0.08f, 0.25f);
            body.transform.localPosition = Vector3.zero;
            body.GetComponent<Renderer>().material =
                MaterialLibrary.Solid(new Color(0.1f, 0.1f, 0.1f));
            Destroy(body.GetComponent<Collider>());

            // Cánh trái
            GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingL.transform.SetParent(bird.transform);
            wingL.transform.localScale    = new Vector3(0.2f, 0.03f, 0.08f);
            wingL.transform.localPosition = new Vector3(-0.15f, 0f, 0f);
            wingL.GetComponent<Renderer>().material =
                MaterialLibrary.Solid(new Color(0.15f, 0.1f, 0.08f));
            Destroy(wingL.GetComponent<Collider>());

            // Cánh phải
            GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingR.transform.SetParent(bird.transform);
            wingR.transform.localScale    = new Vector3(0.2f, 0.03f, 0.08f);
            wingR.transform.localPosition = new Vector3(0.15f, 0f, 0f);
            wingR.GetComponent<Renderer>().material =
                MaterialLibrary.Solid(new Color(0.15f, 0.1f, 0.08f));
            Destroy(wingR.GetComponent<Collider>());

            // Vị trí ngẫu nhiên xung quanh tâm
            Vector3 offset = new Vector3(
                Random.Range(-1.5f, 1.5f), 0.1f, Random.Range(-1.5f, 1.5f));
            bird.transform.localPosition = offset;
            groundPositions[i] = bird.transform.position;
            birds[i] = bird;
        }
    }

    private void Update()
    {
        if (hasFled || resetting) return;
        if (PlayerController.Instance == null) return;

        // Chỉ react khi player đang CHẠY (không react khi đi bộ)
        if (!PlayerController.Instance.IsRunning) return;

        float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        if (dist < triggerRadius)
            StartCoroutine(FlockFlee());
    }

    private IEnumerator FlockFlee()
    {
        hasFled = true;
        Debug.Log("[BirdFlock] Chim bay lên! Kim Đồng bị lộ vị trí.");

        // Phát tín hiệu âm thanh làm địch chú ý
        Collider[] hits = Physics.OverlapSphere(transform.position, noiseSoundRadius);
        foreach (Collider hit in hits)
            hit.GetComponent<EnemyAI>()?.OnSoundHeard(transform.position, noiseSoundRadius);

        // Animation chim bay
        float elapsed = 0f;
        float duration = 3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < birds.Length; i++)
            {
                if (birds[i] == null) continue;
                Vector3 scatter = new Vector3(
                    Mathf.Sin(elapsed * 2f + i * 1.3f) * 2f,
                    flyHeight * Mathf.Clamp01(t * 2f),
                    Mathf.Cos(elapsed * 2f + i * 0.7f) * 2f);
                birds[i].transform.position = Vector3.Lerp(
                    groundPositions[i],
                    groundPositions[i] + scatter,
                    t);

                // Vỗ cánh (xoay cánh)
                Transform wingL = birds[i].transform.GetChild(1);
                Transform wingR = birds[i].transform.GetChild(2);
                float flapAngle = Mathf.Sin(elapsed * 12f + i) * 30f;
                if (wingL) wingL.localRotation = Quaternion.Euler(flapAngle, 0, 0);
                if (wingR) wingR.localRotation = Quaternion.Euler(-flapAngle, 0, 0);
            }
            yield return null;
        }

        // Ẩn chim
        foreach (GameObject b in birds)
            if (b) b.SetActive(false);

        // Reset sau một thời gian
        yield return new WaitForSeconds(resetDelay);
        StartCoroutine(ResetFlock());
    }

    private IEnumerator ResetFlock()
    {
        resetting = true;
        foreach (GameObject b in birds)
        {
            if (b == null) continue;
            b.SetActive(true);
        }

        // Chim đáp xuống
        float elapsed = 0f;
        float duration = 2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < birds.Length; i++)
            {
                if (birds[i] == null) continue;
                birds[i].transform.position = Vector3.Lerp(
                    groundPositions[i] + Vector3.up * flyHeight,
                    groundPositions[i],
                    t);
            }
            yield return null;
        }

        hasFled   = false;
        resetting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, noiseSoundRadius);
    }
}
