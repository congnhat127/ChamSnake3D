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
        [SerializeField] private float wobbleSpeed = 6.0f;
        [SerializeField] private float wobbleAngle = 4.5f;

        private CharacterController characterController;
        private Vector3 currentVelocity;
        private Animator animator;
        private ChibiProceduralAnimator proceduralAnimator;
        private Transform modelBodyTransform;
        private Quaternion targetBodyRot = Quaternion.identity;
        private Quaternion targetHeadRot = Quaternion.identity;

        public bool IsConUt => isConUt;
        public int Index => indexInChain;
        public float BodyRadius => characterController != null ? characterController.radius : 0.35f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            proceduralAnimator = GetComponent<ChibiProceduralAnimator>();
            EnsureSockets();
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketCrown == null) socketCrown = transform.Find("Socket_Crown");
            if (modelBodyTransform == null) modelBodyTransform = transform.Find("Model_Body");
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
            bool isVictory = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Victory;
            bool isGameOver = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver;

            if (proceduralAnimator != null)
            {
                proceduralAnimator.SetCelebrationMode(isVictory, isGameOver);
            }

            bool isDuel = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Duel;

            if (animator != null)
            {
                animator.SetFloat("Speed", isVictory || isGameOver || isDuel ? 0f : currentVelocity.magnitude);
                animator.SetBool("IsVictory", isVictory);
            }

            // End-game celebration: Turn to face camera and hop happily!
            if (isVictory)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, CameraFollow.FacingCamera(transform.position), Time.deltaTime * 6f);
                return;
            }

            if (isGameOver)
            {
                // Dejected slight bow
                transform.rotation = Quaternion.Slerp(transform.rotation, CameraFollow.FacingCamera(transform.position, 15f), Time.deltaTime * 4f);
                return;
            }

            if (isConUt && isDuel)
            {
                currentVelocity = Vector3.zero;
                var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
                if (hunter != null)
                {
                    Vector3 faceHunter = hunter.transform.position - transform.position;
                    faceHunter.y = 0f;
                    if (faceHunter.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceHunter), Time.deltaTime * 12f);
                    }
                }

                // Direct responsive head & body pose for duel dodge
                if (modelBodyTransform != null)
                {
                    modelBodyTransform.localRotation = Quaternion.Slerp(modelBodyTransform.localRotation, targetBodyRot, Time.deltaTime * 18f);
                }
                if (socketHead != null)
                {
                    socketHead.localRotation = Quaternion.Slerp(socketHead.localRotation, targetHeadRot, Time.deltaTime * 18f);
                }
                return;
            }

            if (targetLeader == null) return;

            // Cute cartoon gentle running wobble
            float speed = currentVelocity.magnitude;
            if (speed > 0.5f)
            {
                float tilt = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
                transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, tilt);
            }
        }

        public void SetDuelDodge(DuelDirection dir)
        {
            TriggerDodge(dir);
            switch (dir)
            {
                case DuelDirection.Left:
                    targetBodyRot = Quaternion.Euler(0f, -55f, -18f);
                    targetHeadRot = Quaternion.Euler(0f, -40f, -15f);
                    break;
                case DuelDirection.Right:
                    targetBodyRot = Quaternion.Euler(0f, 55f, 18f);
                    targetHeadRot = Quaternion.Euler(0f, 40f, 15f);
                    break;
                case DuelDirection.Straight:
                    targetBodyRot = Quaternion.Euler(-32f, 0f, 0f);
                    targetHeadRot = Quaternion.Euler(-35f, 0f, 0f);
                    break;
                case DuelDirection.Down:
                    targetBodyRot = Quaternion.Euler(35f, 0f, 0f);
                    targetHeadRot = Quaternion.Euler(30f, 0f, 0f);
                    break;
            }
        }

        public void ResetDuelStance()
        {
            targetBodyRot = Quaternion.identity;
            targetHeadRot = Quaternion.identity;
            if (modelBodyTransform != null) modelBodyTransform.localRotation = Quaternion.identity;
            if (socketHead != null) socketHead.localRotation = Quaternion.identity;
        }

        public void TriggerDodge(DuelDirection dir)
        {
            if (animator == null) return;
            switch (dir)
            {
                case DuelDirection.Left:
                    animator.SetTrigger("DodgeLeft");
                    break;
                case DuelDirection.Right:
                    animator.SetTrigger("DodgeRight");
                    break;
                case DuelDirection.Straight:
                    animator.SetTrigger("LookUp");
                    break;
                case DuelDirection.Down:
                    animator.SetTrigger("Duck");
                    break;
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
