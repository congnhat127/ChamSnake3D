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
        Debug.Log("[Speckit 001] Building Prefabs with 3D Models, Heads, and Expressive Faces...");

        EnsureDir(PrefabsDir);

        BuildMotherPrefab();
        BuildChildPrefab();
        BuildHunterPrefab();

        AssetDatabase.SaveAssets();

        UpdateShowcaseScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[Speckit 001] Complete! Characters now have expressive faces, eyes, and traditional accessories!");
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

    private static GameObject AttachFoot(Transform parent, string socketName, Vector3 pos, Material mat)
    {
        GameObject socket = new GameObject(socketName);
        socket.transform.SetParent(parent, false);
        socket.transform.localPosition = pos;

        GameObject footAsset = LoadModel("Chibi_Foot.obj");
        if (footAsset != null)
        {
            GameObject footInst = (GameObject)PrefabUtility.InstantiatePrefab(footAsset, socket.transform);
            footInst.name = "Foot_Visual";
            footInst.transform.localPosition = Vector3.zero;
            footInst.transform.localRotation = Quaternion.identity;
            footInst.transform.localScale = Vector3.one;
            ApplyMaterial(footInst, mat);
        }
        return socket;
    }

    private static void AttachFace(Transform headTransform, Material faceMat)
    {
        GameObject faceCapAsset = LoadModel("Face_Cap.obj");
        if (faceCapAsset != null)
        {
            GameObject faceInst = (GameObject)PrefabUtility.InstantiatePrefab(faceCapAsset, headTransform);
            faceInst.name = "Face_Expressive";
            faceInst.transform.localPosition = Vector3.zero;
            faceInst.transform.localRotation = Quaternion.identity;
            faceInst.transform.localScale = new Vector3(1.008f, 1.008f, 1.008f);
            ApplyMaterial(faceInst, faceMat);
        }
        else
        {
            Debug.LogWarning("[Speckit 001] Face_Cap.obj model not found!");
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

        // 3. Sockets
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 1.35f, 0.02f);

        GameObject sHandL = new GameObject("Socket_Hand_L");
        sHandL.transform.SetParent(root.transform, false);
        sHandL.transform.localPosition = new Vector3(-1.40f, 0.88f, 0.12f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(1.40f, 0.88f, 0.12f);

        // 4. Attach Body Model: Chibi_Mother.obj (Áo bà ba & curved embracing arms)
        GameObject motherModelAsset = LoadModel("Chibi_Mother.obj");
        if (motherModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(motherModelAsset, root.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var motherMat = GetMat("Mat_Mother_Outfit") ?? GetMat("Mat_Mother");
            ApplyMaterial(modelInst, motherMat);
        }

        // 5. Head (Skin tone sphere on Socket_Head)
        GameObject headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGo.name = "Head";
        headGo.transform.SetParent(sHead.transform, false);
        headGo.transform.localPosition = Vector3.zero;
        headGo.transform.localScale = new Vector3(0.68f, 0.68f, 0.64f);
        UnityEngine.Object.DestroyImmediate(headGo.GetComponent<Collider>());
        var skinMat = GetMat("Mat_Skin");
        if (skinMat != null) headGo.GetComponent<Renderer>().sharedMaterial = skinMat;

        // 6. Face Decal with Eyes & Rosy Cheeks
        var faceMat = GetMat("Mat_Mother_Face");
        AttachFace(headGo.transform, faceMat);

        // 7. Nón Lá on head
        GameObject nonLaAsset = LoadModel("NonLa.obj");
        if (nonLaAsset != null)
        {
            GameObject nonLaInst = (GameObject)PrefabUtility.InstantiatePrefab(nonLaAsset, sHead.transform);
            nonLaInst.name = "NonLa";
            nonLaInst.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            nonLaInst.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
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
        var motherMat = GetMat("Mat_Mother_Outfit") ?? GetMat("Mat_Mother");
        GameObject sFootL = AttachFoot(root.transform, "Socket_Foot_L", new Vector3(-0.16f, 0.08f, 0.04f), motherMat);
        GameObject sFootR = AttachFoot(root.transform, "Socket_Foot_R", new Vector3(0.16f, 0.08f, 0.04f), motherMat);

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

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.55f, 0f);
        cc.height = 1.1f;
        cc.radius = 0.35f;
        cc.minMoveDistance = 0.001f;

        var cf = root.AddComponent<ChildFollower>();

        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 0.82f, 0.02f);

        GameObject sCrown = new GameObject("Socket_Crown");
        sCrown.transform.SetParent(sHead.transform, false);
        sCrown.transform.localPosition = new Vector3(0f, 0.32f, 0f);

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

        // 2. Head with Skin tone
        GameObject headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGo.name = "Head";
        headGo.transform.SetParent(sHead.transform, false);
        headGo.transform.localPosition = Vector3.zero;
        headGo.transform.localScale = new Vector3(0.60f, 0.60f, 0.56f);
        UnityEngine.Object.DestroyImmediate(headGo.GetComponent<Collider>());
        var skinMat = GetMat("Mat_Skin");
        if (skinMat != null) headGo.GetComponent<Renderer>().sharedMaterial = skinMat;

        // 3. Face Decal with Big Anime Eyes & Rosy Cheeks
        var childFaceMat = GetMat("Mat_Child_Face");
        AttachFace(headGo.transform, childFaceMat);

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
        var childFootMat = GetMat("Mat_Child");
        GameObject sFootL = AttachFoot(root.transform, "Socket_Foot_L", new Vector3(-0.12f, 0.06f, 0.03f), childFootMat);
        GameObject sFootR = AttachFoot(root.transform, "Socket_Foot_R", new Vector3(0.12f, 0.06f, 0.03f), childFootMat);

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

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;
        cc.minMoveDistance = 0.001f;

        var ai = root.AddComponent<HunterAI>();

        // Sockets
        GameObject sHead = new GameObject("Socket_Head");
        sHead.transform.SetParent(root.transform, false);
        sHead.transform.localPosition = new Vector3(0f, 1.34f, 0.04f);

        GameObject sHandR = new GameObject("Socket_Hand_R");
        sHandR.transform.SetParent(root.transform, false);
        sHandR.transform.localPosition = new Vector3(0.44f, 0.88f, 0.65f);

        GameObject sPouch = new GameObject("Socket_Pouch");
        sPouch.transform.SetParent(root.transform, false);
        sPouch.transform.localPosition = Vector3.zero;

        // 1. Attach Body Model: Chibi_Hunter.obj (Áo dài tím)
        GameObject hunterModelAsset = LoadModel("Chibi_Hunter.obj");
        if (hunterModelAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(hunterModelAsset, root.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var robeMat = GetMat("Mat_Hunter_Robe") ?? GetMat("Mat_Hunter");
            ApplyMaterial(modelInst, robeMat);
        }

        // 2. Head with Skin tone
        GameObject headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGo.name = "Head";
        headGo.transform.SetParent(sHead.transform, false);
        headGo.transform.localPosition = Vector3.zero;
        headGo.transform.localScale = new Vector3(0.64f, 0.64f, 0.60f);
        UnityEngine.Object.DestroyImmediate(headGo.GetComponent<Collider>());
        var skinMat = GetMat("Mat_Skin");
        if (skinMat != null) headGo.GetComponent<Renderer>().sharedMaterial = skinMat;

        // 3. Face Decal with Crafty Slanted Eyes
        var hunterFaceMat = GetMat("Mat_Hunter_Face");
        AttachFace(headGo.transform, hunterFaceMat);

        // 4. Nón Lá on Head
        GameObject nonLaAsset = LoadModel("NonLa.obj");
        if (nonLaAsset != null)
        {
            GameObject nonLaInst = (GameObject)PrefabUtility.InstantiatePrefab(nonLaAsset, sHead.transform);
            nonLaInst.name = "NonLa";
            nonLaInst.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            nonLaInst.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            nonLaInst.transform.localScale = Vector3.one;

            var nonLaMat = GetMat("Mat_NonLa");
            ApplyMaterial(nonLaInst, nonLaMat);
        }

        // 5. Medicine Pouch slung around torso
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

        // 6. Swipe VFX
        GameObject swipeVfx = new GameObject("SwipeVFX");
        swipeVfx.transform.SetParent(sHandR.transform, false);
        swipeVfx.SetActive(false);

        // 7. Foot Sockets & Procedural Stepping Motor
        var robeMat = GetMat("Mat_Hunter_Robe") ?? GetMat("Mat_Hunter");
        GameObject sFootL = AttachFoot(root.transform, "Socket_Foot_L", new Vector3(-0.16f, 0.08f, 0.04f), robeMat);
        GameObject sFootR = AttachFoot(root.transform, "Socket_Foot_R", new Vector3(0.16f, 0.08f, 0.04f), robeMat);

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
                rootGo.name.Contains("ThayThuoc"))
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        // Instantiate
        var motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Mother.prefab");
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Child.prefab");
        var hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Hunter.prefab");

        // 1. Người Đứng Đầu (Đầu Đàn)
        if (motherPrefab != null)
        {
            var m = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab);
            m.name = "DauDan (Người Đứng Đầu / Đầu Đàn)";
            m.transform.position = new Vector3(0f, 0f, 0f);
            m.transform.rotation = Quaternion.identity;
        }

        // 2. Đoàn Rồng Rắn nối đuôi nhau (Tay chạm và bám chặt lưng/hông bạn đi trước)
        if (childPrefab != null)
        {
            // Khoảng cách xấp xỉ 0.68m để đôi tay vươn dài (z=0.38) chạm khớp vào lưng bạn phía trước
            var c1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c1.name = "KhucThan 1 (Đoàn Rồng Rắn)";
            c1.transform.position = new Vector3(0f, 0f, -0.68f);
            var f1 = c1.GetComponent<ChildFollower>();
            if (f1 != null) f1.Initialize(0, false, null);

            var c2 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c2.name = "KhucThan 2 (Đoàn Rồng Rắn)";
            c2.transform.position = new Vector3(0f, 0f, -1.36f);
            var f2 = c2.GetComponent<ChildFollower>();
            if (f2 != null) f2.Initialize(1, false, null);

            var c3 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c3.name = "KhucDuoi (Bé Út / Con Út)";
            c3.transform.position = new Vector3(0f, 0f, -2.04f);
            var f3 = c3.GetComponent<ChildFollower>();
            if (f3 != null)
            {
                f3.Initialize(2, true, null);
                var conUtMat = GetMat("Mat_ConUt");
                var body = c3.transform.Find("Model_Body");
                if (body != null && conUtMat != null) ApplyMaterial(body.gameObject, conUtMat);
            }
        }

        // 3. Thầy Thuốc (Đối diện Đầu Đàn)
        if (hunterPrefab != null)
        {
            var h = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab);
            h.name = "ThayThuoc (Thầy Thuốc)";
            h.transform.position = new Vector3(0f, 0f, 3.2f);
            h.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // 4. Căn góc Camera góc nghiêng 3/4 nhìn rõ toàn cảnh đối đầu & khuôn mặt
        var camGo = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camGo.transform.position = new Vector3(-3.4f, 1.7f, 0.6f);
            camGo.transform.LookAt(new Vector3(0f, 0.85f, 0.6f));
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Speckit 001] Updated Showcase scene with full faces and accessories: {scenePath}");
    }
}
#endif
