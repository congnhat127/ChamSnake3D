using UnityEngine;

namespace RongRanChamCham
{
    [RequireComponent(typeof(CharacterController))]
    public class MotherController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameBalanceConfig balanceConfig;

        [Header("Rig & Sockets")]
        [SerializeField] private Transform socketHead;
        [SerializeField] private Transform socketHandL;
        [SerializeField] private Transform socketHandR;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject lockAuraVisual;
        [SerializeField] private Transform hunterTarget;

        private CharacterController characterController;
        private Animator animator;
        private ChibiProceduralAnimator proceduralAnim;
        private HunterAI cachedHunterAI;
        private float nextShieldSfxTime = 0f;
        private Quaternion lockedRotation;
        private bool wasFacingLocked = false;
        private float verticalVelocity = 0f;

        // Dynamic Arm Span derived from hand bone sockets
        public float ArmSpan
        {
            get
            {
                if (socketHandL != null && socketHandR != null)
                {
                    return Vector3.Distance(socketHandL.position, socketHandR.position);
                }
                return balanceConfig != null ? balanceConfig.motherNormalSpeed : GameConstants.MotherNormalSpeed;
            }
        }

        public bool IsFacingLocked => TouchInputManager.Instance != null && TouchInputManager.Instance.IsFacingLocked;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;

        private float NormalSpeed => balanceConfig != null ? balanceConfig.motherNormalSpeed : GameConstants.MotherNormalSpeed;
        private float StrafeSpeed => balanceConfig != null ? balanceConfig.motherStrafeSpeed : GameConstants.MotherStrafeSpeed;
        private float TurnSpeed => balanceConfig != null ? balanceConfig.motherTurnSpeed : GameConstants.MotherTurnSpeed;
        private float ArenaRadius => balanceConfig != null ? balanceConfig.arenaRadius : GameConstants.ArenaRadius;

