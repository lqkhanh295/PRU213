using System.Collections;
using UnityEngine;

/// <summary>
/// Thư tình báo - vật phẩm Kim Đồng cần nhặt để hoàn thành nhiệm vụ.
/// Xoay liên tục và phát sáng để dễ nhìn thấy. 
/// Khi nhặt: cập nhật mục tiêu, hiện thông báo.
/// </summary>
public class LetterPickup : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobSpeed    = 2f;
    [SerializeField] private float bobHeight   = 0.15f;

    [Header("Pickup")]
#pragma warning disable 0414
    [SerializeField] private float pickupRadius = 1.2f;
#pragma warning restore 0414

    private Vector3 startPos;
    private bool    collected = false;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        // Xoay
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Lơ lửng lên xuống
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        collected = true;
        Debug.Log("[Letter] Kim Đồng đã nhặt thư tình báo!");

        // Scale up rồi biến mất
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.4f;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 2.5f, t);
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend) rend.material.color = Color.Lerp(Color.white, Color.yellow, t);
            yield return null;
        }

        ObjectiveManager.Instance?.CompleteCurrentObjective();
        Destroy(gameObject);
    }
}
