#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using RongRanChamCham;

public static class SetupNewRongRanMaster
{
    private const string ModelsDir = "Assets/RongRan/Models";
    private const string ConfigsDir = "Assets/RongRan/Configs";
    private const string AnimsDir = "Assets/_Game/Animations/Characters";
    private const string PrefabsDir = "Assets/_Game/Prefabs";
    private const string MaterialsDir = "Assets/_Game/Materials";
    private const string ConfigPath = "Assets/_Game/Settings/GameBalanceConfig.asset";

    private static GameObject CreateSocket(Transform parent, string socketName, Vector3 localPos)
    {
        var go = new GameObject(socketName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go;
    }

    [MenuItem("Rồng Rắn/Tích Hợp Toàn Bộ Bối Cảnh & Nhân Vật Mới Vào Game")]
    public static void Execute()
    {
        Debug.Log("[SetupNewRongRanMaster] Bắt đầu tích hợp Bối cảnh 3D mới + Nhân vật mới + Logic + Chuyển động + Hiệu ứng...");

        // Ensure enhanced food prefabs are generated
        RongRanChamCham.Editor.BuildEnhancedFoodPrefabs.Execute();

        // Ensure animations & controllers are fully baked
        RongRanChamCham.Editor.GenerateCharacterAnimations.Execute();

        // Build for Gameplay.unity and Man1.unity
        BuildMasterScene("Assets/_Game/Scenes/Gameplay.unity", 1);
        BuildMasterScene("Assets/_Game/Scenes/Man1.unity", 1);

        // Also build Man2 .. Man5 with full systems!
        for (int i = 2; i <= 5; i++)
        {
            BuildMasterScene($"Assets/_Game/Scenes/Man{i}.unity", i);
        }

        // Open Gameplay.unity
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupNewRongRanMaster] HOÀN TẤT 100%! Toàn bộ 5 Màn chơi đã có Bối cảnh 3D mới, Nhân vật mới, Trang chủ, Nút bấm, Logic BO3, Chuyển động và Hiệu ứng!");
    }

    public static void BuildMasterScene(string scenePath, int levelNum)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cfgAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{ConfigsDir}/Man{levelNum}.json");
        RRLevelConfig cfg = cfgAsset != null ? JsonUtility.FromJson<RRLevelConfig>(cfgAsset.text) : new RRLevelConfig();
        bool isKr = cfg.kr;

