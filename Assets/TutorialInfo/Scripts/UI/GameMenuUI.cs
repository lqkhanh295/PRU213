using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý UI cho Menu Tạm Dừng (Pause), Thua Cuộc (Game Over) và Qua Màn (Victory).
/// Luôn tự động gắn vào SceneBootstrapper.
/// </summary>
public class GameMenuUI : MonoBehaviour
{
    private GameObject pauseMenuObj;
    private GameObject endMenuObj;

    private void Start()
    {
        // Tự động thêm EventSystem nếu chưa có để đảm bảo UI Clickable
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
        }
        CreatePauseMenu();
        CreateEndMenu();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChange;
        }
    }

    private void Update()
    {
        // Phím tắt ESC để Pause bằng Input System MỚI
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            
            if (state == GameManager.GameState.Playing)
                GameManager.Instance.SetGameState(GameManager.GameState.Paused);
            else if (state == GameManager.GameState.Paused)
                GameManager.Instance.SetGameState(GameManager.GameState.Playing);
        }
    }

    private void HandleGameStateChange(GameManager.GameState state)
    {
        pauseMenuObj.SetActive(state == GameManager.GameState.Paused);
        endMenuObj.SetActive(state == GameManager.GameState.GameOver || state == GameManager.GameState.LevelComplete);

        if (state == GameManager.GameState.GameOver)
        {
            SetupEndMenu("KIM ĐỒNG ĐÃ BỊ BẮT!", Color.red, "CHƠI LẠI", OnRestartClicked);
        }
        else if (state == GameManager.GameState.LevelComplete)
        {
            int nextLvl = GameManager.Instance.currentLevel + 1;
            if (nextLvl > GameManager.Instance.maxLevel)
            {
                SetupEndMenu("CHÚC MỪNG!\nBẠN ĐÃ PHÁ ĐẢO TRÒ CHƠI!", Color.yellow, "CHƠI LẠI TỪ ĐẦU", OnRestartClicked);
            }
            else
            {
                SetupEndMenu($"HOÀN THÀNH MÀN {GameManager.Instance.currentLevel}!", Color.green, "TIẾP TỤC (MÀN KẾ TIẾP)", OnNextLevelClicked);
            }
        }
    }

    // ─── TẠO PAUSE MENU ──────────────────────────────────────────────────────────
    private void CreatePauseMenu()
    {
        pauseMenuObj = CreateCanvas("PauseMenuCanvas", 100);
        pauseMenuObj.SetActive(false);

        // Nền mờ đen
        CreateBackground(pauseMenuObj.transform, new Color(0, 0, 0, 0.7f));
        
        // Tiêu đề
        CreateText(pauseMenuObj.transform, "TẠM DỪNG", 60, Color.white, new Vector2(0, 200));

        // Nút
        CreateButton(pauseMenuObj.transform, "TIẾP TỤC", new Vector2(0, 50), () => GameManager.Instance.SetGameState(GameManager.GameState.Playing));
        CreateButton(pauseMenuObj.transform, "CHƠI LẠI", new Vector2(0, -50), OnRestartClicked);
        CreateButton(pauseMenuObj.transform, "THOÁT GAME", new Vector2(0, -150), OnQuitClicked);
    }

    // ─── TẠO END MENU ────────────────────────────────────────────────────────────
    private void CreateEndMenu()
    {
        endMenuObj = CreateCanvas("EndMenuCanvas", 150);
        endMenuObj.SetActive(false);

        CreateBackground(endMenuObj.transform, new Color(0, 0, 0, 0.85f));
        
        // Title text sẽ được gán động
        CreateText(endMenuObj.transform, "TITLE", 70, Color.white, new Vector2(0, 200)).gameObject.name = "TitleText";
        
        // Button hành động sẽ được gán động
        CreateButton(endMenuObj.transform, "ACTION", new Vector2(0, -50), null).gameObject.name = "ActionButton";
    }

    private void SetupEndMenu(string title, Color titleColor, string btnText, UnityEngine.Events.UnityAction btnAction)
    {
        Text t = endMenuObj.transform.Find("TitleText").GetComponent<Text>();
        t.text = title;
        t.color = titleColor;

        Transform btnTr = endMenuObj.transform.Find("ActionButton");
        btnTr.GetComponentInChildren<Text>().text = btnText;
        Button b = btnTr.GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(btnAction);
    }

    // ─── HÀNH ĐỘNG ───────────────────────────────────────────────────────────────
    private void OnRestartClicked()
    {
        GameManager.Instance?.RestartGame();
    }

    private void OnNextLevelClicked()
    {
        GameManager.Instance?.NextLevel();
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit Game...");
        Application.Quit();
    }

    // ─── HELPER ──────────────────────────────────────────────────────────────────
    private GameObject CreateCanvas(string name, int sortOrder)
    {
        GameObject cObj = new GameObject(name);
        Canvas canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        
        CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        cObj.AddComponent<GraphicRaycaster>();
        return cObj;
    }

    private void CreateBackground(Transform parent, Color color)
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(parent, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = color;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
    }

    private Text CreateText(Transform parent, string textStr, int fontSize, Color color, Vector2 pos)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        Text t = textObj.AddComponent<Text>();
        t.text = textStr;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(1400, 250);
        return t;
    }

    private Button CreateButton(Transform parent, string textStr, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button");
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.4f, 0.2f, 0.9f); 

        Button btn = btnObj.AddComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 80);

        CreateText(btnObj.transform, textStr, 30, Color.white, Vector2.zero).rectTransform.sizeDelta = new Vector2(400, 80);
        return btn;
    }
}
