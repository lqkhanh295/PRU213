using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detection Gauge 3D - World Space Canvas trên đầu địch.
/// Billboard: luôn quay về phía camera.
/// Normal → ẩn | Suspicious → vàng | Detected → đỏ
/// </summary>
public enum DetectionLevel { Normal, Suspicious, Detected }

public class DetectionGaugeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gaugeRoot;
    [SerializeField] private Image      fillImage;
    [SerializeField] private Image      iconImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor     = Color.green;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color detectedColor   = Color.red;

    [Header("Icons")]
    [SerializeField] private Sprite questionMarkSprite;
    [SerializeField] private Sprite exclamationSprite;

    [Header("Billboard")]
    [SerializeField] private float heightOffset = 2.5f; // Cao hơn đầu địch

    private DetectionLevel currentLevel = DetectionLevel.Normal;
    private Transform      camTransform;

    private void Start()
    {
        camTransform = Camera.main.transform;
        // Đưa canvas lên đúng độ cao trên đầu nhân vật
        transform.localPosition = new Vector3(0f, heightOffset, 0f);
        SetLevel(DetectionLevel.Normal);
    }

    private void LateUpdate()
    {
        // Billboard: quay mặt về camera (chỉ trục Y để tránh nghiêng)
        if (camTransform == null) return;
        Vector3 dir = transform.position - camTransform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetLevel(DetectionLevel level)
    {
        currentLevel = level;
        switch (level)
        {
            case DetectionLevel.Normal:
                gaugeRoot?.SetActive(false);
                break;

            case DetectionLevel.Suspicious:
                gaugeRoot?.SetActive(true);
                if (fillImage) fillImage.color = suspiciousColor;
                if (iconImage && questionMarkSprite) iconImage.sprite = questionMarkSprite;
                break;

            case DetectionLevel.Detected:
                gaugeRoot?.SetActive(true);
                if (fillImage) { fillImage.color = detectedColor; fillImage.fillAmount = 1f; }
                if (iconImage && exclamationSprite) iconImage.sprite = exclamationSprite;
                break;
        }
    }

    public void SetProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        if (clamped > 0f && currentLevel == DetectionLevel.Normal)
            SetLevel(DetectionLevel.Suspicious);
        if (clamped <= 0f && currentLevel == DetectionLevel.Suspicious)
            SetLevel(DetectionLevel.Normal);

        gaugeRoot?.SetActive(clamped > 0f);
        if (fillImage) fillImage.fillAmount = clamped;
    }
}
