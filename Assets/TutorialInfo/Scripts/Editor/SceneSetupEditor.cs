using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor utility to set up the Level_01 scene and build the game properly.
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

    [MenuItem("KimDong/Build Game")]
    public static void BuildGame()
    {
        string buildPath = "Builds/Windows/KimDong.exe";
        string[] scenes = { "Assets/Level_01.unity" };

        Debug.Log("[Build] Starting standalone Windows build...");

        // Ensure directory exists
        string directory = Path.GetDirectoryName(buildPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Build] Build succeeded! Size: " + summary.totalSize + " bytes");
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("[Build] Build failed.");
        }
    }

    [MenuItem("KimDong/List Model Animations")]
    public static void ListAnimations()
    {
        string[] models = {
            "Assets/Models/Characters/KimDong.glb",
            "Assets/Models/Characters/FrenchSoldier.glb",
            "Assets/Models/Characters/CanBo.glb"
        };
        foreach (var path in models)
        {
            Debug.Log($"--- Animations in {path} ---");
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip)
                {
                    Debug.Log($"Clip name: {asset.name}");
                }
            }
        }
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

        // 1. Find GLB Assets
        GameObject playerModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/KimDong.glb");
        GameObject enemyModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/FrenchSoldier.glb");
        GameObject escortModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/CanBo.glb");

        GameObject stiltHouseModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/StiltHouse.glb");
        GameObject watchtowerModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Watchtower.glb");
        GameObject mountainModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Mountain.glb");
        GameObject bushModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/JungleBush.glb");
        GameObject treeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/JungleTree.glb");
        GameObject bambooModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/BambooGrove.glb");
        GameObject fenceModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/FencePost.glb");

        GameObject lanternModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/Lantern.glb");
        GameObject letterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/Letter.glb");
        GameObject flagModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/RevFlag.glb");

        // 2. Generate Animator Controllers programmatically
        Object[] playerAssets = playerModel != null ? AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/KimDong.glb") : new Object[0];
        Object[] enemyAssets = enemyModel != null ? AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/FrenchSoldier.glb") : new Object[0];
        Object[] escortAssets = escortModel != null ? AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/CanBo.glb") : new Object[0];

        AnimatorController playerCtrl = playerModel != null ? CreateKimDongController(playerAssets) : null;
        AnimatorController enemyCtrl = enemyModel != null ? CreateEnemyController(enemyAssets) : null;
        AnimatorController escortCtrl = escortModel != null ? CreateEscortController(escortAssets) : null;

        // 3. Assign properties to SceneBootstrapper
        var so = new SerializedObject(bootstrapper);
        so.FindProperty("buildOnStart").boolValue = true;

        so.FindProperty("playerModel").objectReferenceValue = playerModel;
        so.FindProperty("enemyModel").objectReferenceValue = enemyModel;
        so.FindProperty("escortModel").objectReferenceValue = escortModel;

        so.FindProperty("stiltHouseModel").objectReferenceValue = stiltHouseModel;
        so.FindProperty("watchtowerModel").objectReferenceValue = watchtowerModel;
        so.FindProperty("mountainModel").objectReferenceValue = mountainModel;
        so.FindProperty("bushModel").objectReferenceValue = bushModel;
        so.FindProperty("treeModel").objectReferenceValue = treeModel;
        so.FindProperty("bambooModel").objectReferenceValue = bambooModel;
        so.FindProperty("fenceModel").objectReferenceValue = fenceModel;

        so.FindProperty("lanternModel").objectReferenceValue = lanternModel;
        so.FindProperty("letterModel").objectReferenceValue = letterModel;
        so.FindProperty("flagModel").objectReferenceValue = flagModel;

        so.FindProperty("playerController").objectReferenceValue = playerCtrl;
        so.FindProperty("enemyController").objectReferenceValue = enemyCtrl;
        so.FindProperty("escortController").objectReferenceValue = escortCtrl;

        so.ApplyModifiedProperties();

        Debug.Log("[SceneSetup] Auto-configured SceneBootstrapper with GLB models and Animator Controllers.");
    }

    private static AnimationClip FindClip(Object[] assets, params string[] keywords)
    {
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                foreach (var keyword in keywords)
                {
                    if (clip.name.ToLower().Contains(keyword.ToLower()))
                    {
                        return clip;
                    }
                }
            }
        }
        // Fallback to first clip that doesn't start with double underscore
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                return clip;
            }
        }
        return null;
    }

    private static AnimatorController CreateKimDongController(Object[] assets)
    {
        string path = "Assets/Models/Characters/KimDongController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsHidden", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PlayingFlute", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ChoppingWood", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HerdingBuffalo", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(assets, "idle");
        AnimationClip walkClip = FindClip(assets, "walk");
        AnimationClip runClip = FindClip(assets, "run");
        AnimationClip dieClip = FindClip(assets, "die", "death");
        AnimationClip fluteClip = FindClip(assets, "flute", "play");
        AnimationClip woodClip = FindClip(assets, "wood", "chop");
        AnimationClip buffaloClip = FindClip(assets, "buffalo", "herd");

        // 1. Locomotion Blend Tree
        var locomotionState = rootStateMachine.AddState("Locomotion");
        BlendTree blendTree;
        controller.CreateBlendTreeInController("LocomotionTree", out blendTree, 0);
        locomotionState.motion = blendTree;
        blendTree.blendParameter = "Speed";

        if (idleClip != null) blendTree.AddChild(idleClip, 0f);
        if (walkClip != null) blendTree.AddChild(walkClip, 0.5f);
        if (runClip != null) blendTree.AddChild(runClip, 1.0f);

        rootStateMachine.defaultState = locomotionState;

        // 2. Disguise States
        if (fluteClip != null)
        {
            var state = rootStateMachine.AddState("PlayingFlute");
            state.motion = fluteClip;
            var t = rootStateMachine.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, "PlayingFlute");
            var tBack = state.AddTransition(locomotionState);
            tBack.hasExitTime = true;
        }
        if (woodClip != null)
        {
            var state = rootStateMachine.AddState("ChoppingWood");
            state.motion = woodClip;
            var t = rootStateMachine.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, "ChoppingWood");
            var tBack = state.AddTransition(locomotionState);
            tBack.hasExitTime = true;
        }
        if (buffaloClip != null)
        {
            var state = rootStateMachine.AddState("HerdingBuffalo");
            state.motion = buffaloClip;
            var t = rootStateMachine.AddAnyStateTransition(state);
            t.AddCondition(AnimatorConditionMode.If, 0, "HerdingBuffalo");
            var tBack = state.AddTransition(locomotionState);
            tBack.hasExitTime = true;
        }

        // 3. Die State
        if (dieClip != null)
        {
            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = dieClip;
            var t = rootStateMachine.AddAnyStateTransition(dieState);
            t.AddCondition(AnimatorConditionMode.If, 0, "Die");
        }

        return controller;
    }

    private static AnimatorController CreateEnemyController(Object[] assets)
    {
        string path = "Assets/Models/Characters/EnemyController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(assets, "idle");
        AnimationClip walkClip = FindClip(assets, "walk");
        AnimationClip runClip = FindClip(assets, "run", "chase");
        AnimationClip dieClip = FindClip(assets, "die", "death");

        // Locomotion Blend Tree
        var locomotionState = rootStateMachine.AddState("Locomotion");
        BlendTree blendTree;
        controller.CreateBlendTreeInController("LocomotionTree", out blendTree, 0);
        locomotionState.motion = blendTree;
        blendTree.blendParameter = "Speed";

        if (idleClip != null) blendTree.AddChild(idleClip, 0f);
        if (walkClip != null) blendTree.AddChild(walkClip, 0.5f);
        if (runClip != null) blendTree.AddChild(runClip, 1.0f);

        rootStateMachine.defaultState = locomotionState;

        // Die State
        if (dieClip != null)
        {
            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = dieClip;
            var t = rootStateMachine.AddAnyStateTransition(dieState);
            t.AddCondition(AnimatorConditionMode.If, 0, "Die");
        }

        return controller;
    }

    private static AnimatorController CreateEscortController(Object[] assets)
    {
        string path = "Assets/Models/Characters/EscortController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Waiting", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Following", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hiding", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(assets, "idle", "waiting");
        AnimationClip walkClip = FindClip(assets, "walk", "follow");
        AnimationClip hideClip = FindClip(assets, "hide", "crouch", "idle");

        var waitingState = rootStateMachine.AddState("Waiting");
        waitingState.motion = idleClip;

        var followingState = rootStateMachine.AddState("Following");
        followingState.motion = walkClip;

        var hidingState = rootStateMachine.AddState("Hiding");
        hidingState.motion = hideClip;

        // Transitions
        var tFollow = rootStateMachine.AddAnyStateTransition(followingState);
        tFollow.AddCondition(AnimatorConditionMode.If, 0, "Following");

        var tWait = rootStateMachine.AddAnyStateTransition(waitingState);
        tWait.AddCondition(AnimatorConditionMode.If, 0, "Waiting");

        var tHide = rootStateMachine.AddAnyStateTransition(hidingState);
        tHide.AddCondition(AnimatorConditionMode.If, 0, "Hiding");

        rootStateMachine.defaultState = waitingState;

        return controller;
    }
}
