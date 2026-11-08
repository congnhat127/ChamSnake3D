using UnityEngine;

namespace RongRanChamCham
{
    [RequireComponent(typeof(CharacterController))]
    public class ChildFollower : MonoBehaviour
    {
        [Header("Child Configuration")]
        [SerializeField] private int indexInChain; // 0 = Con 1, 1 = Con 2, 2 = Con Út
        [SerializeField] private bool isConUt = false;
        [SerializeField] private Transform targetLeader;

        [Header("Rig & Sockets")]
        [SerializeField] private Transform socketHead;
        [SerializeField] private Transform socketCrown;

        [Header("Visual Elements")]
        [SerializeField] private GameObject crownVisual;
        [SerializeField] private Renderer bodyRenderer;

        [Header("Animation & Wobble")]
        [SerializeField] private float wobbleSpeed = 10f;
        [SerializeField] private float wobbleAngle = 12f;

        private CharacterController characterController;
        private Vector3 currentVelocity;

        public bool IsConUt => isConUt;
        public int Index => indexInChain;
        public float BodyRadius => characterController != null ? characterController.radius : 0.35f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            EnsureSockets();
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketCrown == null) socketCrown = transform.Find("Socket_Crown");
        }

        public void Initialize(int index, bool isTail, Transform leader)
        {
            indexInChain = index;
            isConUt = isTail;
            targetLeader = leader;

            if (crownVisual != null)
            {
                crownVisual.SetActive(isConUt);
            }
        }

        private void Update()
        {
            if (targetLeader == null) return;

            // Cute cartoon running wobble
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isConUt ? Color.magenta : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.45f, BodyRadius);

            if (socketCrown != null && isConUt)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(socketCrown.position, Vector3.one * 0.15f);
            }
        }
    }
}
