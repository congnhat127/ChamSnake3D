#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGL
{
    private const string OutputDir = "Builds/WebGL_Netlify";
    private static readonly string[] BuildScenes = new string[]
    {
        "Assets/_Game/Scenes/Gameplay.unity",
        "Assets/_Game/Scenes/Man1.unity",
        "Assets/_Game/Scenes/Man2.unity",
        "Assets/_Game/Scenes/Man3.unity",
        "Assets/_Game/Scenes/Man4.unity",
        "Assets/_Game/Scenes/Man5.unity"
    };

    [MenuItem("Tools/Rong Ran/Build WebGL for Netlify")]
    public static void Execute()
    {
        Debug.Log("[BuildWebGL] Starting WebGL Build for Netlify Drop...");

        // 0. Ensure scenes are freshly baked with latest UI, food prefabs, and materials
        try
        {
            SetupNewRongRanMaster.Execute();
            Debug.Log("[BuildWebGL] SetupNewRongRanMaster.Execute() finished successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BuildWebGL] SetupNewRongRanMaster warning: {ex.Message}");
        }

        // 1. Ensure scenes exist
        foreach (var sc in BuildScenes)
        {
            if (!File.Exists(sc))
            {
                Debug.LogError($"[BuildWebGL] Scene not found at {sc}!");
                return;
            }
        }

        // 2. Ensure Output Directory
        if (Directory.Exists(OutputDir))
        {
            try { Directory.Delete(OutputDir, true); } catch { }
        }
        Directory.CreateDirectory(OutputDir);

        // 3. Configure WebGL PlayerSettings for 100% Netlify Compatibility
        // Disabled compression ensures Netlify hosts the files immediately without needing special server-side decompression headers
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.companyName = "RongRanChamCham";
        PlayerSettings.productName = "Rong Ran Cham Cham 3D";

        // 4. Set Scenes to Build
        string[] scenes = BuildScenes;

        // 5. Build Options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };

        Debug.Log($"[BuildWebGL] Building Player to: {OutputDir}...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildWebGL] Build SUCCEEDED in {summary.totalTime.TotalSeconds:F1}s! Output size: {summary.totalSize / (1024 * 1024):F1} MB.");

            // Create Netlify _headers file for optimal performance and caching
            string headersFile = Path.Combine(OutputDir, "_headers");
            string headersContent = "/*\n  Access-Control-Allow-Origin: *\n  Cache-Control: public, max-age=3600\n";
            File.WriteAllText(headersFile, headersContent);

            // Unity 6 WebGL IDBFS bulletproof polyfill fix
            string buildFolder = Path.Combine(OutputDir, "Build");
            if (Directory.Exists(buildFolder))
            {
                foreach (string jsFile in Directory.GetFiles(buildFolder, "*.framework.js"))
                {
                    string js = File.ReadAllText(jsFile);
                    string safeDecl = "var IDBFS = { mount: function(m) { return (typeof MEMFS !== 'undefined') ? MEMFS.createNode(null, '/', 16895, 0) : m; }, syncfs: function(m, p, c) { if (c) c(null); }, queuePersist: function(m) {} };";
                    string errString = "var IDBFS = 'IDBFS is no longer included by default; build with -lidbfs.js';";
                    if (js.Contains(errString))
                    {
                        js = js.Replace(errString, safeDecl);
                    }
                    if (!js.Contains("var IDBFS = {"))
                    {
                        js = js.Replace("var moduleRtn;", "var moduleRtn;" + safeDecl);
                    }
                    if (!js.Contains("function injectIndexedDBToAutomaticallyPersist(){return;"))
                    {
                        js = js.Replace("function injectIndexedDBToAutomaticallyPersist(){", "function injectIndexedDBToAutomaticallyPersist(){return;");
                    }
                    File.WriteAllText(jsFile, js);
                    Debug.Log($"[BuildWebGL] Bulletproof IDBFS patch applied to {Path.GetFileName(jsFile)}");
                }
            }

            string indexHtml = Path.Combine(OutputDir, "index.html");
            if (File.Exists(indexHtml))
            {
                string html = File.ReadAllText(indexHtml);
                string polyfillScript = "<script>\n      var dummyIDBFS = {\n        mount: function(m) { return m; },\n        syncfs: function(m, p, c) { if (c) c(null); },\n        queuePersist: function(m) {}\n      };\n      window.IDBFS = dummyIDBFS;\n      globalThis.IDBFS = dummyIDBFS;\n    </script>\n";
                if (!html.Contains("dummyIDBFS"))
                {
                    html = html.Replace("<head>", "<head>\n    " + polyfillScript);
                }
                // Make desktop fullscreen responsive instead of fixed 960x600
                html = html.Replace("fullscreenContainer.style.width = \"960px\";", "fullscreenContainer.style.width = \"100%\";");
                html = html.Replace("fullscreenContainer.style.height = \"600px\";", "fullscreenContainer.style.height = \"100%\";");
                File.WriteAllText(indexHtml, html);
            }

            string styleCss = Path.Combine(OutputDir, "TemplateData", "style.css");
            if (File.Exists(styleCss))
            {
                string css = File.ReadAllText(styleCss);
                css = css.Replace("#unity-container.unity-desktop { left: 50%; top: 50%; transform: translate(-50%, -50%) }",
                                  "#unity-container.unity-desktop { width: 100vw; height: 100vh; position: fixed; left: 0; top: 0; transform: none; display: flex; align-items: center; justify-content: center; background: #0e1217; }");
                if (!css.Contains(".unity-desktop #unity-fullscreen-container"))
                {
                    css += "\n.unity-desktop #unity-fullscreen-container { width: 100%; height: 100%; max-width: 177.78vh; max-height: 56.25vw; position: relative; display: flex; align-items: center; justify-content: center; }\n";
                }
                File.WriteAllText(styleCss, css);
            }

            Debug.Log($"[BuildWebGL] Ready for Netlify Drop! Folder: {Path.GetFullPath(OutputDir)}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[BuildWebGL] Build FAILED with {summary.totalErrors} errors!");
        }
    }
}
#endif

