#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGL
{
    private const string OutputDir = "Builds/WebGL_Netlify";
    private const string MainScene = "Assets/_Game/Scenes/Playground_CorePhysics.unity";

    [MenuItem("Tools/Rong Ran/Build WebGL for Netlify")]
    public static void Execute()
    {
        Debug.Log("[BuildWebGL] Starting WebGL Build for Netlify Drop...");

        // 1. Ensure scene exists
        if (!File.Exists(MainScene))
        {
            Debug.LogError($"[BuildWebGL] Main scene not found at {MainScene}!");
            return;
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
        EditorUserBuildSettings.webGLOptimizationLevel = WebGLOptimizationLevel.None;

        // 4. Set Scenes to Build
        string[] scenes = new string[] { MainScene };

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

            // Unity 6 WebGL IDBFS polyfill fix
            string buildFolder = Path.Combine(OutputDir, "Build");
            if (Directory.Exists(buildFolder))
            {
                foreach (string jsFile in Directory.GetFiles(buildFolder, "*.framework.js"))
                {
                    string js = File.ReadAllText(jsFile);
                    string target = "injectIndexedDBToAutomaticallyPersist(){IDBFS.queuePersist";
                    string polyfill = "injectIndexedDBToAutomaticallyPersist(){if(typeof IDBFS===\"undefined\"){var dummyIDBFS={mount:function(m){return MEMFS.mount(m)},syncfs:function(m,p,c){if(c)c(null)},queuePersist:function(m){}};if(typeof window!==\"undefined\")window.IDBFS=dummyIDBFS;if(typeof globalThis!==\"undefined\")globalThis.IDBFS=dummyIDBFS;if(typeof self!==\"undefined\")self.IDBFS=dummyIDBFS;IDBFS=dummyIDBFS;}IDBFS.queuePersist";
                    if (js.Contains(target))
                    {
                        js = js.Replace(target, polyfill);
                        File.WriteAllText(jsFile, js);
                        Debug.Log($"[BuildWebGL] Patched IDBFS in {Path.GetFileName(jsFile)}");
                    }
                }
            }

            string indexHtml = Path.Combine(OutputDir, "index.html");
            if (File.Exists(indexHtml))
            {
                string html = File.ReadAllText(indexHtml);
                string scriptTag = "<script>";
                string polyfillScript = "<script>\n      if (typeof window.IDBFS === \"undefined\") {\n        window.IDBFS = {\n          mount: function(m) { return (typeof MEMFS !== \"undefined\") ? MEMFS.mount(m) : m; },\n          syncfs: function(m, p, c) { if (c) c(null); },\n          queuePersist: function(m) {}\n        };\n      }";
                if (html.Contains(scriptTag) && !html.Contains("window.IDBFS"))
                {
                    html = html.Replace(scriptTag, polyfillScript);
                    File.WriteAllText(indexHtml, html);
                }
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

