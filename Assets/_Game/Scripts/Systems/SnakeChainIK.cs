using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    public class SnakeChainIK : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameBalanceConfig balanceConfig;

        [Header("Chain Nodes")]
        [SerializeField] private Transform motherRoot;
        [SerializeField] private List<ChildFollower> children = new List<ChildFollower>();

        [Header("Physics Tuning")]
        [Tooltip("Extra padding between child link boundaries (m). Set to 0.05m so child arms naturally reach and touch the person ahead.")]
        [SerializeField] private float paddingBetweenChildren = 0.05f;

        private List<Vector3> nodePositions = new List<Vector3>();
        private List<Vector3> nodeVelocities = new List<Vector3>();
        private bool isInitialized = false;
        private float motherStopDuration = 0f;
        private MotherController cachedMotherCtrl;
        private CharacterController cachedMotherCC;

        public ChildFollower ConUt => (children != null && children.Count > 0) ? children[children.Count - 1] : null;
        public int RemainingChildrenCount => children != null ? children.Count : 0;
        public IReadOnlyList<ChildFollower> Children => children;

        public bool RemoveTailChild()
        {
            if (children == null || children.Count == 0) return false;

            int lastIdx = children.Count - 1;
            var childToRemove = children[lastIdx];
            children.RemoveAt(lastIdx);

            if (nodePositions != null && nodePositions.Count > children.Count + 1)
            {
                nodePositions.RemoveAt(nodePositions.Count - 1);
                nodeVelocities.RemoveAt(nodeVelocities.Count - 1);
            }

            if (childToRemove != null)
            {
                childToRemove.gameObject.SetActive(false);
            }

            // Designate new Con Út if any children remain
            if (children.Count > 0)
            {
                var newConUt = children[children.Count - 1];
                if (newConUt != null)
                {
                    newConUt.Initialize(children.Count - 1, true, null);
                }
            }

            return true;
        }

        public void ResetFormationBehindMother()
        {
            if (motherRoot == null || children == null || children.Count == 0) return;

            nodePositions.Clear();
            nodeVelocities.Clear();

            float spacing = LinkSpacing;
            Vector3 anchorPoint = motherRoot.position - motherRoot.forward * (spacing * 0.75f);
            anchorPoint.y = 0f;
            nodePositions.Add(anchorPoint);
            nodeVelocities.Add(Vector3.zero);

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;
                Vector3 initialPos = anchorPoint - motherRoot.forward * (spacing * (i + 1));
                initialPos.y = 0f;
                nodePositions.Add(initialPos);
                nodeVelocities.Add(Vector3.zero);

                var cc = children[i].GetComponent<CharacterController>();
                bool wasEnabled = cc != null && cc.enabled;
                if (wasEnabled) cc.enabled = false;
                children[i].transform.SetPositionAndRotation(initialPos, motherRoot.rotation);
                if (wasEnabled) cc.enabled = true;
            }

            isInitialized = true;
        }

        private float FollowSpeed => balanceConfig != null ? balanceConfig.chainFollowSpeed : GameConstants.ChainFollowSpeed;
        private float CentrifugalWhipMultiplier => balanceConfig != null ? balanceConfig.centrifugalWhipMultiplier : GameConstants.CentrifugalWhipMultiplier;

        // Dynamic spacing derived from actual child collider radius + padding
        public float LinkSpacing
        {
            get
            {
                if (children != null && children.Count > 0 && children[0] != null)
                {
                    return (children[0].BodyRadius * 2.0f) + paddingBetweenChildren;
                }
                return balanceConfig != null ? balanceConfig.chainLinkDistance : GameConstants.ChainLinkDistance;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        public void SetupChain(Transform mother, List<ChildFollower> childList)
        {
            motherRoot = mother;
            children = childList;
            isInitialized = false;
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (motherRoot == null)
            {
                var motherCtrl = UnityEngine.Object.FindAnyObjectByType<MotherController>();
                if (motherCtrl != null) motherRoot = motherCtrl.transform;
            }

            if (children == null || children.Count == 0)
            {
                var foundChildren = UnityEngine.Object.FindObjectsByType<ChildFollower>(FindObjectsInactive.Exclude);
                if (foundChildren != null && foundChildren.Length > 0)
                {
                    children = new List<ChildFollower>(foundChildren);
                    children.Sort((a, b) => a.Index.CompareTo(b.Index));
                }
            }

            if (motherRoot == null || children == null || children.Count == 0) return;

            nodePositions.Clear();
            nodeVelocities.Clear();

            float spacing = LinkSpacing;

            // Node 0 is Mother's tail anchor
            Vector3 anchorPoint = motherRoot.position - motherRoot.forward * (spacing * 0.75f);
            anchorPoint.y = 0.0f;
            nodePositions.Add(anchorPoint);
            nodeVelocities.Add(Vector3.zero);

            for (int i = 0; i < children.Count; i++)
            {
                Vector3 initialPos = anchorPoint - motherRoot.forward * (spacing * (i + 1));
                initialPos.y = 0.0f;
                nodePositions.Add(initialPos);
                nodeVelocities.Add(Vector3.zero);
                children[i].transform.position = initialPos;
                children[i].transform.rotation = motherRoot.rotation;
            }

            isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized || nodePositions == null || nodePositions.Count != children.Count + 1)
            {
                EnsureInitialized();
                if (!isInitialized) return;
            }

            // Kết trận: chuỗi ngừng điều khiển, để từng bé tự quay mặt về camera ăn mừng hoặc cúi đầu.
            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.CurrentState;
                if (state == GameState.Victory || state == GameState.GameOver || state == GameState.Duel) return;
            }

            float dt = Time.fixedDeltaTime;
            float spacing = LinkSpacing;

            // Check Mother movement speed & direction
            Vector3 motherVel = Vector3.zero;
            if (cachedMotherCC == null && motherRoot != null) cachedMotherCC = motherRoot.GetComponent<CharacterController>();
            if (cachedMotherCC != null) motherVel = cachedMotherCC.velocity;
            motherVel.y = 0f;
            float motherSpeed = motherVel.magnitude;
            float forwardDot = motherSpeed > 0.05f ? Vector3.Dot(motherVel.normalized, motherRoot.forward) : 0f;
            bool isReversing = forwardDot < -0.1f;

            if (motherSpeed < 0.15f)
            {
                motherStopDuration += dt;
            }
            else
            {
                motherStopDuration = 0f;
            }

            // 1. Update Anchor Point behind Mother
            Vector3 targetAnchor = motherRoot.position - (motherRoot.forward * (spacing * 0.75f));
            targetAnchor.y = 0.0f;
            float anchorLerpRate = isReversing ? FollowSpeed * 3.0f : FollowSpeed * 1.5f;
            nodePositions[0] = Vector3.Lerp(nodePositions[0], targetAnchor, dt * anchorLerpRate);

            // 2. Multi-segment Spring-Damper Relaxation
            for (int i = 1; i < nodePositions.Count; i++)
            {
                Vector3 parentPos = nodePositions[i - 1];
                Vector3 currentPos = nodePositions[i];

                Vector3 delta = currentPos - parentPos;
                delta.y = 0f;
                float currentDist = delta.magnitude;

                Vector3 idealPos;
                if (isReversing || Vector3.Dot(delta, motherRoot.forward) > 0.05f)
                {
                    // Mother is stepping backward: children smoothly yield and back up behind parent
                    idealPos = parentPos - motherRoot.forward * spacing;
                }
                else if (motherSpeed < 0.1f)
                {
                    // When Mother is stationary or moving very slowly, snake chain straightens directly behind Mother
                    Vector3 alignDir = (i == 1) ? -motherRoot.forward : (nodePositions[i - 1] - (i >= 2 ? nodePositions[i - 2] : motherRoot.position)).normalized;
                    if (alignDir.sqrMagnitude < 0.001f) alignDir = -motherRoot.forward;
                    idealPos = parentPos + alignDir * spacing;
                }
                else if (currentDist > 0.001f)
                {
                    Vector3 dir = delta / currentDist;
                    idealPos = parentPos + dir * spacing;
                }
                else
                {
                    idealPos = parentPos - motherRoot.forward * spacing;
                }

                // Centrifugal whip effect ONLY when Mother is moving fast and turning sharply
                if (i > 1 && motherSpeed > 0.8f && !isReversing && CentrifugalWhipMultiplier > 1.0f)
                {
                    Vector3 prevDir = (nodePositions[i - 1] - nodePositions[i - 2]).normalized;
                    Vector3 currentDir = delta.sqrMagnitude > 0.001f ? delta.normalized : prevDir;
                    // Detect turning curvature
                    float turnCrossY = Vector3.Cross(prevDir, currentDir).y;
                    if (Mathf.Abs(turnCrossY) > 0.12f)
                    {
                        Vector3 whipOffset = Vector3.Cross(prevDir, Vector3.up) * (Mathf.Sign(turnCrossY) * (CentrifugalWhipMultiplier - 1.0f) * 0.15f);
                        idealPos += whipOffset;
                    }
                }

                Vector3 currentVel = nodeVelocities[i];
                float smoothTime = isReversing ? 0.08f : (motherSpeed < 0.1f ? 0.12f : 0.16f);

                // Luật 4.4: Con theo Mẹ, không vượt Mẹ
                // Khi Mẹ đang di chuyển, tốc độ mỗi con không quá 1.15 lần tốc độ Mẹ.
                // Khi Mẹ dừng, đàn con được phép một khoảng ngắn (dưới 1 giây) để xếp lại hàng rồi dừng hẳn.
                float maxChildSpeed;
                if (motherSpeed >= 0.15f)
                {
                    maxChildSpeed = motherSpeed * 1.15f;
                }
                else
                {
                    maxChildSpeed = motherStopDuration < 0.85f ? Mathf.Lerp(1.8f, 0.05f, motherStopDuration / 0.85f) : 0f;
                }

                Vector3 smoothedPos = Vector3.SmoothDamp(currentPos, idealPos, ref currentVel, smoothTime, maxChildSpeed, dt);
                smoothedPos.y = 0.0f;

                // Strict Ellipse Arena Clamping
                smoothedPos = ArenaBoundary.ClampToArena(smoothedPos, 0.35f);

                // Enforce tight chain spacing (no rubber banding)
                float postDist = Vector3.Distance(smoothedPos, parentPos);
                if (postDist > spacing * 1.25f)
                {
                    smoothedPos = parentPos + (smoothedPos - parentPos).normalized * (spacing * 1.25f);
                }

                nodePositions[i] = smoothedPos;
                nodeVelocities[i] = currentVel;

                // Update Child Transform
                if (i - 1 < children.Count && children[i - 1] != null)
                {
                    Vector3 facingDir = parentPos - smoothedPos;
                    facingDir.y = 0f;
                    if (facingDir.sqrMagnitude < 0.001f) facingDir = motherRoot.forward;
                    children[i - 1].UpdatePhysicsPosition(smoothedPos, facingDir, currentVel);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (nodePositions == null || nodePositions.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < nodePositions.Count; i++)
            {
                Gizmos.DrawWireSphere(nodePositions[i] + Vector3.up * 0.4f, 0.2f);
                if (i > 0)
                {
                    Gizmos.DrawLine(nodePositions[i - 1] + Vector3.up * 0.4f, nodePositions[i] + Vector3.up * 0.4f);
                }
            }
        }
    }
}