        // 1. ÁNH SÁNG & MÔI TRƯỜNG DỊU MẮT, KHÔNG CHÓI LÓA (Chuẩn Mobile Chibi Art)
        var sunGo = new GameObject("Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = H("FFF8F0");
        sun.intensity = levelNum == 3 ? 0.52f : 0.65f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.35f; // Bóng đổ êm dịu 35%, tương phản ấm áp
        sunGo.transform.rotation = Quaternion.Euler(BakeArenaDefinitions.SunEuler);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        if (levelNum == 3)
        {
            // Hội An Night - Đèn lồng ấm áp
            RenderSettings.ambientSkyColor = H("4A5A70") * 0.40f;
            RenderSettings.ambientEquatorColor = H("584D40") * 0.35f;
            RenderSettings.ambientGroundColor = H("2B241E") * 0.25f;
        }
        else
        {
            // Ban ngày - Êm dịu, màu sắc no đủ, không bao giờ bị cháy sáng
            RenderSettings.ambientSkyColor = H("C5DDEF") * 0.28f;
            RenderSettings.ambientEquatorColor = H("E2DACF") * 0.22f;
            RenderSettings.ambientGroundColor = H("C0B6A8") * 0.16f;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = H("74A7C2");
        RenderSettings.fogStartDistance = 50f;
        RenderSettings.fogEndDistance = 150f;

        // Post-processing Volume: Neutral Tonemapping (Đặc trưng Chibi, độ tương phản sâu, không chói)
        var volGo = new GameObject("Global_Volume_PostProcess");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var tonemapping = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tonemapping.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.Neutral);
        var colorAdj = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        colorAdj.postExposure.Override(-0.15f); // Triệt tiêu hoàn toàn hiện tượng chói lóa
        colorAdj.contrast.Override(12f);
        colorAdj.saturation.Override(14f);
        vol.profile = profile;

        // 2. BỐI CẢNH 3D MỚI
        var arenaRoot = new GameObject("Arena_Environment_3D");
        string boiCanhFile = $"Man{levelNum}_BoiCanh.glb";
        var boiCanhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{boiCanhFile}");
        if (boiCanhPrefab != null)
        {
            var arenaInst = (GameObject)PrefabUtility.InstantiatePrefab(boiCanhPrefab, arenaRoot.transform);
            arenaInst.name = $"BoiCanh_Man{levelNum}";
            arenaInst.transform.position = Vector3.zero;
            arenaInst.transform.rotation = Quaternion.identity;

            // Add mesh colliders for grounds
            foreach (var mf in arenaInst.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.ToLower().Contains("ground") || mf.name.ToLower().Contains("san") || mf.name.ToLower().Contains("floor"))
                {
                    var col = mf.gameObject.GetComponent<MeshCollider>();
                    if (col == null) col = mf.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        // Add invisible ground collider if needed
        var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground_Physics_Plane";
        groundPlane.transform.SetParent(arenaRoot.transform, false);
        groundPlane.transform.position = Vector3.zero;
        groundPlane.transform.localScale = new Vector3(8f, 1f, 8f);
        var planeRend = groundPlane.GetComponent<MeshRenderer>();
        if (planeRend != null) planeRend.enabled = false; // invisible physics ground

        // 3. HIỆU ỨNG THỜI TIẾT TỪ DESIGN (Lá rơi / Tuyết rơi)
        var fxRoot = new GameObject("RR_FX_Environment");
        if (cfg.fx == "leaves")
        {
            string[] cols = { "D9572E", "E0733A", "C8442C", "E89A3A" };
            for (int i = 0; i < 40; i++)
            {
                var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
                l.name = "Leaf";
                UnityEngine.Object.DestroyImmediate(l.GetComponent<Collider>());
                l.transform.SetParent(fxRoot.transform, false);
                l.transform.localScale = new Vector3(0.2f, 0.12f, 0.02f);
                l.AddComponent<LeafFall>();
            }
        }
        else if (cfg.fx == "snow")
        {
            for (int i = 0; i < 150; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Snow";
                UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>());
                s.transform.SetParent(fxRoot.transform, false);
                s.transform.localScale = Vector3.one * 0.12f;
                s.AddComponent<SnowFall>();
            }
        }

        // 4. NHÂN VẬT MỚI: MẸ RỒNG (Me_VN.glb / Me_KR.glb)
        var balanceConfig = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(ConfigPath);

        var charRoot = new GameObject("Characters");
        var motherGo = new GameObject("Mother_RongRan");
        motherGo.tag = "Player";
        motherGo.transform.SetParent(charRoot.transform, false);
        motherGo.transform.position = new Vector3(0f, 0f, 0f);

        var cc = motherGo.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.85f, 0f);
        cc.height = 1.7f;
        cc.radius = 0.45f;

        var motherCtrl = motherGo.AddComponent<MotherController>();
        var motherProcAnim = motherGo.AddComponent<ChibiProceduralAnimator>();

        string motherFile = isKr ? "Me_KR.glb" : "Me_VN.glb";
        var motherModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{motherFile}");
        if (motherModelPrefab != null)
        {
            var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(motherModelPrefab, motherGo.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
        }

        // Animator for Mother
        var motherAnim = motherGo.AddComponent<Animator>();
        var ctrlMother = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimsDir}/Ctrl_Mother.controller");
        if (ctrlMother != null) motherAnim.runtimeAnimatorController = ctrlMother;
        motherAnim.applyRootMotion = false;

        // Buff Visual FX (Shield Bubble, Speed Ring)
        var motherBuffFX = motherGo.AddComponent<BuffVisualController>();

        // Defense visual aura
        var auraGo = new GameObject("Golden_Shield_Aura");
        auraGo.transform.SetParent(motherGo.transform, false);
        auraGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        auraGo.SetActive(false);

        // Sockets for Mother
        var sHeadM = CreateSocket(motherGo.transform, "Socket_Head", new Vector3(0f, 1.34f, 0f));
        var sHandLM = CreateSocket(motherGo.transform, "Socket_Hand_L", new Vector3(-1.32f, 0.82f, 0.22f));
        var sHandRM = CreateSocket(motherGo.transform, "Socket_Hand_R", new Vector3(1.32f, 0.82f, 0.22f));
        var sFootLM = CreateSocket(motherGo.transform, "Socket_Foot_L", new Vector3(-0.13f, 0.07f, 0.05f));
        var sFootRM = CreateSocket(motherGo.transform, "Socket_Foot_R", new Vector3(0.13f, 0.07f, 0.05f));

        var mCtrlSO = new SerializedObject(motherCtrl);
        mCtrlSO.FindProperty("socketHead").objectReferenceValue = sHeadM.transform;
        mCtrlSO.FindProperty("socketHandL").objectReferenceValue = sHandLM.transform;
        mCtrlSO.FindProperty("socketHandR").objectReferenceValue = sHandRM.transform;
        mCtrlSO.FindProperty("lockAuraVisual").objectReferenceValue = auraGo;
        mCtrlSO.FindProperty("balanceConfig").objectReferenceValue = balanceConfig;
        mCtrlSO.ApplyModifiedPropertiesWithoutUndo();

        var mAnimSO = new SerializedObject(motherProcAnim);
        mAnimSO.FindProperty("socketFootL").objectReferenceValue = sFootLM.transform;
        mAnimSO.FindProperty("socketFootR").objectReferenceValue = sFootRM.transform;
        mAnimSO.FindProperty("socketHandL").objectReferenceValue = sHandLM.transform;
        mAnimSO.FindProperty("socketHandR").objectReferenceValue = sHandRM.transform;
        var mBody = motherGo.transform.Find("Model_Body");
        if (mBody != null) mAnimSO.FindProperty("bodyVisual").objectReferenceValue = mBody;
        mAnimSO.ApplyModifiedPropertiesWithoutUndo();

        // 5. ĐÀN CON MỚI (Con1_VN, Con2_VN, Con3_VN với Vương Miện Con Út)
        var chainGo = new GameObject("SnakeChain_System");
        chainGo.transform.SetParent(charRoot.transform, false);
        var snakeChain = chainGo.AddComponent<SnakeChainIK>();

        List<ChildFollower> childList = new List<ChildFollower>();
        Transform prevLeader = motherGo.transform;
        float spacing = 0.88f;

        for (int i = 0; i < 3; i++)
        {
            string conFile = isKr ? $"Con{i + 1}_KR.glb" : $"Con{i + 1}_VN.glb";
            var conPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{conFile}");

            var childGo = new GameObject(i == 2 ? "KhucDuoi_ConUt" : $"KhucThan_{i + 1}");
            childGo.transform.SetParent(chainGo.transform, false);
            childGo.transform.position = new Vector3(0f, 0f, -spacing * (i + 1));

            var follower = childGo.AddComponent<ChildFollower>();
            var procAnim = childGo.AddComponent<ChibiProceduralAnimator>();

            var ccChild = childGo.GetComponent<CharacterController>();
            if (ccChild != null)
            {
                ccChild.center = new Vector3(0f, 0.65f, 0f);
                ccChild.height = 1.3f;
                ccChild.radius = 0.32f;
                ccChild.minMoveDistance = 0.001f;
            }

            if (conPrefab != null)
            {
                var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(conPrefab, childGo.transform);
                modelInst.name = "Model_Body";
                modelInst.transform.localPosition = Vector3.zero;
                modelInst.transform.localRotation = Quaternion.identity;
            }

            var childAnim = childGo.AddComponent<Animator>();
            var ctrlChild = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimsDir}/Ctrl_Child.controller");
            if (ctrlChild != null) childAnim.runtimeAnimatorController = ctrlChild;
            childAnim.applyRootMotion = false;

            // Sockets for Child
            var sHeadC = CreateSocket(childGo.transform, "Socket_Head", new Vector3(0f, 0.78f, 0f));
            var sCrownC = CreateSocket(childGo.transform, "Socket_Crown", new Vector3(0f, 1.00f, 0f));
            var sHandLC = CreateSocket(childGo.transform, "Socket_Hand_L", new Vector3(-0.25f, 0.45f, 0.08f));
            var sHandRC = CreateSocket(childGo.transform, "Socket_Hand_R", new Vector3(0.25f, 0.45f, 0.08f));
            var sFootLC = CreateSocket(childGo.transform, "Socket_Foot_L", new Vector3(-0.10f, 0.05f, 0.03f));
            var sFootRC = CreateSocket(childGo.transform, "Socket_Foot_R", new Vector3(0.10f, 0.05f, 0.03f));

            var cFollowSO = new SerializedObject(follower);
            cFollowSO.FindProperty("socketHead").objectReferenceValue = sHeadC.transform;
            cFollowSO.FindProperty("socketCrown").objectReferenceValue = sCrownC.transform;
            cFollowSO.ApplyModifiedPropertiesWithoutUndo();

            var cAnimSO = new SerializedObject(procAnim);
            cAnimSO.FindProperty("socketFootL").objectReferenceValue = sFootLC.transform;
            cAnimSO.FindProperty("socketFootR").objectReferenceValue = sFootRC.transform;
            cAnimSO.FindProperty("socketHandL").objectReferenceValue = sHandLC.transform;
            cAnimSO.FindProperty("socketHandR").objectReferenceValue = sHandRC.transform;
            var cBody = childGo.transform.Find("Model_Body");
            if (cBody != null) cAnimSO.FindProperty("bodyVisual").objectReferenceValue = cBody;
            cAnimSO.ApplyModifiedPropertiesWithoutUndo();

            // Con Út gets the Crown & Shield FX
            bool isUt = (i == 2);
            if (isUt)
            {
                var crownAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Accessories/Crown_ConUt.prefab");
                if (crownAsset != null)
                {
                    var crownInst = (GameObject)PrefabUtility.InstantiatePrefab(crownAsset, childGo.transform);
                    crownInst.name = "Crown_ConUt";
                    crownInst.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                    crownInst.transform.localScale = Vector3.one * 0.8f;
                }

                var utBuffFX = childGo.AddComponent<BuffVisualController>();
                var utBuffSO = new SerializedObject(utBuffFX);
                utBuffSO.FindProperty("isMother").boolValue = false;
                utBuffSO.FindProperty("isConUt").boolValue = true;
                utBuffSO.ApplyModifiedPropertiesWithoutUndo();
            }

            follower.Initialize(i, isUt, prevLeader);
            prevLeader = childGo.transform;
            childList.Add(follower);
        }

