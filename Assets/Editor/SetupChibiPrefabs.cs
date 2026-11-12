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
        Debug.Log("[Speckit 001] Building Polished Chibi Prefabs with Solid Models and Expressive Faces...");

        EnsureDir(PrefabsDir);

        BuildMotherPrefab();
        BuildChildPrefab();
        BuildHunterPrefab();

        AssetDatabase.SaveAssets();

        UpdateShowcaseScene();

        AssetDatabase.SaveAssets();

        try
        {
            RenderCharacterSnapshots.Execute();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupChibiPrefabs] RenderCharacterSnapshots error: {ex}");
        }

        try
        {
            TestGlbLoading.Test();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupChibiPrefabs] TestGlbLoading error: {ex}");
        }

        try
        {
            SetupNewRongRanMaster.Execute();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupChibiPrefabs] SetupNewRongRanMaster error: {ex}");
        }
        AssetDatabase.Refresh();

        Debug.Log("[Speckit 001] Complete! Characters now have expressive faces, traditional accessories, and smooth watertight 3D models!");
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

    private static GameObject CreateSocket(Transform parent, string socketName, Vector3 pos)
    {
        GameObject socket = new GameObject(socketName);
        socket.transform.SetParent(parent, false);
        socket.transform.localPosition = pos;
        return socket;
    }

    private static void BuildMotherPrefab()
    {
        string path = $"{PrefabsDir}/Mother.prefab";
        GameObject root = new GameObject("Mother");
        root.tag = "Player";
        var motherMat = GetMat("Mat_Mother_Outfit") ?? GetMat("Mat_Mother");
        var hairMat = GetMat("Mat_Hair");

        // 1. CharacterController
        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        // 2. MotherController
        var mc = root.AddComponent<MotherController>();

        // 3. Sockets
        GameObject sHead = CreateSocket(root.transform, "Socket_Head", new Vector3(0f, 1.34f, 0.0f));
        GameObject sHandL = CreateSocket(root.transform, "Socket_Hand_L", new Vector3(-1.32f, 0.82f, 0.22f));
        GameObject sHandR = CreateSocket(root.transform, "Socket_Hand_R", new Vector3(1.32f, 0.82f, 0.22f));

        // 4. Attach Body Model: Chibi_Mother.obj (Áo bà ba & curved embracing arms)
        GameObject motherModelAsset = LoadModel("Chibi_Mother.obj");
        if (motherModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(motherModelAsset, root.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            ApplyMaterial(modelInst, motherMat);
        }

        // 5. Solid 3D Chibi Head with Expressive Face
        GameObject headAsset = LoadModel("Chibi_Head.obj");
        if (headAsset != null)
        {
            GameObject headGo = (GameObject)PrefabUtility.InstantiatePrefab(headAsset, sHead.transform);
            headGo.name = "Head";
            headGo.transform.localPosition = Vector3.zero;
            headGo.transform.localRotation = Quaternion.identity;
            headGo.transform.localScale = Vector3.one;
            var faceMat = GetMat("Mat_Mother_Face");
            if (faceMat != null) ApplyMaterial(headGo, faceMat);
        }

        // 6. 3D Styled Hair
        GameObject hairAsset = LoadModel("Chibi_Hair_Mother.obj");
        if (hairAsset != null)
        {
            GameObject hairInst = (GameObject)PrefabUtility.InstantiatePrefab(hairAsset, sHead.transform);
            hairInst.name = "Hair_Mother";
            hairInst.transform.localPosition = Vector3.zero;
            hairInst.transform.localRotation = Quaternion.identity;
            hairInst.transform.localScale = Vector3.one;
            if (hairMat != null) ApplyMaterial(hairInst, hairMat);
        }

        // 7. Nón Lá on head (resting naturally on the hair)
        GameObject nonLaAsset = LoadModel("NonLa.obj");
        if (nonLaAsset != null)
        {
            GameObject nonLaInst = (GameObject)PrefabUtility.InstantiatePrefab(nonLaAsset, sHead.transform);
            nonLaInst.name = "NonLa";
            nonLaInst.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            nonLaInst.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            nonLaInst.transform.localScale = Vector3.one;

            var nonLaMat = GetMat("Mat_NonLa");
            ApplyMaterial(nonLaInst, nonLaMat);
        }

        // 8. Defense Aura
        GameObject auraGo = new GameObject("LockAuraVisual");
        auraGo.transform.SetParent(root.transform, false);
        auraGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        auraGo.SetActive(false);

        // 9. Foot Sockets & Procedural Stepping Motor
        GameObject sFootL = CreateSocket(root.transform, "Socket_Foot_L", new Vector3(-0.13f, 0.07f, 0.05f));
        GameObject sFootR = CreateSocket(root.transform, "Socket_Foot_R", new Vector3(0.13f, 0.07f, 0.05f));

        var procAnim = root.AddComponent<ChibiProceduralAnimator>();
        var animSO = new SerializedObject(procAnim);
        animSO.FindProperty("socketFootL").objectReferenceValue = sFootL.transform;
        animSO.FindProperty("socketFootR").objectReferenceValue = sFootR.transform;
        var bodyTrans = root.transform.Find("Model_Body");
        if (bodyTrans != null) animSO.FindProperty("bodyVisual").objectReferenceValue = bodyTrans;
        animSO.ApplyModifiedPropertiesWithoutUndo();

        // Inject References
        var serializedObj = new SerializedObject(mc);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketHandL").objectReferenceValue = sHandL.transform;
        serializedObj.FindProperty("socketHandR").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("lockAuraVisual").objectReferenceValue = auraGo;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Mother prefab with expressive face: {path}");
    }

    private static void BuildChildPrefab()
    {
        string path = $"{PrefabsDir}/Child.prefab";
        GameObject root = new GameObject("Child");
        root.tag = "Child";
        var hairMat = GetMat("Mat_Hair");

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.45f, 0f);
        cc.height = 0.9f;
        cc.radius = 0.30f;
        cc.minMoveDistance = 0.001f;

        var cf = root.AddComponent<ChildFollower>();

        GameObject sHead = CreateSocket(root.transform, "Socket_Head", new Vector3(0f, 0.78f, 0.0f));
        GameObject sCrown = CreateSocket(sHead.transform, "Socket_Crown", new Vector3(0f, 0.22f, 0f));

        // 1. Attach Body Model: Chibi_Child.obj
        GameObject childModelAsset = LoadModel("Chibi_Child.obj");
        Renderer bodyRend = null;
        if (childModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(childModelAsset, root.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var childMat = GetMat("Mat_Child");
            ApplyMaterial(modelInst, childMat);
            bodyRend = modelInst.GetComponentInChildren<Renderer>();
        }

        // 2. Solid 3D Chibi Head with Cute Face
        GameObject headAsset = LoadModel("Chibi_Head.obj");
        if (headAsset != null)
        {
            GameObject headGo = (GameObject)PrefabUtility.InstantiatePrefab(headAsset, sHead.transform);
            headGo.name = "Head";
            headGo.transform.localPosition = Vector3.zero;
            headGo.transform.localRotation = Quaternion.identity;
            headGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            var childFaceMat = GetMat("Mat_Child_Face");
            if (childFaceMat != null) ApplyMaterial(headGo, childFaceMat);
        }

        // 3. Traditional Vietnamese Folk Chỏm Tóc (Hair)
        GameObject hairAsset = LoadModel("Chibi_Hair_Child.obj");
        if (hairAsset != null)
        {
            GameObject hairInst = (GameObject)PrefabUtility.InstantiatePrefab(hairAsset, sHead.transform);
            hairInst.name = "Hair_Child";
            hairInst.transform.localPosition = Vector3.zero;
            hairInst.transform.localRotation = Quaternion.identity;
            hairInst.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            if (hairMat != null) ApplyMaterial(hairInst, hairMat);
        }

        // 4. Crown on Socket_Crown (for Con Út)
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
            crownInst.SetActive(false);
        }

        // 5. Foot Sockets & Procedural Stepping Motor
        GameObject sFootL = CreateSocket(root.transform, "Socket_Foot_L", new Vector3(-0.09f, 0.04f, 0.03f));
        GameObject sFootR = CreateSocket(root.transform, "Socket_Foot_R", new Vector3(0.09f, 0.04f, 0.03f));

        var procAnim = root.AddComponent<ChibiProceduralAnimator>();
        var animSO = new SerializedObject(procAnim);
        animSO.FindProperty("socketFootL").objectReferenceValue = sFootL.transform;
        animSO.FindProperty("socketFootR").objectReferenceValue = sFootR.transform;
        var childBody = root.transform.Find("Model_Body");
        if (childBody != null) animSO.FindProperty("bodyVisual").objectReferenceValue = childBody;
        animSO.ApplyModifiedPropertiesWithoutUndo();

        // Inject References
        var serializedObj = new SerializedObject(cf);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketCrown").objectReferenceValue = sCrown.transform;
        serializedObj.FindProperty("crownVisual").objectReferenceValue = crownInst;
        serializedObj.FindProperty("bodyRenderer").objectReferenceValue = bodyRend;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Child prefab with expressive face: {path}");
    }

    private static void BuildHunterPrefab()
    {
        string path = $"{PrefabsDir}/Hunter.prefab";
        GameObject root = new GameObject("Hunter");
        root.tag = "Hunter";
        var robeMat = GetMat("Mat_Hunter_Robe") ?? GetMat("Mat_Hunter");
        var hairMat = GetMat("Mat_Hair");

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        var ai = root.AddComponent<HunterAI>();

        // Sockets
        GameObject sHead = CreateSocket(root.transform, "Socket_Head", new Vector3(0f, 1.34f, 0.0f));
        GameObject sHandR = CreateSocket(root.transform, "Socket_Hand_R", new Vector3(0.30f, 0.78f, 0.16f));

        // 1. Attach Body Model: Chibi_Hunter.obj (Áo dài tím)
        GameObject hunterModelAsset = LoadModel("Chibi_Hunter.obj");
        if (hunterModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(hunterModelAsset, root.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            ApplyMaterial(modelInst, robeMat);
        }

        // 2. Solid 3D Chibi Head with Crafty Face
        GameObject headAsset = LoadModel("Chibi_Head.obj");
        if (headAsset != null)
        {
            GameObject headGo = (GameObject)PrefabUtility.InstantiatePrefab(headAsset, sHead.transform);
            headGo.name = "Head";
            headGo.transform.localPosition = Vector3.zero;
            headGo.transform.localRotation = Quaternion.identity;
            headGo.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            var hunterFaceMat = GetMat("Mat_Hunter_Face");
            if (hunterFaceMat != null) ApplyMaterial(headGo, hunterFaceMat);
        }

        // 3. Khăn Đóng (Turban) on Head
        GameObject khanDongAsset = LoadModel("Chibi_KhanDong_Hunter.obj");
        if (khanDongAsset != null)
        {
            GameObject turbanInst = (GameObject)PrefabUtility.InstantiatePrefab(khanDongAsset, sHead.transform);
            turbanInst.name = "KhanDong";
            turbanInst.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            turbanInst.transform.localRotation = Quaternion.identity;
            turbanInst.transform.localScale = Vector3.one;

            if (hairMat != null) ApplyMaterial(turbanInst, hairMat);
        }

        // 4. Medicine Pouch slung across chest
        GameObject pouchAsset = LoadModel("MedicinePouch.obj");
        if (pouchAsset != null)
        {
            GameObject pouchInst = (GameObject)PrefabUtility.InstantiatePrefab(pouchAsset, root.transform);
            pouchInst.name = "MedicinePouch_3D";
            pouchInst.transform.localPosition = Vector3.zero;
            pouchInst.transform.localRotation = Quaternion.identity;
            pouchInst.transform.localScale = Vector3.one;

            var pouchMat = GetMat("Mat_Pouch");
            ApplyMaterial(pouchInst, pouchMat);
        }

        // 5. Swipe VFX
        GameObject swipeVfx = new GameObject("SwipeVFX");
        swipeVfx.transform.SetParent(sHandR.transform, false);
        swipeVfx.SetActive(false);

        // 6. Foot Sockets & Procedural Stepping Motor
        GameObject sFootL = CreateSocket(root.transform, "Socket_Foot_L", new Vector3(-0.11f, 0.07f, 0.04f));
        GameObject sFootR = CreateSocket(root.transform, "Socket_Foot_R", new Vector3(0.11f, 0.07f, 0.04f));

        var procAnim = root.AddComponent<ChibiProceduralAnimator>();
        var animSO = new SerializedObject(procAnim);
        animSO.FindProperty("socketFootL").objectReferenceValue = sFootL.transform;
        animSO.FindProperty("socketFootR").objectReferenceValue = sFootR.transform;
        var hunterBody = root.transform.Find("Model_Body");
        if (hunterBody != null) animSO.FindProperty("bodyVisual").objectReferenceValue = hunterBody;
        animSO.ApplyModifiedPropertiesWithoutUndo();

        // Inject References
        var serializedObj = new SerializedObject(ai);
        serializedObj.FindProperty("socketHead").objectReferenceValue = sHead.transform;
        serializedObj.FindProperty("socketGrabHand").objectReferenceValue = sHandR.transform;
        serializedObj.FindProperty("swipeVfxVisual").objectReferenceValue = swipeVfx;
        serializedObj.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[Speckit 001] Saved Hunter prefab with expressive face: {path}");
    }

    private static void UpdateShowcaseScene()
    {
        string scenePath = $"{ScenesDir}/Character_Showcase.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Clean old
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name.Contains("Mother") || rootGo.name.Contains("Hunter") || rootGo.name.Contains("Child") || 
                rootGo.name.Contains("Con Út") || rootGo.name.Contains("DauDan") || rootGo.name.Contains("Khuc") || 
                rootGo.name.Contains("Chibi") || rootGo.name.Contains("Pedestal") || rootGo.name.Contains("Showcase_Pedestal"))
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        GameObject motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Mother.prefab");
        GameObject childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Child.prefab");
        GameObject hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Hunter.prefab");

        // 1. Rắn Mẹ (X = -3.6)
        if (motherPrefab != null)
        {
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab, scene);
            m.name = "Showcase_Mother";
            m.transform.position = new Vector3(-3.6f, 0f, 0f);
            m.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var mc = m.GetComponent<MotherController>();
            if (mc != null) UnityEngine.Object.DestroyImmediate(mc);
        }

        // 2. Rắn Con (X = -1.2)
        if (childPrefab != null)
        {
            GameObject c1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, scene);
            c1.name = "Showcase_Child_1";
            c1.transform.position = new Vector3(-1.2f, 0f, 0f);
            c1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cf = c1.GetComponent<ChildFollower>();
            if (cf != null) UnityEngine.Object.DestroyImmediate(cf);
        }

        // 3. Bé Con Út với Vương Miện (X = +1.2)
        if (childPrefab != null)
        {
            GameObject conUt = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, scene);
            conUt.name = "Showcase_ConUt_Crown";
            conUt.transform.position = new Vector3(1.2f, 0f, 0f);
            conUt.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cf = conUt.GetComponent<ChildFollower>();
            if (cf != null) UnityEngine.Object.DestroyImmediate(cf);

            Transform crown = conUt.transform.Find("Socket_Head/Socket_Crown/Crown_ConUt");
            if (crown != null) crown.gameObject.SetActive(true);
        }

        // 4. Thầy Thuốc (X = +3.6)
        if (hunterPrefab != null)
        {
            GameObject h = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab, scene);
            h.name = "Showcase_Hunter";
            h.transform.position = new Vector3(3.6f, 0f, 0f);
            h.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var ai = h.GetComponent<HunterAI>();
            if (ai != null) UnityEngine.Object.DestroyImmediate(ai);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Speckit 001] Updated Showcase scene: Assets/_Game/Scenes/Character_Showcase.unity");
    }
}
#endif
