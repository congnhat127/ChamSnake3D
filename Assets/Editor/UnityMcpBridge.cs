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
    private const int BasePort = 8090;
    // Sau một lần nạp lại code, listener cũ đôi khi không dừng hẳn và vẫn giữ cổng.
    // Khi đó thử các cổng kế tiếp và ghi cổng đang dùng ra file để công cụ bên ngoài đọc.
    private const int PortAttempts = 10;
    private const string PortFile = "Temp/mcp_bridge_port.txt";
    private static int Port = BasePort;

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
        try { PlaytestBridge.EditorTick(); }
        catch (Exception ex) { Debug.LogException(ex); }

        if (File.Exists("Temp/trigger_refresh.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_refresh.tmp");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

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

        if (File.Exists("Temp/trigger_build_webgl.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_build_webgl.tmp");
                BuildWebGL.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_render_snapshots.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_render_snapshots.tmp");
                RenderCharacterSnapshots.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_setup_rongran.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_setup_rongran.tmp");
                RongRanSetup.BuildAndSaveAllScenes();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_generate_anims.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_generate_anims.tmp");
                RongRanChamCham.Editor.GenerateCharacterAnimations.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_setup_master.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_setup_master.tmp");
                RongRanChamCham.Editor.GenerateCharacterAnimations.Execute();
                SetupNewRongRanMaster.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_play.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_play.tmp");
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_stop.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_stop.tmp");
                EditorApplication.isPlaying = false;
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
        if (File.Exists("Temp/trigger_play.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_play.tmp");
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (File.Exists("Temp/trigger_stop.tmp"))
        {
            try
            {
                File.Delete("Temp/trigger_stop.tmp");
                EditorApplication.isPlaying = false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (isRunning) return;

        try
        {
            if (listener != null)
            {
                try { listener.Stop(); listener.Close(); } catch { }
                listener = null;
            }

            Exception lastError = null;
            for (int attempt = 0; attempt < PortAttempts; attempt++)
            {
                int candidate = BasePort + attempt;
                try
                {
                    var candidateListener = new HttpListener();
                    candidateListener.Prefixes.Add($"http://127.0.0.1:{candidate}/");
                    candidateListener.Start();
                    listener = candidateListener;
                    Port = candidate;
                    lastError = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }
            if (lastError != null) throw lastError;
            File.WriteAllText(PortFile, Port.ToString());
            isRunning = true;

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true
            };
            listenerThread.Start();

            Debug.Log($"[Unity MCP Bridge] Active and listening on http://127.0.0.1:{Port}/");
        }
        catch (Exception)
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

            case "build_levels":
                {
                    EditorApplication.CallbackFunction bCb = null;
                    bCb = () =>
                    {
                        EditorApplication.update -= bCb;
                        try
                        {
                            RongRanSetup.BuildAndSaveAllScenes();
                            SendJsonResponse(res, new { status = "success", message = "Built and saved all 5 Rong Ran scenes!" });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            SendJsonResponse(res, new { status = "error", message = ex.Message });
                        }
                    };
                    EditorApplication.update += bCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "setup_master":
                {
                    EditorApplication.CallbackFunction mCb = null;
                    mCb = () =>
                    {
                        EditorApplication.update -= mCb;
                        try
                        {
                            SetupNewRongRanMaster.Execute();
                            SendJsonResponse(res, new { status = "success", message = "Built and saved all 5 master scenes with updated lighting, characters, and physics!" });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            SendJsonResponse(res, new { status = "error", message = ex.Message });
                        }
                    };
                    EditorApplication.update += mCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "open_scene":
                {
                    EditorApplication.CallbackFunction openCb = null;
                    openCb = () =>
                    {
                        EditorApplication.update -= openCb;
                        try
                        {
                            string sceneName = req.QueryString["name"] ?? "Gameplay";
                            string scenePath = $"Assets/_Game/Scenes/{sceneName}.unity";
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                            SendJsonResponse(res, new { status = "success", scene = scenePath });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += openCb;
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
                                isCompiling = EditorApplication.isCompiling,
                                isUpdating = EditorApplication.isUpdating,
                                isPaused = EditorApplication.isPaused,
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

            case "start_match":
                {
                    EditorApplication.CallbackFunction smCb = null;
                    smCb = () =>
                    {
                        EditorApplication.update -= smCb;
                        try
                        {
                            var gm = GameManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<GameManager>();
                            if (gm != null)
                            {
                                gm.StartMatch();
                                SendJsonResponse(res, new { status = "success", message = "Match started!" });
                            }
                            else
                            {
                                SendJsonResponse(res, new { status = "error", message = "GameManager not found" });
                            }
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += smCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "test_victory":
                {
                    EditorApplication.CallbackFunction vicCb = null;
                    vicCb = () =>
                    {
                        EditorApplication.update -= vicCb;
                        try
                        {
                            var gm = GameManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<GameManager>();
                            if (gm != null)
                            {
                                gm.ForceVictoryForTesting();
                                SendJsonResponse(res, new { status = "success", message = "Victory state triggered!" });
                            }
                            else
                            {
                                SendJsonResponse(res, new { error = "GameManager not found" });
                            }
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += vicCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "test_gameover":
                {
                    EditorApplication.CallbackFunction goCb = null;
                    goCb = () =>
                    {
                        EditorApplication.update -= goCb;
                        try
                        {
                            var gm = GameManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<GameManager>();
                            if (gm != null)
                            {
                                gm.ForceGameOverForTesting();
                                SendJsonResponse(res, new { status = "success", message = "GameOver state triggered!" });
                            }
                            else
                            {
                                SendJsonResponse(res, new { error = "GameManager not found" });
                            }
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += goCb;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                break;

            case "victory_status":
                {
                    EditorApplication.CallbackFunction vsCb = null;
                    vsCb = () =>
                    {
                        EditorApplication.update -= vsCb;
                        try
                        {
                            var gm = GameManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<GameManager>();
                            var mother = UnityEngine.Object.FindAnyObjectByType<MotherController>();
                            var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
                            var cam = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();

                            var motherAnim = mother != null ? mother.GetComponent<Animator>() : null;
                            var hunterAnim = hunter != null ? hunter.GetComponent<Animator>() : null;

                            SendJsonResponse(res, new
                            {
                                gameState = gm != null ? gm.CurrentState.ToString() : "null",
                                motherFacing = mother != null ? mother.transform.eulerAngles.ToString() : "null",
                                motherIsVictoryAnim = motherAnim != null ? motherAnim.GetBool("IsVictory") : false,
                                hunterFacing = hunter != null ? hunter.transform.eulerAngles.ToString() : "null",
                                hunterIsAngryAnim = hunterAnim != null ? hunterAnim.GetBool("IsAngry") : false,
                                hunterIsVictoryAnim = hunterAnim != null ? hunterAnim.GetBool("IsVictory") : false,
                                cameraPos = cam != null ? cam.transform.position.ToString() : "null",
                                cameraRot = cam != null ? cam.transform.eulerAngles.ToString() : "null"
                            });
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, new { error = ex.Message });
                        }
                    };
                    EditorApplication.update += vsCb;
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
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                        EditorApplication.ExecuteMenuItem("Edit/Play");
                        SendJsonResponse(res, new { status = "toggled", isPlaying = EditorApplication.isPlaying });
                    }
                    catch (Exception ex)
                    {
                        SendJsonResponse(res, new { error = ex.Message });
                    }
                };
                EditorApplication.QueuePlayerLoopUpdate();
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

            case "render_snapshots":
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        RenderCharacterSnapshots.Execute();
                        SendJsonResponse(res, new { status = "success", message = "All character snapshots rendered successfully!" });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        SendJsonResponse(res, new { status = "error", message = ex.Message }, 500);
                    }
                };
                break;

            default:
                if (PlaytestBridge.TryHandle(path, req, res)) break;
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
