using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD tối giản theo GDD - Screen Space Overlay.
/// Hiển thị: mạng sống, mục tiêu hiện tại, ammo (sỏi/động vật), trạng thái ẩn nấp.
/// Không có minimap.
/// </summary>
public class GameHUD : MonoBehaviour
{
    private Canvas     canvas;
    private Text       objectiveText;
    private Image[]    heartImages;
    private Sprite     heartSprite;
    private Text       rockQtyText;
    private Text       fluteQtyText;
    private Text       animalQtyText;
    private Text       stealthText;
    private Text       alertText;
    private Image      alertBorder;      // Viền đỏ khi bị phát hiện

    private DistractionSystem distraction;

    private void Awake() => BuildHUD();

    private void Start()
    {
        distraction = FindAnyObjectByType<DistractionSystem>();
        // If not found yet, will be found in Update
    }

    private void Update()
    {
        RefreshObjective();
        RefreshLives();
        RefreshAmmo();
        RefreshStealth();
        RefreshAlert();
    }

    // Helper loading texture as sprite
    private Sprite LoadUISprite(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    // ─── Build procedural UI ──────────────────────────────────────────────────
    private void BuildHUD()
    {
        GameObject canvasObj = new GameObject("HUD_Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Sprite bgSprite = LoadUISprite("UI/hud_panel_bg");
        Sprite rockSprite = LoadUISprite("UI/rock_icon");
        Sprite fluteSprite = LoadUISprite("UI/flute_icon");
        Sprite animalSprite = LoadUISprite("UI/animal_icon");
        heartSprite = LoadUISprite("UI/heart_icon");

        // ── Panel trái trên: Mục tiêu ─────────────────────────────────────────
        GameObject objPanel = CreatePanel(canvasObj, "ObjectivePanel",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(420, 80));
        Image objImg = objPanel.GetComponent<Image>();
        if (bgSprite != null) { objImg.sprite = bgSprite; objImg.color = Color.white; }
        else objImg.color = new Color(0, 0, 0, 0.55f);

        objectiveText = CreateText(objPanel, "ObjectiveText",
            "Mục tiêu: Nhặt thư tình báo",
            new Vector2(0, 0), new Vector2(1, 1), 18,
            new Color(1f, 0.95f, 0.6f));
        SetRectPadding(objectiveText.rectTransform, 12, 8);

        // ── Panel trái dưới: Trạng thái ẩn nấp ───────────────────────────────
        stealthText = CreateText(canvasObj, "StealthText",
            "", new Vector2(0, 1), new Vector2(0, 1), 16, Color.cyan);
        SetAnchoredPos(stealthText.rectTransform, new Vector2(14, -100));

        // ── Panel phải trên: Mạng sống ────────────────────────────────────────
        GameObject livesPanel = CreatePanel(canvasObj, "LivesPanel",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -10), new Vector2(180, 60));
        Image livesImg = livesPanel.GetComponent<Image>();
        if (bgSprite != null) { livesImg.sprite = bgSprite; livesImg.color = Color.white; }
        else livesImg.color = new Color(0, 0, 0, 0.55f);

        // Tạo 3 quả tim hình ảnh
        heartImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(livesPanel.transform, false);
            Image img = heartObj.AddComponent<Image>();
            img.sprite = heartSprite;
            img.color = new Color(1f, 0.2f, 0.2f, 1f);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-50f + i * 50f, 0f);
            rt.sizeDelta = new Vector2(36, 36);
            heartImages[i] = img;
        }

