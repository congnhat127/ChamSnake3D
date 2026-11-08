#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RongRanChamCham;

public static class SetupCorePlayground
{
    private const string ScenesDir = "Assets/_Game/Scenes";
    private const string PrefabsDir = "Assets/_Game/Prefabs";
    private const string SettingsDir = "Assets/_Game/Settings";
    private const string MaterialsDir = "Assets/_Game/Materials/Characters";

    [MenuItem("Tools/Rong Ran/Setup Core Playground (Spec 001-core)")]
    public static void Execute()
    {
        Debug.Log("[Speckit 001-core] Setting up Core Playground Physics Sandbox (Vietnam & Korea Fusion)...");

        // 0. Ensure Chibi Prefabs are freshly baked with feet and expressive faces
        SetupChibiPrefabs.Execute();

        EnsureDir(SettingsDir);
        var config = EnsureGameBalanceConfig();

        string scenePath = $"{ScenesDir}/Playground_CorePhysics.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Clean previous entities
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name.Contains("Arena") || rootGo.name.Contains("Mother") || 
                rootGo.name.Contains("Hunter") || rootGo.name.Contains("Child") || 
                rootGo.name.Contains("MANAGERS") || rootGo.name.Contains("SnakeChain") ||
                rootGo.name.Contains("DauDan") || rootGo.name.Contains("ThayThuoc"))
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        // 1. Arena Environment (Vietnam & Korea Cultural Fusion)
        var arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Environment/Arena.prefab");
        if (arenaPrefab != null)
        {
            var arenaInst = (GameObject)PrefabUtility.InstantiatePrefab(arenaPrefab);
            arenaInst.name = "Arena";
            arenaInst.transform.position = Vector3.zero;
            arenaInst.transform.rotation = Quaternion.identity;

            // Apply Vietnam-Korea Cultural Fusion Ground Material
            var groundTrans = arenaInst.transform.Find("Ground");
            var vkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/Mat_Arena_Vietnam_Korea.mat");
            if (groundTrans != null && vkMat != null)
            {
                var rend = groundTrans.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = vkMat;
            }

            // Decorate Posts with Alternating Hoi An & Cheongsachorong Lanterns
            var fence = arenaInst.transform.Find("Boundary_Fence");
            var hoianModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/Environment/Lantern_HoiAn.obj");
            var koreaModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/Environment/Lantern_Cheongsachorong.obj");
            var hoianMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/Mat_Lantern_Vietnam.mat");
            var koreaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Environment/Mat_Lantern_Korea.mat");

            if (fence != null)
            {
                for (int i = 0; i < 24; i++)
                {
                    var post = fence.Find($"Post_{i}");
                    if (post != null)
                    {
                        bool isHoiAn = (i % 2 == 0);
                        var lanternAsset = isHoiAn ? hoianModel : koreaModel;
                        var lanternMat = isHoiAn ? hoianMat : koreaMat;

                        if (lanternAsset != null)
                        {
                            var lanternInst = (GameObject)PrefabUtility.InstantiatePrefab(lanternAsset, post);
                            lanternInst.name = isHoiAn ? "Lantern_HoiAn_VN" : "Lantern_Cheongsachorong_KR";
                            Vector3 dirToCenter = (Vector3.zero - post.position).normalized;
                            lanternInst.transform.localPosition = new Vector3(0f, 2.2f, 0f) + (post.InverseTransformDirection(dirToCenter) * 0.45f);
                            lanternInst.transform.localRotation = Quaternion.identity;
                            lanternInst.transform.localScale = Vector3.one * 1.35f;

                            foreach (var r in lanternInst.GetComponentsInChildren<Renderer>(true))
                            {
                                r.sharedMaterial = lanternMat;
                            }
                        }
                    }
                }
            }
        }

        // 2. Managers (Touch Input & OnGUI Virtual Controls)
        GameObject managersGo = new GameObject("--- MANAGERS ---");
        var inputMgr = managersGo.AddComponent<TouchInputManager>();

        // 3. Mother (Đầu Đàn)
        var motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Characters/Mother.prefab");
        GameObject motherInst = null;
        MotherController motherCtrl = null;
        if (motherPrefab != null)
        {
            motherInst = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab);
            motherInst.name = "Mother (Dau Dan)";
            motherInst.transform.position = Vector3.zero;
            motherInst.transform.rotation = Quaternion.identity;
            motherCtrl = motherInst.GetComponent<MotherController>();

