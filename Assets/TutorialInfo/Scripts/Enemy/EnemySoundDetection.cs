using UnityEngine;

/// <summary>
/// Phát hiện âm thanh 3D của địch.
/// Lắng nghe tiếng chạy của player, ném sỏi, thả động vật.
/// Player đi qua suối → xóa dấu vết, chó săn không theo được.
/// </summary>
public class EnemySoundDetection : MonoBehaviour
{
    [Header("Hearing")]
    [SerializeField] private float hearingRange = 6f;

    private bool    soundHeard      = false;
    private Vector3 lastSoundPos;
    private float   soundClearTimer = 0f;
    private const float SOUND_MEMORY = 3f;

    public bool    HasHeardSound()   => soundHeard;
    public Vector3 LastSoundPosition => lastSoundPos;

    private void Update()
    {
        if (PlayerController.Instance != null)
        {
            float soundRadius = PlayerController.Instance.CurrentSoundRadius;
            if (soundRadius > 0f)
            {
                float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
                if (dist <= soundRadius && !PlayerController.Instance.IsInWater)
                    HearSound(PlayerController.Instance.transform.position);
            }
        }

        if (soundHeard)
        {
            soundClearTimer -= Time.deltaTime;
            if (soundClearTimer <= 0f) soundHeard = false;
        }
    }

    public void HearSound(Vector3 origin)
    {
        float dist = Vector3.Distance(transform.position, origin);
        if (dist <= hearingRange)
        {
            soundHeard      = true;
            lastSoundPos    = origin;
            soundClearTimer = SOUND_MEMORY;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
