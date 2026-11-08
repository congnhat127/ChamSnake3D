using UnityEngine;

namespace RongRanChamCham
{
    public static class GameConstants
    {
        // Arena Dimensions
        public const float ArenaRadius = 14.5f;

        // Mother Speed & Physics (Mobile Balanced)
        public const float MotherNormalSpeed = 6.5f;
        public const float MotherStrafeSpeed = 6.0f;
        public const float MotherTurnSpeed = 12.0f;

        // Hunter Speed (Fair - Equal to Mother)
        public const float HunterNormalSpeed = 6.5f;
        public const float HunterFlankSpeed = 6.5f;
        public const float HunterGrabRange = 1.6f;
        public const float HunterGrabWindup = 0.25f;
        public const float HunterGrabCooldown = 0.5f;

        // Snake Chain Physics
        public const int ChildCount = 3;
        public const float ChainLinkDistance = 1.30f;
        public const float ChainFollowSpeed = 18.0f;
        public const float ChainMaxStretch = 1.70f;
        public const float CentrifugalWhipMultiplier = 1.15f;

        // Match Settings
        public const int ConUtMaxHealth = 3;
        public const float ChaseRoundDuration = 45.0f;
        public const float BuffDuration = 7.0f;
    }
}
