#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using RongRanChamCham;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Các lệnh playtest bổ sung cho UnityMcpBridge: chụp màn hình, bơm input, tự lái, đo số liệu runtime, chạy test.
/// Chỉ chạy trong Editor.
/// </summary>
[InitializeOnLoad]
public static class PlaytestBridge
{
    private static readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---- Tự lái ----
    private static string autopilotMode = "off";
    private static bool autopilotLock = false;
    private static string autoDuelMode = "off";

    // ---- Số liệu đo ----
    private const float SpeedWindowSeconds = 0.25f;   // cửa sổ tính tốc độ trung bình, đủ dài để khử nhiễu FixedUpdate 50Hz
    private const float TeleportThreshold = 2.0f;     // dịch quá 2m trong một lần lấy mẫu là dịch chuyển tức thời
    private const float MotherMovingSpeed = 1.0f;     // Mẹ được coi là đang chạy
    private const float MotherStoppedSpeed = 0.05f;   // Mẹ được coi là đứng yên
    private const float SettleSeconds = 1.0f;         // thời gian cho phép đàn con xếp hàng sau khi Mẹ dừng
    private const float MotherPadding = 0.5f, HunterPadding = 0.45f, ChildPadding = 0.35f; // đúng bằng padding các script runtime truyền vào ClampToArena

    private static double sessionStart;
    private static int samples, frameStart;
    private static float maxFrameMs;
    private static int gcStart;
    private static float lastSampleTime = -1f;
    private static float maxOutside; private static string maxOutsideWho = "";
    private static float maxChildRatio, ratioChildSpeed, ratioMotherSpeed;
    private static float maxChildSpeed, maxChildSpeedAfterStop, maxMotherSpeed, maxHunterSpeed;
    private static float minGap = float.MaxValue, maxGap;
    private static float minHunterTail = float.MaxValue, minHunterMother = float.MaxValue;
    private static int teleportsInChase;
    private static int chaseSamples, hunterOffscreen, tailOffscreen, motherOffscreen;
    private static float motherStoppedFor;
    private static float windowTime;
    private static readonly List<Vector3> lastPos = new List<Vector3>();
    private static readonly List<float> windowDist = new List<float>();
    private static readonly List<string> transitions = new List<string>();
    private static string lastState = "";

    static PlaytestBridge()
    {
        ResetProbe();
    }

    // =====================================================================
    // HTTP
    // =====================================================================
    public static bool TryHandle(string path, HttpListenerRequest req, HttpListenerResponse res)
    {
        return Dispatch(path, req.QueryString, res);
    }

    // ---- Kênh file: Temp/playtest/cmd.txt chứa "<id> <path>?<query>", kết quả ghi ra Temp/playtest/out_<id>.json ----
    private const string CommandFile = "Temp/playtest/cmd.txt";
    private static bool directMode;
    private static string directResult;

    /// <summary>Gọi mỗi lần Editor cập nhật (từ UnityMcpBridge.CheckAndEnsureRunning), luôn trên luồng chính.</summary>
    public static void EditorTick()
    {
        PumpFileCommand();
        Tick();
    }

    private static void PumpFileCommand()
    {
        if (!File.Exists(CommandFile)) return;
        string line;
        try { line = File.ReadAllText(CommandFile).Trim(); File.Delete(CommandFile); }
        catch { return; }

        int space = line.IndexOf(' ');
        if (space <= 0) return;
        string id = Sanitize(line.Substring(0, space));
        string request = line.Substring(space + 1).Trim();
        string path = request, queryText = "";
        int qm = request.IndexOf('?');
        if (qm >= 0) { path = request.Substring(0, qm); queryText = request.Substring(qm + 1); }

        var query = new System.Collections.Specialized.NameValueCollection();
        foreach (string pair in queryText.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq > 0) query[pair.Substring(0, eq)] = Uri.UnescapeDataString(pair.Substring(eq + 1));
        }