            if (motherCtrl != null)
            {
                var so = new SerializedObject(motherCtrl);
                so.FindProperty("balanceConfig").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 4. Snake Chain System (3 Children)
        var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Characters/Child.prefab");
        GameObject chainGo = new GameObject("SnakeChain_System");
        var chainIK = chainGo.AddComponent<SnakeChainIK>();
        List<ChildFollower> childList = new List<ChildFollower>();
        ChildFollower conUtFollower = null;

        if (childPrefab != null && motherInst != null)
        {
            float spacing = 0.68f;

            // Con 1
            var c1 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c1.name = "KhucThan 1 (Doan Rong Ran)";
            c1.transform.position = new Vector3(0f, 0f, -spacing);
            var f1 = c1.GetComponent<ChildFollower>();
            if (f1 != null)
            {
                f1.Initialize(0, false, motherInst.transform);
                childList.Add(f1);
            }

            // Con 2
            var c2 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c2.name = "KhucThan 2 (Doan Rong Ran)";
            c2.transform.position = new Vector3(0f, 0f, -spacing * 2f);
            var f2 = c2.GetComponent<ChildFollower>();
            if (f2 != null)
            {
                f2.Initialize(1, false, c1.transform);
                childList.Add(f2);
            }

            // Khúc Đuôi (Con Út đội vương miện)
            var c3 = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
            c3.name = "KhucDuoi (Con Ut)";
            c3.transform.position = new Vector3(0f, 0f, -spacing * 3f);
            var f3 = c3.GetComponent<ChildFollower>();
            if (f3 != null)
            {
                f3.Initialize(2, true, c2.transform);
                conUtFollower = f3;
                childList.Add(f3);

                var conUtMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Mat_ConUt.mat");
                var body = c3.transform.Find("Model_Body");
                if (body != null && conUtMat != null)
                {
                    foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterial = conUtMat;
                    }
                }
            }

            // Link references into SnakeChainIK
            var chainSO = new SerializedObject(chainIK);
            chainSO.FindProperty("motherRoot").objectReferenceValue = motherInst.transform;
            chainSO.FindProperty("balanceConfig").objectReferenceValue = config;
            chainSO.FindProperty("paddingBetweenChildren").floatValue = 0.05f;

            var childrenProp = chainSO.FindProperty("children");
            childrenProp.ClearArray();
            for (int i = 0; i < childList.Count; i++)
            {
                childrenProp.InsertArrayElementAtIndex(i);
                childrenProp.GetArrayElementAtIndex(i).objectReferenceValue = childList[i];
            }
            chainSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5. Hunter (Thầy Thuốc)
        var hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Characters/Hunter.prefab");
        GameObject hunterInst = null;
        if (hunterPrefab != null)
        {
            hunterInst = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab);
            hunterInst.name = "Hunter (Thay Thuoc)";
            hunterInst.transform.position = new Vector3(0f, 0f, 4.5f);
            hunterInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var hunterAI = hunterInst.GetComponent<HunterAI>();
            if (hunterAI != null)
            {
                var hSO = new SerializedObject(hunterAI);
                hSO.FindProperty("balanceConfig").objectReferenceValue = config;
                if (motherInst != null) hSO.FindProperty("motherTransform").objectReferenceValue = motherInst.transform;
                if (conUtFollower != null) hSO.FindProperty("conUtTarget").objectReferenceValue = conUtFollower;
                hSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Connect hunterTarget into MotherController
            if (motherCtrl != null)
            {
                var mSO = new SerializedObject(motherCtrl);
                mSO.FindProperty("hunterTarget").objectReferenceValue = hunterInst.transform;
                mSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 6. Camera Follow System
        var camGo = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
        if (camGo != null)
        {
            var camFollow = camGo.GetComponent<CameraFollow>() ?? camGo.AddComponent<CameraFollow>();
            camFollow.SetTargets(
                motherInst != null ? motherInst.transform : null,
                conUtFollower != null ? conUtFollower.transform : null,
                hunterInst != null ? hunterInst.transform : null
            );
            camGo.transform.position = new Vector3(0f, 16f, -12f);
            camGo.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }

        // 7. Directional Light
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var lightComp = lightGo.GetComponent<Light>();
            if (lightComp != null)
            {
                lightComp.intensity = 1.25f;
                lightComp.color = new Color(1f, 0.96f, 0.90f); // Warm sun
            }
        }

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Speckit 001-core] Complete! Playground_CorePhysics.unity is now fully rigged and playable: {scenePath}");
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

    private static GameBalanceConfig EnsureGameBalanceConfig()
    {
        string assetPath = $"{SettingsDir}/GameBalanceConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            config.arenaRadius = 16f;
            config.motherNormalSpeed = 6.5f;
            config.motherStrafeSpeed = 5.0f;
            config.motherTurnSpeed = 12f;
            config.hunterNormalSpeed = 6.5f;
            config.hunterFlankSpeed = 6.5f;
            config.hunterGrabRange = 1.6f;
            config.hunterGrabWindup = 0.25f;
            config.hunterGrabCooldown = 0.5f;
            config.childCount = 3;
            config.chainLinkDistance = 0.68f;
            config.chainFollowSpeed = 18.0f;
            config.chainMaxStretch = 1.2f;

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Speckit 001-core] Created GameBalanceConfig.asset: {assetPath}");
        }
        return config;
    }
}
#endif
