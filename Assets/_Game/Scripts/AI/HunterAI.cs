using System;
using UnityEngine;

namespace RongRanChamCham
{
    [RequireComponent(typeof(CharacterController))]
    public class HunterAI : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform motherTransform;
        [SerializeField] private ChildFollower conUtTarget;

        [Header("Movement & Tuning (Equal to Mother)")]
        [SerializeField] private float moveSpeed = GameConstants.HunterNormalSpeed;
        [SerializeField] private float grabRange = GameConstants.HunterGrabRange;
        [SerializeField] private float grabCooldown = GameConstants.HunterGrabCooldown;

        [Header("Visual Feedback")]
        [SerializeField] private Transform rightHand;
        [SerializeField] private GameObject swipeVfxVisual;

        private CharacterController controller;
        private Vector3 externalPushVelocity = Vector3.zero;
        private float stunTimer = 0f;
        private float nextGrabAvailableTime = 0f;
        private bool isPerformingGrab = false;
        private float grabWindupTimer = 0f;
        private float verticalVelocity = 0f;

        public event Action OnCaughtConUt;
        public bool IsGrabbing => isPerformingGrab;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (moveSpeed <= 0.5f) moveSpeed = GameConstants.HunterNormalSpeed;
            if (grabRange <= 0.5f) grabRange = GameConstants.HunterGrabRange;
        }

        private void Start()
        {
            EnsureTargets();
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
                var snake = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
                if (snake != null && snake.ConUt != null)
                {
                    conUtTarget = snake.ConUt;
                }
                else
                {
                    var allChildren = UnityEngine.Object.FindObjectsByType<ChildFollower>(FindObjectsInactive.Exclude);
                    foreach (var c in allChildren)
                    {
                        if (c.IsConUt) { conUtTarget = c; break; }
                    }
                }
            }
        }

        public void GetShovedByMother(Vector3 pushDir, float force)
        {
            externalPushVelocity = pushDir.normalized * force;
            stunTimer = 0.45f;
            isPerformingGrab = false;
        }

        private void Update()
        {
            EnsureTargets();
            if (conUtTarget == null) return;

            // Decay external shove force
            if (externalPushVelocity.sqrMagnitude > 0.05f)
            {
                controller.Move(externalPushVelocity * Time.deltaTime);
                externalPushVelocity = Vector3.Lerp(externalPushVelocity, Vector3.zero, Time.deltaTime * 8.0f);
            }

            // Hard boundary clamp & ground lock
            ClampWithinArena();

            // Stunned cooldown
            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            // Grab Action State
            if (isPerformingGrab)
            {
                HandleGrabExecution();
                return;
            }

            // AI Navigation Loop
            HandleNavigation();
        }

        private void HandleNavigation()
        {
            Vector3 myPos = transform.position;
            myPos.y = 0f;

            Vector3 conUtPos = conUtTarget.transform.position;
            conUtPos.y = 0f;

            Vector3 toConUt = conUtPos - myPos;
            float distToConUt = toConUt.magnitude;

            // Check if within grab range
            if (distToConUt <= grabRange && Time.time >= nextGrabAvailableTime)
            {
                StartGrabAction();
                return;
            }

            // Check if Mother is blocking the straight path
            Vector3 desiredDir = toConUt.normalized;

            if (motherTransform != null)
            {
                Vector3 motherPos = motherTransform.position;
                motherPos.y = 0f;
                Vector3 toMother = motherPos - myPos;
                float distToMother = toMother.magnitude;

                if (distToMother < distToConUt && distToMother > 0.1f)
                {
                    // Projection onto direct path
                    float proj = Vector3.Dot(toMother, toConUt.normalized);
                    if (proj > 0 && proj < distToConUt)
                    {
                        Vector3 closestPoint = myPos + toConUt.normalized * proj;
                        float perpDist = (motherPos - closestPoint).magnitude;

                        // Within Mother's blocking zone (< 2.6m)
                        if (perpDist < 2.6f)
                        {
                            // Steer tangentially around Mother towards Con Út's open side
                            Vector3 tangent = Vector3.Cross(Vector3.up, toMother).normalized;
                            float sideDot = Vector3.Dot(tangent, toConUt);
                            Vector3 flankSide = sideDot >= 0 ? tangent : -tangent;
                            desiredDir = (flankSide * 1.3f + toConUt.normalized * 0.7f).normalized;
                        }
                    }
                }

                // If too close to Mother's body (< 1.6m), repel
                if (distToMother < 1.6f)
                {
                    Vector3 repel = (myPos - motherPos).normalized;
                    desiredDir = (desiredDir + repel * 1.5f).normalized;
                }
            }

            // Arena boundary soft repulsion
            float distFromCenter = myPos.magnitude;
            if (distFromCenter > GameConstants.ArenaRadius - 2.0f)
            {
                Vector3 toCenter = -myPos.normalized;
                desiredDir = (desiredDir + toCenter * 1.2f).normalized;
            }

            // Move at fair speed (equal to Mother)
            if (controller.isGrounded)
            {
                verticalVelocity = -4.0f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime * 2.0f;
            }

            if (desiredDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * 10f);
            }

            Vector3 moveDelta = (desiredDir * moveSpeed);
            moveDelta.y = verticalVelocity;
            controller.Move(moveDelta * Time.deltaTime);

            ClampWithinArena();
        }

        private void ClampWithinArena()
        {
            Vector3 clampedPos = transform.position;
            float horizontalDist = Mathf.Sqrt(clampedPos.x * clampedPos.x + clampedPos.z * clampedPos.z);
            float maxAllowedRadius = GameConstants.ArenaRadius - 0.8f;
            if (horizontalDist > maxAllowedRadius)
            {
                Vector2 clampedXZ = new Vector2(clampedPos.x, clampedPos.z).normalized * maxAllowedRadius;
                clampedPos.x = clampedXZ.x;
                clampedPos.z = clampedXZ.y;
            }
            if (clampedPos.y < 0f || controller.isGrounded)
            {
                clampedPos.y = 0f;
            }
            transform.position = clampedPos;
        }

        private void StartGrabAction()
        {
            isPerformingGrab = true;
            grabWindupTimer = GameConstants.HunterGrabWindup; // 0.25s windup

            // Face Con Út
            Vector3 toConUt = (conUtTarget.transform.position - transform.position);
            toConUt.y = 0f;
            if (toConUt.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(toConUt.normalized);
            }

            if (swipeVfxVisual != null) swipeVfxVisual.SetActive(true);
            Debug.Log("[HunterAI] VUNG TAY VỒ CON ÚT!");
        }

        private void HandleGrabExecution()
        {
            grabWindupTimer -= Time.deltaTime;
            if (grabWindupTimer <= 0f)
            {
                // Execute Swipe Hitbox Check
                CheckGrabHit();

                isPerformingGrab = false;
                nextGrabAvailableTime = Time.time + grabCooldown;
                if (swipeVfxVisual != null) swipeVfxVisual.SetActive(false);
            }
        }

        private void CheckGrabHit()
        {
            if (conUtTarget == null) return;

            Vector3 diff = conUtTarget.transform.position - transform.position;
            diff.y = 0f;

            // Check if still within swipe reach
            if (diff.magnitude <= grabRange * 1.15f)
            {
                // Check if Golden Shield buff is active
                if (BuffSystem.Instance != null && BuffSystem.Instance.IsShieldActive)
                {
                    Vector3 bounceDir = -diff.normalized;
                    GetShovedByMother(bounceDir, 12.0f);
                    Debug.Log("[HunterAI] Bị Giáp Vàng đẩy nảy ra!");
                    return;
                }

                // Hit Con Út!
                Debug.Log("[HunterAI] CHÉM TRÚNG CON ÚT! BẮT THÀNH CÔNG!");
                OnCaughtConUt?.Invoke();
            }
            else
            {
                Debug.Log("[HunterAI] VỒ HỤT! Con Út đã kịp né thoát!");
            }
        }
    }
}
