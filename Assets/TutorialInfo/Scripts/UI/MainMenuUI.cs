using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Giao diện Main Menu cho Kim Đồng - Bước Chân Thầm Lặng.
/// Tự động tạo Canvas lúc runtime.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private GameObject menuCanvasObj;

    private void Start()
    {
        CreateMainMenu();
    }

    private void CreateMainMenu()
    {
        // Tạo Canvas Overlay
        menuCanvasObj = new GameObject("MainMenuCanvas");
        Canvas canvas = menuCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = menuCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        menuCanvasObj.AddComponent<GraphicRaycaster>();

        // Thêm EventSystem nếu chưa có để nút bấm hoạt động
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Ảnh Nền (Background)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(menuCanvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Cố gắng load ảnh nền Cinematic nếu có
        Texture2D bgTex = Resources.Load<Texture2D>("Textures/mainmenu_bg");
        if (bgTex != null)
        {
            bgImg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), Vector2.zero);
        }
        else
        {
            bgImg.color = new Color(0.1f, 0.2f, 0.1f, 1f); // Màu xanh lục tối dự phòng
        }

        // Tạo Title Text
        CreateText(menuCanvasObj.transform, "Title", "KIM ĐỒNG\n<size=50>BƯỚC CHÂN THẦM LẶNG</size>", 
            new Vector2(0, 300), 100, Color.yellow, TextAnchor.MiddleCenter);

        // Nút Start
        CreateButton(menuCanvasObj.transform, "StartButton", "BẮT ĐẦU", new Vector2(0, -50), OnPlayClicked);

        // Nút Quit
        CreateButton(menuCanvasObj.transform, "QuitButton", "THOÁT GAME", new Vector2(0, -170), OnQuitClicked);
    }

    private void CreateText(Transform parent, string name, string text, Vector2 pos, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text t = textObj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = alignment;
        t.fontStyle = FontStyle.Bold;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(1200, 200);
    }

    private void CreateButton(Transform parent, string name, string textStr, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.4f, 0.2f, 0.9f); // Nút màu xanh rêu

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(350, 80);

        // Chữ trong nút
        CreateText(btnObj.transform, "Text", textStr, Vector2.zero, 35, Color.white, TextAnchor.MiddleCenter);
    }

    private void OnPlayClicked()
    {
        Destroy(menuCanvasObj);
        
        SceneBootstrapper bootstrapper = Object.FindAnyObjectByType<SceneBootstrapper>();
        if (bootstrapper != null)
        {
            bootstrapper.StartCoroutine(bootstrapper.BuildScene());
        }
        else
        {
            Debug.LogError("[MainMenu] Không tìm thấy SceneBootstrapper trong Scene!");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }
}
