#if UNITY_EDITOR
using System.Collections.Generic;
using RongRanChamCham;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đọc Assets/RongRan/Configs/ManX.json (hệ toạ độ thiết kế) và ghi thành component ArenaDefinition
/// (toạ độ world) trong scene của từng màn, kèm model vật cản di động.
/// </summary>
public static class BakeArenaDefinitions
{
    private const string ConfigsDir = "Assets/RongRan/Configs";
    private const string ModelsDir = "Assets/RongRan/Models";
    private const string ScenesDir = "Assets/_Game/Scenes";
    private const string DefinitionObjectName = "Arena_Definition";
    private const string MoversRootName = "Arena_Movers";
    /// <summary>Hướng mặt trời: chiếu từ phía camera (camera đứng ở +Z nhìn về -Z) chếch sang một bên, để mặt nhân vật được chiếu sáng.</summary>
    public static readonly Vector3 SunEuler = new Vector3(50f, 145f, 0f);

    // Tỉ lệ thu nhỏ đường xuất phát so với mép sân, và vị trí Thầy Thuốc: lấy nguyên từ RongRanGame.ResetActors.
    private const float RectSpawnScale = 0.82f;
    private static readonly Vector2 RectHunterSpawnDesign = new Vector2(8.6f, 1.5f);

