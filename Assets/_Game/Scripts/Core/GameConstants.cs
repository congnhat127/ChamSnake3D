using UnityEngine;

namespace RongRanChamCham
{
    public static class GameConstants
    {
        // Arena Dimensions (Accurate Ellipse Boundary: A = 12.8m, B = 6.7m with 0.8m margin)
        public const float ArenaLimitA = 12.0f;          // Major axis X (Sân đình làng quê)
        public const float ArenaLimitB = 5.9f;           // Minor axis Z (Tránh văng ra sau mái đình)
        public const float ArenaRadius = 11.5f;
        // Khoảng lùi từ mép sân trong file thiết kế (Man1.json: A=12.8, B=6.7) vào tới tường/bậc thềm: 12.8-12.0 = 6.7-5.9 = 0.8m.
        public const float ArenaWallMargin = 0.8f;

        // Mother Speed & Physics (Mobile Balanced)
        public const float MotherNormalSpeed = 6.0f;
        public const float MotherStrafeSpeed = 5.5f;
        public const float MotherTurnSpeed = 10.0f;

        // Hunter Speed (Fair - Equal to Mother)
        public const float HunterNormalSpeed = 6.0f;
        public const float HunterFlankSpeed = 6.0f;
        public const float HunterGrabRange = 1.05f;      // Precise physical contact range (hand to body)
        public const float HunterPrepareRange = 1.35f;   // Distance to initiate lunge windup
        public const float HunterGrabWindup = 0.22f;
        public const float HunterGrabCooldown = 1.2f;    // Fair pause after miss or hit
        // Khoảng thở cho người chơi khi vào trận hoặc sau khi xếp lại đội hình (giá trị cũ của hàm reset sau khi đấu)
        public const float RoundStartHunterFreeze = 2.0f;      // Thầy Thuốc đứng yên
        public const float RoundStartGrabCooldown = 4.5f;      // Thầy Thuốc chưa được vồ
        public const float RoundStartPlayerPrepTime = 1.8f;    // thời gian banner "tái lập đội hình"

        // Snake Chain Physics (Smooth, Natural Trailing Synchronized with Mother)
        public const int ChildCount = 3;
        public const float ChainLinkDistance = 0.92f;    // Snug natural spacing between kids
        public const float ChainFollowSpeed = 6.8f;      // Matched to Mother speed (NO MORE RUSHING/JITTERING!)
        public const float ChainMaxStretch = 1.25f;
        public const float CentrifugalWhipMultiplier = 1.08f;

        // Match Settings
        public const float MaxHearts = 3.0f;
        public const float HeartLossOnWin = 0.5f;
        public const float ChaseRoundDuration = 90.0f;
        public const float BuffDuration = 10.0f;
        public const float CloneDuration = 5.0f;
        public const float CloneFadeDelay = 2.0f;
    }
}
