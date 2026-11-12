using System.Collections.Generic;
using UnityEngine;

// Điều khiển chơi thử: kéo/chạm trên sân hoặc W A S D để dẫn đoàn; thầy thuốc đuổi theo đuôi.
// Toàn bộ mô phỏng chạy trong hệ tọa độ thiết kế, rồi đổi sang Unity bằng sx, sz (do menu dựng màn tự đặt).
public class RongRanGame : MonoBehaviour
{
    public TextAsset configJson;
    public Transform mother, doctor;
    public Transform[] kids;
    public Transform[] movers;
    public Camera cam;
    [Tooltip("Hệ số đổi trục từ tọa độ thiết kế sang Unity, menu dựng màn tự đặt")] public float sx = -1f, sz = 1f;

    [Header("Thông số chơi")]
    public float kidSpacing = 0.88f;
    public float headSpeed = 5.2f;
    public float doctorSpeed = 3.6f;
    public float edgeMargin = 0.6f;
    public float bodyRadius = 0.42f;
    public float blockDistance = 0.95f;
    public float catchDistance = 0.72f;

    RRLevelConfig cfg;
    readonly List<Vector2> path = new List<Vector2>();
    Vector2 head, doctorPos;
    Vector2? target;
    float cooldown, msgTimer, t;
    string msg = "";
    Vector2[] moverPos;
    readonly Dictionary<Transform, float> phase = new Dictionary<Transform, float>();
    readonly Dictionary<Transform, Transform[]> parts = new Dictionary<Transform, Transform[]>();

    Vector3 W(Vector2 p) { return new Vector3(p.x * sx, 0, p.y * sz); }
    Vector2 D(Vector3 w) { return new Vector2(w.x / sx, w.z / sz); }

    void Start()
    {
        cfg = JsonUtility.FromJson<RRLevelConfig>(configJson.text);
        if (cam == null) cam = Camera.main;
        moverPos = new Vector2[movers != null ? movers.Length : 0];
        ResetActors();
    }

