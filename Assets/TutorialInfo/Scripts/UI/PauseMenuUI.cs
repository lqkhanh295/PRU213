using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Pause Menu - New Input System (Esc để pause).
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private GameObject panelRoot;
    private bool isPaused = false;

    private void Start() => BuildPauseMenu();

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Only allow pause/unpause when playing or paused
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState == GameManager.GameState.Playing ||
                GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            {
                TogglePause();
            }
        }
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

    private void BuildPauseMenu()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        panelRoot = CreateOverlay(canvasObj, new Color(0f, 0.05f, 0f, 0.7f));
        panelRoot.SetActive(false);

        Sprite cardSprite = LoadUISprite("UI/hud_panel_bg");

        // Centered Card
        GameObject pauseCard = new GameObject("PauseCard");
        pauseCard.transform.SetParent(panelRoot.transform, false);
        Image cardImg = pauseCard.AddComponent<Image>();
        if (cardSprite != null) { cardImg.sprite = cardSprite; cardImg.color = Color.white; }
        else cardImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform rt = cardImg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460, 380);

        CreateLabel(pauseCard, "TẠM DỪNG",                      120f, 34, Color.white);
        CreateLabel(pauseCard, "KIM ĐỒNG - BƯỚC CHÂN THẦM LẶNG", 75f, 15, new Color(1f, 0.95f, 0.6f));
        CreateButton(pauseCard, "Tiếp tục",    10f,    OnResume);
        CreateButton(pauseCard, "Chơi lại",   -55f,   OnRestart);
        CreateButton(pauseCard, "Thoát game", -120f,  OnQuit);
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        panelRoot?.SetActive(isPaused);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(isPaused
                ? GameManager.GameState.Paused
                : GameManager.GameState.Playing);
        }
        else
        {
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }

    private void OnResume()
    {
        isPaused = false;
        panelRoot?.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameManager.GameState.Playing);
        else
            Time.timeScale = 1f;
    }

    private void OnRestart()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit() { Application.Quit(); }

    private GameObject CreateOverlay(GameObject parent, Color color)
    {
        GameObject p = new GameObject("Panel");
        p.transform.SetParent(parent.transform, false);
        Image img = p.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return p;
    }

    private void CreateLabel(GameObject parent, string text, float y, int size, Color color)
    {
        GameObject obj = new GameObject("Lbl");
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
        rt.sizeDelta = new Vector2(600, 55);
    }

    private void CreateButton(GameObject parent, string label, float y, System.Action onClick)
    {
        GameObject btn = new GameObject("Btn");
        btn.transform.SetParent(parent.transform, false);
        Image bg = btn.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(240, 48);
        Button b = btn.AddComponent<Button>();
        b.targetGraphic = bg;
        b.onClick.AddListener(() => onClick?.Invoke());

        b.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = b.colors;
        colors.normalColor      = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        colors.highlightedColor = new Color(0.2f, 0.55f, 0.2f, 1f);
        colors.pressedColor     = new Color(0.12f, 0.4f, 0.12f, 1f);
        colors.selectedColor    = new Color(0.2f, 0.55f, 0.2f, 1f);
        colors.fadeDuration     = 0.12f;
        b.colors = colors;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btn.transform, false);
        Text t = txtObj.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = 20;
        t.color     = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
    }
}
