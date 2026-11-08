#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using RongRanChamCham;

public static class CharacterAssetPipeline
{
    private const string ModelsDir = "Assets/_Game/Models/Characters";
    private const string MaterialsDir = "Assets/_Game/Materials/Characters";
    private const string PrefabsDir = "Assets/_Game/Prefabs/Characters";
    private const string AnimDir = "Assets/_Game/Animations/Characters";
    private const string ScenesDir = "Assets/_Game/Scenes";

    // AutoRunOnce removed: We do not automatically inject placeholder models.
    // Use manual menu items or Speckit workflow.

    [MenuItem("Tools/Rong Ran/Phase 1 - Build Characters & Showcase")]
    public static void BuildPhase1()
    {
        Debug.Log("[Phase 1] Starting Character Asset Pipeline...");

        EnsureDirectories();
        CreateMaterials();
        CreateAnimatorControllers();
        CreatePrefabs();
        AssetDatabase.SaveAssets();
        BuildShowcaseScene();
        AssetDatabase.SaveAssets();

        Debug.Log("[Phase 1] Character Asset Pipeline completed successfully!");
    }

    private static void EnsureDirectories()
    {
        EnsureDir(MaterialsDir);
        EnsureDir(PrefabsDir);
        EnsureDir(AnimDir);
        EnsureDir(ScenesDir);
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDir(parent);
            }
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static Shader GetURPShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    private static void CreateMaterials()
    {
        Shader shader = GetURPShader();

        CreateMaterial($"{MaterialsDir}/M_Mother.mat", $"{ModelsDir}/knight_texture.png", Color.white, shader);
        CreateMaterial($"{MaterialsDir}/M_Hunter.mat", $"{ModelsDir}/rogue_texture.png", Color.white, shader);
        CreateMaterial($"{MaterialsDir}/M_Child.mat", $"{ModelsDir}/mage_texture.png", Color.white, shader);
        CreateMaterial($"{MaterialsDir}/M_ConUt.mat", $"{ModelsDir}/mage_texture.png", new Color(1f, 0.82f, 0.88f, 1f), shader);
    }

