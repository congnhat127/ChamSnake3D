using UnityEngine;

namespace RongRanChamCham
{
    /// <summary>
    /// Giữ mọi thực thể trong sân và ngoài vật cản.
    /// Hình học lấy từ ArenaDefinition của màn đang chạy; nếu scene không có thì dùng elip Màn 1 trong GameConstants.
    /// Kích thước di chuyển được = mép sân - GameConstants.ArenaWallMargin - padding (bán kính thân của thực thể).
    /// </summary>
    public static class ArenaBoundary
    {
        private static ArenaDefinition activeArena;

        public static ArenaDefinition ActiveArena => activeArena;

        public static void SetActiveArena(ArenaDefinition arena)
        {
            activeArena = arena;
        }

        public static void ClearActiveArena(ArenaDefinition arena)
        {
            if (activeArena == arena) activeArena = null;
        }

        /// <summary>Kẹp vị trí vào trong biên sân và đẩy ra khỏi vật cản. Trả về y = 0.</summary>
        public static Vector3 ClampToArena(Vector3 pos, float padding = 0.5f)
        {
            ArenaDefinition arena = activeArena;
            Vector2 p = ClampToShape(arena, new Vector2(pos.x, pos.z), padding);

            if (arena != null && HasObstacles(arena))
            {
                p = PushOutOfObstacles(arena, p, padding);
                // Đẩy khỏi vật cản có thể đưa ra ngoài mép sân, nên kẹp lại lần nữa.
                p = ClampToShape(arena, p, padding);
            }

            return new Vector3(p.x, 0f, p.y);
        }

        /// <summary>true nếu vị trí đã nằm trong sân và ngoài vật cản (sai số <paramref name="tolerance"/> mét).</summary>
        public static bool IsInside(Vector3 pos, float padding, float tolerance = 0.01f)
        {
            Vector3 clamped = ClampToArena(pos, padding);
            float dx = clamped.x - pos.x;
            float dz = clamped.z - pos.z;
            return dx * dx + dz * dz <= tolerance * tolerance;
        }

        /// <summary>Lấy một điểm ngẫu nhiên phân bố đều trong vùng di chuyển được của sân.</summary>
        public static Vector3 RandomPointInside(float padding)
        {
            Vector2 center;
            Vector2 half;
            GetBounds(activeArena, padding, out center, out half);

            for (int i = 0; i < RandomPointMaxTries; i++)
            {
                Vector3 candidate = new Vector3(
                    center.x + Random.Range(-half.x, half.x),
                    0f,
                    center.y + Random.Range(-half.y, half.y));
                if (IsInside(candidate, padding)) return candidate;
            }

            // Không tìm được điểm hợp lệ (sân gần kín vật cản): lấy điểm gần tâm nhất mà hợp lệ.
            return ClampToArena(new Vector3(center.x, 0f, center.y), padding);
        }

        // Tỉ lệ diện tích elip / hình chữ nhật bao là pi/4 (~0.79), nên 24 lần thử gần như chắc chắn trúng.
        private const int RandomPointMaxTries = 24;

        private static bool HasObstacles(ArenaDefinition arena)
        {
            return (arena.obstacles != null && arena.obstacles.Length > 0) || arena.MoverCount > 0;
        }

        private static void GetBounds(ArenaDefinition arena, float padding, out Vector2 center, out Vector2 half)
        {
            float inset = GameConstants.ArenaWallMargin + padding;
            if (arena == null)
            {
                center = Vector2.zero;
                half = new Vector2(GameConstants.ArenaLimitA - padding, GameConstants.ArenaLimitB - padding);
            }
            else if (arena.shape == ArenaShape.Ellipse)
            {
                center = Vector2.zero;
                half = new Vector2(arena.ellipseRadii.x - inset, arena.ellipseRadii.y - inset);
            }
            else
            {
                center = arena.rectCenter;
                half = new Vector2(arena.rectHalfExtents.x - inset, arena.rectHalfExtents.y - inset);
            }
            half.x = Mathf.Max(half.x, MinExtent);
            half.y = Mathf.Max(half.y, MinExtent);
        }

