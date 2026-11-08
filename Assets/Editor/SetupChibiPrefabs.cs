#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RongRanChamCham;

public static class SetupChibiPrefabs
{
    private const string PrefabsDir = "Assets/_Game/Prefabs/Characters";
    private const string MaterialsDir = "Assets/_Game/Materials/Characters";
    private const string ModelsDir = "Assets/_Game/Models/Characters";
    private const string ScenesDir = "Assets/_Game/Scenes";

    [MenuItem("Tools/Rong Ran/Setup Chibi Rigs & Sockets (Spec 001)")]
    public static void Execute()
    {
        Debug.Log("[Speckit 001] Building Prefabs with REAL 3D Chibi Models (No Primitives)...");

        EnsureDir(PrefabsDir);

        BuildMotherPrefab();
        BuildChildPrefab();
        BuildHunterPrefab();

        AssetDatabase.SaveAssets();

        UpdateShowcaseScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[Speckit 001] Complete! All characters updated with smooth 3D Chibi models!");
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static Material GetMat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/{name}.mat");
    }

    private static GameObject LoadModel(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{filename}");
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        if (go == null || mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }
    }

    private static void BuildMotherPrefab()
    {
        string path = $"{PrefabsDir}/Mother.prefab";
        GameObject root = new GameObject("Mother");
        root.tag = "Player";

        // 1. CharacterController
        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        // 2. MotherController
        var mc = root.AddComponent<MotherController>();

        // 3. Sockets (Hierarchical attachment points)
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 1.45f, 0.02f);

        GameObject sHandL = new GameObject("Socket_Hand_L");
        sHandL.transform.SetParent(root.transform, false);
        sHandL.transform.localPosition = new Vector3(-1.40f, 0.88f, 0.12f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(1.40f, 0.88f, 0.12f);

        // 4. Attach REAL 3D Model: Chibi_Mother.obj (Natural curved wing arms)
        GameObject motherModelAsset = LoadModel("Chibi_Mother.obj");
        if (motherModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(motherModelAsset, root.transform);
            modelInst.name = "Model_Chibi_Mother";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var motherMat = GetMat("Mat_Mother_Outfit") ?? GetMat("Mat_Mother");
            ApplyMaterial(modelInst, motherMat);
        }

        // 5. Attach REAL 3D Accessory: NonLa.obj
        GameObject nonLaAsset = LoadModel("NonLa.obj");
        if (nonLaAsset != null)
        {
            GameObject nonLaInst = (GameObject)PrefabUtility.InstantiatePrefab(nonLaAsset, sHead.transform);
            nonLaInst.name = "NonLa";
            nonLaInst.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            nonLaInst.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            nonLaInst.transform.localScale = Vector3.one;

            var nonLaMat = GetMat("Mat_NonLa");
            ApplyMaterial(nonLaInst, nonLaMat);
        }

        // 6. Defense Aura (Visual cue when lock-facing)
        GameObject aura = LoadModel("NonLa.obj"); // fallback or flat circle
        GameObject auraGo = new GameObject("LockAuraVisual");
        auraGo.transform.SetParent(root.transform, false);
        auraGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        auraGo.SetActive(false);

        // Inject References
        var serializedObj = new SerializedObject(mc);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketHandL").objectReferenceValue = sHandL.transform;
        serializedObj.FindProperty("socketHandR").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("lockAuraVisual").objectReferenceValue = auraGo;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Mother prefab with 3D curved arms: {path}");
    }

    private static void BuildChildPrefab()
    {
        string path = $"{PrefabsDir}/Child.prefab";
        GameObject root = new GameObject("Child");
        root.tag = "Child";

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.55f, 0f);
        cc.height = 1.1f;
        cc.radius = 0.35f;
        cc.minMoveDistance = 0.001f;

