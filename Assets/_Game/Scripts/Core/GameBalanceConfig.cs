using UnityEngine;

namespace RongRanChamCham
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "RongRanChamCham/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Arena Dimensions")]
        [Tooltip("Bán kính tối đa của sân đấu (meters)")]
        public float arenaRadius = GameConstants.ArenaRadius;

        [Header("Rắn Mẹ (Mother Balance)")]
        [Tooltip("Tốc độ di chuyển tiến thông thường (m/s)")]
        public float motherNormalSpeed = GameConstants.MotherNormalSpeed;
        [Tooltip("Tốc độ khi đi lùi / đi ngang ở thế khóa hướng (m/s)")]
        public float motherStrafeSpeed = GameConstants.MotherStrafeSpeed;
        [Tooltip("Tốc độ xoay người")]
        public float motherTurnSpeed = GameConstants.MotherTurnSpeed;

        [Header("Thợ Săn / Thầy Thuốc (Hunter Balance)")]
        [Tooltip("Tốc độ tối đa khi rượt đuổi thẳng (m/s)")]
        public float hunterNormalSpeed = GameConstants.HunterNormalSpeed;
        [Tooltip("Tốc độ khi chạy lách sườn (m/s)")]
        public float hunterFlankSpeed = GameConstants.HunterFlankSpeed;
        [Tooltip("Khoảng cách tối đa kích hoạt vồ bắt Út (m)")]
        public float hunterGrabRange = GameConstants.HunterGrabRange;
        [Tooltip("Thời gian chuẩn bị vung tay (windup seconds)")]
        public float hunterGrabWindup = GameConstants.HunterGrabWindup;
        [Tooltip("Thời gian khựng nếu vồ hụt (cooldown seconds)")]
        public float hunterGrabCooldown = GameConstants.HunterGrabCooldown;

        [Header("Chuỗi Đàn Con (Snake Chain Physics)")]
        [Tooltip("Số lượng con trong đàn")]
        public int childCount = GameConstants.ChildCount;
        [Tooltip("Khoảng cách tiêu chuẩn giữa các con (m)")]
        public float chainLinkDistance = GameConstants.ChainLinkDistance;
        [Tooltip("Tốc độ bám theo đường đi của Mẹ")]
        public float chainFollowSpeed = GameConstants.ChainFollowSpeed;
        [Tooltip("Độ dãn tối đa cho phép trước khi kéo giật")]
        public float chainMaxStretch = GameConstants.ChainMaxStretch;
        [Tooltip("Hệ số văng ly tâm khi Mẹ quay cua gấp")]
        public float centrifugalWhipMultiplier = GameConstants.CentrifugalWhipMultiplier;

        [Header("Trận Đấu & Match Rules")]
        [Tooltip("Tổng số tim ban đầu của Rồng Rắn")]
        public float maxHearts = GameConstants.MaxHearts;
        [Tooltip("Số tim bị trừ khi thắng Chăm Chăm Chăm")]
        public float heartLossOnWin = GameConstants.HeartLossOnWin;
        [Tooltip("Thời gian một trận đấu (seconds)")]
        public float chaseRoundDuration = GameConstants.ChaseRoundDuration;
        [Tooltip("Thời lượng của Buff thức ăn (seconds)")]
        public float buffDuration = GameConstants.BuffDuration;
        [Tooltip("Thời lượng phân thân Bánh Gạo Cay (seconds)")]
        public float cloneDuration = GameConstants.CloneDuration;
        [Tooltip("Thời gian sau khi phân thân thì làm mờ (seconds)")]
        public float cloneFadeDelay = GameConstants.CloneFadeDelay;
    }
}