        // Link SnakeChainIK
        var snakeSO = new SerializedObject(snakeChain);
        snakeSO.FindProperty("motherRoot").objectReferenceValue = motherGo.transform;
        snakeSO.FindProperty("balanceConfig").objectReferenceValue = balanceConfig;
        var childrenProp = snakeSO.FindProperty("children");
        childrenProp.ClearArray();
        for (int i = 0; i < childList.Count; i++)
        {
            childrenProp.InsertArrayElementAtIndex(i);
            childrenProp.GetArrayElementAtIndex(i).objectReferenceValue = childList[i];
        }
        snakeSO.ApplyModifiedPropertiesWithoutUndo();
        snakeChain.ResetFormationBehindMother();

        // 6. THẦY THUỐC MỚI (ThayThuoc.glb)
        var hunterGo = new GameObject("Hunter_ThayThuoc");
        hunterGo.transform.SetParent(charRoot.transform, false);
        hunterGo.transform.position = new Vector3(0f, 0f, 7.5f);
        hunterGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var ccHunter = hunterGo.AddComponent<CharacterController>();
        ccHunter.center = new Vector3(0f, 0.85f, 0f);
        ccHunter.height = 1.7f;
        ccHunter.radius = 0.45f;
        ccHunter.minMoveDistance = 0.001f;

