#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RenderCharacterSnapshots
{
    private const string ArtifactDir = @"C:\Users\laptop\.gemini\antigravity\brain\b26809de-9d0d-4b2d-a0d9-21f7b579c32c";
    private const string WebGLOutputDir = @"D:\Projects\RongRanChamCham3D\Builds\WebGL_Netlify\TemplateData";

    [MenuItem("Tools/Rong Ran/Render Character Snapshots")]
    public static void Execute()
    {
        Debug.Log("[RenderCharacterSnapshots] Starting character studio rendering...");

        Directory.CreateDirectory(ArtifactDir);
        Directory.CreateDirectory(WebGLOutputDir);

        // 1. Create temporary studio environment
        GameObject studioRoot = new GameObject("Temp_Studio_Root");

        // Lights
        GameObject keyLightGo = new GameObject("Studio_KeyLight");
        keyLightGo.transform.SetParent(studioRoot.transform);
        Light keyLight = keyLightGo.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.3f;
        keyLight.color = new Color(1.0f, 0.96f, 0.90f);
        keyLightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        GameObject fillLightGo = new GameObject("Studio_FillLight");
        fillLightGo.transform.SetParent(studioRoot.transform);
        Light fillLight = fillLightGo.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.6f;
        fillLight.color = new Color(0.85f, 0.90f, 1.0f);
        fillLightGo.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

        // Camera
        GameObject camGo = new GameObject("Studio_Camera");
        camGo.transform.SetParent(studioRoot.transform);
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f); // Modern dark studio gradient feel
        cam.fieldOfView = 36f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;

        RenderTexture rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;

        // Load Prefabs
        GameObject motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Mother.prefab");
        GameObject childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Child.prefab");
        GameObject hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Characters/Hunter.prefab");

        try
        {
            // --- 1. RENDER MOTHER ---
            if (motherPrefab != null)
            {
                GameObject motherInst = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab, studioRoot.transform);
                motherInst.transform.position = Vector3.zero;
                motherInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face +Z (towards camera at -Z)

                // Perspective 3/4 view
                camGo.transform.position = new Vector3(-1.0f, 1.25f, -2.6f);
                camGo.transform.LookAt(new Vector3(0f, 0.90f, 0f));
                RenderToFile(cam, rt, "char_mother_perspective.png");

                // Front view
                camGo.transform.position = new Vector3(0f, 1.15f, -2.7f);
                camGo.transform.LookAt(new Vector3(0f, 0.90f, 0f));
                RenderToFile(cam, rt, "char_mother_front.png");

                UnityEngine.Object.DestroyImmediate(motherInst);
            }

            // --- 2. RENDER CHILD ---
            if (childPrefab != null)
            {
                GameObject childInst = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, studioRoot.transform);
                childInst.transform.position = Vector3.zero;
                childInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                // Perspective 3/4 view
                camGo.transform.position = new Vector3(-0.7f, 0.85f, -1.9f);
                camGo.transform.LookAt(new Vector3(0f, 0.60f, 0f));
                RenderToFile(cam, rt, "char_child_perspective.png");

                // Front view
                camGo.transform.position = new Vector3(0f, 0.80f, -1.9f);
                camGo.transform.LookAt(new Vector3(0f, 0.60f, 0f));
                RenderToFile(cam, rt, "char_child_front.png");

                UnityEngine.Object.DestroyImmediate(childInst);
            }

            // --- 3. RENDER HUNTER ---
            if (hunterPrefab != null)
            {
                GameObject hunterInst = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab, studioRoot.transform);
                hunterInst.transform.position = Vector3.zero;
                hunterInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                // Perspective 3/4 view
                camGo.transform.position = new Vector3(-0.9f, 1.30f, -2.7f);
                camGo.transform.LookAt(new Vector3(0f, 0.95f, 0f));
                RenderToFile(cam, rt, "char_hunter_perspective.png");

                // Front view
                camGo.transform.position = new Vector3(0f, 1.20f, -2.8f);
                camGo.transform.LookAt(new Vector3(0f, 0.95f, 0f));
                RenderToFile(cam, rt, "char_hunter_front.png");

                UnityEngine.Object.DestroyImmediate(hunterInst);
            }

            // --- 4. RENDER LINEUP (ALL 3 CHARACTERS TOGETHER) ---
            if (motherPrefab != null && childPrefab != null && hunterPrefab != null)
            {
                GameObject mother = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab, studioRoot.transform);
                mother.transform.position = new Vector3(-0.85f, 0f, 0f);
                mother.transform.rotation = Quaternion.Euler(0f, 195f, 0f);

                GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, studioRoot.transform);
                child.transform.position = new Vector3(0.05f, 0f, 0.35f);
                child.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                GameObject hunter = (GameObject)PrefabUtility.InstantiatePrefab(hunterPrefab, studioRoot.transform);
                hunter.transform.position = new Vector3(1.15f, 0f, -0.2f);
                hunter.transform.rotation = Quaternion.Euler(0f, 160f, 0f);

                camGo.transform.position = new Vector3(0f, 1.25f, -3.8f);
                camGo.transform.LookAt(new Vector3(0.1f, 0.85f, 0f));
                RenderToFile(cam, rt, "char_lineup_all.png");

                UnityEngine.Object.DestroyImmediate(mother);
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(hunter);
            }

            Debug.Log("[RenderCharacterSnapshots] All character snapshots rendered successfully!");
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