        var cf = root.AddComponent<ChildFollower>();

        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 0.85f, 0.02f);

        GameObject sCrown = new GameObject("Socket_Crown");
        sCrown.transform.SetParent(sHead.transform, false);
        sCrown.transform.localPosition = new Vector3(0f, 0.30f, 0f);

        // 1. Attach REAL 3D Model: Chibi_Child.obj
        GameObject childModelAsset = LoadModel("Chibi_Child.obj");
        Renderer bodyRend = null;
        if (childModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(childModelAsset, root.transform);
            modelInst.name = "Model_Chibi_Child";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var childMat = GetMat("Mat_Child");
            ApplyMaterial(modelInst, childMat);
            bodyRend = modelInst.GetComponentInChildren<Renderer>();
        }

        // 2. Attach REAL 3D Accessory: Crown_ConUt.obj
        GameObject crownAsset = LoadModel("Crown_ConUt.obj");
        GameObject crownInst = null;
        if (crownAsset != null)
        {
            crownInst = (GameObject)PrefabUtility.InstantiatePrefab(crownAsset, sCrown.transform);
            crownInst.name = "Crown_ConUt";
            crownInst.transform.localPosition = Vector3.zero;
            crownInst.transform.localRotation = Quaternion.identity;
            crownInst.transform.localScale = Vector3.one;

            var crownMat = GetMat("Mat_Crown");
            ApplyMaterial(crownInst, crownMat);
            crownInst.SetActive(false); // only enabled when isConUt
        }

        // Inject References
        var serializedObj = new SerializedObject(cf);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketCrown").objectReferenceValue = sCrown.transform;
        serializedObj.FindProperty("crownVisual").objectReferenceValue = crownInst;
        serializedObj.FindProperty("bodyRenderer").objectReferenceValue = bodyRend;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Child prefab: {path}");
    }

    private static void BuildHunterPrefab()
    {
        string path = $"{PrefabsDir}/Hunter.prefab";
        GameObject root = new GameObject("Hunter");
        root.tag = "Hunter";

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        var ai = root.AddComponent<HunterAI>();

        // Sockets
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 1.45f, 0.04f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(0.44f, 0.88f, 0.65f);

        GameObject sPouch = new GameObject("Socket_Pouch");
        sPouch.transform.SetParent(root.transform, false);
        sPouch.transform.localPosition = Vector3.zero; // Origin of MedicinePouch.obj wraps around body

        // 1. Attach REAL 3D Model: Chibi_Hunter.obj
        GameObject hunterModelAsset = LoadModel("Chibi_Hunter.obj");
        if (hunterModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(hunterModelAsset, root.transform);
            modelInst.name = "Model_Chibi_Hunter";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var robeMat = GetMat("Mat_Hunter_Robe") ?? GetMat("Mat_Hunter");
            ApplyMaterial(modelInst, robeMat);
        }

        // 2. Attach REAL 3D Accessory: NonLa.obj
        GameObject nonLaAsset = LoadModel("NonLa.obj");
        if (nonLaAsset != null)
        {
            GameObject nonLaInst = (GameObject)PrefabUtility.InstantiatePrefab(nonLaAsset, sHead.transform);
            nonLaInst.name = "NonLa";
            nonLaInst.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            nonLaInst.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            nonLaInst.transform.localScale = Vector3.one;

            var nonLaMat = GetMat("Mat_NonLa");
            ApplyMaterial(nonLaInst, nonLaMat);
        }

        // 3. Attach REAL 3D Accessory: MedicinePouch.obj (Đeo chéo qua vai)
        GameObject pouchAsset = LoadModel("MedicinePouch.obj");
        if (pouchAsset != null)
        {
            GameObject pouchInst = (GameObject)PrefabUtility.InstantiatePrefab(pouchAsset, sPouch.transform);
            pouchInst.name = "MedicinePouch_3D";
            pouchInst.transform.localPosition = Vector3.zero;
            pouchInst.transform.localRotation = Quaternion.identity;
            pouchInst.transform.localScale = Vector3.one;

            var pouchMat = GetMat("Mat_Pouch");
            ApplyMaterial(pouchInst, pouchMat);
        }

        // 4. Grab VFX
        GameObject swipeVfx = new GameObject("SwipeVFX");
        swipeVfx.transform.SetParent(sHandR.transform, false);
        swipeVfx.SetActive(false);

        // Inject References
        var serializedObj = new SerializedObject(ai);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketGrabHand").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("swipeVfxVisual").objectReferenceValue = swipeVfx;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Hunter prefab with 3D model & medicine pouch: {path}");
    }

    private static void UpdateShowcaseScene()
    {
        string scenePath = $"{ScenesDir}/Character_Showcase.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Clean old
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name.Contains("Mother") || rootGo.name.Contains("Hunter") || rootGo.name.Contains("Child") || rootGo.name.Contains("Con Út"))
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        // Instantiate
        var motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Mother.prefab");
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Child.prefab");
        var hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Hunter.prefab");

        if (motherPrefab != null)
        {
            var m = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab);
            m.name = "Mother (Rắn Mẹ)";
            m.transform.position = new Vector3(0f, 0f, 0f);
        }

        if (childPrefab != null)
        {
            var c1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c1.name = "Child 1";
            c1.transform.position = new Vector3(0f, 0f, -1.3f);
            var f1 = c1.GetComponent<ChildFollower>();
            if (f1 != null) f1.Initialize(0, false, null);

            var c2 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c2.name = "Child 2";
            c2.transform.position = new Vector3(0f, 0f, -2.6f);
            var f2 = c2.GetComponent<ChildFollower>();
            if (f2 != null) f2.Initialize(1, false, null);

            var c3 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c3.name = "Con Út";
            c3.transform.position = new Vector3(0f, 0f, -3.9f);
            var f3 = c3.GetComponent<ChildFollower>();
            if (f3 != null)
            {
                f3.Initialize(2, true, null);
                var conUtMat = GetMat("Mat_ConUt");
                ApplyMaterial(c3, conUtMat);
            }
        }

        if (hunterPrefab != null)
        {
            var h = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab);
            h.name = "Hunter (Thầy Thuốc)";
            h.transform.position = new Vector3(0f, 0f, 4.5f);
            h.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Speckit 001] Updated Showcase scene with 3D OBJ models: {scenePath}");
    }
}
#endif
