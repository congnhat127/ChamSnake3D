using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    public class SnakeChainIK : MonoBehaviour
    {
        [Header("Chain Nodes")]
        [SerializeField] private Transform motherRoot;
        [SerializeField] private List<ChildFollower> children = new List<ChildFollower>();

        [Header("Physics Tuning")]
        [SerializeField] private float linkSpacing = GameConstants.ChainLinkDistance;
        [SerializeField] private float followSpeed = GameConstants.ChainFollowSpeed;
        [SerializeField] private float centrifugalWhipMultiplier = GameConstants.CentrifugalWhipMultiplier;

        private List<Vector3> nodePositions = new List<Vector3>();
        private List<Vector3> nodeVelocities = new List<Vector3>();
        private bool isInitialized = false;

        public ChildFollower ConUt => (children != null && children.Count > 0) ? children[children.Count - 1] : null;

        private void Awake()
        {
            ValidateTuning();
            EnsureInitialized();
        }

        private void Start()
        {
            ValidateTuning();
            EnsureInitialized();
        }

        private void ValidateTuning()
        {
            if (followSpeed <= 0.5f) followSpeed = GameConstants.ChainFollowSpeed;
            if (linkSpacing <= 0.2f) linkSpacing = GameConstants.ChainLinkDistance;
            if (centrifugalWhipMultiplier <= 0.1f) centrifugalWhipMultiplier = GameConstants.CentrifugalWhipMultiplier;
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
            ValidateTuning();

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

            // Node 0 is Mother's tail anchor
            Vector3 anchorPoint = motherRoot.position - motherRoot.forward * (linkSpacing * 0.75f);
            anchorPoint.y = 0.5f;
            nodePositions.Add(anchorPoint);
            nodeVelocities.Add(Vector3.zero);

            for (int i = 0; i < children.Count; i++)
            {
                Vector3 initialPos = anchorPoint - motherRoot.forward * (linkSpacing * (i + 1));
                initialPos.y = 0.5f;
                nodePositions.Add(initialPos);
                nodeVelocities.Add(Vector3.zero);
                children[i].transform.position = initialPos;
            }

            isInitialized = true;
        }

        private void FixedUpdate()
        {
            ValidateTuning();

            if (!isInitialized || nodePositions == null || nodePositions.Count != children.Count + 1)
            {
                EnsureInitialized();
                if (!isInitialized) return;
            }

            // Node 0 strictly tracks Mother's rear anchor
            Vector3 anchorPoint = motherRoot.position - (motherRoot.forward * (linkSpacing * 0.75f));
            anchorPoint.y = 0.5f;
            nodePositions[0] = anchorPoint;

            // Update each child in chain sequence
            for (int i = 0; i < children.Count; i++)
            {
                int nodeIdx = i + 1;
                Vector3 parentPos = nodePositions[nodeIdx - 1];
                Vector3 currentPos = nodePositions[nodeIdx];

                Vector3 toParent = parentPos - currentPos;
                toParent.y = 0f;
                float currentDist = toParent.magnitude;

                if (currentDist > 0.001f)
                {
                    Vector3 idealDir = toParent / currentDist;
                    Vector3 targetPos = parentPos - (idealDir * linkSpacing);
                    targetPos.y = 0.5f;

                    // Direct, reliable, springy follow
                    float step = Mathf.Max(followSpeed, 18.0f) * Time.fixedDeltaTime;
                    currentPos = Vector3.MoveTowards(currentPos, targetPos, step);

                    // Add lateral centrifugal whip on sharp turns
                    if (i > 0 && nodeVelocities.Count > nodeIdx - 1)
                    {
                        Vector3 tangent = Vector3.Cross(Vector3.up, idealDir);
                        float lateralVelocity = Vector3.Dot(nodeVelocities[nodeIdx - 1], tangent);
                        currentPos += tangent * (lateralVelocity * centrifugalWhipMultiplier * Time.fixedDeltaTime);
                    }

                    // Enforce strict distance constraint
                    Vector3 offsetFromParent = (currentPos - parentPos);
                    offsetFromParent.y = 0f;
                    if (offsetFromParent.magnitude > GameConstants.ChainMaxStretch)
                    {
                        currentPos = parentPos + (offsetFromParent.normalized * linkSpacing);
                    }
                    currentPos.y = 0.5f;

                    nodeVelocities[nodeIdx] = (currentPos - nodePositions[nodeIdx]) / Time.fixedDeltaTime;
                    nodePositions[nodeIdx] = currentPos;

                    // Apply to Child Transform
                    Vector3 facing = idealDir;
                    children[i].UpdatePhysicsPosition(currentPos, facing, nodeVelocities[nodeIdx]);
                }
            }
        }
    }
}
