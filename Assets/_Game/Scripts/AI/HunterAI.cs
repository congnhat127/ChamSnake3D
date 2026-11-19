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
        private Vector3 defaultBodyPos;
        private Quaternion defaultBodyRot = Quaternion.identity;
        private Vector3 defaultHandPos;
        private Quaternion defaultHandRot = Quaternion.identity;
        private Vector3 targetHunterBodyPos;
        private Quaternion targetHunterBodyRot = Quaternion.identity;
        private Vector3 targetHandPos;
        private Quaternion targetHandRot = Quaternion.identity;
        private GameObject dizzyStarsRoot;

        public event Action OnCaughtConUt;
        public bool IsGrabbing => isPerformingGrab;

        private float MoveSpeed => (balanceConfig != null ? balanceConfig.hunterNormalSpeed : GameConstants.HunterNormalSpeed) * LevelSpeedMultiplier;
        private float FlankSpeed => (balanceConfig != null ? balanceConfig.hunterFlankSpeed : GameConstants.HunterFlankSpeed) * LevelSpeedMultiplier;
        private static float LevelSpeedMultiplier => LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelData.hunterSpeedMult : 1f;
        private float GrabRange => balanceConfig != null ? balanceConfig.hunterGrabRange : GameConstants.HunterGrabRange;
        private float GrabWindup => balanceConfig != null ? balanceConfig.hunterGrabWindup : GameConstants.HunterGrabWindup;
        private float GrabCooldown => balanceConfig != null ? balanceConfig.hunterGrabCooldown : GameConstants.HunterGrabCooldown;
        private bool HasAnimator => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.isInitialized;

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
            if (animator != null)
            {
                animator.enabled = false;
                animator = null;
            }
            proceduralAnim = GetComponent<ChibiProceduralAnimator>();
            EnsureSockets();
            EnsureDuelTransforms();
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketGrabHand == null) socketGrabHand = transform.Find("Socket_Hand_R");
        }

        private void Start()
        {
            EnsureTargets();
            EnsureDuelTransforms();
            if (hunterBodyTransform != null)
            {
                defaultBodyPos = hunterBodyTransform.localPosition;
                defaultBodyRot = hunterBodyTransform.localRotation;
                targetHunterBodyPos = defaultBodyPos;
                targetHunterBodyRot = defaultBodyRot;
            }
            if (socketGrabHand != null)
            {
                defaultHandPos = socketGrabHand.localPosition;
                defaultHandRot = socketGrabHand.localRotation;
                targetHandPos = defaultHandPos;
                targetHandRot = defaultHandRot;
            }

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
            if (hunterBodyTransform == null)
            {
                if (proceduralAnim != null && proceduralAnim.BodyVisual != null)
                {
                    hunterBodyTransform = proceduralAnim.BodyVisual;
                }
                else
                {
                    var rend = GetComponentInChildren<Renderer>();
                    if (rend != null && rend.transform != transform) hunterBodyTransform = rend.transform;
                }
                if (hunterBodyTransform == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        var child = transform.GetChild(i);
                        if (!child.name.StartsWith("Socket_") && !child.name.Contains("Swipe") && !child.name.Contains("VFX"))
                        {
                            hunterBodyTransform = child;
                            break;
                        }
                    }
                }
            }
            if (socketGrabHand == null) socketGrabHand = transform.Find("Socket_Hand_R");
            if (wandTransform == null)
            {
                if (socketGrabHand != null) wandTransform = socketGrabHand.Find("Pointer_Wand");
                if (wandTransform == null) wandTransform = transform.Find("Pointer_Wand");
                if (wandTransform == null && socketGrabHand != null) wandTransform = socketGrabHand;
            }
        }

        public void PlayDuelChantStomp(int countdownStep)
        {
            EnsureDuelTransforms();
            // Thầy Thuốc giậm chân nhún nhảy và vung gậy theo nhịp hô "CHĂM!"
            targetHunterBodyPos = defaultBodyPos + new Vector3(0f, 0.12f, 0.10f);
            targetHunterBodyRot = defaultBodyRot * Quaternion.Euler(14f, countdownStep % 2 == 0 ? -10f : 10f, 0f);
            if (socketGrabHand != null)
            {
                targetHandPos = defaultHandPos + new Vector3(0f, 0.35f, 0.15f);
                targetHandRot = defaultHandRot * Quaternion.Euler(-50f, 0f, 30f);
            }
        }

        public void PlayDuelTriumphSwipe()
        {
            EnsureDuelTransforms();
            targetHunterBodyPos = defaultBodyPos + new Vector3(0f, 0.15f, 0.05f);
            targetHunterBodyRot = defaultBodyRot * Quaternion.Euler(-18f, 25f, 0f);
            if (socketGrabHand != null)
            {
                targetHandPos = defaultHandPos + new Vector3(0.2f, 0.45f, 0.35f);
                targetHandRot = defaultHandRot * Quaternion.Euler(-65f, 35f, 0f);
            }
        }

        public void PlayDuelFrustration()
        {
            EnsureDuelTransforms();
            targetHunterBodyPos = defaultBodyPos + new Vector3(0f, -0.1f, 0f);
            targetHunterBodyRot = defaultBodyRot * Quaternion.Euler(22f, -15f, 0f);
            if (socketGrabHand != null)
            {
                targetHandPos = defaultHandPos + new Vector3(0f, -0.1f, 0.1f);
                targetHandRot = defaultHandRot * Quaternion.Euler(45f, 0f, 0f);
            }
        }

        public void TriggerDuelPoint(DuelDirection dir)
        {
            EnsureDuelTransforms();
            if (HasAnimator)
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

            Vector3 worldBodyOffset = Vector3.zero;
            Quaternion rotBodyOffset = Quaternion.identity;
            Vector3 worldHandOffset = Vector3.zero;
            Quaternion rotHandOffset = Quaternion.identity;

            switch (dir)
            {
                case DuelDirection.Left:
                    // Thầy Thuốc nhoài người và chĩa gậy sang TRÁI màn hình (-X) nhắm thẳng vào Bé Út
                    worldBodyOffset = new Vector3(-0.95f, 0.15f, 0f);
                    rotBodyOffset = Quaternion.Euler(0f, 0f, 25f);
                    worldHandOffset = new Vector3(-1.10f, 0.35f, 0f);
                    rotHandOffset = Quaternion.Euler(20f, -90f, 40f);
                    break;
                case DuelDirection.Right:
                    // Thầy Thuốc vung gậy ngược sang PHẢI màn hình (+X)
                    worldBodyOffset = new Vector3(0.95f, 0.15f, 0f);
                    rotBodyOffset = Quaternion.Euler(0f, 0f, -25f);
                    worldHandOffset = new Vector3(1.10f, 0.35f, 0f);
                    rotHandOffset = Quaternion.Euler(20f, 90f, -40f);
                    break;
                case DuelDirection.Straight: // Thầy Thuốc chỉ gậy vút thẳng LÊN trời (+Y)
                    worldBodyOffset = new Vector3(0f, 0.65f, 0f);
                    rotBodyOffset = Quaternion.Euler(-30f, 0f, 0f);
                    worldHandOffset = new Vector3(0f, 1.35f, 0f);
                    rotHandOffset = Quaternion.Euler(-85f, 0f, 0f);
                    break;
                case DuelDirection.Down: // Thầy Thuốc dộng gậy thẳng XUỐNG đất (-Y)
                    worldBodyOffset = new Vector3(0f, -0.45f, 0f);
                    rotBodyOffset = Quaternion.Euler(40f, 0f, 0f);
                    worldHandOffset = new Vector3(0f, -0.55f, 0f);
                    rotHandOffset = Quaternion.Euler(80f, 0f, 0f);
                    break;
            }

            targetHunterBodyPos = defaultBodyPos + transform.InverseTransformVector(worldBodyOffset);
            targetHunterBodyRot = defaultBodyRot * rotBodyOffset;
            if (socketGrabHand != null)
            {
                targetHandPos = defaultHandPos + transform.InverseTransformVector(worldHandOffset);
                targetHandRot = defaultHandRot * rotHandOffset;
            }
        }

        public void ResetDuelStance()
        {
            EnsureDuelTransforms();
            targetHunterBodyPos = defaultBodyPos;
            targetHunterBodyRot = defaultBodyRot;
            targetHandPos = defaultHandPos;
            targetHandRot = defaultHandRot;
            if (hunterBodyTransform != null)
            {
                hunterBodyTransform.localPosition = defaultBodyPos;
                hunterBodyTransform.localRotation = defaultBodyRot;
            }
            if (socketGrabHand != null)
            {
                socketGrabHand.localPosition = defaultHandPos;
                socketGrabHand.localRotation = defaultHandRot;
            }
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
                if (GameManager.Instance.CurrentState == GameState.Duel || (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive))
                {
                    EnsureTargets();
                    EnsureDuelTransforms();
                    if (conUtTarget != null)
                    {
                        Vector3 faceUt = conUtTarget.transform.position - transform.position;
                        faceUt.y = 0f;
                        if (faceUt.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceUt), Time.deltaTime * 14f);
                        }
                    }

                    if (hunterBodyTransform != null)
                    {
                        hunterBodyTransform.localPosition = Vector3.Lerp(hunterBodyTransform.localPosition, targetHunterBodyPos, Time.deltaTime * 14f);
                        hunterBodyTransform.localRotation = Quaternion.Slerp(hunterBodyTransform.localRotation, targetHunterBodyRot, Time.deltaTime * 16f);
                    }
                    if (socketGrabHand != null)
                    {
                        socketGrabHand.localPosition = Vector3.Lerp(socketGrabHand.localPosition, targetHandPos, Time.deltaTime * 18f);
                        socketGrabHand.localRotation = Quaternion.Slerp(socketGrabHand.localRotation, targetHandRot, Time.deltaTime * 20f);
                    }

                    if (HasAnimator)
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

                if (HasAnimator)
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
                if (HasAnimator) animator.SetFloat("Speed", 0f);
                UpdateDizzyVisual(true);
                ApplyExternalPush();
                return;
            }
            else
            {
                UpdateDizzyVisual(false);
            }

            if (isPerformingGrab)
            {
                HandleGrabExecution();
                return;
            }

            if (conUtTarget == null) return;

            // 2. Navigation & Steering
            Vector3 targetPos = conUtTarget.transform.position;

            // Nếu có Phân Thân Ảo Ảnh đang hoạt động: Thầy Thuốc bị đánh lừa rượt đuổi khúc đuôi đàn rắn ảo!
            if (DecoyCloneActor.ActiveClone != null && DecoyCloneActor.ActiveClone.IsActive)
            {
                targetPos = DecoyCloneActor.ActiveClone.TailPosition;
                if (DecoyCloneActor.ActiveClone.CanBeCaught && Vector3.Distance(transform.position, targetPos) < 1.35f)
                {
                    DecoyCloneActor.ActiveClone.Vanish(true);
                    return;
                }
            }

            float distToConUt = Vector3.Distance(transform.position, targetPos);
            bool isMotherBlocking = DecoyCloneActor.ActiveClone == null && IsMotherBlockingLineOfSight();

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

            if (HasAnimator)
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

        private void UpdateDizzyVisual(bool active)
        {
            if (active)
            {
                if (dizzyStarsRoot == null)
                {
                    dizzyStarsRoot = new GameObject("Hunter_Dizzy_Stars");
                    dizzyStarsRoot.transform.SetParent(socketHead != null ? socketHead : transform, false);
                    dizzyStarsRoot.transform.localPosition = new Vector3(0f, 0.45f, 0f);

                    var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                    var starMat = new Material(shader);
                    starMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.1f));

                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * 120f * Mathf.Deg2Rad;
                        var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        star.name = $"Star_{i}";
                        star.transform.SetParent(dizzyStarsRoot.transform, false);
                        star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.35f, 0f, Mathf.Sin(angle) * 0.35f);
                        star.transform.localScale = Vector3.one * 0.14f;
                        Destroy(star.GetComponent<Collider>());
                        star.GetComponent<Renderer>().sharedMaterial = starMat;
                    }
                }
                dizzyStarsRoot.SetActive(true);
                dizzyStarsRoot.transform.Rotate(0f, 280f * Time.deltaTime, 0f);
            }
            else if (dizzyStarsRoot != null && dizzyStarsRoot.activeSelf)
            {
                dizzyStarsRoot.SetActive(false);
            }
        }

        public void ApplyExternalStun(float seconds)
        {
            stunTimer = Mathf.Max(stunTimer, seconds);
            externalPushVelocity = Vector3.zero;
            if (isPerformingGrab)
            {
                CancelGrab();
            }
            if (HasAnimator)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsAngry", true);
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
            if (HasAnimator) animator.SetTrigger("Lunge");
            AudioManager.Instance?.PlaySFX(SoundType.Whoosh);
        }

        private void HandleGrabExecution()
        {
            float dt = Time.deltaTime;
            grabWindupTimer -= dt;

            // Trong lúc lấy đà vồ: Thầy Thuốc nhoài người phi nhanh về phía trước (Lunge forward) để bắt kịp Bé Út
            if (grabWindupTimer > 0f)
            {
                Vector3 lungeDir = transform.forward;
                lungeDir.y = 0f;
                if (controller != null && controller.enabled)
                {
                    controller.Move(lungeDir * (MoveSpeed * 1.35f) * dt);
                }
                else
                {
                    transform.position += lungeDir * (MoveSpeed * 1.35f) * dt;
                }
                return;
            }

            // Physical Contact Resolution: Vồ phải chạm thật (Luật 4.1)
            // Bàn tay Thầy Thuốc hoặc thân mình Thầy chạm tới thân mục tiêu trong sải tay vồ (2.0m)
            float distToTarget = conUtTarget != null ? Vector3.Distance(transform.position, conUtTarget.transform.position) : 999f;
            float currentGrabRange = GrabRange * 1.45f; // Sải tay vồ khi nhoài người
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

            // Facing check: Hunter must be generally facing Con Ut
            Vector3 toUt = conUtTarget != null ? (conUtTarget.transform.position - transform.position) : Vector3.forward;
            toUt.y = 0f;
            float grabFacingDot = toUt.sqrMagnitude > 0.01f ? Vector3.Dot(transform.forward, toUt.normalized) : 0f;

            bool isBlocked = false;
            // Mẹ chỉ cản phá thành công nếu Mẹ đang chủ động Dang Tay Cản (IsFacingLocked) VÀ đứng chắn giữa Thầy và Bé Út
            var motherCtrl = cachedMotherCtrl != null ? cachedMotherCtrl : (motherTransform != null ? (cachedMotherCtrl = motherTransform.GetComponent<MotherController>()) : null);
            if (motherCtrl != null && motherCtrl.IsFacingLocked)
            {
                isBlocked = IsMotherBlockingLineOfSight();
            }

            // Nhận diện va chạm vật lý chân thực: Sải tay vồ tới (<= 1.95m) HOẶC thân Thầy áp sát Bé Út (<= 2.05m)
            bool hasDirectContact = (handDistToBody <= currentGrabRange || distToTarget <= 2.05f) && grabFacingDot >= 0.15f;

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

        public void ResetAfterDuel(float grabCooldownSeconds = 5.0f, float freezeSeconds = 2.0f)
        {
            CancelGrab();
            nextGrabAvailableTime = Time.time + Mathf.Max(grabCooldownSeconds, 5.0f);
            stunTimer = Mathf.Max(freezeSeconds, 2.0f);
            externalPushVelocity = Vector3.zero;
            if (HasAnimator)
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
