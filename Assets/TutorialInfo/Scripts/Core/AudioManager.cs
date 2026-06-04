using UnityEngine;

/// <summary>
/// Quản lý âm thanh toàn cục: nhạc nền, hiệu ứng âm thanh.
/// Chuyển nhạc theo trạng thái cảnh giới.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip ambientClip;   // Tiếng suối, côn trùng
    [SerializeField] private AudioClip suspenseClip;  // Nhạc khi bị nghi ngờ
    [SerializeField] private AudioClip alertClip;     // Nhạc dồn dập khi bị truy đuổi

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip footstepGrass;
    [SerializeField] private AudioClip footstepWater;
    [SerializeField] private AudioClip whistleClip;      // Huýt sáo tiếng chim
    [SerializeField] private AudioClip rockThrowClip;    // Ném sỏi
    [SerializeField] private AudioClip disguiseClip;     // Giả vờ chăn trâu/thổi sáo

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-assign AudioSources if not set in Inspector
        AudioSource[] sources = GetComponents<AudioSource>();
        if (musicSource == null && sources.Length > 0) musicSource = sources[0];
        if (sfxSource == null && sources.Length > 1) sfxSource = sources[1];

        // Create AudioSources if still missing
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = 0.5f;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 0.8f;
        }
    }

    private void Start()
    {
        PlayAmbientMusic();
    }

    // ─── Music ────────────────────────────────────────────────────────────────
    public void PlayAmbientMusic()  => SwapMusic(ambientClip);
    public void PlaySuspenseMusic() => SwapMusic(suspenseClip);
    public void PlayAlertMusic()    => SwapMusic(alertClip);

    private void SwapMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ─── SFX ──────────────────────────────────────────────────────────────────
    public void PlayFootstep(bool isInWater)
        => PlaySFX(isInWater ? footstepWater : footstepGrass);

    public void PlayWhistle()    => PlaySFX(whistleClip);
    public void PlayRockThrow()  => PlaySFX(rockThrowClip);
    public void PlayDisguise()   => PlaySFX(disguiseClip);

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
