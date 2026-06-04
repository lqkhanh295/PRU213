using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tự động cài đặt Post-Processing (URP) lúc runtime.
/// Nâng cấp Đồ họa phong cách Cinematic (Chân thực điện ảnh).
/// </summary>
[RequireComponent(typeof(Volume))]
public class PostProcessingSetup : MonoBehaviour
{
    [Header("Bloom - Ánh sáng phát quang")]
    [SerializeField] private float bloomIntensity  = 0.4f;
    [SerializeField] private float bloomThreshold  = 1.1f;
    [SerializeField] private float bloomScatter    = 0.7f;

    [Header("Color Grading - Cinematic")]
    [SerializeField] private float saturation      = 8f;    
    [SerializeField] private float contrast        = 10f;
    [SerializeField] private Color colorFilter     = new Color(0.98f, 1f, 0.95f); // Slight green tint for jungle

    [Header("Vignette - Viền tối")]
    [SerializeField] private float vignetteIntensity = 0.2f;  // Much lighter vignette
    [SerializeField] private float vignetteSmoothness= 0.4f;

    [Header("Shadows/Midtones/Highlights")]
    [SerializeField] private Color shadowColor    = new Color(0.08f, 0.10f, 0.08f); // Slightly green shadows
    [SerializeField] private Color highlightColor = new Color(1f,    0.98f, 0.92f); // Warm highlights

    [Header("Depth of Field (Cinematic Blur)")]
    [SerializeField] private float focusDistance = 6f;
    [SerializeField] private float focalLength   = 35f;
    [SerializeField] private float aperture      = 8f;   // Less blur

    private Volume volume;

    private void Awake()
    {
        volume                 = GetComponent<Volume>();
        volume.isGlobal        = true;
        volume.priority        = 10;
        volume.profile         = ScriptableObject.CreateInstance<VolumeProfile>();

        ApplyBloom();
        ApplyColorAdjustments();
        ApplyVignette();
        ApplyShadowsMidtonesHighlights();
        ApplyFilmGrain();
        ApplyDepthOfField();
    }

    private void ApplyBloom()
    {
        if (!volume.profile.TryGet<Bloom>(out var bloom))
            bloom = volume.profile.Add<Bloom>(true);

        bloom.active              = true;
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        bloom.highQualityFiltering.Override(true);
    }

    private void ApplyColorAdjustments()
    {
        if (!volume.profile.TryGet<ColorAdjustments>(out var ca))
            ca = volume.profile.Add<ColorAdjustments>(true);

        ca.active = true;
        ca.postExposure.Override(0.8f); // Brighter exposure
        ca.contrast.Override(contrast);
        ca.colorFilter.Override(colorFilter);
        ca.saturation.Override(saturation);
    }

    private void ApplyVignette()
    {
        if (!volume.profile.TryGet<Vignette>(out var vig))
            vig = volume.profile.Add<Vignette>(true);

        vig.active            = true;
        vig.intensity.Override(vignetteIntensity);
        vig.smoothness.Override(vignetteSmoothness);
        vig.rounded.Override(true);
    }

    private void ApplyShadowsMidtonesHighlights()
    {
        if (!volume.profile.TryGet<ShadowsMidtonesHighlights>(out var smh))
            smh = volume.profile.Add<ShadowsMidtonesHighlights>(true);

        smh.active = false; // Disable to avoid darkening the scene
        smh.shadows.Override(new Vector4(shadowColor.r, shadowColor.g, shadowColor.b, 0f));
        smh.highlights.Override(new Vector4(highlightColor.r, highlightColor.g, highlightColor.b, 0f));
    }

    private void ApplyFilmGrain()
    {
        if (!volume.profile.TryGet<FilmGrain>(out var grain))
            grain = volume.profile.Add<FilmGrain>(true);

        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.06f);   // Subtle grain
        grain.response.Override(0.6f);
    }

    private void ApplyDepthOfField()
    {
        if (!volume.profile.TryGet<DepthOfField>(out var dof))
            dof = volume.profile.Add<DepthOfField>(true);

        // Disable DoF by default - it can make the scene look blurry/dark
        dof.active = false;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(focusDistance);
        dof.focalLength.Override(focalLength);
        dof.aperture.Override(aperture);
    }

    /// <summary>Gọi khi bị phát hiện - tăng vignette đỏ.</summary>
    public void SetAlertEffect(bool alert)
    {
        if (!volume.profile.TryGet<Vignette>(out var vig)) return;
        if (alert)
        {
            vig.color.Override(new Color(0.8f, 0.1f, 0.1f));
            vig.intensity.Override(0.5f);
        }
        else
        {
            vig.color.Override(Color.black);
            vig.intensity.Override(vignetteIntensity);
        }
    }
}
