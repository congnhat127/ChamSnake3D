using UnityEngine;

namespace RongRanChamCham
{
    [RequireComponent(typeof(CharacterController))]
    public class MotherController : MonoBehaviour
    {
        [Header("Movement Tuning")]
        [SerializeField] private float normalSpeed = GameConstants.MotherNormalSpeed;
        [SerializeField] private float strafeSpeed = GameConstants.MotherStrafeSpeed;
        [SerializeField] private float turnSpeed = GameConstants.MotherTurnSpeed;

        [Header("Visual & State")]
        [SerializeField] private GameObject lockAuraVisual;
        [SerializeField] private Transform hunterTarget;

        private CharacterController characterController;
        private Quaternion lockedRotation;
        private bool wasFacingLocked = false;
        private Vector3 currentVelocity;
        private float verticalVelocity = 0f;

        public bool IsFacingLocked => TouchInputManager.Instance != null && TouchInputManager.Instance.IsFacingLocked;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (normalSpeed <= 0.5f) normalSpeed = GameConstants.MotherNormalSpeed;
            if (strafeSpeed <= 0.5f) strafeSpeed = GameConstants.MotherStrafeSpeed;
            if (turnSpeed <= 0.5f) turnSpeed = GameConstants.MotherTurnSpeed;
        }

        private void Start()
        {
            lockedRotation = transform.rotation;
            EnsureHunterTarget();
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

            // Get input from TouchInputManager
            Vector2 input = TouchInputManager.Instance != null ? TouchInputManager.Instance.MoveDirection : Vector2.zero;
            bool isLocked = TouchInputManager.Instance != null && TouchInputManager.Instance.IsFacingLocked;

            // When Lock button is first pressed, capture the current forward facing angle
            if (isLocked && !wasFacingLocked)
            {
                lockedRotation = transform.rotation;
            }
            wasFacingLocked = isLocked;

            // Visual feedback for locked stance
            if (lockAuraVisual != null)
            {
                lockAuraVisual.SetActive(isLocked);
            }

            Vector3 moveDirection = new Vector3(input.x, 0f, input.y);
            float effectiveSpeed = isLocked ? strafeSpeed : normalSpeed;

            // 1. Rotation Logic
            if (isLocked)
            {
                // LOCK-FACING: Absolutely hold current facing angle!
                // Mother walks backward or sideways WITHOUT turning her back!
                transform.rotation = lockedRotation;
            }
            else if (moveDirection.sqrMagnitude > 0.001f)
            {
                // FREE RUN 360°: Face moving direction smoothly
                Quaternion targetRot = Quaternion.LookRotation(moveDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
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

            Vector3 moveDelta = (moveDirection * effectiveSpeed);
            moveDelta.y = verticalVelocity;
            characterController.Move(moveDelta * Time.deltaTime);

            // 3. Keep within Arena Boundary & clamp to ground
            Vector3 clampedPos = transform.position;
            float horizontalDist = Mathf.Sqrt(clampedPos.x * clampedPos.x + clampedPos.z * clampedPos.z);
            float maxAllowedRadius = GameConstants.ArenaRadius - 0.8f;
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

            // If Hunter is within Mother's front defense perimeter (< 2.0m)
            if (dist < 2.0f && Vector3.Dot(transform.forward, toHunter.normalized) > 0.1f)
            {
                var hunterAI = hunterTarget.GetComponent<HunterAI>();
                if (hunterAI != null)
                {
                    hunterAI.GetShovedByMother(toHunter.normalized, IsFacingLocked ? 10.0f : 6.0f);
                }
            }
        }
    }
}
