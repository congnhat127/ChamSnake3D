#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: Rồng Rắn > Dựng Màn 1 … 5
// Cần cài gói glTFast (com.unity.cloud.gltfast) để Unity đọc được các file .glb trong Assets/RongRan/Models.
public static class RongRanSetup
{
    const string Root = "Assets/RongRan";

    [MenuItem("Rồng Rắn/Dựng Màn 1 – Sân đình")] static void M1() { Build(1); }
    [MenuItem("Rồng Rắn/Dựng Màn 2 – Bukchon")] static void M2() { Build(2); }
    [MenuItem("Rồng Rắn/Dựng Màn 3 – Hội An")] static void M3() { Build(3); }
    [MenuItem("Rồng Rắn/Dựng Màn 4 – Gyeongbokgung")] static void M4() { Build(4); }
    [MenuItem("Rồng Rắn/Dựng Màn 5 – Cầu Rồng")] static void M5() { Build(5); }
    [MenuItem("Rồng Rắn/Dựng & Lưu cả 5 Màn")] public static void BuildAndSaveAllScenes()
    {
        for (int i = 1; i <= 5; i++)
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Build(i);
            string scenePath = $"Assets/_Game/Scenes/Man{i}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[Rồng Rắn] Đã tạo và lưu thành công {scenePath}!");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void Build(int n)
    {
        var cfgAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Configs/Man" + n + ".json");
        if (cfgAsset == null) { EditorUtility.DisplayDialog("Rồng Rắn", "Không tìm thấy " + Root + "/Configs/Man" + n + ".json", "OK"); return; }
        var cfg = JsonUtility.FromJson<RRLevelConfig>(cfgAsset.text);

        foreach (var nm in new[] { "RR_Level", "RR_GameManager", "RR_Lights", "RR_FX" }) { var o = GameObject.Find(nm); if (o) Object.DestroyImmediate(o); }

        var root = new GameObject("RR_Level").transform;
        var lv = Inst("Man" + n + "_BoiCanh.glb", "BoiCanh", root);
        if (lv == null) return;

        // Tự nhận hướng trục của bộ đọc glTF bằng hai điểm mốc trong mô hình
        float sx = -1f, sz = 1f;
        var mx = RongRanGame.FindDeep(lv.transform, "RR_Marker_PlusX");
        var mz = RongRanGame.FindDeep(lv.transform, "RR_Marker_PlusZ");
        if (mx) sx = Mathf.Sign(mx.position.x);
        if (mz) sz = Mathf.Sign(mz.position.z);

        string suf = cfg.kr ? "_KR" : "_VN";
        var chars = new GameObject("NhanVat").transform; chars.SetParent(root, false);
        var doctor = Inst("ThayThuoc.glb", "ThayThuoc", chars);
        var mother = Inst("Me" + suf + ".glb", "Me_DauRong", chars);
        var kids = new Transform[3];
        for (int i = 0; i < 3; i++) { var k = Inst("Con" + (i + 1) + suf + ".glb", "Con_" + (i + 1), chars); kids[i] = k ? k.transform : null; }

        var moverList = new List<Transform>();
        if (cfg.movers != null && cfg.movers.Length > 0)
        {
            var mv = new GameObject("VatCanDiDong").transform; mv.SetParent(root, false);
            foreach (var m in cfg.movers)
            {
                string file = m.kind == "buggy" ? "XeDien" + (m.index + 1) + ".glb" : "CauTuyet.glb";
                var g = Inst(file, m.kind == "buggy" ? "XeDien" : "CauTuyet", mv);
                moverList.Add(g ? g.transform : null);
            }
        }

        // Camera chéo 55°, khung ngang
        var cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
        cam.transform.position = new Vector3(0, 21f, 16.5f * sz);
        cam.transform.LookAt(new Vector3(0, 0, -2.2f * sz));
        cam.fieldOfView = 36f; cam.farClipPlane = 220f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = H(cfg.bg);

        // Ánh sáng
        Light sun = null;
        foreach (var l in Object.FindObjectsOfType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null) { var lg = new GameObject("Directional Light"); sun = lg.AddComponent<Light>(); sun.type = LightType.Directional; }
        var sp = new Vector3(cfg.sunX * sx, cfg.sunY, cfg.sunZ * sz);
        sun.transform.rotation = Quaternion.LookRotation(-sp.normalized);
        sun.color = H(cfg.sun); sun.intensity = cfg.sunI * 1.1f; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = H(cfg.sky) * cfg.hemi * 1.3f;
        RenderSettings.ambientEquatorColor = Color.Lerp(H(cfg.sky), H(cfg.ground), 0.5f) * cfg.hemi;
        RenderSettings.ambientGroundColor = H(cfg.ground) * cfg.hemi * 0.8f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = H(cfg.bg);
        RenderSettings.fogStartDistance = 38f; RenderSettings.fogEndDistance = 90f;

        if (cfg.lights != null && cfg.lights.Length > 0)
        {
            var lr = new GameObject("RR_Lights").transform;
            foreach (var p in cfg.lights)
            {
                var pl = new GameObject("DenPoint").AddComponent<Light>();
                pl.type = LightType.Point; pl.color = H(p.color); pl.range = 12f; pl.intensity = 1.6f;
                pl.transform.SetParent(lr, false); pl.transform.position = new Vector3(p.x * sx, p.y, p.z * sz);
            }
        }

        // Hiệu ứng
        var fx = new GameObject("RR_FX").transform;
        if (cfg.fx == "leaves")
        {
            string[] cols = { "D9572E", "E0733A", "C8442C", "E89A3A" };
            for (int i = 0; i < 46; i++) { var l = Prim(PrimitiveType.Cube, fx, new Vector3(0.2f, 0.13f, 0.01f), FxMat(cols[i % 4], false)); l.AddComponent<LeafFall>(); }
        }
        if (cfg.fx == "snow")
            for (int i = 0; i < 220; i++) { var s = Prim(PrimitiveType.Sphere, fx, Vector3.one * 0.12f, FxMat("FFFFFF", false)); s.AddComponent<SnowFall>(); }
        if (cfg.fx == "fire")
        {
            var origin = new Vector3(cfg.fireX * sx, cfg.fireY, cfg.fireZ * sz);
            var dir = new Vector3(cfg.fireDX * sx, cfg.fireDY, cfg.fireDZ * sz).normalized;
            for (int i = 0; i < 36; i++)
            {
                var f = Prim(PrimitiveType.Sphere, fx, Vector3.one * 0.6f, FxMat(i % 3 == 0 ? "FFD36B" : "FF7A1A", true));
                var fp = f.AddComponent<FireParticle>(); fp.origin = origin; fp.direction = dir; fp.k = i / 36f;
            }
        }

        // Bộ điều khiển chơi thử
        var gm = new GameObject("RR_GameManager").AddComponent<RongRanGame>();
        gm.configJson = cfgAsset; gm.cam = cam; gm.sx = sx; gm.sz = sz;
        gm.mother = mother ? mother.transform : null; gm.doctor = doctor ? doctor.transform : null;
        gm.kids = kids; gm.movers = moverList.ToArray();

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root.gameObject;
        Debug.Log("[Rồng Rắn] Đã dựng " + cfg.name + ". Nhớ lưu scene (Ctrl+S).");
    }

    static GameObject Inst(string file, string name, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + file);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Rồng Rắn", "Không đọc được " + file + ".\nKiểm tra đã cài gói glTFast và chép thư mục Models vào " + Root + " chưa.", "OK");
            return null;
        }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.name = name; g.transform.SetParent(parent, false);
        return g;
    }

    static GameObject Prim(PrimitiveType t, Transform parent, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(t);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localScale = scale;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
        return g;
    }

    static readonly Dictionary<string, Material> fxMats = new Dictionary<string, Material>();
    static Material FxMat(string hex, bool emissive)
    {
        string key = hex + (emissive ? "_e" : "");
        if (fxMats.TryGetValue(key, out var m) && m) return m;
        if (!AssetDatabase.IsValidFolder(Root + "/FXMaterials")) AssetDatabase.CreateFolder(Root, "FXMaterials");
        string path = Root + "/FXMaterials/FX_" + key + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh); AssetDatabase.CreateAsset(m, path);
        }
        Color c = H(hex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (emissive && m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.5f); }
        EditorUtility.SetDirty(m);
        fxMats[key] = m;
        return m;
    }

    static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
}
#endif