        private bool HasAnimator => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.isInitialized;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator = null;
            }
            proceduralAnim = GetComponent<ChibiProceduralAnimator>();
        }

        private void Start()
        {
            lockedRotation = transform.rotation;
            EnsureHunterTarget();
            EnsureSockets();
            IgnoreCollisionsWithChildren();

            if (BuffSystem.Instance != null)
            {
                BuffSystem.Instance.OnBuffApplied += HandleBuffApplied;
            }
        }

        private void IgnoreCollisionsWithChildren()
        {
            var allChildren = Object.FindObjectsByType<ChildFollower>(FindObjectsInactive.Include);
            if (allChildren == null || allChildren.Length == 0) return;

            foreach (var child in allChildren)
            {
                var childCC = child.GetComponent<CharacterController>();
                if (childCC != null && characterController != null)
                {
                    Physics.IgnoreCollision(characterController, childCC, true);
                }
            }

            for (int i = 0; i < allChildren.Length; i++)
            {
                var ccA = allChildren[i].GetComponent<CharacterController>();
                if (ccA == null) continue;
                for (int j = i + 1; j < allChildren.Length; j++)
                {
                    var ccB = allChildren[j].GetComponent<CharacterController>();
                    if (ccB != null) Physics.IgnoreCollision(ccA, ccB, true);
                }
            }
        }

        private void HandleBuffApplied(FoodType food, bool isSnake)
        {
            if (isSnake)
            {
                var procAnim = GetComponent<ChibiProceduralAnimator>();
                if (procAnim != null) procAnim.TriggerPowerUpPulse(0.65f);
            }
        }

        private void OnDestroy()
        {
            if (BuffSystem.Instance != null)
            {
                BuffSystem.Instance.OnBuffApplied -= HandleBuffApplied;
            }
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketHandL == null) socketHandL = transform.Find("Socket_Hand_L");
            if (socketHandR == null) socketHandR = transform.Find("Socket_Hand_R");
        }

        private void EnsureHunterTarget()
        {
            if (hunterTarget == null)
            {
                var hunter = Object.FindAnyObjectByType<HunterAI>();
                if (hunter != null)
                {
                    hunterTarget = hunter.transform;
                    cachedHunterAI = hunter;
                }
            }
            else if (cachedHunterAI == null)
            {
                cachedHunterAI = hunterTarget.GetComponent<HunterAI>();
            }
        }

        public void SetHunterTarget(Transform hunter)
        {
            hunterTarget = hunter;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Chasing)
            {
                bool isVic = GameManager.Instance.CurrentState == GameState.Victory;
                bool isDef = GameManager.Instance.CurrentState == GameState.GameOver;

                if (proceduralAnim != null)
                {
                    proceduralAnim.SetCelebrationMode(isVic, isDef);
                }

                if (HasAnimator)
                {
                    animator.SetFloat("Speed", 0f);
                    animator.SetBool("IsGuarding", false);
                    animator.SetBool("IsVictory", isVic);
                }

                // End-game celebration: Rotate to face the camera and celebrate!
                if (isVic)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, CameraFollow.FacingCamera(transform.position), Time.deltaTime * 6f);
                }
                else if (isDef)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, CameraFollow.FacingCamera(transform.position, 15f), Time.deltaTime * 4f);
                }

                return;
            }

            var activeAnim = GetComponent<ChibiProceduralAnimator>();
            if (activeAnim != null) activeAnim.SetCelebrationMode(false, false);

            EnsureHunterTarget();
            HandleMovement();
            HandleShoveHunter();

            if (HasAnimator)
            {
                Vector2 input = TouchInputManager.Instance != null ? TouchInputManager.Instance.MoveDirection : Vector2.zero;
                animator.SetFloat("Speed", input.magnitude * NormalSpeed);
                animator.SetBool("IsGuarding", IsFacingLocked);
                animator.SetBool("IsVictory", false);
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            Vector2 input = TouchInputManager.Instance != null ? TouchInputManager.Instance.MoveDirection : Vector2.zero;
            bool isLocked = IsFacingLocked;

            // When Lock button is first pressed, capture the current forward facing angle
            if (isLocked && !wasFacingLocked)
            {
                lockedRotation = transform.rotation;
            }
            wasFacingLocked = isLocked;

            if (lockAuraVisual != null)
            {
                lockAuraVisual.SetActive(isLocked);
            }

            // Joystick tính theo màn hình: đẩy lên là chạy ra xa camera.
            Vector3 moveDirection = CameraFollow.InputToWorld(input);
            float effectiveSpeed = isLocked ? StrafeSpeed : NormalSpeed;

            // Apply Food Buffs
            if (BuffSystem.Instance != null)
            {
                if (BuffSystem.Instance.IsSnakeSpeedActive)
                {
                    effectiveSpeed *= 1.35f;
                }

                float targetScale = BuffSystem.Instance.IsSnakeGiantActive ? 1.30f : 1.0f;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 6f);
            }

            // 1. Rotation Logic
            if (isLocked)
            {
                // Lock-facing: Hold facing angle, step backward or strafe
                transform.rotation = lockedRotation;
            }
            else if (moveDirection.sqrMagnitude > 0.001f)
            {
                // Free run 360: Face moving direction smoothly
                Quaternion targetRot = Quaternion.LookRotation(moveDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSpeed);
                lockedRotation = transform.rotation;
            }

            // 2. Translation & Grounding Logic
            if (characterController.isGrounded)
            {
                verticalVelocity = -4.0f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime * 2.0f;
            }

            Vector3 moveDelta = moveDirection * effectiveSpeed;
            moveDelta.y = verticalVelocity;
            characterController.Move(moveDelta * Time.deltaTime);

            // 3. Keep strictly within Arena Ellipse Boundary
            Vector3 clampedPos = ArenaBoundary.ClampToArena(transform.position, 0.5f);
            transform.position = clampedPos;
        }

        // Dynamic Defense Radius: Natural arm reach (1.40m base, 1.65m when actively guarding, +30% with Giant buff)
        public float CurrentDefenseRadius
        {
            get
            {
                float baseRadius = IsFacingLocked ? 1.65f : 1.40f;
                if (BuffSystem.Instance != null && BuffSystem.Instance.IsSnakeGiantActive)
                {
                    baseRadius *= 1.30f; // -> 1.82m normal, 2.15m when guarding
                }
                return baseRadius;
            }
        }

        private void HandleShoveHunter()
        {
            if (hunterTarget == null) return;

            Vector3 toHunter = hunterTarget.position - transform.position;
            toHunter.y = 0f;
            float dist = toHunter.magnitude;

            float defenseRadius = CurrentDefenseRadius;

            // Covers Mother's entire 180-degree forward and lateral arm span (dot > -0.05f)
            if (dist < defenseRadius && Vector3.Dot(transform.forward, toHunter.normalized) > -0.05f)
            {
                var hunterAI = cachedHunterAI != null ? cachedHunterAI : (cachedHunterAI = hunterTarget.GetComponent<HunterAI>());
                if (hunterAI != null)
                {
                    float shoveForce = IsFacingLocked ? 9.5f : 6.0f;
                    hunterAI.GetShovedByMother(toHunter.normalized, shoveForce);

                    if (Time.time >= nextShieldSfxTime)
                    {
                        nextShieldSfxTime = Time.time + 0.45f;
                        AudioManager.Instance?.PlaySFX(SoundType.ShieldBlock);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw Arm Span in Scene View
            Gizmos.color = Color.green;
            if (socketHandL != null && socketHandR != null)
            {
                Gizmos.DrawLine(socketHandL.position, socketHandR.position);
                Gizmos.DrawWireSphere(socketHandL.position, 0.15f);
                Gizmos.DrawWireSphere(socketHandR.position, 0.15f);
            }
            else
            {
                Vector3 left = transform.position + transform.right * -1.35f + Vector3.up * 1.0f;
                Vector3 right = transform.position + transform.right * 1.35f + Vector3.up * 1.0f;
                Gizmos.DrawLine(left, right);
            }

            // Draw Shove Defense Arc
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
            float defenseRadius = CurrentDefenseRadius;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.35f, defenseRadius);
        }
    }
}
