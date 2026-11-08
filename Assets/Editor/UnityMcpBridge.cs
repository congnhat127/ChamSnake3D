#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RongRanChamCham;

[InitializeOnLoad]
public static class UnityMcpBridge
{
    private static HttpListener listener;
    private static Thread listenerThread;
    private static bool isRunning = false;
    private static readonly int Port = 8090;

    private static readonly List<string> consoleLogs = new List<string>();
    private static readonly object logLock = new object();

    static UnityMcpBridge()
    {
        Application.runInBackground = true;
        Application.logMessageReceived += OnLogMessageReceived;
        StartServer();
        AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        EditorApplication.quitting += StopServer;
        EditorApplication.update += CheckAndEnsureRunning;
    }

    private static double lastCheckTime = 0;
    private static void CheckAndEnsureRunning()
    {
        if (File.Exists("Temp/trigger_setup_chibi.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_setup_chibi.tmp");
                SetupChibiPrefabs.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_setup_playground.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_setup_playground.tmp");
                SetupCorePlayground.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (isRunning) return;
        if (EditorApplication.timeSinceStartup - lastCheckTime < 2.0) return;
        lastCheckTime = EditorApplication.timeSinceStartup;
        StartServer();
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        lock (logLock)
        {
            if (consoleLogs.Count > 100) consoleLogs.RemoveAt(0);
            consoleLogs.Add($"[{type}] {condition}");
        }
    }

    [MenuItem("Tools/Unity MCP Bridge/Check Status")]
    public static void CheckStatus()
    {
        string status = isRunning ? $"Running on port {Port}" : "Stopped";
        EditorUtility.DisplayDialog("Unity MCP Bridge", $"Status: {status}\nUnity Version: {Application.unityVersion}", "OK");
    }

    [MenuItem("Tools/Unity MCP Bridge/Restart Server")]
    public static void RestartServer()
    {
        StopServer();
        StartServer();
        Debug.Log($"[Unity MCP Bridge] Server restarted on http://127.0.0.1:{Port}/");
    }

    private static void StartServer()
    {
        if (isRunning) return;

        try
        {
            if (listener != null)
            {
                try { listener.Stop(); listener.Close(); } catch { }
                listener = null;
            }

            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();
            isRunning = true;

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true
            };
            listenerThread.Start();

            Debug.Log($"[Unity MCP Bridge] Active and listening on http://127.0.0.1:{Port}/");
        }
        catch (Exception ex)
        {
            // Silently wait for next tick or log once
            isRunning = false;
        }
    }

    private static void StopServer()
    {
        isRunning = false;
        if (listener != null)
        {
            try { listener.Stop(); listener.Close(); } catch { }
            listener = null;
        }
    }