        // Tránh chia cho 0 khi padding lớn hơn kích thước sân.
        private const float MinExtent = 0.1f;

        public static Vector2 ClampToShape(ArenaDefinition arena, Vector2 p, float padding)
        {
            Vector2 center;
            Vector2 half;
            GetBounds(arena, padding, out center, out half);

            if (arena == null || arena.shape == ArenaShape.Ellipse)
            {
                return ClampToEllipse(p, half.x, half.y);
            }

            float corner = Mathf.Max(arena.rectCornerRadius - GameConstants.ArenaWallMargin - padding, MinExtent);
            return ClampToRoundRect(p, center, half, corner);
        }

        /// <summary>Kẹp điểm vào elip tâm gốc, bán trục a (X) và b (Z).</summary>
        public static Vector2 ClampToEllipse(Vector2 p, float a, float b)
        {
            float norm = (p.x * p.x) / (a * a) + (p.y * p.y) / (b * b);
            if (norm > 1f) p /= Mathf.Sqrt(norm);
            return p;
        }

        /// <summary>Kẹp điểm vào hình chữ nhật bo góc.</summary>
        public static Vector2 ClampToRoundRect(Vector2 p, Vector2 center, Vector2 half, float cornerRadius)
        {
            cornerRadius = Mathf.Min(cornerRadius, Mathf.Min(half.x, half.y));
            float dx = p.x - center.x;
            float dz = p.y - center.y;
            float qx = Mathf.Abs(dx) - (half.x - cornerRadius);
            float qz = Mathf.Abs(dz) - (half.y - cornerRadius);

            if (qx > 0f && qz > 0f)
            {
                // Vùng góc: kẹp vào cung tròn bo góc.
                float d = Mathf.Sqrt(qx * qx + qz * qz);
                if (d > cornerRadius)
                {
                    qx *= cornerRadius / d;
                    qz *= cornerRadius / d;
                }
                return new Vector2(
                    center.x + Mathf.Sign(dx) * (half.x - cornerRadius + qx),
                    center.y + Mathf.Sign(dz) * (half.y - cornerRadius + qz));
            }

            return new Vector2(
                center.x + Mathf.Clamp(dx, -half.x, half.x),
                center.y + Mathf.Clamp(dz, -half.y, half.y));
        }

        /// <summary>Đẩy điểm ra khỏi hình tròn tâm c nếu đang cách tâm gần hơn minDistance.</summary>
        public static Vector2 PushOutOfCircle(Vector2 p, Vector2 c, float minDistance)
        {
            Vector2 d = p - c;
            float sqr = d.sqrMagnitude;
            if (sqr >= minDistance * minDistance) return p;

            float len = Mathf.Sqrt(sqr);
            if (len < DegenerateDistance)
            {
                // Đứng đúng tâm vật cản: không có hướng đẩy tự nhiên, chọn +X cho xác định.
                return c + Vector2.right * minDistance;
            }
            return c + d / len * minDistance;
        }

        private const float DegenerateDistance = 0.0001f;

        private static Vector2 PushOutOfObstacles(ArenaDefinition arena, Vector2 p, float bodyRadius)
        {
            ArenaObstacle[] obstacles = arena.obstacles;
            if (obstacles != null)
            {
                for (int i = 0; i < obstacles.Length; i++)
                {
                    p = PushOutOfCircle(p, obstacles[i].center, obstacles[i].radius + bodyRadius);
                }
            }

            int moverCount = arena.MoverCount;
            for (int i = 0; i < moverCount; i++)
            {
                p = PushOutOfCircle(p, arena.GetMoverPosition(i), arena.movers[i].radius + bodyRadius);
            }

            return p;
        }
    }
}
