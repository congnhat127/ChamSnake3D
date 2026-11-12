using System;
using UnityEngine;

namespace RongRanChamCham
{
    public enum ArenaShape
    {
        Ellipse,
        RoundRect
    }

    /// <summary>Vật cản tĩnh hình tròn trên mặt sân (toạ độ world XZ).</summary>
    [Serializable]
    public struct ArenaObstacle
    {
        public Vector2 center;
        public float radius;
    }

    /// <summary>
    /// Vật cản di động (cầu tuyết, xe điện). Chạy qua lại giữa hai điểm hoặc chạy vòng elip.
    /// Mọi toạ độ là world XZ, đã đổi từ hệ thiết kế lúc bake.
    /// </summary>
    [Serializable]
    public class ArenaMover
    {
        public Transform visual;
        public float radius;
        [Tooltip("true: chạy vòng elip; false: chạy qua lại giữa pointA và pointB")]
        public bool loop;
        public Vector2 pointA;
        public Vector2 pointB;
        public Vector2 loopCenter;
        [Tooltip("Bán trục của vòng chạy, có dấu theo hướng trục của bối cảnh")]
        public Vector2 loopRadii;
        public float speed;
        public float pause;
        [Tooltip("Độ lệch pha ban đầu của chu kỳ qua lại, 0..1")]
        public float cycleOffset01;
        [Tooltip("Góc pha ban đầu của vòng chạy (radian)")]
        public float loopPhase;
        public float loopDirection = 1f;
        [Tooltip("Vật lăn (cầu tuyết) thay vì quay đầu theo hướng chạy")]
        public bool rolls;
    }

    /// <summary>
    /// Hình học sân đấu của một màn: biên, vật cản tĩnh, vật cản di động, vị trí xuất phát.
    /// Dữ liệu được bake từ Assets/RongRan/Configs/ManX.json bởi công cụ editor BakeArenaDefinitions.
    /// Khi component này bật, ArenaBoundary dùng nó thay cho elip mặc định trong GameConstants.
    /// </summary>
    public class ArenaDefinition : MonoBehaviour
    {
        [Tooltip("Số thứ tự màn mà scene này dựng (1..5)")]
        public int levelIndex = 1;

        [Header("Biên sân (world XZ, kích thước mép sân trước khi trừ lề tường)")]
        public ArenaShape shape = ArenaShape.Ellipse;
        [Tooltip("Ellipse: bán trục X và Z")]
        public Vector2 ellipseRadii = new Vector2(GameConstants.ArenaLimitA + GameConstants.ArenaWallMargin, GameConstants.ArenaLimitB + GameConstants.ArenaWallMargin);
        [Tooltip("RoundRect: tâm")]
        public Vector2 rectCenter;
        [Tooltip("RoundRect: nửa chiều rộng X và Z")]
        public Vector2 rectHalfExtents;
        [Tooltip("RoundRect: bán kính bo góc")]
        public float rectCornerRadius;

        [Header("Vật cản")]
        public ArenaObstacle[] obstacles = Array.Empty<ArenaObstacle>();
        public ArenaMover[] movers = Array.Empty<ArenaMover>();
        [Tooltip("Góc xoay bù quanh trục Y cho model vật cản di động (độ)")]
        public float moverModelYaw;

        [Header("Vị trí xuất phát (world XZ)")]
        public Vector2 motherSpawn = new Vector2(0f, -2.5f);
        public Vector2 motherSpawnForward = new Vector2(0f, 1f);
        public Vector2 hunterSpawn = new Vector2(0f, 6f);

        private Vector2[] moverPositions = Array.Empty<Vector2>();
        private Transform[] moverRollParts = Array.Empty<Transform>();
        private float moverTime;

        public int MoverCount => moverPositions.Length;
        public Vector2 GetMoverPosition(int index) => moverPositions[index];

        private void Awake()
        {
            int count = movers != null ? movers.Length : 0;
            moverPositions = new Vector2[count];
            moverRollParts = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                moverPositions[i] = EvaluateMover(movers[i], 0f);
                if (movers[i].rolls && movers[i].visual != null)
                {
                    moverRollParts[i] = FindDeep(movers[i].visual, "Ball");
                }
                PlaceMoverVisual(i, moverPositions[i], Vector2.zero);
            }
        }