    private static void ListenLoop()
    {
        while (isRunning && listener != null && listener.IsListening)
        {
            try
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem((state) => HandleRequest(context));
            }
            catch (HttpListenerException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Unity MCP Bridge] Listener error: {ex.Message}");
            }
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        string path = req.Url.AbsolutePath.Trim('/').ToLowerInvariant();
        string body = "";
        if (req.HasEntityBody && req.ContentLength64 > 0)
        {
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }
        }

        switch (path)
        {
            case "ping":
                SendJsonResponse(res, new
                {
                    status = "ok",
                    version = Application.unityVersion
                });
                break;

            case "setup_chibi":
                {
                    EditorApplication.CallbackFunction buildCb = null;
                    buildCb = () =>
                    {
                        EditorApplication.update -= buildCb;
                        try
                        {
                            SetupChibiPrefabs.Execute();
                            SendJsonResponse(res, new { status = "success", message = "Speckit 001: Chibi Prefabs and Showcase Scene updated successfully!" });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            SendJsonResponse(res, new { status = "error", message = ex.Message });
                        }
                    };
                    EditorApplication.update += buildCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "test_status":
                {
                    EditorApplication.CallbackFunction testCb = null;
                    testCb = () =>
                    {
                        EditorApplication.update -= testCb;
                        try
                        {
                            var mother = UnityEngine.Object.FindAnyObjectByType<MotherController>();
                            var snake = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
                            var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
                            var children = UnityEngine.Object.FindObjectsByType<ChildFollower>(FindObjectsInactive.Exclude);

                            var childPositions = new List<string>();
                            if (children != null)
                            {
                                foreach (var c in children)
                                {
                                    childPositions.Add($"{c.name}: pos={c.transform.position}, isConUt={c.IsConUt}");
                                }
                            }

                            SendJsonResponse(res, new
                            {
                                isPlaying = EditorApplication.isPlaying,
                                motherFound = mother != null,
                                motherPos = mother != null ? mother.transform.position.ToString() : "null",
                                hunterFound = hunter != null,
                                hunterPos = hunter != null ? hunter.transform.position.ToString() : "null",
                                childrenCount = children != null ? children.Length : 0,
                                children = childPositions,
                                conUtFound = snake != null && snake.ConUt != null
                            });
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += testCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "clear_logs":
                lock (logLock)
                {
                    consoleLogs.Clear();
                    SendJsonResponse(res, new { status = "cleared" });
                }
                break;

            case "console_logs":
            case "console":
            case "logs":
                lock (logLock)
                {
                    SendJsonResponse(res, new { logs = consoleLogs });
                }
                break;

            case "hierarchy":
                {
                    EditorApplication.CallbackFunction hierCb = null;
                    hierCb = () =>
                    {
                        EditorApplication.update -= hierCb;
                        try
                        {
                            var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
                            var list = new List<object>();
                            foreach (var go in rootObjs)
                            {
                                list.Add(DumpGameObject(go));
                            }
                            SendJsonResponse(res, new { scene = SceneManager.GetActiveScene().name, objects = list });
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += hierCb;
                }
                break;

            case "play":
                {
                    EditorApplication.CallbackFunction playCb = null;
                    playCb = () =>
                    {
                        EditorApplication.update -= playCb;
                        EditorApplication.isPlaying = !EditorApplication.isPlaying;
                        SendJsonResponse(res, new { isPlaying = EditorApplication.isPlaying });
                    };
                    EditorApplication.update += playCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "create":
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        var data = JsonUtility.FromJson<CreateObjectRequest>(body);
                        GameObject newGo = null;

                        if (!string.IsNullOrEmpty(data.primitiveType))
                        {
                            if (Enum.TryParse<PrimitiveType>(data.primitiveType, true, out var pType))
                            {
                                newGo = GameObject.CreatePrimitive(pType);
                            }
                        }

                        if (newGo == null)
                        {
                            newGo = new GameObject(data.name ?? "New GameObject");
                        }
                        else if (!string.IsNullOrEmpty(data.name))
                        {
                            newGo.name = data.name;
                        }

                        if (data.position != null && data.position.Length == 3)
                        {
                            newGo.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
                        }
                        if (data.scale != null && data.scale.Length == 3)
                        {
                            newGo.transform.localScale = new Vector3(data.scale[0], data.scale[1], data.scale[2]);
                        }

                        Undo.RegisterCreatedObjectUndo(newGo, "Create via MCP");
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                        SendJsonResponse(res, new { status = "created", name = newGo.name, id = newGo.GetHashCode() });
                    }
                    catch (Exception ex)
                    {
                        SendJsonResponse(res, new { error = ex.Message }, 500);
                    }
                };
                break;

            default:
                SendJsonResponse(res, new { error = "Endpoint not found" }, 404);
                break;
        }
    }

    private static object DumpGameObject(GameObject go)
    {
        var compList = new List<string>();
        foreach (var c in go.GetComponents<Component>())
        {
            if (c != null) compList.Add(c.GetType().Name);
        }

        var childrenList = new List<object>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            childrenList.Add(DumpGameObject(go.transform.GetChild(i).gameObject));
        }

        return new
        {
            name = go.name,
            active = go.activeSelf,
            position = new float[] { go.transform.position.x, go.transform.position.y, go.transform.position.z },
            components = compList,
            children = childrenList
        };
    }

    private static void SendJsonResponse(HttpListenerResponse res, object obj, int statusCode = 200)
    {
        try
        {
            string json = JsonUtility.ToJson(obj);
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                json = SimpleJsonSerialize(obj);
            }
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.StatusCode = statusCode;
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
        }
        catch { }
        finally
        {
            try { res.Close(); } catch { }
        }
    }

    private static string SimpleJsonSerialize(object obj)
    {
        // Fallback for anonymous types
        var props = obj.GetType().GetProperties();
        var sb = new StringBuilder("{");
        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];
            var val = p.GetValue(obj);
            sb.Append($"\"{p.Name}\":");
            if (val is string s) sb.Append($"\"{s}\"");
            else if (val is bool b) sb.Append(b ? "true" : "false");
            else if (val is int || val is float || val is double) sb.Append(val);
            else if (val is System.Collections.IEnumerable list)
            {
                sb.Append("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{item?.ToString().Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"");
                    first = false;
                }
                sb.Append("]");
            }
            else sb.Append($"\"{val}\"");

            if (i < props.Length - 1) sb.Append(",");
        }
        sb.Append("}");
        return sb.ToString();
    }

    [Serializable]
    private class CreateObjectRequest
    {
        public string name;
        public string primitiveType; // Cube, Sphere, Capsule, Cylinder, Plane, Quad
        public float[] position;
        public float[] scale;
    }
}
#endif
