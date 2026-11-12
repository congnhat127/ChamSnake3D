using System;
using UnityEngine;
using UnityEngine.AI;

namespace RongRanChamCham
{
    [RequireComponent(typeof(CharacterController))]
    public class HunterAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameBalanceConfig balanceConfig;

        [Header("Targets")]
        [SerializeField] private Transform motherTransform;
        [SerializeField] private ChildFollower conUtTarget;

        [Header("Rig & Sockets")]
        [SerializeField] private Transform socketHead;
        [SerializeField] private Transform socketGrabHand;
        [SerializeField] private GameObject swipeVfxVisual;

        private CharacterController controller;
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private ChibiProceduralAnimator proceduralAnim;
        private MotherController cachedMotherCtrl;

        private Vector3 externalPushVelocity = Vector3.zero;
        private float stunTimer = 0f;
        private float nextGrabAvailableTime = 0f;
        private float currentFlankSign = 1f; // Hysteresis to prevent left-right jitter when facing Mother head-on
        private bool isPerformingGrab = false;
        private float grabWindupTimer = 0f;
        private float verticalVelocity = 0f;

        private Transform hunterBodyTransform;
        private Transform wandTransform;
        private Quaternion targetHunterBodyRot = Quaternion.identity;
        private Quaternion targetWandRot = Quaternion.identity;

        public event Action OnCaughtConUt;
        public bool IsGrabbing => isPerformingGrab;

