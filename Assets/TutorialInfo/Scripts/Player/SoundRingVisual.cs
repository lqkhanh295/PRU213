using UnityEngine;

/// <summary>
/// Vẽ vòng tròn âm thanh phát quang dưới chân Kim Đồng khi chạy.
/// Cải thiện UX theo tiêu chuẩn game premium.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SoundRingVisual : MonoBehaviour
{
    [SerializeField] private int segments = 48;
    [SerializeField] private float yOffset = 0.05f; // Sát mặt đất để tránh z-fighting

    private LineRenderer lineRenderer;
    private PlayerController player;
    private float pulsePhase = 0f;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        player = GetComponent<PlayerController>();

        // Thiết lập LineRenderer
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = 0.06f;
        lineRenderer.endWidth = 0.06f;
        
        // Thử lấy material từ MaterialLibrary, nếu không gán tạm material mặc định
        Material mat = MaterialLibrary.SoundRing();
        if (mat != null)
        {
            lineRenderer.material = mat;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0f, 0.9f, 1f, 0.4f);
            lineRenderer.endColor = new Color(0f, 0.9f, 1f, 0.4f);
        }
        
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (player == null || lineRenderer == null) return;

        float radius = player.CurrentSoundRadius;
        if (radius <= 0f)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        pulsePhase += Time.deltaTime * 5f;
        
        // Tạo hiệu ứng sóng lan tỏa dập dềnh nhẹ cho bán kính và độ dày
        float activeRadius = radius + Mathf.Sin(pulsePhase) * 0.15f;
        float alpha = 0.4f + Mathf.Sin(pulsePhase) * 0.15f;
        
        Color col = new Color(0f, 0.9f, 1f, alpha);
        lineRenderer.startColor = col;
        lineRenderer.endColor = col;

        DrawCircle(activeRadius);
    }

    private void DrawCircle(float radius)
    {
        Vector3 center = transform.position;
        center.y += yOffset - 0.85f; // Đưa xuống chân nhân vật (capsule height = 1.8f, pivot ở tâm y=0.9f, chân ở y = 0)

        for (int i = 0; i < segments; i++)
        {
            float angle = ((float)i / segments) * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            lineRenderer.SetPosition(i, new Vector3(center.x + x, center.y, center.z + z));
        }
    }
}
