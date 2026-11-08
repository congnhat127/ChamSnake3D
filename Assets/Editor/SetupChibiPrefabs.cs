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
    private const string ScenesDir = "Assets/_Game/Scenes";

    [MenuItem("Tools/Rong Ran/Setup Chibi Rigs & Sockets (Spec 001)")]
    public static void Execute()
    {
        Debug.Log("[Speckit 001] Setting up Chibi Rigs and Sockets...");

        EnsureDir(PrefabsDir);

        BuildMotherPrefab();
        BuildChildPrefab();
        BuildHunterPrefab();

        AssetDatabase.SaveAssets();

        UpdateShowcaseScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[Speckit 001] Chibi Rigs and Sockets setup completed successfully!");
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
        sHead.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        GameObject sHandL = new GameObject("Socket_Hand_L");
        sHandL.transform.SetParent(root.transform, false);
        sHandL.transform.localPosition = new Vector3(-1.4f, 0.9f, 0.2f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(1.4f, 0.9f, 0.2f);

        // 4. Stylized Chibi Visual Representation
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Torso (Áo bà ba)
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(visual.transform, false);
        torso.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        torso.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
        UnityEngine.Object.DestroyImmediate(torso.GetComponent<Collider>());
        var torsoMat = GetMat("Mat_Mother_Outfit") ?? GetMat("Mat_Mother");
        if (torsoMat != null) torso.GetComponent<Renderer>().sharedMaterial = torsoMat;

        // Head (Chibi Face)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visual.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        head.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        var headMat = GetMat("Mat_Mother_Face") ?? GetMat("Mat_Skin");
        if (headMat != null) head.GetComponent<Renderer>().sharedMaterial = headMat;

        // Protective Arms (Dang rộng 2.8m cản đường)
        GameObject armLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        armLeft.name = "Arm_L";
        armLeft.transform.SetParent(sHandL.transform, false);
        armLeft.transform.localPosition = new Vector3(0.7f, 0f, 0f);
        armLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        armLeft.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
        UnityEngine.Object.DestroyImmediate(armLeft.GetComponent<Collider>());
        if (torsoMat != null) armLeft.GetComponent<Renderer>().sharedMaterial = torsoMat;

        GameObject armRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        armRight.name = "Arm_R";
        armRight.transform.SetParent(sHandR.transform, false);
        armRight.transform.localPosition = new Vector3(-0.7f, 0f, 0f);
        armRight.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        armRight.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
        UnityEngine.Object.DestroyImmediate(armRight.GetComponent<Collider>());
        if (torsoMat != null) armRight.GetComponent<Renderer>().sharedMaterial = torsoMat;

        // Nón Lá (Attached to Socket_Head)
        GameObject nonLa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nonLa.name = "NonLa";
        nonLa.transform.SetParent(sHead.transform, false);
        nonLa.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        nonLa.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        nonLa.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
        UnityEngine.Object.DestroyImmediate(nonLa.GetComponent<Collider>());
        var nonLaMat = GetMat("Mat_NonLa");
        if (nonLaMat != null) nonLa.GetComponent<Renderer>().sharedMaterial = nonLaMat;

        // Defense Stance Aura
        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aura.name = "LockAuraVisual";
        aura.transform.SetParent(root.transform, false);
        aura.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        aura.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
        UnityEngine.Object.DestroyImmediate(aura.GetComponent<Collider>());
        var auraMat = GetMat("Mat_LockAura");
        if (auraMat != null) aura.GetComponent<Renderer>().sharedMaterial = auraMat;
        aura.SetActive(false);

        // Inject References into MotherController
        var serializedObj = new SerializedObject(mc);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketHandL").objectReferenceValue = sHandL.transform;
        serializedObj.FindProperty("socketHandR").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("lockAuraVisual").objectReferenceValue = aura;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Mother prefab: {path}");
    }

    private static void BuildChildPrefab()
    {
        string path = $"{PrefabsDir}/Child.prefab";
        GameObject root = new GameObject("Child");
        root.tag = "Child";

        // 1. CharacterController
        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.55f, 0f);
        cc.height = 1.1f;
        cc.radius = 0.35f;
        cc.minMoveDistance = 0.001f;

        // 2. ChildFollower
        var cf = root.AddComponent<ChildFollower>();

        // 3. Sockets
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        GameObject sCrown = new GameObject("Socket_Crown");
        sCrown.transform.SetParent(sHead.transform, false);
        sCrown.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        // 4. Stylized Chibi Visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // Chubby body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(visual.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.5f, 0.55f);
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        var childMat = GetMat("Mat_Child");
        if (childMat != null) body.GetComponent<Renderer>().sharedMaterial = childMat;

        // Chibi Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visual.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        var childFaceMat = GetMat("Mat_Child_Face") ?? GetMat("Mat_Skin");
        if (childFaceMat != null) head.GetComponent<Renderer>().sharedMaterial = childFaceMat;

        // Golden Crown (Attached to Socket_Crown for Con Út)
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crown.name = "Crown";
        crown.transform.SetParent(sCrown.transform, false);
        crown.transform.localPosition = Vector3.zero;
        crown.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
        UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
        var crownMat = GetMat("Mat_Crown");
        if (crownMat != null) crown.GetComponent<Renderer>().sharedMaterial = crownMat;
        crown.SetActive(false); // only enabled when isConUt is true

        // Inject References
        var serializedObj = new SerializedObject(cf);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketCrown").objectReferenceValue = sCrown.transform;
        serializedObj.FindProperty("crownVisual").objectReferenceValue = crown;
        serializedObj.FindProperty("bodyRenderer").objectReferenceValue = body.GetComponent<Renderer>();
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

        // 1. CharacterController
        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        // 2. HunterAI
        var ai = root.AddComponent<HunterAI>();

        // 3. Sockets
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(0.4f, 0.9f, 0.6f);

        // 4. Stylized Visual (Áo dài tím truyền thống)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject robe = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        robe.name = "Robe";
        robe.transform.SetParent(visual.transform, false);
        robe.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        robe.transform.localScale = new Vector3(0.65f, 0.6f, 0.5f);
        UnityEngine.Object.DestroyImmediate(robe.GetComponent<Collider>());
        var robeMat = GetMat("Mat_Hunter_Robe") ?? GetMat("Mat_Hunter");
        if (robeMat != null) robe.GetComponent<Renderer>().sharedMaterial = robeMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visual.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        var headMat = GetMat("Mat_Hunter_Face") ?? GetMat("Mat_Skin");
        if (headMat != null) head.GetComponent<Renderer>().sharedMaterial = headMat;

        // Nón Lá
        GameObject nonLa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nonLa.name = "NonLa";
        nonLa.transform.SetParent(sHead.transform, false);
        nonLa.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        nonLa.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        nonLa.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
        UnityEngine.Object.DestroyImmediate(nonLa.GetComponent<Collider>());
        var nonLaMat = GetMat("Mat_NonLa");
        if (nonLaMat != null) nonLa.GetComponent<Renderer>().sharedMaterial = nonLaMat;

        // Grab Swipe VFX Visual
        GameObject swipeVfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        swipeVfx.name = "SwipeVFX";
        swipeVfx.transform.SetParent(sHandR.transform, false);
        swipeVfx.transform.localPosition = Vector3.zero;
        swipeVfx.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        UnityEngine.Object.DestroyImmediate(swipeVfx.GetComponent<Collider>());
        var rubyMat = GetMat("Mat_Ruby");
        if (rubyMat != null) swipeVfx.GetComponent<Renderer>().sharedMaterial = rubyMat;
        swipeVfx.SetActive(false);

        // Inject References
        var serializedObj = new SerializedObject(ai);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketGrabHand").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("swipeVfxVisual").objectReferenceValue = swipeVfx;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Hunter prefab: {path}");
    }

    private static void UpdateShowcaseScene()
    {
        string scenePath = $"{ScenesDir}/Character_Showcase.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old character instances if any
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name.Contains("Mother") || rootGo.name.Contains("Hunter") || rootGo.name.Contains("Child") || rootGo.name.Contains("Con Út"))
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        // Instantiate new clean Prefabs
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
            // Child 1
            var c1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c1.name = "Child 1";
            c1.transform.position = new Vector3(0f, 0f, -1.3f);
            var f1 = c1.GetComponent<ChildFollower>();
            if (f1 != null) f1.Initialize(0, false, null);

            // Child 2
            var c2 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c2.name = "Child 2";
            c2.transform.position = new Vector3(0f, 0f, -2.6f);
            var f2 = c2.GetComponent<ChildFollower>();
            if (f2 != null) f2.Initialize(1, false, null);

            // Con Út
            var c3 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c3.name = "Con Út";
            c3.transform.position = new Vector3(0f, 0f, -3.9f);
            var f3 = c3.GetComponent<ChildFollower>();
            if (f3 != null)
            {
                f3.Initialize(2, true, null);
                // Apply Con Út pink material
                var conUtMat = GetMat("Mat_ConUt");
                var body = c3.transform.Find("Visual/Body");
                if (body != null && conUtMat != null) body.GetComponent<Renderer>().sharedMaterial = conUtMat;
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
        Debug.Log($"[Speckit 001] Updated Showcase scene: {scenePath}");
    }
}
#endif