        private float MoveSpeed => (balanceConfig != null ? balanceConfig.hunterNormalSpeed : GameConstants.HunterNormalSpeed) * LevelSpeedMultiplier;
        private float FlankSpeed => (balanceConfig != null ? balanceConfig.hunterFlankSpeed : GameConstants.HunterFlankSpeed) * LevelSpeedMultiplier;
        private static float LevelSpeedMultiplier => LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelData.hunterSpeedMult : 1f;
        private float GrabRange => balanceConfig != null ? balanceConfig.hunterGrabRange : GameConstants.HunterGrabRange;
        private float GrabWindup => balanceConfig != null ? balanceConfig.hunterGrabWindup : GameConstants.HunterGrabWindup;
        private float GrabCooldown => balanceConfig != null ? balanceConfig.hunterGrabCooldown : GameConstants.HunterGrabCooldown;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.center = new Vector3(0f, 0.85f, 0f);
                controller.height = 1.7f;
                controller.radius = 0.45f;
                controller.minMoveDistance = 0.001f;
            }
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            proceduralAnim = GetComponent<ChibiProceduralAnimator>();
            EnsureSockets();
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketGrabHand == null) socketGrabHand = transform.Find("Socket_Hand_R");
        }

        private void Start()
        {
            EnsureTargets();
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = MoveSpeed;
                navMeshAgent.stoppingDistance = GrabRange * 0.75f;
            }
        }

        public void SetTargets(Transform mother, ChildFollower conUt)
        {
            motherTransform = mother;
            conUtTarget = conUt;
        }

        private void EnsureTargets()
        {
            if (motherTransform == null)
            {
                var motherCtrl = UnityEngine.Object.FindAnyObjectByType<MotherController>();
                if (motherCtrl != null)
                {
                    motherTransform = motherCtrl.transform;
                    cachedMotherCtrl = motherCtrl;
                }
            }
            else if (cachedMotherCtrl == null)
            {
                cachedMotherCtrl = motherTransform.GetComponent<MotherController>();
            }

            var snakeChain = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
            if (snakeChain != null)
            {
                if (conUtTarget == null || !conUtTarget.gameObject.activeInHierarchy || conUtTarget != snakeChain.ConUt)
                {
                    conUtTarget = snakeChain.ConUt;
                }
            }
        }

        private void EnsureDuelTransforms()
        {
            if (hunterBodyTransform == null) hunterBodyTransform = transform.Find("Model_Body");
            if (wandTransform == null)
            {
                wandTransform = transform.Find("Pointer_Wand");
                if (wandTransform == null && socketGrabHand != null) wandTransform = socketGrabHand.Find("Pointer_Wand");
            }
        }

        public void TriggerDuelPoint(DuelDirection dir)
        {
            EnsureDuelTransforms();
            if (animator != null)
            {
                switch (dir)
                {
                    case DuelDirection.Left:
                        animator.SetTrigger("PointLeft");
                        break;
                    case DuelDirection.Right:
                        animator.SetTrigger("PointRight");
                        break;
                    case DuelDirection.Down:
                        animator.SetTrigger("PointDown");
                        break;
                    default:
                        animator.SetTrigger("PointCenter");
                        break;
                }
            }

            // Direct expressive pointing stance & wand angle
            switch (dir)
            {
                case DuelDirection.Left:
                    targetHunterBodyRot = Quaternion.Euler(0f, -48f, -14f);
                    targetWandRot = Quaternion.Euler(0f, -70f, 0f);
                    break;
                case DuelDirection.Right:
                    targetHunterBodyRot = Quaternion.Euler(0f, 48f, 14f);
                    targetWandRot = Quaternion.Euler(0f, 70f, 0f);
                    break;
                case DuelDirection.Straight: // Up
                    targetHunterBodyRot = Quaternion.Euler(-26f, 0f, 0f);
                    targetWandRot = Quaternion.Euler(-55f, 0f, 0f);
                    break;
                case DuelDirection.Down: // Down
                    targetHunterBodyRot = Quaternion.Euler(28f, 0f, 0f);
                    targetWandRot = Quaternion.Euler(55f, 0f, 0f);
                    break;
            }
        }

        public void ResetDuelStance()
        {
            EnsureDuelTransforms();
            targetHunterBodyRot = Quaternion.identity;
            targetWandRot = Quaternion.identity;
            if (hunterBodyTransform != null) hunterBodyTransform.localRotation = Quaternion.identity;
            if (wandTransform != null) wandTransform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Chasing)
            {
                if (proceduralAnim != null)
                {
                    bool hunterDefeated = GameManager.Instance.CurrentState == GameState.Victory;
                    bool hunterWon = GameManager.Instance.CurrentState == GameState.GameOver;
                    proceduralAnim.SetCelebrationMode(hunterWon, hunterDefeated);
                }

                // If in Duel: Thầy Thuốc quay mặt trực diện vào bé Út để đấu QTE
                if (GameManager.Instance.CurrentState == GameState.Duel)
                {
                    EnsureTargets();
                    EnsureDuelTransforms();
                    if (conUtTarget != null)
                    {
                        Vector3 faceUt = conUtTarget.transform.position - transform.position;
                        faceUt.y = 0f;
                        if (faceUt.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceUt), Time.deltaTime * 12f);
                        }
                    }

                    if (hunterBodyTransform != null)
                    {
                        hunterBodyTransform.localRotation = Quaternion.Slerp(hunterBodyTransform.localRotation, targetHunterBodyRot, Time.deltaTime * 16f);
                    }
                    if (wandTransform != null)
                    {
                        wandTransform.localRotation = Quaternion.Slerp(wandTransform.localRotation, targetWandRot, Time.deltaTime * 18f);
                    }

                    if (animator != null)
                    {
                        animator.SetFloat("Speed", 0f);
                        animator.SetBool("IsVictory", false);
                        animator.SetBool("IsAngry", false);
                    }
                    return;
                }

                // If player won: Hunter stands in the distance stomping in anger (Luật 6.2)
                if (GameManager.Instance.CurrentState == GameState.Victory)
                {
                    if (motherTransform != null)
                    {
                        Vector3 toHunter = transform.position - motherTransform.position;
                        toHunter.y = 0f;
                        float dist = toHunter.magnitude;
                        // Move back if too close to celebrating group
                        if (dist < 6.0f)
                        {
                            Vector3 awayDir = dist > 0.1f ? toHunter.normalized : Vector3.back;
                            Vector3 retreatPos = motherTransform.position + awayDir * 6.0f;
                            retreatPos = ArenaBoundary.ClampToArena(retreatPos, 0.45f);
                            transform.position = Vector3.MoveTowards(transform.position, retreatPos, Time.deltaTime * 3.5f);
                        }
                        // Face towards celebrating group in anger
                        Vector3 faceDir = (motherTransform.position - transform.position).normalized;
                        faceDir.y = 0f;
                        if (faceDir.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDir), Time.deltaTime * 4f);
                        }
                    }
                }
                else if (GameManager.Instance.CurrentState == GameState.GameOver)
                {
                    // Hunter won: Face camera and gloat/celebrate!
                    transform.rotation = Quaternion.Slerp(transform.rotation, CameraFollow.FacingCamera(transform.position), Time.deltaTime * 6f);
                }

                if (animator != null)
                {
                    bool hunterDefeated = GameManager.Instance.CurrentState == GameState.Victory;
                    bool hunterWon = GameManager.Instance.CurrentState == GameState.GameOver;
                    animator.SetFloat("Speed", 0f);
                    animator.SetBool("IsVictory", hunterWon);
                    animator.SetBool("IsAngry", hunterDefeated);
                }
                return;
            }

            if (proceduralAnim != null) proceduralAnim.SetCelebrationMode(false, false);

            EnsureTargets();

            // 1. Handle Stun / Windup Cooldowns
            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                if (animator != null) animator.SetFloat("Speed", 0f);
                ApplyExternalPush();
                return;
            }

            if (isPerformingGrab)
            {
                HandleGrabExecution();
                return;
            }

            if (conUtTarget == null) return;

            // 2. Navigation & Steering
            Vector3 targetPos = conUtTarget.transform.position;
            float distToConUt = Vector3.Distance(transform.position, targetPos);
            bool isMotherBlocking = IsMotherBlockingLineOfSight();

            // 3. Trigger Grab Action only if in Range, Facing target, NOT blocked by Mother, & Cooldown elapsed
            Vector3 toConUt = targetPos - transform.position;
            toConUt.y = 0f;
            float facingDot = toConUt.sqrMagnitude > 0.01f ? Vector3.Dot(transform.forward, toConUt.normalized) : 0f;

            if (distToConUt <= GameConstants.HunterPrepareRange && !isMotherBlocking && facingDot > 0.45f && Time.time >= nextGrabAvailableTime)
            {
                StartGrabAction();
                return;
            }

            // 4. Movement Execution
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(targetPos);
            }
            else
            {
                ManualSteering(targetPos, isMotherBlocking);
            }

            ApplyExternalPush();

            if (animator != null)
            {
                float currentSpeed = controller != null ? controller.velocity.magnitude : (navMeshAgent != null ? navMeshAgent.velocity.magnitude : 0f);
                animator.SetFloat("Speed", currentSpeed);
                animator.SetBool("IsVictory", false);
                animator.SetBool("IsAngry", false);
            }
        }

        private bool IsMotherBlockingLineOfSight()
        {
            if (motherTransform == null || conUtTarget == null) return false;

            Vector3 hPos = transform.position;
            hPos.y = 0f;
            Vector3 utPos = conUtTarget.transform.position;
            utPos.y = 0f;
            Vector3 mPos = motherTransform.position;
            mPos.y = 0f;

            var motherCtrl = cachedMotherCtrl != null ? cachedMotherCtrl : (motherTransform != null ? (cachedMotherCtrl = motherTransform.GetComponent<MotherController>()) : null);
            float barrierRadius = motherCtrl != null ? motherCtrl.CurrentDefenseRadius : 1.40f;

            Vector3 toHunter = hPos - mPos;
            float distToMother = toHunter.magnitude;
            float hunterFrontDot = toHunter.sqrMagnitude > 0.01f ? Vector3.Dot(motherTransform.forward, toHunter.normalized) : 0f;

            // 1. Nếu Thầy Thuốc đang ở phía trước mặt Mẹ (bán cầu phòng thủ):
            // Mẹ luôn là vật chắn tuyệt đối bảo vệ đàn con (Luật 4.2). Thầy Thuốc KHÔNG THỂ vồ xuyên qua Mẹ.
            if (hunterFrontDot > -0.20f)
            {
                // Nếu Thầy ở gần phía trước Mẹ (trong tầm sải tay phòng vệ): bị Mẹ cản phá ngay
                if (distToMother < barrierRadius * 1.65f)
                {
                    return true;
                }

                // Nếu Bé Út đang ở phía sau lưng Mẹ:
                // Thầy Thuốc ở đằng trước, Bé Út ở đằng sau => Mẹ đứng chính diện chắn giữa!
                Vector3 toConUt = utPos - mPos;
                float utFrontDot = toConUt.sqrMagnitude > 0.01f ? Vector3.Dot(motherTransform.forward, toConUt.normalized) : 0f;
                if (utFrontDot < 0.35f)
                {
                    return true;
                }
            }

            // 2. Nếu Thầy Thuốc đang ở gần Mẹ hơn Bé Út và đang quay mặt về phía Mẹ:
            float distToUt = Vector3.Distance(hPos, utPos);
            if (distToMother < distToUt)
            {
                Vector3 hunterFaceDir = transform.forward;
                hunterFaceDir.y = 0f;
                if (toHunter.sqrMagnitude > 0.01f && Vector3.Dot(hunterFaceDir, (-toHunter).normalized) > 0.35f)
                {
                    return true;
                }
            }

            // 3. Kiểm tra tia vồ cắt ngang qua sải tay dang rộng của Mẹ (Ray-Capsule / Segment-Distance)
            Vector3 lineDir = utPos - hPos;
            float lineLen = lineDir.magnitude;
            if (lineLen < 0.1f) return false;
            Vector3 lineNorm = lineDir / lineLen;

            float proj = Vector3.Dot(mPos - hPos, lineNorm);
            if (proj > 0f && proj < lineLen)
            {
                Vector3 closestPoint = hPos + lineNorm * proj;
                float distToLine = Vector3.Distance(mPos, closestPoint);
                // Sải tay của Mẹ dang rộng che chở
                if (distToLine < barrierRadius * 1.25f)
                {
                    return true;
                }
            }

            return false;
        }

        private void ManualSteering(Vector3 targetPos, bool isMotherBlocking)
        {
            Vector3 steerTarget = targetPos;

            var motherCtrl = cachedMotherCtrl != null ? cachedMotherCtrl : (motherTransform != null ? (cachedMotherCtrl = motherTransform.GetComponent<MotherController>()) : null);
            float defenseRad = motherCtrl != null ? motherCtrl.CurrentDefenseRadius : 1.40f;

            // Flanking steering: If Mother is blocking direct route to Con Ut, steer laterally around Mother's flank
            if (isMotherBlocking && motherTransform != null)
            {
                Vector3 toMother = motherTransform.position - transform.position;
                toMother.y = 0f;
                Vector3 lateralDir = Vector3.Cross(Vector3.up, toMother.normalized);

                // Pick the flank that leads closer to Con Ut with hysteresis (prevents twitching jitter)
                Vector3 toConUt = targetPos - transform.position;
                toConUt.y = 0f;
                float flankDot = Vector3.Dot(lateralDir, toConUt);
                if (flankDot > 0.25f) currentFlankSign = 1f;
                else if (flankDot < -0.25f) currentFlankSign = -1f;
                lateralDir *= currentFlankSign;

                // Wide flanking clearance so Hunter does not bump into Mother's sải tay
                float flankClearance = defenseRad + 1.15f;
                steerTarget = motherTransform.position + lateralDir * flankClearance;
            }
            else if (motherTransform != null)
            {
                // Soft avoidance if cutting too close to Mother's personal boundary
                float distToMother = Vector3.Distance(transform.position, motherTransform.position);
                if (distToMother < defenseRad + 0.35f)
                {
                    Vector3 away = (transform.position - motherTransform.position).normalized;
                    away.y = 0f;
                    steerTarget += away * 1.2f;
                }
            }

            Vector3 toTarget = steerTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Vector3 moveDir = toTarget.normalized;

                // Face movement direction
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);

                // Apply movement
                if (controller.isGrounded) verticalVelocity = -1.5f;
                else verticalVelocity += Physics.gravity.y * Time.deltaTime;

                // Apply movement with buffs
                float currentSpeed = isMotherBlocking ? FlankSpeed : MoveSpeed;
                if (BuffSystem.Instance != null)
                {
                    if (BuffSystem.Instance.IsHunterSpeedActive) currentSpeed *= 1.35f;

                    float targetScale = BuffSystem.Instance.IsHunterGiantActive ? 1.30f : 1.0f;
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 6f);
                }

                Vector3 moveDelta = moveDir * currentSpeed;
                moveDelta.y = verticalVelocity;
                controller.Move(moveDelta * Time.deltaTime);
                transform.position = ArenaBoundary.ClampToArena(transform.position, 0.45f);
            }
        }

        private void ApplyExternalPush()
        {
            if (externalPushVelocity.sqrMagnitude > 0.01f)
            {
                controller.Move(externalPushVelocity * Time.deltaTime);
                transform.position = ArenaBoundary.ClampToArena(transform.position, 0.45f);
                externalPushVelocity = Vector3.Lerp(externalPushVelocity, Vector3.zero, Time.deltaTime * 5f);
            }
        }

        public void GetShovedByMother(Vector3 shoveDirection, float force)
        {
            externalPushVelocity = shoveDirection * force;
            stunTimer = 0.35f;
            if (isPerformingGrab)
            {
                CancelGrab();
            }
        }

        private void StartGrabAction()
        {
            isPerformingGrab = true;
            grabWindupTimer = GrabWindup;
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = true;
            if (swipeVfxVisual != null) swipeVfxVisual.SetActive(true);
            if (animator != null) animator.SetTrigger("Lunge");
            AudioManager.Instance?.PlaySFX(SoundType.Whoosh);
        }

        private void HandleGrabExecution()
        {
            grabWindupTimer -= Time.deltaTime;
            if (grabWindupTimer <= 0f)
            {
                // Physical Contact Resolution: Vồ phải chạm thật (Luật 4.1)
                // Bàn tay Thầy Thuốc (socketGrabHand) chạm thân mục tiêu trong ngưỡng chạm theo GameBalanceConfig (1.05m).
                float distToTarget = conUtTarget != null ? Vector3.Distance(transform.position, conUtTarget.transform.position) : 999f;
                float currentGrabRange = GrabRange;
                if (BuffSystem.Instance != null && BuffSystem.Instance.IsHunterGiantActive)
                {
                    currentGrabRange *= 1.30f;
                }

                // Tâm thân Bé Út (chiều cao 1.3m -> tâm thân ở y = 0.65m)
                Vector3 targetBodyPos = conUtTarget != null
                    ? (conUtTarget.transform.position + Vector3.up * 0.65f)
                    : (transform.position + transform.forward * 0.85f + Vector3.up * 0.65f);

                Vector3 handPos = socketGrabHand != null
                    ? socketGrabHand.position
                    : (transform.position + transform.forward * 0.85f + Vector3.up * 0.85f);

                float handDistToBody = Vector3.Distance(handPos, targetBodyPos);

                // Facing check: Hunter must be facing Con Ut
                Vector3 toUt = conUtTarget != null ? (conUtTarget.transform.position - transform.position) : Vector3.forward;
                toUt.y = 0f;
                float grabFacingDot = toUt.sqrMagnitude > 0.01f ? Vector3.Dot(transform.forward, toUt.normalized) : 0f;

                bool isBlocked = IsMotherBlockingLineOfSight();
                if (!isBlocked && motherTransform != null)
                {
                    Vector3 toH = transform.position - motherTransform.position;
                    toH.y = 0f;
                    if (toH.magnitude < 2.2f && Vector3.Dot(motherTransform.forward, toH.normalized) > -0.1f)
                    {
                        isBlocked = true;
                    }
                }
                bool hasDirectContact = (handDistToBody <= currentGrabRange) && grabFacingDot >= 0.35f;

                if (isBlocked)
                {
                    // Blocked by Mother's barrier! Hunter gets shoved back
                    Debug.Log("[HunterAI] Mẹ đã kịp thời giương sải tay cản phá đòn vồ của Thầy Thuốc!");
                    if (motherTransform != null)
                    {
                        Vector3 awayFromMother = (transform.position - motherTransform.position).normalized;
                        awayFromMother.y = 0f;
                        GetShovedByMother(awayFromMother, 8.0f);
                    }
                }
                else if (hasDirectContact)
                {
                    bool hunterHasPierce = BuffSystem.Instance != null && BuffSystem.Instance.HunterPierceShield;
                    bool snakeHasShield = BuffSystem.Instance != null && BuffSystem.Instance.SnakeHasShield;

                    if (snakeHasShield && !hunterHasPierce)
                    {
                        // Shield absorbs hit
                        BuffSystem.Instance.ConsumeSnakeShield();
                        Debug.Log("[HunterAI] Lá chắn Bánh Mì của Rồng Rắn đã chặn đứng đòn vồ của Thầy Thuốc!");
                    }
                    else
                    {
                        if (snakeHasShield && hunterHasPierce)
                        {
                            BuffSystem.Instance.ConsumeSnakeShield();
                            Debug.Log("[HunterAI] Thầy Thuốc vồ XUYÊN LÁ CHẮN nhờ hiệu ứng Bánh Mì!");
                        }
                        // Direct physical grab success!
                        Debug.Log($"[HunterAI] THẦY THUỐC ĐÃ CHẠM TAY VÀO BÉ ÚT! (Hand dist: {handDistToBody:F2}m <= {currentGrabRange:F2}m, Body dist: {distToTarget:F2}m)");
                        OnCaughtConUt?.Invoke();
                    }
                }
                else
                {
                    Debug.Log($"[HunterAI] Vồ hụt trong không khí! (Hand dist: {handDistToBody:F2}m > {currentGrabRange:F2}m, Body dist: {distToTarget:F2}m)");
                }

                // Cooldown
                nextGrabAvailableTime = Time.time + GrabCooldown;
                isPerformingGrab = false;
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
                if (swipeVfxVisual != null) swipeVfxVisual.SetActive(false);
            }
        }

        public void ResetAfterDuel(float grabCooldownSeconds = 5.0f, float freezeSeconds = 2.0f)
        {
            CancelGrab();
            nextGrabAvailableTime = Time.time + Mathf.Max(grabCooldownSeconds, 5.0f);
            stunTimer = Mathf.Max(freezeSeconds, 2.0f);
            externalPushVelocity = Vector3.zero;
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
        }

        private void CancelGrab()
        {
            isPerformingGrab = false;
            nextGrabAvailableTime = Time.time + GrabCooldown;
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
            if (swipeVfxVisual != null) swipeVfxVisual.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw Grab Reach
            Gizmos.color = Color.red;
            Vector3 handPos = socketGrabHand != null ? socketGrabHand.position : transform.position + transform.forward * 0.8f + Vector3.up * 1f;
            Gizmos.DrawWireSphere(handPos, GrabRange);
        }
    }
}