        private void OnEnable()
        {
            ArenaBoundary.SetActiveArena(this);
        }

        private void OnDisable()
        {
            ArenaBoundary.ClearActiveArena(this);
        }

        private void Update()
        {
            if (moverPositions.Length == 0) return;

            // Vật cản chỉ chạy khi đang rượt đuổi, để không xô nhân vật lúc đấu hay lúc xếp lại đội hình.
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Chasing) return;

            moverTime += Time.deltaTime;
            for (int i = 0; i < moverPositions.Length; i++)
            {
                Vector2 next = EvaluateMover(movers[i], moverTime);
                Vector2 delta = next - moverPositions[i];
                moverPositions[i] = next;
                PlaceMoverVisual(i, next, delta);
            }
        }

        private void PlaceMoverVisual(int index, Vector2 pos, Vector2 delta)
        {
            Transform visual = movers[index].visual;
            if (visual == null) return;

            visual.position = new Vector3(pos.x, visual.position.y, pos.y);

            float moved = delta.magnitude;
            if (moved <= MinMoveForFacing) return;

            Vector3 dir = new Vector3(delta.x, 0f, delta.y) / moved;
            Quaternion facing = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, moverModelYaw, 0f);

            if (movers[index].rolls)
            {
                visual.rotation = facing;
                Transform ball = moverRollParts[index];
                if (ball != null && movers[index].radius > 0f)
                {
                    // Lăn không trượt: góc quay = quãng đường / bán kính.
                    ball.Rotate(Vector3.right, moved / movers[index].radius * Mathf.Rad2Deg, Space.Self);
                }
            }
            else
            {
                visual.rotation = Quaternion.Slerp(visual.rotation, facing, Mathf.Min(1f, Time.deltaTime * MoverTurnRate));
            }
        }

        // Dưới ngưỡng này coi như vật cản đang đứng yên (đang nghỉ ở đầu mút), không đổi hướng.
        private const float MinMoveForFacing = 0.0005f;
        private const float MoverTurnRate = 10f;

        /// <summary>Vị trí vật cản di động tại thời điểm <paramref name="time"/> (giây). Hàm thuần, dùng được trong test.</summary>
        public static Vector2 EvaluateMover(ArenaMover m, float time)
        {
            if (m.loop)
            {
                float longest = Mathf.Max(Mathf.Abs(m.loopRadii.x), Mathf.Abs(m.loopRadii.y));
                if (longest <= 0f) return m.loopCenter;
                float dir = Mathf.Approximately(m.loopDirection, 0f) ? 1f : m.loopDirection;
                float angle = dir * time * m.speed / longest + m.loopPhase;
                return new Vector2(m.loopCenter.x + Mathf.Cos(angle) * m.loopRadii.x, m.loopCenter.y + Mathf.Sin(angle) * m.loopRadii.y);
            }

            float length = Vector2.Distance(m.pointA, m.pointB);
            if (length <= 0f || m.speed <= 0f) return m.pointA;

            float travel = length / m.speed;
            float cycle = 2f * travel + 2f * m.pause;
            float t = Mathf.Repeat(time + m.cycleOffset01 * cycle, cycle);

            float f;
            if (t < travel) f = t / travel;
            else if (t < travel + m.pause) f = 1f;
            else if (t < 2f * travel + m.pause) f = 1f - (t - travel - m.pause) / travel;
            else f = 0f;

            return Vector2.Lerp(m.pointA, m.pointB, f);
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (obstacles != null)
            {
                foreach (var o in obstacles) Gizmos.DrawWireSphere(new Vector3(o.center.x, 0f, o.center.y), o.radius);
            }

            Gizmos.color = Color.cyan;
            const int segments = 72;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                // Chiếu một điểm ở rất xa vào trong biên để vẽ đường mép sân di chuyển được.
                Vector2 far = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 100f;
                Vector2 edge = ArenaBoundary.ClampToShape(this, far, 0f);
                Vector3 p = new Vector3(edge.x, 0.05f, edge.y);
                if (i > 0) Gizmos.DrawLine(prev, p);
                prev = p;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(motherSpawn.x, 0f, motherSpawn.y), 0.4f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(hunterSpawn.x, 0f, hunterSpawn.y), 0.4f);
        }
    }
}
