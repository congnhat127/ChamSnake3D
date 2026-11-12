using NUnit.Framework;
using UnityEngine;

namespace RongRanChamCham.Tests
{
    public class ArenaBoundaryTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Ellipse_PointInside_IsUnchanged()
        {
            Vector2 p = new Vector2(3f, 2f);
            Vector2 clamped = ArenaBoundary.ClampToEllipse(p, 11.5f, 5.4f);
            Assert.AreEqual(p.x, clamped.x, Tolerance);
            Assert.AreEqual(p.y, clamped.y, Tolerance);
        }

        [Test]
        public void Ellipse_PointsFarOutside_LandOnEdge()
        {
            const float a = 11.5f, b = 5.4f;
            for (int deg = 0; deg < 360; deg += 15)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector2 far = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 50f;
                Vector2 c = ArenaBoundary.ClampToEllipse(far, a, b);
                float norm = c.x * c.x / (a * a) + c.y * c.y / (b * b);
                Assert.AreEqual(1f, norm, Tolerance, $"góc {deg}");
            }
        }

        [Test]
        public void RoundRect_PointInside_IsUnchanged()
        {
            Vector2 center = new Vector2(0f, -0.4f);
            Vector2 half = new Vector2(11f, 5.5f);
            Vector2 p = new Vector2(-6f, 3f);
            Vector2 clamped = ArenaBoundary.ClampToRoundRect(p, center, half, 2f);
            Assert.AreEqual(p.x, clamped.x, Tolerance);
            Assert.AreEqual(p.y, clamped.y, Tolerance);
        }

        [Test]
        public void RoundRect_PointBeyondStraightEdge_ClampsToEdge()
        {
            Vector2 center = new Vector2(0f, -0.4f);
            Vector2 half = new Vector2(11f, 5.5f);
            Vector2 clamped = ArenaBoundary.ClampToRoundRect(new Vector2(40f, 1f), center, half, 2f);
            Assert.AreEqual(11f, clamped.x, Tolerance);
            Assert.AreEqual(1f, clamped.y, Tolerance);
        }

        [Test]
        public void RoundRect_PointBeyondCorner_LandsOnCornerArc()
        {
            Vector2 center = Vector2.zero;
            Vector2 half = new Vector2(10f, 6f);
            const float r = 2f;
            Vector2 clamped = ArenaBoundary.ClampToRoundRect(new Vector2(30f, 30f), center, half, r);

            Vector2 arcCenter = new Vector2(half.x - r, half.y - r);
            Assert.AreEqual(r, Vector2.Distance(clamped, arcCenter), Tolerance);
            Assert.LessOrEqual(clamped.x, half.x + Tolerance);
            Assert.LessOrEqual(clamped.y, half.y + Tolerance);
        }

        [Test]
        public void RoundRect_ClampIsIdempotent()
        {
            Vector2 center = new Vector2(0f, -0.4f);
            Vector2 half = new Vector2(11f, 5.5f);
            for (int deg = 0; deg < 360; deg += 10)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector2 far = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 40f;
                Vector2 once = ArenaBoundary.ClampToRoundRect(far, center, half, 2.1f);
                Vector2 twice = ArenaBoundary.ClampToRoundRect(once, center, half, 2.1f);
                Assert.AreEqual(0f, Vector2.Distance(once, twice), Tolerance, $"góc {deg}");
            }
        }

        [Test]
        public void PushOutOfCircle_PointInside_MovesToRim()
        {
            Vector2 c = new Vector2(2f, 1f);
            Vector2 pushed = ArenaBoundary.PushOutOfCircle(new Vector2(2.3f, 1f), c, 1.2f);
            Assert.AreEqual(1.2f, Vector2.Distance(pushed, c), Tolerance);
            Assert.AreEqual(1f, pushed.y, Tolerance);
        }

        [Test]
        public void PushOutOfCircle_PointOutside_IsUnchanged()
        {
            Vector2 p = new Vector2(5f, 5f);
            Vector2 pushed = ArenaBoundary.PushOutOfCircle(p, Vector2.zero, 1.2f);
            Assert.AreEqual(0f, Vector2.Distance(p, pushed), Tolerance);
        }

        [Test]
        public void PushOutOfCircle_PointAtCenter_StillEndsOnRim()
        {
            Vector2 c = new Vector2(-3f, 4f);
            Vector2 pushed = ArenaBoundary.PushOutOfCircle(c, c, 0.9f);
            Assert.AreEqual(0.9f, Vector2.Distance(pushed, c), Tolerance);
        }

        [Test]
        public void DefaultArena_MatchesLevelOneEllipse()
        {
            // Không có ArenaDefinition nào đang bật: dùng elip Màn 1 trong GameConstants.
            Assert.IsNull(ArenaBoundary.ActiveArena, "Test này cần chạy ở Edit Mode, không có ArenaDefinition đang bật.");
            const float padding = 0.5f;
            Vector3 c = ArenaBoundary.ClampToArena(new Vector3(100f, 3f, 0f), padding);
            Assert.AreEqual(GameConstants.ArenaLimitA - padding, c.x, Tolerance);
            Assert.AreEqual(0f, c.y, Tolerance);
            Assert.AreEqual(0f, c.z, Tolerance);
        }

        [Test]
        public void Mover_PingPong_StaysOnSegmentAndReturns()
        {
            var m = new ArenaMover
            {
                loop = false,
                pointA = new Vector2(-7.2f, 0f),
                pointB = new Vector2(7.2f, 0f),
                speed = 1.6f,
                pause = 0.9f
            };
            float travel = 14.4f / 1.6f;

            Assert.AreEqual(0f, Vector2.Distance(m.pointA, ArenaDefinition.EvaluateMover(m, 0f)), Tolerance);
            Assert.AreEqual(0f, Vector2.Distance(m.pointB, ArenaDefinition.EvaluateMover(m, travel + 0.4f)), Tolerance);
            Assert.AreEqual(0f, Vector2.Distance(m.pointA, ArenaDefinition.EvaluateMover(m, 2f * travel + 0.9f + 0.4f)), Tolerance);

            for (float t = 0f; t < 60f; t += 0.37f)
            {
                Vector2 p = ArenaDefinition.EvaluateMover(m, t);
                Assert.GreaterOrEqual(p.x, -7.2f - Tolerance);
                Assert.LessOrEqual(p.x, 7.2f + Tolerance);
                Assert.AreEqual(0f, p.y, Tolerance);
            }
        }

        [Test]
        public void Mover_Loop_StaysOnEllipse()
        {
            var m = new ArenaMover
            {
                loop = true,
                loopCenter = new Vector2(0f, -0.4f),
                loopRadii = new Vector2(-7.2f, 3.8f),
                speed = 3.6f,
                loopDirection = 1f
            };
            for (float t = 0f; t < 30f; t += 0.41f)
            {
                Vector2 p = ArenaDefinition.EvaluateMover(m, t) - m.loopCenter;
                float norm = p.x * p.x / (7.2f * 7.2f) + p.y * p.y / (3.8f * 3.8f);
                Assert.AreEqual(1f, norm, Tolerance);
            }
        }
    }
}
