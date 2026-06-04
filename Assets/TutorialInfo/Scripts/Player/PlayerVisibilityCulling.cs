using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống sương mù/chống nhìn xuyên tường (Line of Sight Culling).
/// Ẩn toàn bộ hình ảnh (Renderers) và UI (Canvases) của Kẻ địch/NPC 
/// nếu chúng bị khuất sau vật cản hoặc nằm ngoài bán kính nhìn thấy.
/// </summary>
public class PlayerVisibilityCulling : MonoBehaviour
{
    [Header("Tầm nhìn")]
    [SerializeField] private float maxViewDistance = 40f; 
    [SerializeField] private LayerMask obstacleMask; // Những vật cản tầm nhìn
    [SerializeField] private float eyeHeight = 1.6f;

    private List<GameObject> cullingTargets = new List<GameObject>();
    private float checkInterval = 0.1f; // Kiểm tra mỗi 0.1s thay vì mỗi frame để tối ưu
    private float timer = 0f;

    private void Start()
    {
        obstacleMask = LayerMask.GetMask("Default");
        // Delay refresh to allow all enemies to be spawned first
        Invoke(nameof(RefreshTargets), 1f);
    }

    /// <summary>
    /// Tìm tất cả các mục tiêu cần ẩn hiện trong màn chơi.
    /// </summary>
    public void RefreshTargets()
    {
        cullingTargets.Clear();
        // Tìm kẻ địch
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e.gameObject != this.gameObject) cullingTargets.Add(e.gameObject);
        }

        // Tìm NPC (Cán bộ)
        EscortTarget[] npcs = FindObjectsByType<EscortTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var n in npcs)
        {
            if (n.gameObject != this.gameObject) cullingTargets.Add(n.gameObject);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckVisibility();
        }
    }

    private void CheckVisibility()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        foreach (var target in cullingTargets)
        {
            if (target == null) continue;

            bool isVisible = false;
            Vector3 targetCenter = target.transform.position + Vector3.up * 1f; // Ngắm vào ngực địch
            float distance = Vector3.Distance(eyePos, targetCenter);

            if (distance <= maxViewDistance)
            {
                // Chỉ dùng khoảng cách, bỏ chức năng tia nhìn (Raycast) xuyên tường
                // để người chơi Góc nhìn thứ 3 (TPS) có thể xoay camera nhìn lén qua góc nhà.
                isVisible = true; 
            }

            SetTargetVisibility(target, isVisible);
        }
    }

    private void SetTargetVisibility(GameObject target, bool isVisible)
    {
        // Ẩn/hiện toàn bộ Renderer (Mesh)
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Tránh tắt EnemyVisionCone (mesh vẽ vùng nhìn dưới mặt đất) nếu ta muốn thấy tầm nhìn của chúng.
            // Nhưng tốt nhất là nếu địch ẩn thì tầm nhìn của chúng cũng ẩn.
            if (r.enabled != isVisible) r.enabled = isVisible;
        }

        // Ẩn/hiện UI Canvas (Vạch phát hiện)
        Canvas[] canvases = target.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.enabled != isVisible) c.enabled = isVisible;
        }
    }
}
