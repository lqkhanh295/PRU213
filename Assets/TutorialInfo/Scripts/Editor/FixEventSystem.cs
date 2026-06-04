using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class FixEventSystem
{
    [MenuItem("KimDong/Fix EventSystem Input Module")]
    public static void Fix()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            Debug.LogError("[FixEventSystem] No EventSystem found!");
            return;
        }

        // Remove StandaloneInputModule
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
            Debug.Log("[FixEventSystem] Removed StandaloneInputModule.");
        }

        // Add InputSystemUIInputModule if not present
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[FixEventSystem] Added InputSystemUIInputModule.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[FixEventSystem] Done!");
    }
}
