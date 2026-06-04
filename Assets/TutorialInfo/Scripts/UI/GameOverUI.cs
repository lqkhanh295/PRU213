using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình Game Over và Level Complete.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private GameObject gameOverPanel;
    private GameObject levelCompletePanel;

    private void Start()
    {
        BuildUI();
        // Subscribe immediately if GameManager exists, otherwise wait
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        else
            StartCoroutine(WaitForGameManager());
    }

    private System.Collections.IEnumerator WaitForGameManager()
    {
        while (GameManager.Instance == null)
            yield return null;
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        gameOverPanel?.SetActive(state == GameManager.GameState.GameOver);
        levelCompletePanel?.SetActive(state == GameManager.GameState.LevelComplete);
        if (state == GameManager.GameState.GameOver || state == GameManager.GameState.LevelComplete)
            Time.timeScale = 0f;
    }

    private Sprite LoadUISprite(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("GameOverCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Sprite cardSprite = LoadUISprite("UI/hud_panel_bg");

        // ── Game Over ─────────────────────────────────────────────────────────
        gameOverPanel = CreateOverlayPanel(canvasObj, new Color(0.12f, 0.02f, 0.02f, 0.85f));
        gameOverPanel.SetActive(false);
        
        GameObject goCard = new GameObject("GameOverCard");
        goCard.transform.SetParent(gameOverPanel.transform, false);
        Image goCardImg = goCard.AddComponent<Image>();
        if (cardSprite != null) { goCardImg.sprite = cardSprite; goCardImg.color = Color.white; }
        else goCardImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform goRt = goCardImg.rectTransform;
        goRt.anchorMin = new Vector2(0.5f, 0.5f);
        goRt.anchorMax = new Vector2(0.5f, 0.5f);
        goRt.pivot = new Vector2(0.5f, 0.5f);
        goRt.sizeDelta = new Vector2(500, 420);

        CreateCenteredLabel(goCard, "BỊ BẮT!", 120f, 48, Color.red);
        CreateCenteredLabel(goCard, "Kim Đồng đã bị địch phát hiện.", 40f, 20, Color.white);
        CreateCenteredButton(goCard, "Chơi lại", -60f, () => { Time.timeScale = 1f; SceneManager.LoadScene(0); });
        CreateCenteredButton(goCard, "Thoát game", -130f, () => Application.Quit());

        // ── Level Complete ────────────────────────────────────────────────────
        levelCompletePanel = CreateOverlayPanel(canvasObj, new Color(0.02f, 0.12f, 0.02f, 0.85f));
        levelCompletePanel.SetActive(false);
        
        GameObject lcCard = new GameObject("LevelCompleteCard");
        lcCard.transform.SetParent(levelCompletePanel.transform, false);
        Image lcCardImg = lcCard.AddComponent<Image>();
        if (cardSprite != null) { lcCardImg.sprite = cardSprite; lcCardImg.color = Color.white; }
        else lcCardImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform lcRt = lcCardImg.rectTransform;
        lcRt.anchorMin = new Vector2(0.5f, 0.5f);
        lcRt.anchorMax = new Vector2(0.5f, 0.5f);
        lcRt.pivot = new Vector2(0.5f, 0.5f);
        lcRt.sizeDelta = new Vector2(520, 460);

        CreateCenteredLabel(lcCard, "NHIỆM VỤ HOÀN THÀNH!", 140f, 32, new Color(1f, 0.95f, 0.3f));
        CreateCenteredLabel(lcCard, "Kim Đồng đã hoàn thành sứ mệnh.", 70f, 18, Color.white);
        CreateCenteredLabel(lcCard, "\"Măng non chí thép\"", 20f, 16, new Color(1f, 0.8f, 0.4f));
        CreateCenteredButton(lcCard, "Màn tiếp theo", -60f, () => { Time.timeScale = 1f; GameManager.Instance?.NextLevel(); });
        CreateCenteredButton(lcCard, "Chơi lại",      -130f, () => { Time.timeScale = 1f; GameManager.Instance?.RestartGame(); });
    }

    private GameObject CreateOverlayPanel(GameObject parent, Color color)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent.transform, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return panel;
    }

    private void CreateCenteredLabel(GameObject parent, string text, float y, int size, Color color)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent.transform, false);
        Text t = obj.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(700, 60);
    }

    private void CreateCenteredButton(GameObject parent, string label, float y,
                                       System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn");
        btnObj.transform.SetParent(parent.transform, false);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(260, 52);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor      = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        colors.highlightedColor = new Color(0.2f, 0.55f, 0.2f, 1f);
        colors.pressedColor     = new Color(0.12f, 0.4f, 0.12f, 1f);
        colors.selectedColor    = new Color(0.2f, 0.55f, 0.2f, 1f);
        colors.fadeDuration     = 0.12f;
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = 22;
        t.color     = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
    }
}
