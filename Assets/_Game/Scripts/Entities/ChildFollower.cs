using UnityEngine;

namespace RongRanChamCham
{
    public class ChildFollower : MonoBehaviour
    {
        [Header("Child Configuration")]
        [SerializeField] private int indexInChain; // 0 = First Child, 1 = Middle, 2 = Con Út
        [SerializeField] private bool isConUt = false;
        [SerializeField] private Transform targetLeader; // Leader to follow
        [SerializeField] private GameObject crownOrBow;

        [Header("Animation & Wobble")]
        [SerializeField] private float wobbleSpeed = 10f;
        [SerializeField] private float wobbleAngle = 12f;

        private Vector3 targetVelocity;
        private Vector3 currentVelocity;

        public bool IsConUt => isConUt;
        public int Index => indexInChain;

        public void Initialize(int index, bool isTail, Transform leader)
        {
            indexInChain = index;
            isConUt = isTail;
            targetLeader = leader;

            if (crownOrBow != null)
            {
                crownOrBow.SetActive(isConUt);
            }
        }

        private void Update()
        {
            if (targetLeader == null) return;

            // Simple cute running wobble
            float speed = currentVelocity.magnitude;
            if (speed > 0.5f)
            {
                float tilt = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
                transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, tilt);
            }
        }

        public void UpdatePhysicsPosition(Vector3 newPos, Vector3 facingDir, Vector3 vel)
        {
            transform.position = newPos;
            currentVelocity = vel;
            if (facingDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(facingDir);
            }
        }
    }
}
