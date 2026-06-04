using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý trạng thái toàn cục: game state, alert level, lives, respawn.
/// Singleton - DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Paused, GameOver, LevelComplete }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Global Alert")]
    [Range(0, 2)]
    public int globalAlertLevel = 0;

    [Header("Player")]
    [Header("Player")]
    public int totalLives = 3;

    [Header("Progression")]
    public int currentLevel = 1;
    public int maxLevel = 3;

    public GameState CurrentState => currentState;

    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<int>       OnAlertLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── State ────────────────────────────────────────────────────────────────
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);

        if (newState == GameState.Paused)
            Time.timeScale = 0f;
        else if (newState != GameState.GameOver && newState != GameState.LevelComplete)
            Time.timeScale = 1f;
    }

    // ─── Alert ────────────────────────────────────────────────────────────────
    public void SetGlobalAlertLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 0, 2);
        if (globalAlertLevel == clamped) return;
        globalAlertLevel = clamped;
        OnAlertLevelChanged?.Invoke(globalAlertLevel);

        if (globalAlertLevel == 2)   AudioManager.Instance?.PlayAlertMusic();
        else if (globalAlertLevel == 1) AudioManager.Instance?.PlaySuspenseMusic();
        else                            AudioManager.Instance?.PlayAmbientMusic();
    }

    public void RaiseAlertLevel() => SetGlobalAlertLevel(globalAlertLevel + 1);
    public void LowerAlertLevel() => SetGlobalAlertLevel(globalAlertLevel - 1);

    // ─── Player caught / Respawn ───────────────────────────────────────────────
    public void PlayerCaught()
    {
        totalLives--;
        SetGlobalAlertLevel(0);
        Debug.Log($"[GameManager] Bị bắt! Còn {totalLives} mạng.");

        if (totalLives <= 0)
        {
            SetGameState(GameState.GameOver);
        }
        else
        {
            Invoke(nameof(RespawnPlayer), 1.5f);
        }
    }

    private void RespawnPlayer()
    {
        if (PlayerController.Instance == null) return;

        Vector3 respawnPos = CheckpointSystem.Instance != null
            ? CheckpointSystem.Instance.GetRespawnPosition()
            : new Vector3(-12, 0.9f, -12);

        // Tái tạo player controller state
        Rigidbody rb = PlayerController.Instance.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;
        PlayerController.Instance.transform.position = respawnPos;

        // Reset enemy alert
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        foreach (EnemyAI enemy in allEnemies)
            enemy.TransitionTo(new PatrolState());

        SetGameState(GameState.Playing);
        Debug.Log("[GameManager] Kim Đồng hồi sinh!");
    }

    // ─── Level Complete ───────────────────────────────────────────────────────
    // ─── Level Complete ───────────────────────────────────────────────────────
    public void LevelComplete()
    {
        SetGameState(GameState.LevelComplete);
    }

    public void NextLevel()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
        }
        else
        {
            // Reset nếu đã phá đảo
            currentLevel = 1;
        }
        
        // Reset máu
        totalLives = 3;
        SetGlobalAlertLevel(0);
        SetGameState(GameState.Playing);

        // Load lại cảnh hiện tại (do game sinh map tự động)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void RestartGame()
    {
        currentLevel = 1;
        totalLives = 3;
        SetGlobalAlertLevel(0);
        SetGameState(GameState.Playing);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
