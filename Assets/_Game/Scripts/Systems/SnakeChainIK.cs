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

        public ChildFollower ConUt => (children != null && children.Count > 0) ? children[children.Count - 1] : null;

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
            anchorPoint.y = 0.5f;
            nodePositions.Add(anchorPoint);
            nodeVelocities.Add(Vector3.zero);

            for (int i = 0; i < children.Count; i++)
            {
                Vector3 initialPos = anchorPoint - motherRoot.forward * (spacing * (i + 1));
                initialPos.y = 0.5f;
                nodePositions.Add(initialPos);
                nodeVelocities.Add(Vector3.zero);
                children[i].transform.position = initialPos;
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

            float dt = Time.fixedDeltaTime;
            float spacing = LinkSpacing;

            // 1. Update Anchor Point behind Mother
            Vector3 targetAnchor = motherRoot.position - (motherRoot.forward * (spacing * 0.75f));
            targetAnchor.y = 0.0f;
            nodePositions[0] = Vector3.Lerp(nodePositions[0], targetAnchor, dt * FollowSpeed * 1.5f);

            // 2. Multi-segment Spring-Damper Relaxation
            for (int i = 1; i < nodePositions.Count; i++)
            {
                Vector3 parentPos = nodePositions[i - 1];
                Vector3 currentPos = nodePositions[i];

                Vector3 delta = currentPos - parentPos;
                delta.y = 0f;
                float currentDist = delta.magnitude;

                Vector3 idealPos;
                if (currentDist > 0.001f)
                {
                    Vector3 dir = delta / currentDist;
                    idealPos = parentPos + dir * spacing;
                }
                else
                {
                    idealPos = parentPos - motherRoot.forward * spacing;
                }

                // Centrifugal whip effect during sharp turns
                if (i > 1 && CentrifugalWhipMultiplier > 1.0f)
                {
                    Vector3 prevDir = (nodePositions[i - 1] - nodePositions[i - 2]).normalized;
                    Vector3 whipOffset = Vector3.Cross(prevDir, Vector3.up) * (CentrifugalWhipMultiplier - 1.0f) * 0.3f;
                    idealPos += whipOffset;
                }

                Vector3 currentVel = nodeVelocities[i];
                Vector3 smoothedPos = Vector3.SmoothDamp(currentPos, idealPos, ref currentVel, 1.0f / FollowSpeed, Mathf.Infinity, dt);
                smoothedPos.y = 0.0f;

                nodePositions[i] = smoothedPos;
                nodeVelocities[i] = currentVel;

                // Update Child Transform
                if (i - 1 < children.Count && children[i - 1] != null)
                {
                    Vector3 facingDir = parentPos - smoothedPos;
                    facingDir.y = 0f;
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
