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

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            lockedRotation = transform.rotation;
            EnsureHunterTarget();
            EnsureSockets();
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
                if (hunter != null) hunterTarget = hunter.transform;
            }
        }

        public void SetHunterTarget(Transform hunter)
        {
            hunterTarget = hunter;
        }

        private void Update()
        {
            EnsureHunterTarget();
            HandleMovement();
            HandleShoveHunter();
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

            Vector3 moveDirection = new Vector3(input.x, 0f, input.y);
            float effectiveSpeed = isLocked ? StrafeSpeed : NormalSpeed;

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

            // 3. Keep within Arena Boundary & strictly clamp ground Y = 0
            Vector3 clampedPos = transform.position;
            float horizontalDist = Mathf.Sqrt(clampedPos.x * clampedPos.x + clampedPos.z * clampedPos.z);
            float maxAllowedRadius = ArenaRadius - 0.8f;
            if (horizontalDist > maxAllowedRadius)
            {
                Vector2 clampedXZ = new Vector2(clampedPos.x, clampedPos.z).normalized * maxAllowedRadius;
                clampedPos.x = clampedXZ.x;
                clampedPos.z = clampedXZ.y;
            }
            if (clampedPos.y < 0f || characterController.isGrounded)
            {
                clampedPos.y = 0f;
            }
            transform.position = clampedPos;
        }

        private void HandleShoveHunter()
        {
            if (hunterTarget == null) return;

            Vector3 toHunter = hunterTarget.position - transform.position;
            toHunter.y = 0f;
            float dist = toHunter.magnitude;

            // Sải tay bảo vệ: bán kính cản dựa trên nửa sải tay hoặc cự ly chuẩn
            float defenseRadius = ArmSpan > 0.5f ? (ArmSpan * 0.5f + 0.3f) : 1.7f;

            if (dist < defenseRadius && Vector3.Dot(transform.forward, toHunter.normalized) > 0.1f)
            {
                var hunterAI = hunterTarget.GetComponent<HunterAI>();
                if (hunterAI != null)
                {
                    float shoveForce = IsFacingLocked ? 10.0f : 6.0f;
                    hunterAI.GetShovedByMother(toHunter.normalized, shoveForce);
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
                Vector3 left = transform.position + transform.right * -1.4f + Vector3.up * 1.0f;
                Vector3 right = transform.position + transform.right * 1.4f + Vector3.up * 1.0f;
                Gizmos.DrawLine(left, right);
            }

            // Draw Shove Defense Arc
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
            float defenseRadius = ArmSpan > 0.5f ? (ArmSpan * 0.5f + 0.3f) : 1.7f;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, defenseRadius);
        }
    }
}