        // ── Panel phải dưới: Ammo ─────────────────────────────────────────────
        GameObject ammoPanel = CreatePanel(canvasObj, "AmmoPanel",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -80), new Vector2(220, 90));
        Image ammoImg = ammoPanel.GetComponent<Image>();
        if (bgSprite != null) { ammoImg.sprite = bgSprite; ammoImg.color = Color.white; }
        else ammoImg.color = new Color(0, 0, 0, 0.45f);

        // Tạo 3 dòng Ammo
        CreateAmmoRow(ammoPanel, rockSprite, "[E]", "5", new Vector2(10, 22), out rockQtyText);
        CreateAmmoRow(ammoPanel, fluteSprite, "[Q]", "Sáo", new Vector2(10, -3), out fluteQtyText);
        CreateAmmoRow(ammoPanel, animalSprite, "[R]", "2", new Vector2(10, -28), out animalQtyText);

        // ── Alert level text (trung tâm trên) ────────────────────────────────
        alertText = CreateText(canvasObj, "AlertText",
            "", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 24, Color.red);
        SetAnchoredPos(alertText.rectTransform, new Vector2(0, -20));

        // ── Viền đỏ khi bị phát hiện ─────────────────────────────────────────
        GameObject borderObj = new GameObject("AlertBorder");
        borderObj.transform.SetParent(canvasObj.transform, false);
        alertBorder = borderObj.AddComponent<Image>();
        alertBorder.color = new Color(1f, 0f, 0f, 0f);
        RectTransform brt = alertBorder.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.sizeDelta = Vector2.zero;
        alertBorder.type = Image.Type.Simple;

        // ── Controls hint (góc dưới trái) ─────────────────────────────────────
        Text hint = CreateText(canvasObj, "ControlHint",
            "[WASD] Di chuyển  [Shift] Chạy  [F] Giả vờ  [Q] Sáo  [E] Sỏi  [R] Thú  [Esc] Pause",
            new Vector2(0, 0), new Vector2(0, 0), 13, new Color(1, 1, 1, 0.5f));
        SetAnchoredPos(hint.rectTransform, new Vector2(10, 10));
        hint.rectTransform.sizeDelta = new Vector2(900, 24);
    }

    private void CreateAmmoRow(GameObject parent, Sprite icon, string keyHint, string defaultVal, Vector2 pos, out Text outText)
    {
        GameObject row = new GameObject("AmmoRow");
        row.transform.SetParent(parent.transform, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(0, 24);

        Text hintText = CreateText(row, "KeyHint", keyHint, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 13, new Color(0.8f, 0.8f, 0.8f));
        hintText.rectTransform.anchoredPosition = new Vector2(10, 0);
        hintText.rectTransform.sizeDelta = new Vector2(30, 20);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(row.transform, false);
        Image img = iconObj.AddComponent<Image>();
        img.sprite = icon;
        img.color = Color.white;
        RectTransform iconRt = img.rectTransform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(40, 0);
        iconRt.sizeDelta = new Vector2(20, 20);

        outText = CreateText(row, "ValueText", "x" + defaultVal, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 13, Color.white);
        outText.rectTransform.anchoredPosition = new Vector2(70, 0);
        outText.rectTransform.sizeDelta = new Vector2(120, 20);
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────
    private void RefreshObjective()
    {
        if (objectiveText == null || ObjectiveManager.Instance == null) return;
        var obj = ObjectiveManager.Instance.CurrentObjective;
        objectiveText.text = obj != null
            ? $"Mục tiêu: {obj.description}"
            : "Mục tiêu: Hoàn thành!";
    }

    private void RefreshLives()
    {
        if (heartImages == null || GameManager.Instance == null) return;
        int lives = GameManager.Instance.totalLives;
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].gameObject.SetActive(i < lives);
        }
    }

    private void RefreshAmmo()
    {
        if (distraction == null)
        {
            distraction = FindAnyObjectByType<DistractionSystem>();
            return;
        }
        var (rocks, animals) = distraction.GetAmmo();
        if (rockQtyText != null) rockQtyText.text = "x" + rocks;
        if (animalQtyText != null) animalQtyText.text = "x" + animals;
    }

    private void RefreshStealth()
    {
        if (stealthText == null || PlayerController.Instance == null) return;
        var player = PlayerController.Instance;

        if (player.IsHidden)
            stealthText.text = "[ AN TOÀN - Đang ẩn ]";
        else if (player.IsDisguised)
            stealthText.text = "[ Đang giả vờ... ]";
        else if (player.IsInWater)
            stealthText.text = "[ Đang qua suối ]";
        else
            stealthText.text = "";
    }

    private float alertPulse = 0f;
    private void RefreshAlert()
    {
        if (GameManager.Instance == null) return;
        int level = GameManager.Instance.globalAlertLevel;

        if (level == 0) { alertText.text = ""; alertBorder.color = new Color(1, 0, 0, 0); return; }
        if (level == 1) { alertText.text = "⚠ Nghi ngờ"; alertText.color = Color.yellow; }
        if (level == 2)
        {
            alertPulse += Time.deltaTime * 3f;
            alertText.text  = "! BÁO ĐỘNG !";
            alertText.color = Color.red;
            alertBorder.color = new Color(1f, 0f, 0f, Mathf.Abs(Mathf.Sin(alertPulse)) * 0.4f);
        }
    }

    // ─── UI Builder Helpers ───────────────────────────────────────────────────
    private GameObject CreatePanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);
        Image img = panel.AddComponent<Image>();
        RectTransform rt = img.rectTransform;
        rt.anchorMin     = anchorMin;
        rt.anchorMax     = anchorMax;
        rt.pivot         = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta     = size;
        return panel;
    }

    private Text CreateText(GameObject parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        Text txt = obj.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleLeft;
        RectTransform rt = txt.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        return txt;
    }

    private void SetRectPadding(RectTransform rt, float left, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, top);
        rt.offsetMax = new Vector2(-left, -top);
    }

    private void SetAnchoredPos(RectTransform rt, Vector2 pos)
    {
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 30);
    }
}
