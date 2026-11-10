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

        private Vector3 externalPushVelocity = Vector3.zero;
        private float stunTimer = 0f;
        private float nextGrabAvailableTime = 0f;
        private bool isPerformingGrab = false;
        private float grabWindupTimer = 0f;
        private float verticalVelocity = 0f;

        public event Action OnCaughtConUt;
        public bool IsGrabbing => isPerformingGrab;

        private float MoveSpeed => balanceConfig != null ? balanceConfig.hunterNormalSpeed : GameConstants.HunterNormalSpeed;
        private float GrabRange => balanceConfig != null ? balanceConfig.hunterGrabRange : GameConstants.HunterGrabRange;
        private float GrabWindup => balanceConfig != null ? balanceConfig.hunterGrabWindup : GameConstants.HunterGrabWindup;
        private float GrabCooldown => balanceConfig != null ? balanceConfig.hunterGrabCooldown : GameConstants.HunterGrabCooldown;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            navMeshAgent = GetComponent<NavMeshAgent>();
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
                if (motherCtrl != null) motherTransform = motherCtrl.transform;
            }

            if (conUtTarget == null)
            {
                var snakeChain = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
                if (snakeChain != null && snakeChain.ConUt != null)
                {
                    conUtTarget = snakeChain.ConUt;
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Chasing)
            {
                return;
            }

            EnsureTargets();

            // 1. Handle Stun / Windup Cooldowns
            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
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

            // 3. Trigger Grab Action if in Range & Cooldown elapsed
            if (distToConUt <= GrabRange && Time.time >= nextGrabAvailableTime)
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
                ManualSteering(targetPos);
            }

            ApplyExternalPush();
        }

        private void ManualSteering(Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Vector3 moveDir = toTarget.normalized;

                // Face movement direction
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);

                // Apply movement
                if (controller.isGrounded) verticalVelocity = -4f;
                else verticalVelocity += Physics.gravity.y * Time.deltaTime * 2f;

                // Apply movement with buffs
                float currentSpeed = MoveSpeed;
                if (BuffSystem.Instance != null)
                {
                    if (BuffSystem.Instance.IsHunterSpeedActive) currentSpeed *= 1.35f;

                    float targetScale = BuffSystem.Instance.IsHunterGiantActive ? 1.30f : 1.0f;
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 6f);
                }

                Vector3 moveDelta = moveDir * currentSpeed;
                moveDelta.y = verticalVelocity;
                controller.Move(moveDelta * Time.deltaTime);
            }
        }

        private void ApplyExternalPush()
        {
            if (externalPushVelocity.sqrMagnitude > 0.01f)
            {
                controller.Move(externalPushVelocity * Time.deltaTime);
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
        }

        private void HandleGrabExecution()
        {
            grabWindupTimer -= Time.deltaTime;
            if (grabWindupTimer <= 0f)
            {
                // Grab resolution with Shields & Giant reach
                float distToTarget = conUtTarget != null ? Vector3.Distance(transform.position, conUtTarget.transform.position) : 999f;
                float currentGrabRange = GrabRange;
                if (BuffSystem.Instance != null && BuffSystem.Instance.IsHunterGiantActive)
                {
                    currentGrabRange *= 1.30f;
                }

                bool hunterHasPierce = BuffSystem.Instance != null && BuffSystem.Instance.HunterPierceShield;
                bool snakeHasShield = BuffSystem.Instance != null && BuffSystem.Instance.SnakeHasShield;

                if (distToTarget <= currentGrabRange)
                {
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
                        // Grab success!
                        OnCaughtConUt?.Invoke();
                    }
                }

                // Cooldown
                nextGrabAvailableTime = Time.time + GrabCooldown;
                isPerformingGrab = false;
                if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
                if (swipeVfxVisual != null) swipeVfxVisual.SetActive(false);
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