        string json;
        directMode = true;
        directResult = null;
        try
        {
            bool known = Dispatch(path.Trim('/').ToLowerInvariant(), query, null);
            json = known ? (directResult ?? "{}") : "{\"error\":\"unknown command\"}";
        }
        catch (Exception ex)
        {
            json = "{\"error\":" + Q(ex.GetType().Name + ": " + ex.Message) + "}";
        }
        finally { directMode = false; }

        try { File.WriteAllText("Temp/playtest/out_" + id + ".json", json); } catch { }
    }

    private static bool Dispatch(string path, System.Collections.Specialized.NameValueCollection q, HttpListenerResponse res)
    {
        switch (path)
        {
            case "wait":
                {
                    int ms = Mathf.Clamp(ParseInt(q["ms"], 1000), 0, 60000);
                    if (!directMode) Thread.Sleep(ms);
                    Send(res, "{\"waited\":" + ms + "}");
                    return true;
                }
            case "playmode":
                {
                    bool on = ParseInt(q["on"], 1) != 0;
                    OnMain(res, () =>
                    {
                        if (on && !EditorApplication.isPlaying)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                            EditorApplication.isPlaying = true;
                        }
                        else if (!on && EditorApplication.isPlaying)
                        {
                            autopilotMode = "off";
                            autoDuelMode = "off";
                            EditorApplication.isPlaying = false;
                        }
                        return "{\"requested\":" + (on ? "\"play\"" : "\"stop\"") + "}";
                    });
                    return true;
                }
            case "bake_arena":
                OnMain(res, () => { BakeArenaDefinitions.BakeAllScenes(); return "{\"status\":\"baked\"}"; });
                return true;
            case "bake_master":
                OnMain(res, () => {
                    RongRanChamCham.Editor.GenerateCharacterAnimations.Execute();
                    SetupNewRongRanMaster.Execute();
                    return "{\"status\":\"master_baked\"}";
                });
                return true;
            case "build_webgl":
                OnMain(res, () => {
                    BuildWebGL.Execute();
                    return "{\"status\":\"webgl_built\"}";
                });
                return true;
            case "run_tests":
                OnMain(res, RunEditModeTests);
                return true;
            case "screenshot":
                {
                    string name = Sanitize(q["name"] ?? "shot");
                    OnMain(res, () =>
                    {
                        if (!EditorApplication.isPlaying) return "{\"error\":\"not playing\"}";
                        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "playtest");
                        Directory.CreateDirectory(dir);
                        string file = Path.Combine(dir, name + ".png");
                        if (File.Exists(file)) File.Delete(file);
                        ScreenCapture.CaptureScreenshot(file);
                        return "{\"file\":" + Q(file.Replace('\\', '/')) + ",\"screen\":\"" + Screen.width + "x" + Screen.height + "\"}";
                    });
                    return true;
                }
            case "input":
                {
                    bool active = ParseInt(q["active"], 1) != 0;
                    float x = ParseFloat(q["x"], 0f), y = ParseFloat(q["y"], 0f);
                    bool lck = ParseInt(q["lock"], 0) != 0;
                    OnMain(res, () =>
                    {
                        autopilotMode = "off";
                        if (TouchInputManager.Instance == null) return "{\"error\":\"no TouchInputManager\"}";
                        TouchInputManager.Instance.SetScriptedInput(active, new Vector2(x, y), lck);
                        return "{\"status\":\"ok\"}";
                    });
                    return true;
                }
            case "autopilot":
                {
                    string mode = (q["mode"] ?? "off").ToLowerInvariant();
                    bool lck = ParseInt(q["lock"], 0) != 0;
                    OnMain(res, () =>
                    {
                        autopilotMode = mode;
                        autopilotLock = lck;
                        if (mode == "off" && TouchInputManager.Instance != null)
                            TouchInputManager.Instance.SetScriptedInput(false, Vector2.zero, false);
                        return "{\"autopilot\":" + Q(mode) + "}";
                    });
                    return true;
                }
            case "autoduel":
                autoDuelMode = (q["mode"] ?? "off").ToLowerInvariant();
                Send(res, "{\"autoduel\":" + Q(autoDuelMode) + "}");
                return true;
            case "duel":
                {
                    string dir = (q["dir"] ?? "straight").ToLowerInvariant();
                    OnMain(res, () =>
                    {
                        if (ChamChamDuelManager.Instance == null) return "{\"error\":\"no duel manager\"}";
                        ChamChamDuelManager.Instance.ChooseDirection(ParseDir(dir));
                        return "{\"chosen\":" + Q(dir) + "}";
                    });
                    return true;
                }
            case "force_duel":
                OnMain(res, () =>
                {
                    var gm = GameManager.Instance;
                    if (gm == null) return "{\"error\":\"no GameManager\"}";
                    if (gm.CurrentState != GameState.Chasing)
                    {
                        var begin = typeof(GameManager).GetMethod("BeginMatch", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (begin != null) begin.Invoke(gm, null);
                    }
                    var m = typeof(GameManager).GetMethod("HandleConUtCaught", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (m == null) return "{\"error\":\"HandleConUtCaught not found\"}";
                    m.Invoke(gm, null);
                    return "{\"state\":" + Q(gm.CurrentState.ToString()) + "}";
                });
                return true;
            case "apply_buff":
                {
                    string type = q["type"] ?? "BanhMi";
                    bool snake = ParseInt(q["snake"], 1) != 0;
                    OnMain(res, () =>
                    {
                        if (BuffSystem.Instance == null) return "{\"error\":\"no BuffSystem\"}";
                        if (!Enum.TryParse<FoodType>(type, true, out var food)) return "{\"error\":\"bad type\"}";
                        BuffSystem.Instance.ApplyBuff(food, snake);
                        return "{\"applied\":" + Q(food.ToString()) + "}";
                    });
                    return true;
                }
            case "select_level":
                {
                    int n = ParseInt(q["n"], 1);
                    OnMain(res, () =>
                    {
                        if (LevelManager.Instance == null) return "{\"error\":\"no LevelManager\"}";
                        LevelManager.Instance.SelectLevel(n);
                        return "{\"selected\":" + LevelManager.Instance.CurrentSelectedLevel + ",\"unlocked\":" + LevelManager.Instance.HighestUnlockedLevel + "}";
                    });
                    return true;
                }
            case "menu":
                OnMain(res, () =>
                {
                    if (GameManager.Instance == null) return "{\"error\":\"no GameManager\"}";
                    string what = (q["to"] ?? "main").ToLowerInvariant();
                    if (what == "showcase") GameManager.Instance.OpenShowcase(); else GameManager.Instance.ShowMainMenu();
                    return "{\"state\":" + Q(GameManager.Instance.CurrentState.ToString()) + "}";
                });
                return true;
            case "timescale":
                {
                    float v = Mathf.Clamp(ParseFloat(q["v"], 1f), 0f, 8f);
                    OnMain(res, () => { Time.timeScale = v; return "{\"timeScale\":" + F(v) + "}"; });
                    return true;
                }
            case "start_match":
                OnMain(res, () =>
                {
                    if (GameManager.Instance == null) return "{\"error\":\"no GameManager\"}";
                    GameManager.Instance.StartMatch();
                    return "{\"state\":" + Q(GameManager.Instance.CurrentState.ToString()) + "}";
                });
                return true;
            case "pause":
                OnMain(res, () =>
                {
                    if (GameManager.Instance == null) return "{\"error\":\"no GameManager\"}";
                    GameManager.Instance.TogglePause();
                    return "{\"state\":" + Q(GameManager.Instance.CurrentState.ToString()) + ",\"timeScale\":" + F(Time.timeScale) + "}";
                });
                return true;
            case "force_end":
                {
                    bool win = ParseInt(q["win"], 1) != 0;
                    OnMain(res, () =>
                    {
                        if (GameManager.Instance == null) return "{\"error\":\"no GameManager\"}";
                        if (win) GameManager.Instance.ForceVictoryForTesting(); else GameManager.Instance.ForceGameOverForTesting();
                        return "{\"state\":" + Q(GameManager.Instance.CurrentState.ToString()) + "}";
                    });
                    return true;
                }
            case "open_scene":
                {
                    string sceneName = Sanitize(q["name"] ?? "Gameplay");
                    OnMain(res, () =>
                    {
                        if (EditorApplication.isPlaying) return "{\"error\":\"exit play mode first\"}";
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + sceneName + ".unity");
                        return "{\"scene\":" + Q(sceneName) + "}";
                    });
                    return true;
                }
            case "status":
                OnMain(res, () => "{\"isPlaying\":" + (EditorApplication.isPlaying ? "true" : "false") +
                    ",\"isCompiling\":" + (EditorApplication.isCompiling ? "true" : "false") +
                    ",\"scene\":" + Q(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) +
                    ",\"state\":" + Q(GameManager.Instance != null ? GameManager.Instance.CurrentState.ToString() : "null") +
                    ",\"screen\":\"" + Screen.width + "x" + Screen.height + "\"}");
                return true;
            case "probe_reset":
                OnMain(res, () => { ResetProbe(); return "{\"status\":\"reset\"}"; });
                return true;
            case "probe":
                OnMain(res, BuildProbeJson);
                return true;
            case "inspect":
                OnMain(res, BuildInspectJson);
                return true;
        }
        return false;
    }

    private static void OnMain(HttpListenerResponse res, Func<string> work)
    {
        if (directMode)
        {
            try { directResult = work(); }
            catch (Exception ex)
            {
                Exception inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                directResult = "{\"error\":" + Q(inner.GetType().Name + ": " + inner.Message) + "}";
            }
            return;
        }
        mainThreadQueue.Enqueue(() =>
        {
            string json;
            try { json = work(); }
            catch (Exception ex)
            {
                Exception inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                json = "{\"error\":" + Q(inner.GetType().Name + ": " + inner.Message) + "}";
            }
            Send(res, json);
        });
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private static void Send(HttpListenerResponse res, string json)
    {
        if (res == null) { directResult = json; return; }
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
        }
        catch { }
        finally { try { res.Close(); } catch { } }
    }

    // =====================================================================
    // Vòng cập nhật trong Editor
    // =====================================================================
    private static void Tick()
    {
        while (mainThreadQueue.TryDequeue(out var action)) action();

        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        DriveAutopilot(gm);
        Sample(gm);
    }

    private static void DriveAutopilot(GameManager gm)
    {
        if (autoDuelMode == "random" && ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive
            && ChamChamDuelManager.Instance.ReactionTimeRemaining > 0f)
        {
            ChamChamDuelManager.Instance.ChooseDirection((DuelDirection)UnityEngine.Random.Range(0, 4));
        }

        if (autopilotMode == "off" || TouchInputManager.Instance == null) return;

        float t = Time.time;
        Vector2 dir = Vector2.zero;
        switch (autopilotMode)
        {
            case "walls":
                {
                    // Lần lượt lao vào tường theo 8 hướng, mỗi hướng 3 giây.
                    float a = Mathf.Floor(t / 3f) * 45f * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    break;
                }
            case "orbit":
                {
                    float a = t * 0.8f;
                    dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    break;
                }
            case "stopgo":
                {
                    // Chạy 2 giây rồi đứng 2 giây, đổi hướng mỗi chu kỳ.
                    float phase = Mathf.Repeat(t, 4f);
                    float a = Mathf.Floor(t / 4f) * 100f * Mathf.Deg2Rad;
                    dir = phase < 2f ? new Vector2(Mathf.Cos(a), Mathf.Sin(a)) : Vector2.zero;
                    break;
                }
            case "zigzag":
                {
                    // Đảo chiều ngang mỗi 0.6 giây để thử quất đuôi.
                    float side = Mathf.Floor(t / 0.6f) % 2f < 1f ? 1f : -1f;
                    float fwd = Mathf.Floor(t / 6f) % 2f < 1f ? 0.5f : -0.5f;
                    dir = new Vector2(side, fwd).normalized;
                    break;
                }
            case "idle":
                dir = Vector2.zero;
                break;
        }
        TouchInputManager.Instance.SetScriptedInput(true, dir, autopilotLock);
    }

    private static void ResetProbe()
    {
        sessionStart = EditorApplication.timeSinceStartup;
        samples = 0; frameStart = Time.frameCount; maxFrameMs = 0f;
        gcStart = GC.CollectionCount(0);
        lastSampleTime = -1f;
        maxOutside = 0f; maxOutsideWho = "";
        maxChildRatio = ratioChildSpeed = ratioMotherSpeed = 0f;
        maxChildSpeed = maxChildSpeedAfterStop = maxMotherSpeed = maxHunterSpeed = 0f;
        minGap = float.MaxValue; maxGap = 0f;
        minHunterTail = float.MaxValue; minHunterMother = float.MaxValue;
        teleportsInChase = 0;
        chaseSamples = hunterOffscreen = tailOffscreen = motherOffscreen = 0;
        motherStoppedFor = 0f; windowTime = 0f;
        lastPos.Clear(); windowDist.Clear(); transitions.Clear(); lastState = "";
    }

    private static void Sample(GameManager gm)
    {
        float now = Time.time;
        if (lastSampleTime < 0f) { lastSampleTime = now; }
        float dt = now - lastSampleTime;
        if (dt <= 0f) return;
        lastSampleTime = now;
        samples++;
        maxFrameMs = Mathf.Max(maxFrameMs, Time.unscaledDeltaTime * 1000f);

        string state = gm.CurrentState.ToString();
        if (state != lastState)
        {
            if (transitions.Count < 80) transitions.Add(now.ToString("F1", Inv) + "s " + state);
            lastState = state;
        }

        var mother = gm.Mother; var hunter = gm.Hunter; var chain = gm.SnakeChain;
        if (mother == null || hunter == null || chain == null) return;
        var children = chain.Children;
        bool chasing = gm.CurrentState == GameState.Chasing;

        // Thứ tự: 0 = Mẹ, 1 = Thầy, 2.. = các con
        int count = 2 + children.Count;
        if (lastPos.Count != count)
        {
            lastPos.Clear(); windowDist.Clear();
            for (int i = 0; i < count; i++) { lastPos.Add(GetPos(i, mother, hunter, children)); windowDist.Add(0f); }
            windowTime = 0f;
            return;
        }

        bool teleported = false;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = GetPos(i, mother, hunter, children);
            float pad = i == 0 ? MotherPadding : (i == 1 ? HunterPadding : ChildPadding);

            Vector3 clamped = ArenaBoundary.ClampToArena(p, pad);
            float outside = new Vector2(clamped.x - p.x, clamped.z - p.z).magnitude;
            if (outside > maxOutside)
            {
                maxOutside = outside;
                maxOutsideWho = (i == 0 ? "Mother" : i == 1 ? "Hunter" : "Child" + (i - 1)) + " in " + state + " at " + p.ToString("F2");
            }

            Vector3 d = p - lastPos[i]; d.y = 0f;
            float moved = d.magnitude;
            if (moved > TeleportThreshold) { teleported = true; if (chasing) teleportsInChase++; }
            else windowDist[i] += moved;
            lastPos[i] = p;
        }

        if (!chasing || teleported)
        {
            for (int i = 0; i < count; i++) windowDist[i] = 0f;
            windowTime = 0f; motherStoppedFor = 0f;
            return;
        }

        // Khoảng cách trong chuỗi và tới Thầy Thuốc
        Vector3 prev = mother.transform.position;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null) continue;
            float gap = Flat(children[i].transform.position - prev).magnitude;
            minGap = Mathf.Min(minGap, gap); maxGap = Mathf.Max(maxGap, gap);
            prev = children[i].transform.position;
        }
        if (chain.ConUt != null) minHunterTail = Mathf.Min(minHunterTail, Flat(hunter.transform.position - chain.ConUt.transform.position).magnitude);
        minHunterMother = Mathf.Min(minHunterMother, Flat(hunter.transform.position - mother.transform.position).magnitude);

        // Khung hình camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            chaseSamples++;
            if (!InView(cam, hunter.transform.position)) hunterOffscreen++;
            if (!InView(cam, mother.transform.position)) motherOffscreen++;
            if (chain.ConUt != null && !InView(cam, chain.ConUt.transform.position)) tailOffscreen++;
        }

        // Tốc độ trung bình theo cửa sổ
        windowTime += dt;
        if (windowTime >= SpeedWindowSeconds)
        {
            float motherSpeed = windowDist[0] / windowTime;
            float hunterSpeed = windowDist[1] / windowTime;
            maxMotherSpeed = Mathf.Max(maxMotherSpeed, motherSpeed);
            maxHunterSpeed = Mathf.Max(maxHunterSpeed, hunterSpeed);

            if (motherSpeed < MotherStoppedSpeed) motherStoppedFor += windowTime; else motherStoppedFor = 0f;

            for (int i = 2; i < count; i++)
            {
                float childSpeed = windowDist[i] / windowTime;
                maxChildSpeed = Mathf.Max(maxChildSpeed, childSpeed);
                if (motherSpeed > MotherMovingSpeed)
                {
                    float ratio = childSpeed / motherSpeed;
                    if (ratio > maxChildRatio) { maxChildRatio = ratio; ratioChildSpeed = childSpeed; ratioMotherSpeed = motherSpeed; }
                }
                if (motherStoppedFor > SettleSeconds) maxChildSpeedAfterStop = Mathf.Max(maxChildSpeedAfterStop, childSpeed);
            }

            for (int i = 0; i < count; i++) windowDist[i] = 0f;
            windowTime = 0f;
        }
    }

    private static Vector3 GetPos(int index, MotherController mother, HunterAI hunter, IReadOnlyList<ChildFollower> children)
    {
        if (index == 0) return mother.transform.position;
        if (index == 1) return hunter.transform.position;
        var c = children[index - 2];
        return c != null ? c.transform.position : Vector3.zero;
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    private static bool InView(Camera cam, Vector3 world)
    {
        // Lấy điểm ngang ngực nhân vật để xét có nằm trong khung hình không.
        Vector3 vp = cam.WorldToViewportPoint(world + Vector3.up * 0.8f);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    // =====================================================================
    // Báo cáo
    // =====================================================================
    private static string BuildProbeJson()
    {
        var sb = new StringBuilder("{");
        var gm = GameManager.Instance;
        double elapsed = EditorApplication.timeSinceStartup - sessionStart;
        int frames = Time.frameCount - frameStart;

        Add(sb, "isPlaying", EditorApplication.isPlaying ? "true" : "false");
        Add(sb, "state", Q(gm != null ? gm.CurrentState.ToString() : "null"));
        if (gm != null)
        {
            Add(sb, "timer", F(gm.RemainingTime));
            Add(sb, "hearts", F(gm.CurrentHearts));
            Add(sb, "children", gm.RemainingChildren.ToString());
        }
        Add(sb, "level", LevelManager.Instance != null ? LevelManager.Instance.CurrentSelectedLevel.ToString() : "0");
        Add(sb, "arena", Q(ArenaBoundary.ActiveArena != null ? ArenaBoundary.ActiveArena.shape.ToString() : "none(default ellipse)"));
        Add(sb, "elapsedSec", F((float)elapsed));
        Add(sb, "samples", samples.ToString());
        Add(sb, "avgFps", F(elapsed > 0 ? (float)(frames / elapsed) : 0f));
        Add(sb, "maxFrameMs", F(maxFrameMs));
        Add(sb, "gen0Collections", (GC.CollectionCount(0) - gcStart).ToString());
        Add(sb, "maxOutsideM", F(maxOutside));
        Add(sb, "maxOutsideWho", Q(maxOutsideWho));
        Add(sb, "maxMotherSpeed", F(maxMotherSpeed));
        Add(sb, "maxHunterSpeed", F(maxHunterSpeed));
        Add(sb, "maxChildSpeed", F(maxChildSpeed));
        Add(sb, "maxChildToMotherRatio", F(maxChildRatio));
        Add(sb, "ratioAt", Q("child " + F(ratioChildSpeed) + " / mother " + F(ratioMotherSpeed)));
        Add(sb, "maxChildSpeedAfterMotherStopped1s", F(maxChildSpeedAfterStop));
        Add(sb, "minLinkGap", F(minGap == float.MaxValue ? -1f : minGap));
        Add(sb, "maxLinkGap", F(maxGap));
        Add(sb, "minHunterToTail", F(minHunterTail == float.MaxValue ? -1f : minHunterTail));
        Add(sb, "minHunterToMother", F(minHunterMother == float.MaxValue ? -1f : minHunterMother));
        Add(sb, "teleportsInChase", teleportsInChase.ToString());
        Add(sb, "chaseSamples", chaseSamples.ToString());
        Add(sb, "hunterOffscreenPct", F(chaseSamples > 0 ? 100f * hunterOffscreen / chaseSamples : 0f));
        Add(sb, "tailOffscreenPct", F(chaseSamples > 0 ? 100f * tailOffscreen / chaseSamples : 0f));
        Add(sb, "motherOffscreenPct", F(chaseSamples > 0 ? 100f * motherOffscreen / chaseSamples : 0f));

        if (gm != null && gm.Mother != null && gm.Hunter != null)
        {
            Add(sb, "motherPos", Q(gm.Mother.transform.position.ToString("F2")));
            Add(sb, "hunterPos", Q(gm.Hunter.transform.position.ToString("F2")));
            var kids = new List<string>();
            if (gm.SnakeChain != null) foreach (var c in gm.SnakeChain.Children) if (c != null) kids.Add(c.transform.position.ToString("F2"));
            Add(sb, "childPos", Arr(kids));
        }
        if (Camera.main != null)
        {
            Add(sb, "camPos", Q(Camera.main.transform.position.ToString("F2")));
            Add(sb, "camEuler", Q(Camera.main.transform.eulerAngles.ToString("F1")));
        }

        var items = new List<string>();
        foreach (var item in UnityEngine.Object.FindObjectsByType<FoodPickup>(FindObjectsInactive.Exclude))
        {
            Vector3 p = item.transform.position;
            bool inside = ArenaBoundary.IsInside(p, 0f);
            bool visible = item.GetComponentInChildren<Renderer>() != null;
            items.Add(item.Type + " " + p.ToString("F1") + (inside ? " inside" : " OUTSIDE") + (visible ? "" : " INVISIBLE"));
        }
        Add(sb, "items", Arr(items));
        Add(sb, "transitions", Arr(transitions), true);
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>Kiểm tra cấu trúc nhân vật trong scene: socket, animator, renderer.</summary>
    private static string BuildInspectJson()
    {
        var sb = new StringBuilder("{");
        var mother = UnityEngine.Object.FindAnyObjectByType<MotherController>();
        var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
        var kids = UnityEngine.Object.FindObjectsByType<ChildFollower>(FindObjectsInactive.Include);

        Add(sb, "scene", Q(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
        Add(sb, "mother", Q(DescribeActor(mother != null ? mother.transform : null, "Socket_Hand_L", "Socket_Hand_R", "Socket_Head", "Socket_Foot_L", "Socket_Foot_R")));
        Add(sb, "hunter", Q(DescribeActor(hunter != null ? hunter.transform : null, "Socket_Hand_R", "Socket_Head", "Socket_Foot_L", "Socket_Foot_R")));
        var kidDesc = new List<string>();
        foreach (var k in kids) kidDesc.Add(k.name + ": " + DescribeActor(k.transform, "Socket_Head", "Socket_Crown", "Socket_Foot_L", "Socket_Foot_R"));
        Add(sb, "children", Arr(kidDesc));

        var arena = UnityEngine.Object.FindAnyObjectByType<ArenaDefinition>();
        Add(sb, "arenaDefinition", Q(arena == null ? "MISSING" :
            arena.shape + " obstacles=" + arena.obstacles.Length + " movers=" + arena.movers.Length +
            " motherSpawn=" + arena.motherSpawn.ToString("F2") + " hunterSpawn=" + arena.hunterSpawn.ToString("F2")));

        var spawner = UnityEngine.Object.FindAnyObjectByType<ItemSpawner>();
        if (spawner != null)
        {
            var so = new SerializedObject(spawner);
            var parts = new List<string>();
            foreach (string f in new[] { "banhMiPrefab", "phoPrefab", "kimbapPrefab", "tteokbokkiPrefab" })
                parts.Add(f + "=" + (so.FindProperty(f).objectReferenceValue != null ? "ok" : "NULL"));
            Add(sb, "itemSpawner", Q(string.Join(", ", parts)));
        }

        Add(sb, "balanceConfigAssigned", Q(
            "mother=" + HasRef(mother, "balanceConfig") + " hunter=" + HasRef(hunter, "balanceConfig") +
            " chain=" + HasRef(UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>(), "balanceConfig")), true);
        sb.Append("}");
        return sb.ToString();
    }

    private static string HasRef(UnityEngine.Object target, string field)
    {
        if (target == null) return "n/a";
        var prop = new SerializedObject(target).FindProperty(field);
        return prop != null && prop.objectReferenceValue != null ? "yes" : "NO";
    }

    private static string DescribeActor(Transform t, params string[] sockets)
    {
        if (t == null) return "NOT FOUND";
        var parts = new List<string>();
        foreach (string s in sockets) parts.Add(s + "=" + (t.Find(s) != null ? "ok" : "MISSING"));
        var anim = t.GetComponent<Animator>();
        parts.Add("animatorController=" + (anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NONE"));
        parts.Add("avatar=" + (anim != null && anim.avatar != null ? "yes" : "no"));
        parts.Add("renderers=" + t.GetComponentsInChildren<Renderer>(true).Length);
        parts.Add("skinned=" + t.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
        parts.Add("modelBody=" + (t.Find("Model_Body") != null ? "ok" : "MISSING"));
        return string.Join(", ", parts);
    }

    // =====================================================================
    // Chạy test EditMode bằng reflection (không phụ thuộc API TestRunner lúc biên dịch)
    // =====================================================================
    private static string RunEditModeTests()
    {
        const string asmName = "RongRanChamCham.Tests.EditMode";
        Assembly asm = null;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) if (a.GetName().Name == asmName) { asm = a; break; }
        if (asm == null) return "{\"error\":\"assembly " + asmName + " not loaded\"}";

        int passed = 0;
        var failures = new List<string>();
        foreach (var type in asm.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                bool isTest = false;
                foreach (var attr in method.GetCustomAttributes(false)) if (attr.GetType().Name == "TestAttribute") { isTest = true; break; }
                if (!isTest) continue;
                try
                {
                    object instance = Activator.CreateInstance(type);
                    method.Invoke(instance, null);
                    passed++;
                }
                catch (Exception ex)
                {
                    Exception inner = ex.InnerException ?? ex;
                    failures.Add(type.Name + "." + method.Name + ": " + inner.Message);
                }
            }
        }

        var sb = new StringBuilder("{");
        Add(sb, "passed", passed.ToString());
        Add(sb, "failed", failures.Count.ToString());
        Add(sb, "failures", Arr(failures), true);
        sb.Append("}");
        return sb.ToString();
    }

    // =====================================================================
    // Tiện ích
    // =====================================================================
    private static void Add(StringBuilder sb, string key, string rawValue, bool last = false)
    {
        sb.Append('"').Append(key).Append("\":").Append(rawValue);
        if (!last) sb.Append(',');
    }

    private static string F(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? "0" : v.ToString("0.###", Inv); }

    private static string Q(string s)
    {
        if (s == null) return "\"\"";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }

    private static string Arr(List<string> list)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); sb.Append(Q(list[i])); }
        return sb.Append(']').ToString();
    }

    private static int ParseInt(string s, int fallback) { return int.TryParse(s, NumberStyles.Integer, Inv, out int v) ? v : fallback; }
    private static float ParseFloat(string s, float fallback) { return float.TryParse(s, NumberStyles.Float, Inv, out float v) ? v : fallback; }

    private static DuelDirection ParseDir(string s)
    {
        if (s == "left") return DuelDirection.Left;
        if (s == "right") return DuelDirection.Right;
        if (s == "down") return DuelDirection.Down;
        return DuelDirection.Straight;
    }

    private static string Sanitize(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name) if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
        return sb.Length > 0 ? sb.ToString() : "shot";
    }
}
#endif