        var hunterAI = hunterGo.AddComponent<HunterAI>();
        var hunterProcAnim = hunterGo.AddComponent<ChibiProceduralAnimator>();

        var thayThuocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/ThayThuoc.glb");
        if (thayThuocPrefab != null)
        {
            var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(thayThuocPrefab, hunterGo.transform);
            modelInst.name = "Model_Body";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
        }

        var hunterAnim = hunterGo.AddComponent<Animator>();
        var ctrlHunter = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimsDir}/Ctrl_Hunter.controller");
        if (ctrlHunter != null) hunterAnim.runtimeAnimatorController = ctrlHunter;
        hunterAnim.applyRootMotion = false;

        // Sockets for Hunter
        var sHeadH = CreateSocket(hunterGo.transform, "Socket_Head", new Vector3(0f, 1.38f, 0f));
        var sHandRH = CreateSocket(hunterGo.transform, "Socket_Hand_R", new Vector3(0.30f, 0.78f, 0.16f));

        // Pointer Wand for Thầy Thuốc (Gậy chỉ hướng tinh tế gắn trực tiếp ở tay phải)
        var wandGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wandGo.name = "Pointer_Wand";
        wandGo.transform.SetParent(sHandRH.transform, false);
        wandGo.transform.localPosition = new Vector3(0f, 0f, 0.20f);
        wandGo.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
        wandGo.transform.localScale = new Vector3(0.035f, 0.20f, 0.035f);
        var wandMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Characters/Mat_Crown.mat");
        if (wandMat != null) wandGo.GetComponent<Renderer>().sharedMaterial = wandMat;
        UnityEngine.Object.DestroyImmediate(wandGo.GetComponent<Collider>());

        var sFootLH = CreateSocket(hunterGo.transform, "Socket_Foot_L", new Vector3(-0.14f, 0.07f, 0.05f));
        var sFootRH = CreateSocket(hunterGo.transform, "Socket_Foot_R", new Vector3(0.14f, 0.07f, 0.05f));

        // Wire Hunter targets & sockets
        var hSO = new SerializedObject(hunterAI);
        hSO.FindProperty("motherTransform").objectReferenceValue = motherGo.transform;
        hSO.FindProperty("conUtTarget").objectReferenceValue = childList[childList.Count - 1];
        hSO.FindProperty("socketHead").objectReferenceValue = sHeadH.transform;
        hSO.FindProperty("socketGrabHand").objectReferenceValue = sHandRH.transform;
        hSO.FindProperty("balanceConfig").objectReferenceValue = balanceConfig;
        hSO.ApplyModifiedPropertiesWithoutUndo();

        var hAnimSO = new SerializedObject(hunterProcAnim);
        hAnimSO.FindProperty("socketFootL").objectReferenceValue = sFootLH.transform;
        hAnimSO.FindProperty("socketFootR").objectReferenceValue = sFootRH.transform;
        var hBody = hunterGo.transform.Find("Model_Body");
        if (hBody != null) hAnimSO.FindProperty("bodyVisual").objectReferenceValue = hBody;
        hAnimSO.ApplyModifiedPropertiesWithoutUndo();

        var mSO = new SerializedObject(motherCtrl);
        mSO.FindProperty("hunterTarget").objectReferenceValue = hunterGo.transform;
        mSO.ApplyModifiedPropertiesWithoutUndo();

        // 7. CAMERA CHÍNH THEO DÕI ĐOÀN RỒNG RẮN
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = H("74A7C2");
        cam.fieldOfView = 50f;

        var camData = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        var camFollow = camGo.AddComponent<CameraFollow>();
        camFollow.SetTargets(motherGo.transform, childList[childList.Count - 1].transform, hunterGo.transform);
        camGo.transform.position = new Vector3(0f, 19.5f, -13.0f);
        camGo.transform.rotation = Quaternion.Euler(54f, 0f, 0f);

        // 8. HỆ THỐNG CORE GAMEPLAY & UI
        var sysGo = new GameObject("--- GAME SYSTEMS ---");
        var inputMgr = sysGo.AddComponent<TouchInputManager>();
        var buffSystem = sysGo.AddComponent<BuffSystem>();
        var duelMgr = sysGo.AddComponent<ChamChamDuelManager>();
        var itemSpawner = sysGo.AddComponent<ItemSpawner>();
        var levelMgr = sysGo.AddComponent<LevelManager>();
        var gameMgr = sysGo.AddComponent<GameManager>();
        var hud = sysGo.AddComponent<GameplayHUD>();

        // Wire AudioManager with Audio Clips
        var audioMgr = sysGo.AddComponent<AudioManager>();
        var audioSO = new SerializedObject(audioMgr);
        audioSO.FindProperty("bgmGameplay").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/BGM/BGM_Gameplay.wav");
        audioSO.FindProperty("sfxPickup").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Pickup.wav");
        audioSO.FindProperty("sfxShieldBlock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Shield_Block.wav");
        audioSO.FindProperty("sfxWhoosh").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Whoosh.wav");
        audioSO.FindProperty("sfxVictory").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Victory.wav");
        audioSO.FindProperty("sfxDefeat").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Defeat.wav");
        audioSO.FindProperty("sfxChamBeat").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Cham_Beat.wav");
        audioSO.FindProperty("sfxDodge").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_Dodge.wav");
        audioSO.FindProperty("sfxUIClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/SFX_UI_Click.wav");
        audioSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire GameManager
        var gmSO = new SerializedObject(gameMgr);
        gmSO.FindProperty("mother").objectReferenceValue = motherCtrl;
        gmSO.FindProperty("snakeChain").objectReferenceValue = snakeChain;
        gmSO.FindProperty("hunter").objectReferenceValue = hunterAI;
        gmSO.FindProperty("cameraFollow").objectReferenceValue = camFollow;
        gmSO.FindProperty("duelManager").objectReferenceValue = duelMgr;
        gmSO.FindProperty("itemSpawner").objectReferenceValue = itemSpawner;
        gmSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ItemSpawner with food prefabs
        var spawnerSO = new SerializedObject(itemSpawner);
        var bmPref = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Items/Food_BanhMi.prefab");
        var phoPref = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Items/Food_Pho.prefab");
        var kimPref = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Items/Food_Kimbap.prefab");
        var tteokPref = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Items/Food_Tteokbokki.prefab");
        if (bmPref != null) spawnerSO.FindProperty("banhMiPrefab").objectReferenceValue = bmPref;
        if (phoPref != null) spawnerSO.FindProperty("phoPrefab").objectReferenceValue = phoPref;
        if (kimPref != null) spawnerSO.FindProperty("kimbapPrefab").objectReferenceValue = kimPref;
        if (tteokPref != null) spawnerSO.FindProperty("tteokbokkiPrefab").objectReferenceValue = tteokPref;
        spawnerSO.FindProperty("spawnCloneFood").boolValue = true;
        spawnerSO.ApplyModifiedPropertiesWithoutUndo();

        // 9. BIÊN SÂN, VẬT CẢN, VỊ TRÍ XUẤT PHÁT CỦA MÀN
        BakeArenaDefinitions.BakeIntoActiveScene(levelNum);

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[SetupNewRongRanMaster] Saved: {scenePath} (Level {levelNum}: {cfg.name})");
    }

    private static Color H(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
        return Color.white;
    }
}
#endif
