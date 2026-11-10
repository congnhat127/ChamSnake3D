#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RenderCharacterSnapshots
{
    private const string ArtifactDir = @"C:\Users\laptop\.gemini\antigravity\brain\b26809de-9d0d-4b2d-a0d9-21f7b579c32c";
    private const string WebGLOutputDir = @"D:\Projects\RongRanChamCham3D\Builds\WebGL_Netlify\TemplateData";
    private static readonly Vector3 StudioOrigin = new Vector3(0f, 600f, 0f);

    [MenuItem("Tools/Rong Ran/Render Character Snapshots")]
    public static void Execute()
    {
        Debug.Log("[RenderCharacterSnapshots] Starting isolated character studio rendering...");

        Directory.CreateDirectory(ArtifactDir);
        Directory.CreateDirectory(WebGLOutputDir);

        // 1. Create completely isolated studio far above everything (Y = 600)
        GameObject studioRoot = new GameObject("Temp_Studio_Isolated_Root");
        studioRoot.transform.position = StudioOrigin;

        // Key Light (Soft Warm Studio Key)
        GameObject keyLightGo = new GameObject("Studio_KeyLight");
        keyLightGo.transform.SetParent(studioRoot.transform, false);
        Light keyLight = keyLightGo.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 0.85f;
        keyLight.color = new Color(1.0f, 0.98f, 0.94f);
        keyLightGo.transform.rotation = Quaternion.Euler(26f, -32f, 0f);

        // Fill Light (Gentle Cool Studio Fill)
        GameObject fillLightGo = new GameObject("Studio_FillLight");
        fillLightGo.transform.SetParent(studioRoot.transform, false);
        Light fillLight = fillLightGo.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.35f;
        fillLight.color = new Color(0.86f, 0.92f, 1.0f);
        fillLightGo.transform.rotation = Quaternion.Euler(15f, 145f, 0f);

        // Rim Light (Golden Silhouette Rim)
        GameObject rimLightGo = new GameObject("Studio_RimLight");
        rimLightGo.transform.SetParent(studioRoot.transform, false);
        Light rimLight = rimLightGo.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.intensity = 0.25f;
        rimLight.color = new Color(1.0f, 0.95f, 0.82f);
        rimLightGo.transform.rotation = Quaternion.Euler(-20f, 180f, 0f);

        // Studio Pedestal (Clean circular platform)
        GameObject pedestalGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestalGo.name = "Studio_Pedestal";
        pedestalGo.transform.SetParent(studioRoot.transform, false);
        pedestalGo.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        pedestalGo.transform.localScale = new Vector3(2.6f, 0.04f, 2.6f);
        var pedRend = pedestalGo.GetComponent<Renderer>();
        if (pedRend != null)
        {
            Material pedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pedMat.SetColor("_BaseColor", new Color(0.18f, 0.21f, 0.26f));
            pedMat.SetFloat("_Smoothness", 0.25f);
            pedRend.material = pedMat;
        }

        // Camera
        GameObject camGo = new GameObject("Studio_Camera");
        camGo.transform.SetParent(studioRoot.transform, false);
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f); // Professional dark charcoal studio backdrop
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 30f;

        RenderTexture rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;

        // Load Prefabs
        GameObject motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Mother.prefab");
        GameObject childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Child.prefab");
        GameObject hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Hunter.prefab");

        try
        {
            // ==========================================
            // 1. RENDER MOTHER (COMPLETELY ISOLATED ALONE)
            // ==========================================
            if (motherPrefab != null)
            {
                GameObject mother = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab, studioRoot.transform);
                mother.transform.localPosition = Vector3.zero;
                mother.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                // Front full-body view
                camGo.transform.localPosition = new Vector3(0f, 0.85f, -3.55f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.85f, 0f));
                RenderToFile(cam, rt, "char_mother_isolated.png");

                // Perspective 3/4 full-body view
                camGo.transform.localPosition = new Vector3(-1.65f, 0.98f, -3.20f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.85f, 0f));
                RenderToFile(cam, rt, "char_mother_side.png");

                UnityEngine.Object.DestroyImmediate(mother);
            }

            // ==========================================
            // 2. RENDER CHILD (COMPLETELY ISOLATED ALONE)
            // ==========================================
            if (childPrefab != null)
            {
                GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, studioRoot.transform);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                // Front view
                camGo.transform.localPosition = new Vector3(0f, 0.50f, -2.25f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.50f, 0f));
                RenderToFile(cam, rt, "char_child_isolated.png");

                // Perspective 3/4 view
                camGo.transform.localPosition = new Vector3(-1.15f, 0.62f, -2.00f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.50f, 0f));
                RenderToFile(cam, rt, "char_child_side.png");

                // Crown version (Con Út)
                Transform crownTrans = child.transform.Find("Socket_Head/Socket_Crown/Crown_ConUt");
                if (crownTrans != null)
                {
                    crownTrans.gameObject.SetActive(true);
                    camGo.transform.localPosition = new Vector3(0f, 0.54f, -2.45f);
                    camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.54f, 0f));
                    RenderToFile(cam, rt, "char_con_ut_crown.png");
                }

                UnityEngine.Object.DestroyImmediate(child);
            }

            // ==========================================
            // 3. RENDER HUNTER (COMPLETELY ISOLATED ALONE)
            // ==========================================
            if (hunterPrefab != null)
            {
                GameObject hunter = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab, studioRoot.transform);
                hunter.transform.localPosition = Vector3.zero;
                hunter.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                // Front full-body view
                camGo.transform.localPosition = new Vector3(0f, 0.85f, -3.55f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.85f, 0f));
                RenderToFile(cam, rt, "char_hunter_isolated.png");

                // Perspective 3/4 full-body view
                camGo.transform.localPosition = new Vector3(-1.65f, 0.98f, -3.20f);
                camGo.transform.LookAt(StudioOrigin + new Vector3(0f, 0.85f, 0f));
                RenderToFile(cam, rt, "char_hunter_side.png");

                UnityEngine.Object.DestroyImmediate(hunter);
            }

            Debug.Log("[RenderCharacterSnapshots] All isolated individual character portraits rendered successfully!");
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(studioRoot);
        }
    }

    private static void RenderToFile(Camera cam, RenderTexture rt, string filename)
    {
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        string brainPath = Path.Combine(ArtifactDir, filename);
        File.WriteAllBytes(brainPath, bytes);

        string webglPath = Path.Combine(WebGLOutputDir, filename);
        File.WriteAllBytes(webglPath, bytes);

        Debug.Log($"[RenderCharacterSnapshots] Saved: {filename} ({bytes.Length / 1024} KB)");
    }
}
#endif
