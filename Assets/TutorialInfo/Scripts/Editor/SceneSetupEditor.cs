using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Editor utility to set up the Level_01 scene properly.
/// </summary>
public class SceneSetupEditor : EditorWindow
{
    [MenuItem("KimDong/Setup Scene")]
    public static void SetupScene()
    {
        // 1. Ensure required tags exist
        EnsureTag("Player");
        EnsureTag("Ground");
        EnsureTag("HidingZone");
        EnsureTag("WaterZone");
        EnsureTag("InteractPoint");

        // 2. Setup Main Camera with CameraController and post-processing
        SetupMainCamera();

        // 3. Add GameManager to scene
        SetupGameManager();

        // 4. Add AudioManager to scene
        SetupAudioManager();

        // 5. Add EventSystem
        SetupEventSystem();

        // 6. Configure SceneBootstrapper
        SetupBootstrapper();

        // 7. Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SceneSetup] Scene setup complete! Press Play to start the game.");
    }

    private static void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[SceneSetup] Added tag: {tag}");
    }

    private static void SetupMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        // Add CameraController if missing
        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();

        // Enable post-processing on camera
        var urpData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpData != null)
            urpData.renderPostProcessing = true;

        // Add PostProcessing Volume if missing
        if (Object.FindAnyObjectByType<PostProcessingSetup>() == null)
        {
            GameObject ppObj = new GameObject("PostProcessing_Volume");
            ppObj.AddComponent<PostProcessingSetup>();
            Debug.Log("[SceneSetup] Added PostProcessing Volume.");
        }

        Debug.Log("[SceneSetup] Main Camera configured.");
    }

    private static void SetupGameManager()
    {
        if (GameManager.Instance != null) return;
        if (Object.FindAnyObjectByType<GameManager>() != null) return;

        GameObject gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
        Debug.Log("[SceneSetup] GameManager added.");
    }

    private static void SetupAudioManager()
    {
        if (Object.FindAnyObjectByType<AudioManager>() != null) return;

        GameObject am = new GameObject("AudioManager");
        am.AddComponent<AudioSource>(); // music source
        am.AddComponent<AudioSource>(); // sfx source
        am.AddComponent<AudioManager>();
        Debug.Log("[SceneSetup] AudioManager added.");
    }

    private static void SetupEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
        Debug.Log("[SceneSetup] EventSystem added.");
    }

    private static void SetupBootstrapper()
    {
        SceneBootstrapper bootstrapper = Object.FindAnyObjectByType<SceneBootstrapper>();
        if (bootstrapper == null)
        {
            GameObject gb = new GameObject("GameBootstrapper");
            bootstrapper = gb.AddComponent<SceneBootstrapper>();
        }

        // Ensure GameOverUI is on the bootstrapper (handles Game Over and Level Complete screens)
        if (bootstrapper.GetComponent<GameOverUI>() == null)
            bootstrapper.gameObject.AddComponent<GameOverUI>();

        // Set buildOnStart = true so the scene builds when Play is pressed
        var so = new SerializedObject(bootstrapper);
        var prop = so.FindProperty("buildOnStart");
        if (prop != null)
        {
            prop.boolValue = true;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[SceneSetup] SceneBootstrapper configured with buildOnStart=true.");
    }
}