    public void ResetActors()
    {
        path.Clear();
        bool ell = cfg.boundaryType == "ellipse";
        float ax = ell ? cfg.A * 0.78f : cfg.hx * 0.82f, bz = ell ? cfg.B * 0.78f : cfg.hz * 0.82f, cz = ell ? 0 : cfg.cz;
        for (int i = 0; i <= 80; i++)
        {
            float a = (cfg.spawnA0 + i * 1.4f) * Mathf.Deg2Rad;
            path.Add(new Vector2(Mathf.Cos(a) * ax, cz + Mathf.Sin(a) * bz));
        }
        head = path[0];
        doctorPos = ell ? new Vector2(8f, 2.2f) : new Vector2(8.6f, 1.5f);
        target = null; cooldown = 0;
        if (mother) mother.position = W(head);
        for (int i = 0; kids != null && i < kids.Length; i++) if (kids[i]) kids[i].position = W(PointOnPath((i + 1) * kidSpacing));
        if (doctor) doctor.position = W(doctorPos);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        t += dt;

        // Vật cản di động
        for (int i = 0; i < moverPos.Length; i++)
        {
            var m = cfg.movers[i];
            Vector2 p = MoverPos(m, t);
            float d = (p - moverPos[i]).magnitude;
            moverPos[i] = p;
            bool ball = m.kind == "snowball";
            Animate(movers[i], p, dt, d > 0.0005f && !ball);
            if (ball && d < 1f) { var b = FindDeep(movers[i], "Ball"); if (b) b.Rotate(Vector3.right, d / 0.75f * Mathf.Rad2Deg, Space.Self); }
        }

        // Điều khiển đầu rồng
        Vector2 prev = head, dir = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir.y -= 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir.y += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir.x += 1;
        bool pressing = Input.GetMouseButton(0);
        if (pressing && cam)
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(r, out float e)) target = D(r.GetPoint(e));
        }
        if (dir.sqrMagnitude > 0) target = null;
        else if (target.HasValue) { dir = target.Value - head; if (dir.magnitude < 0.15f) { dir = Vector2.zero; if (!pressing) target = null; } }
        if (dir.sqrMagnitude > 0) head += dir.normalized * headSpeed * dt;
        head = Clamp(head); head = PushOut(head, bodyRadius); head = Clamp(head);
        bool moved = (head - prev).sqrMagnitude > 0.0000005f;

        if ((head - path[0]).magnitude > 0.08f) { path.Insert(0, head); if (path.Count > 160) path.RemoveAt(path.Count - 1); }
        else path[0] = head;

        Animate(mother, head, dt, moved);
        Vector2 tail = head;
        for (int i = 0; kids != null && i < kids.Length; i++) { Vector2 p = PointOnPath((i + 1) * kidSpacing); Animate(kids[i], p, dt, moved); tail = p; }

        // Thầy thuốc đuổi theo đuôi, đầu rồng chắn đường
        if (cooldown > 0) cooldown -= dt;
        else { Vector2 to = tail - doctorPos; if (to.magnitude > 0.01f) doctorPos += to.normalized * doctorSpeed * dt; }
        Vector2 hd = doctorPos - head;
        if (hd.magnitude < blockDistance && hd.magnitude > 0.001f) doctorPos = head + hd.normalized * blockDistance;
        doctorPos = Clamp(PushOut(Clamp(doctorPos), bodyRadius));
        Animate(doctor, doctorPos, dt, cooldown <= 0);

        if (cooldown <= 0 && (doctorPos - tail).magnitude < catchDistance)
        {
            msg = "Thầy thuốc chạm đuôi! Vào Cham Cham Cham"; msgTimer = 1.6f; cooldown = 2.2f;
            doctorPos = Clamp(doctorPos + (doctorPos - head).normalized * 3f);
            // TODO: chuyển sang minigame Cham Cham Cham tại đây
        }
        if (msgTimer > 0) msgTimer -= Time.deltaTime;
    }

    Vector2 MoverPos(RRMover m, float time)
    {
        if (m.hasLoop)
        {
            float a = (m.dir == 0 ? 1 : m.dir) * time * m.speed / Mathf.Max(m.lrx, m.lrz) + m.ph;
            return new Vector2(m.lcx + Mathf.Cos(a) * m.lrx, m.lcz + Mathf.Sin(a) * m.lrz);
        }
        float len = Vector2.Distance(new Vector2(m.ax, m.az), new Vector2(m.bx, m.bz)), go = len / m.speed, cyc = 2 * go + 2 * m.pause;
        float ph = ((time + m.t0 * cyc) % cyc + cyc) % cyc, f;
        if (ph < go) f = ph / go; else if (ph < go + m.pause) f = 1; else if (ph < 2 * go + m.pause) f = 1 - (ph - go - m.pause) / go; else f = 0;
        return new Vector2(m.ax + (m.bx - m.ax) * f, m.az + (m.bz - m.az) * f);
    }

    bool IsEllipse { get { return cfg.boundaryType == "ellipse"; } }

    Vector2 Clamp(Vector2 v)
    {
        float m = edgeMargin;
        if (IsEllipse)
        {
            float a = cfg.A - m, b = cfg.B - m, k = v.x * v.x / (a * a) + v.y * v.y / (b * b);
            if (k > 1) v /= Mathf.Sqrt(k);
            return v;
        }
        float hx = cfg.hx - m, hz = cfg.hz - m, R = Mathf.Max(0.1f, cfg.R - m);
        float dx = v.x - cfg.cx, dz = v.y - cfg.cz;
        float qx = Mathf.Abs(dx) - (hx - R), qz = Mathf.Abs(dz) - (hz - R);
        if (qx > 0 && qz > 0)
        {
            float d = Mathf.Sqrt(qx * qx + qz * qz);
            if (d > R) { qx *= R / d; qz *= R / d; }
            return new Vector2(cfg.cx + Mathf.Sign(dx) * (hx - R + qx), cfg.cz + Mathf.Sign(dz) * (hz - R + qz));
        }
        return new Vector2(cfg.cx + Mathf.Clamp(dx, -hx, hx), cfg.cz + Mathf.Clamp(dz, -hz, hz));
    }

    Vector2 PushOut(Vector2 v, float rad)
    {
        if (cfg.obstacles != null)
            foreach (var o in cfg.obstacles) v = Push(v, new Vector2(o.x, o.z), o.r + rad);
        for (int i = 0; i < moverPos.Length; i++) v = Push(v, moverPos[i], cfg.movers[i].r + rad);
        return v;
    }
    static Vector2 Push(Vector2 v, Vector2 c, float min)
    {
        Vector2 d = v - c; float l = d.magnitude;
        return (l < min && l > 0.0001f) ? c + d / l * min : v;
    }

    Vector2 PointOnPath(float dist)
    {
        float acc = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float seg = Vector2.Distance(path[i], path[i + 1]);
            if (acc + seg >= dist) return Vector2.Lerp(path[i], path[i + 1], (dist - acc) / Mathf.Max(seg, 0.0001f));
            acc += seg;
        }
        return path[path.Count - 1];
    }

    void Animate(Transform ch, Vector2 to, float dt, bool moving)
    {
        if (ch == null) return;
        Vector3 w = W(to), d = w - ch.position; d.y = 0;
        if (d.sqrMagnitude > 0.000004f)
        {
            Quaternion want = Quaternion.LookRotation(d) * Quaternion.Euler(0, sz < 0 ? 180 : 0, 0);
            ch.rotation = Quaternion.Slerp(ch.rotation, want, Mathf.Min(1f, dt * 10f));
        }
        ch.position = w;
        if (!parts.TryGetValue(ch, out var p)) { p = new[] { FindDeep(ch, "Rig"), FindDeep(ch, "FootL"), FindDeep(ch, "FootR") }; parts[ch] = p; }
        if (!phase.ContainsKey(ch)) phase[ch] = Random.value * 6f;
        if (moving) phase[ch] += dt * 11f;
        float ph = phase[ch], s = moving ? 1f : 0f;
        if (p[0]) { var lp = p[0].localPosition; p[0].localPosition = new Vector3(lp.x, Mathf.Abs(Mathf.Sin(ph)) * 0.08f * s, lp.z); }
        if (p[1]) { var lp = p[1].localPosition; p[1].localPosition = new Vector3(lp.x, lp.y, (0.05f + Mathf.Sin(ph) * 0.16f * s) * sz); }
        if (p[2]) { var lp = p[2].localPosition; p[2].localPosition = new Vector3(lp.x, lp.y, (0.05f - Mathf.Sin(ph) * 0.16f * s) * sz); }
    }

    public static Transform FindDeep(Transform t, string name)
    {
        if (t == null) return null;
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
        return null;
    }

    void OnGUI()
    {
        if (msgTimer <= 0) return;
        var st = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.035f), fontStyle = FontStyle.Bold };
        st.normal.textColor = Color.white;
        float w = Screen.width * 0.6f, h = Screen.height * 0.12f;
        GUI.Box(new Rect((Screen.width - w) / 2, Screen.height * 0.36f, w, h), msg, st);
    }
}