    private static Material CreateMaterial(string matPath, string texPath, Color tint, Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static AnimationClip FindClip(string fbxPath, string[] preferredNames)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

        foreach (var pref in preferredNames)
        {
            var match = clips.FirstOrDefault(c => c.name.Equals(pref, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        foreach (var pref in preferredNames)
        {
            var match = clips.FirstOrDefault(c => c.name.IndexOf(pref, StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) return match;
        }

        return clips.FirstOrDefault();
    }

    private static AnimatorController CreateController(string path, string fbxPath, string idlePref, string walkPref, string runPref, string actionPref)
    {
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Action", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Find clips
        var idleClip = FindClip(fbxPath, new[] { idlePref, "Idle", "Unarmed_Idle" });
        var walkClip = FindClip(fbxPath, new[] { walkPref, "Walking_A", "Walking_B", "Walking" });
        var runClip = FindClip(fbxPath, new[] { runPref, "Running_A", "Running_B", "Running" });
        var actionClip = FindClip(fbxPath, new[] { actionPref, "Block", "Hit_A", "Cheer" });

        // Idle State (Default)
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        // Walk State
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkClip != null ? walkClip : idleClip;

        // Run State
        var runState = rootStateMachine.AddState("Run");
        runState.motion = runClip != null ? runClip : (walkClip != null ? walkClip : idleClip);

        // Transitions: Idle <-> Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Transitions: Walk <-> Run
        var toRun = walkState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 3.5f, "Speed");

        var backToWalk = runState.AddTransition(walkState);
        backToWalk.hasExitTime = false;
        backToWalk.duration = 0.15f;
        backToWalk.AddCondition(AnimatorConditionMode.Less, 3.5f, "Speed");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreateAnimatorControllers()
    {
        CreateController($"{AnimDir}/Ctrl_Mother.controller", $"{ModelsDir}/Knight.fbx", "Idle", "Walking_A", "Running_A", "Block");
        CreateController($"{AnimDir}/Ctrl_Hunter.controller", $"{ModelsDir}/RogueHooded.fbx", "Idle", "Walking_A", "Running_A", "Interact");
        CreateController($"{AnimDir}/Ctrl_Child.controller", $"{ModelsDir}/Mage.fbx", "Idle", "Walking_A", "Running_A", "Cheer");
    }

    private static void ApplyMaterialRecursively(GameObject go, Material mat)
    {
        foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rend.sharedMaterials = mats;
        }
    }

    private static void CreatePrefabs()
    {
        // 1. Mother Prefab
        CreateMotherPrefab();

        // 2. Hunter Prefab
        CreateHunterPrefab();

        // 3. Child Prefab
        CreateChildPrefab();
    }

    private static void CreateMotherPrefab()
    {
        string path = $"{PrefabsDir}/Mother.prefab";
        GameObject root = new GameObject("Mother");
        root.tag = "Player";

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.minMoveDistance = 0.001f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/Ctrl_Mother.controller");

        var mc = root.AddComponent<MotherController>();

        // Attach Model
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Knight.fbx");
        if (fbxAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
            modelInst.name = "Model";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Mother.mat");
            if (mat != null) ApplyMaterialRecursively(modelInst, mat);
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Phase 1] Saved Mother prefab: {path}");
    }

    private static void CreateHunterPrefab()
    {
        string path = $"{PrefabsDir}/Hunter.prefab";
        GameObject root = new GameObject("Hunter");
        root.tag = "Hunter";

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.minMoveDistance = 0.001f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/Ctrl_Hunter.controller");

        var hunterAI = root.AddComponent<HunterAI>();

        // Attach Model
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/RogueHooded.fbx");
        if (fbxAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
            modelInst.name = "Model";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Hunter.mat");
            if (mat != null) ApplyMaterialRecursively(modelInst, mat);
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Phase 1] Saved Hunter prefab: {path}");
    }

    private static void CreateChildPrefab()
    {
        string path = $"{PrefabsDir}/Child.prefab";
        GameObject root = new GameObject("Child");
        root.tag = "Child";

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.55f, 0f);
        cc.height = 1.1f;
        cc.radius = 0.35f;
        cc.minMoveDistance = 0.001f;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/Ctrl_Child.controller");

        var follower = root.AddComponent<ChildFollower>();

        // Attach Model (Scaled chibi)
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Mage.fbx");
        if (fbxAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
            modelInst.name = "Model";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f); // Chibi scale

            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Child.mat");
            if (mat != null) ApplyMaterialRecursively(modelInst, mat);
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Phase 1] Saved Child prefab: {path}");
    }

    private static void BuildShowcaseScene()
    {
        string scenePath = $"{ScenesDir}/Character_Showcase.unity";
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Environment: Directional Light
        GameObject lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 2. Camera: Framed view
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 40f;
        camGo.transform.position = new Vector3(0f, 2.5f, -6.5f);
        camGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

        // 3. Ground Plane (Clean, flat Y = 0)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Showcase_Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f); // 20m x 20m plane

        Shader urpShader = GetURPShader();
        Material groundMat = new Material(urpShader);
        if (groundMat.HasProperty("_BaseColor")) groundMat.SetColor("_BaseColor", new Color(0.25f, 0.35f, 0.3f));
        if (groundMat.HasProperty("_Color")) groundMat.SetColor("_Color", new Color(0.25f, 0.35f, 0.3f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // 4. Instantiate Prefabs on Stage at Y = 0
        var motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Mother.prefab");
        var hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Hunter.prefab");
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Child.prefab");
        var conUtMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_ConUt.mat");

        Debug.Log($"[Showcase] Loaded prefabs: mother={(motherPrefab != null)}, hunter={(hunterPrefab != null)}, child={(childPrefab != null)}");

        if (motherPrefab != null)
        {
            GameObject mother = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab);
            SceneManager.MoveGameObjectToScene(mother, scene);
            mother.name = "Mother (Rắn Mẹ)";
            mother.transform.position = new Vector3(-2.2f, 0f, 0f);
            mother.transform.rotation = Quaternion.identity;
        }

        if (hunterPrefab != null)
        {
            GameObject hunter = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab);
            SceneManager.MoveGameObjectToScene(hunter, scene);
            hunter.name = "Hunter (Thầy Thuốc)";
            hunter.transform.position = new Vector3(2.2f, 0f, 0f);
            hunter.transform.rotation = Quaternion.identity;
        }

        if (childPrefab != null)
        {
            // Child 1
            GameObject child1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            SceneManager.MoveGameObjectToScene(child1, scene);
            child1.name = "Child 1";
            child1.transform.position = new Vector3(0f, 0f, 0.8f);
            child1.transform.rotation = Quaternion.identity;

            // Child 2
            GameObject child2 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            SceneManager.MoveGameObjectToScene(child2, scene);
            child2.name = "Child 2";
            child2.transform.position = new Vector3(0f, 0f, -0.4f);
            child2.transform.rotation = Quaternion.identity;

            // Con Út (Last child, distinct pink tint)
            GameObject conUt = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            SceneManager.MoveGameObjectToScene(conUt, scene);
            conUt.name = "Con Út";
            conUt.transform.position = new Vector3(0f, 0f, -1.6f);
            conUt.transform.rotation = Quaternion.identity;
            if (conUtMat != null) ApplyMaterialRecursively(conUt, conUtMat);
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[Phase 1] Showcase scene saved: {scenePath}");
    }
}
#endif