    [MenuItem("Rồng Rắn/Bake Biên Sân & Vật Cản Vào Các Màn")]
    public static void BakeAllScenes()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[BakeArenaDefinitions] Hãy thoát Play Mode trước khi bake.");
            return;
        }

        string previousScene = SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        BakeScene("Gameplay", 1);
        for (int level = 1; level <= 5; level++) BakeScene("Man" + level, level);

        if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        Debug.Log("[BakeArenaDefinitions] Đã bake biên sân và vật cản cho Gameplay + Man1..Man5.");
    }

    public static void BakeScene(string sceneName, int level)
    {
        string path = $"{ScenesDir}/{sceneName}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            Debug.LogWarning($"[BakeArenaDefinitions] Không thấy scene {path}, bỏ qua.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        BakeIntoActiveScene(level);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>Tạo lại Arena_Definition và Arena_Movers trong scene đang mở từ cấu hình của màn <paramref name="level"/>.</summary>
    public static ArenaDefinition BakeIntoActiveScene(int level)
    {
        var cfgAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{ConfigsDir}/Man{level}.json");
        if (cfgAsset == null)
        {
            Debug.LogError($"[BakeArenaDefinitions] Không thấy {ConfigsDir}/Man{level}.json");
            return null;
        }
        RRLevelConfig cfg = JsonUtility.FromJson<RRLevelConfig>(cfgAsset.text);

        DestroyIfExists(DefinitionObjectName);
        DestroyIfExists(MoversRootName);

        // Hướng trục của bối cảnh sau khi import glTF: đọc từ hai điểm mốc trong model, giống RongRanSetup.
        float sx = -1f, sz = 1f;
        GameObject backdrop = GameObject.Find($"BoiCanh_Man{level}");
        if (backdrop != null)
        {
            Transform markerX = FindDeep(backdrop.transform, "RR_Marker_PlusX");
            Transform markerZ = FindDeep(backdrop.transform, "RR_Marker_PlusZ");
            if (markerX != null) sx = Mathf.Sign(markerX.position.x);
            if (markerZ != null) sz = Mathf.Sign(markerZ.position.z);
        }
        else
        {
            Debug.LogWarning($"[BakeArenaDefinitions] Scene không có BoiCanh_Man{level}; dùng hướng trục mặc định sx={sx}, sz={sz}.");
        }

        var go = new GameObject(DefinitionObjectName);
        var def = go.AddComponent<ArenaDefinition>();
        bool isEllipse = cfg.boundaryType == "ellipse";

        def.levelIndex = level;
        def.shape = isEllipse ? ArenaShape.Ellipse : ArenaShape.RoundRect;
        def.ellipseRadii = new Vector2(cfg.A, cfg.B);
        def.rectCenter = ToWorld(cfg.cx, cfg.cz, sx, sz);
        def.rectHalfExtents = new Vector2(cfg.hx, cfg.hz);
        def.rectCornerRadius = cfg.R;
        def.moverModelYaw = sz < 0f ? 180f : 0f;

        // Vật cản tĩnh
        var obstacles = new List<ArenaObstacle>();
        if (cfg.obstacles != null)
        {
            foreach (var o in cfg.obstacles)
            {
                obstacles.Add(new ArenaObstacle { center = ToWorld(o.x, o.z, sx, sz), radius = o.r });
            }
        }
        def.obstacles = obstacles.ToArray();

        // Vật cản di động
        var movers = new List<ArenaMover>();
        if (cfg.movers != null && cfg.movers.Length > 0)
        {
            var moversRoot = new GameObject(MoversRootName).transform;
            foreach (var m in cfg.movers)
            {
                bool isBuggy = m.kind == "buggy";
                string file = isBuggy ? $"XeDien{m.index + 1}.glb" : "CauTuyet.glb";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{file}");
                Transform visual = null;
                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, moversRoot);
                    inst.name = isBuggy ? $"XeDien_{movers.Count + 1}" : $"CauTuyet_{movers.Count + 1}";
                    visual = inst.transform;
                }
                else
                {
                    Debug.LogWarning($"[BakeArenaDefinitions] Không thấy model {file} cho vật cản di động.");
                }

                movers.Add(new ArenaMover
                {
                    visual = visual,
                    radius = m.r,
                    loop = m.hasLoop,
                    pointA = ToWorld(m.ax, m.az, sx, sz),
                    pointB = ToWorld(m.bx, m.bz, sx, sz),
                    loopCenter = ToWorld(m.lcx, m.lcz, sx, sz),
                    loopRadii = new Vector2(m.lrx * sx, m.lrz * sz),
                    speed = m.speed,
                    pause = m.pause,
                    cycleOffset01 = m.t0,
                    loopPhase = m.ph,
                    loopDirection = Mathf.Approximately(m.dir, 0f) ? 1f : m.dir,
                    rolls = !isBuggy
                });
            }
        }
        def.movers = movers.ToArray();

        // Vị trí xuất phát. Màn elip (Màn 1) giữ vị trí mặc định sẵn có của GameManager;
        // màn chữ nhật bo góc lấy theo RongRanGame: Mẹ đứng trên đường elip thu nhỏ tại góc spawnA0.
        if (!isEllipse)
        {
            float a = cfg.spawnA0 * Mathf.Deg2Rad;
            float ax = cfg.hx * RectSpawnScale;
            float bz = cfg.hz * RectSpawnScale;
            def.motherSpawn = ToWorld(Mathf.Cos(a) * ax, cfg.cz + Mathf.Sin(a) * bz, sx, sz);
            // Đàn con xếp theo chiều góc tăng, nên Mẹ quay mặt theo tiếp tuyến chiều góc giảm.
            Vector2 forward = new Vector2(Mathf.Sin(a) * ax * sx, -Mathf.Cos(a) * bz * sz);
            def.motherSpawnForward = forward.normalized;
            def.hunterSpawn = ToWorld(RectHunterSpawnDesign.x, RectHunterSpawnDesign.y, sx, sz);
        }

        WireItemSpawner();
        GameObject sun = GameObject.Find("Directional Light");
        if (sun != null) sun.transform.rotation = Quaternion.Euler(SunEuler);
        EditorUtility.SetDirty(def);
        Debug.Log($"[BakeArenaDefinitions] Màn {level}: {def.shape}, {def.obstacles.Length} vật cản tĩnh, {def.movers.Length} vật cản di động, sx={sx}, sz={sz}.");
        return def;
    }

    /// <summary>Gán đủ bốn prefab món ăn cho ItemSpawner trong scene đang mở.</summary>
    private static void WireItemSpawner()
    {
        var spawner = Object.FindAnyObjectByType<ItemSpawner>();
        if (spawner == null) return;

        var so = new SerializedObject(spawner);
        string[,] map =
        {
            { "banhMiPrefab", "Food_BanhMi" },
            { "phoPrefab", "Food_Pho" },
            { "kimbapPrefab", "Food_Kimbap" },
            { "tteokbokkiPrefab", "Food_Tteokbokki" }
        };
        for (int i = 0; i < map.GetLength(0); i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Prefabs/Items/{map[i, 1]}.prefab");
            if (prefab != null) so.FindProperty(map[i, 0]).objectReferenceValue = prefab;
            else Debug.LogWarning($"[BakeArenaDefinitions] Không thấy prefab {map[i, 1]}.");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Vector2 ToWorld(float designX, float designZ, float sx, float sz)
    {
        return new Vector2(designX * sx, designZ * sz);
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
