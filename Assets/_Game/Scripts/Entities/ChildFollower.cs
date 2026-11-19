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
        private Vector3 defaultBodyPos;
        private Quaternion defaultBodyRot = Quaternion.identity;
        private Vector3 defaultBodyScale = Vector3.one;
        private Quaternion defaultHeadRot = Quaternion.identity;
        private Vector3 targetBodyPos;
        private Quaternion targetBodyRot = Quaternion.identity;
        private Vector3 targetBodyScale = Vector3.one;
        private Quaternion targetHeadRot = Quaternion.identity;

        public bool IsConUt => isConUt;
        public int Index => indexInChain;
        public float BodyRadius => characterController != null ? characterController.radius : 0.35f;
        private bool HasAnimator => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.isInitialized;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator = null;
            }
            proceduralAnimator = GetComponent<ChibiProceduralAnimator>();
            EnsureSockets();
        }

        private void Start()
        {
            EnsureSockets();
            if (modelBodyTransform != null)
            {
                defaultBodyPos = modelBodyTransform.localPosition;
                defaultBodyRot = modelBodyTransform.localRotation;
                defaultBodyScale = modelBodyTransform.localScale;
                targetBodyPos = defaultBodyPos;
                targetBodyRot = defaultBodyRot;
                targetBodyScale = defaultBodyScale;
            }
            if (socketHead != null)
            {
                defaultHeadRot = socketHead.localRotation;
                targetHeadRot = defaultHeadRot;
            }
        }

        private void EnsureSockets()
        {
            if (socketHead == null) socketHead = transform.Find("Socket_Head");
            if (socketCrown == null) socketCrown = transform.Find("Socket_Crown");
            if (modelBodyTransform == null)
            {
                if (proceduralAnimator != null && proceduralAnimator.BodyVisual != null)
                {
                    modelBodyTransform = proceduralAnimator.BodyVisual;
                }
                else
                {
                    var rend = GetComponentInChildren<Renderer>();
                    if (rend != null && rend.transform != transform) modelBodyTransform = rend.transform;
                }
                if (modelBodyTransform == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        var child = transform.GetChild(i);
                        if (!child.name.StartsWith("Socket_") && !child.name.Contains("Crown") && !child.name.Contains("VFX"))
                        {
                            modelBodyTransform = child;
                            break;
                        }
                    }
                }
            }
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

            bool isDuel = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Duel) ||
                          (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive);

            if (HasAnimator)
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
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceHunter), Time.deltaTime * 14f);
                    }
                }

                // Diễn hoạt chuyển động né đòn, nhún nhảy nổi bật trong không gian 3D
                if (modelBodyTransform != null)
                {
                    modelBodyTransform.localPosition = Vector3.Lerp(modelBodyTransform.localPosition, targetBodyPos, Time.deltaTime * 18f);
                    modelBodyTransform.localRotation = Quaternion.Slerp(modelBodyTransform.localRotation, targetBodyRot, Time.deltaTime * 20f);
                    modelBodyTransform.localScale = Vector3.Lerp(modelBodyTransform.localScale, targetBodyScale, Time.deltaTime * 18f);
                }
                if (socketHead != null)
                {
                    socketHead.localRotation = Quaternion.Slerp(socketHead.localRotation, targetHeadRot, Time.deltaTime * 20f);
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

        public void PlayDuelAnticipation(int countdownStep)
        {
            // Bé Út nhún gối chuẩn bị né đòn cực kỳ cảnh giác theo nhịp "CHĂM!"
            targetBodyPos = defaultBodyPos + new Vector3(0f, -0.12f, 0f);
            targetBodyRot = defaultBodyRot * Quaternion.Euler(14f, countdownStep % 2 == 0 ? -12f : 12f, 0f);
            targetBodyScale = new Vector3(defaultBodyScale.x * 1.15f, defaultBodyScale.y * 0.88f, defaultBodyScale.z * 1.15f);
            targetHeadRot = defaultHeadRot * Quaternion.Euler(-15f, 0f, 0f);
        }

        public void PlayDuelVictoryHop()
        {
            // Bé Út nhảy cẫng lên ăn mừng khi né thoát thành công
            targetBodyPos = defaultBodyPos + new Vector3(0f, 0.60f, 0f);
            targetBodyRot = defaultBodyRot * Quaternion.Euler(-18f, 0f, 15f);
            targetBodyScale = defaultBodyScale;
            targetHeadRot = defaultHeadRot * Quaternion.Euler(-25f, 0f, 0f);
        }

        public void PlayDuelHurtStumble()
        {
            // Bé Út choáng váng lùi lại phía sau khi bị bắt trúng
            targetBodyPos = defaultBodyPos + new Vector3(0f, -0.08f, -0.50f);
            targetBodyRot = defaultBodyRot * Quaternion.Euler(24f, 0f, -18f);
            targetBodyScale = defaultBodyScale;
            targetHeadRot = defaultHeadRot * Quaternion.Euler(25f, 20f, 0f);
        }

        public void SetDuelDodge(DuelDirection dir)
        {
            EnsureSockets();
            TriggerDodge(dir);

            Vector3 worldOffset = Vector3.zero;
            Quaternion rotOffset = Quaternion.identity;
            Vector3 scaleOffset = defaultBodyScale;

            switch (dir)
            {
                case DuelDirection.Left:
                    // Nhảy vọt sang TRÁI màn hình (-X) 1.25m, bay lên một chút
                    worldOffset = new Vector3(-1.25f, 0.40f, 0f);
                    rotOffset = Quaternion.Euler(0f, 0f, 35f);
                    scaleOffset = defaultBodyScale;
                    break;
                case DuelDirection.Right:
                    // Nhảy vọt luồn sang PHẢI màn hình (+X) 1.25m
                    worldOffset = new Vector3(1.25f, 0.35f, 0f);
                    rotOffset = Quaternion.Euler(0f, 0f, -35f);
                    scaleOffset = defaultBodyScale;
                    break;
                case DuelDirection.Straight: // Nhảy vút LÊN cao (+Y) 1.45m
                    worldOffset = new Vector3(0f, 1.45f, 0f);
                    rotOffset = Quaternion.Euler(-25f, 0f, 0f);
                    scaleOffset = new Vector3(defaultBodyScale.x * 0.85f, defaultBodyScale.y * 1.35f, defaultBodyScale.z * 0.85f);
                    break;
                case DuelDirection.Down: // Cúi rạp bẹp dí XUỐNG sàn (-Y)
                    worldOffset = new Vector3(0f, -0.42f, 0f);
                    rotOffset = Quaternion.Euler(35f, 0f, 0f);
                    scaleOffset = new Vector3(defaultBodyScale.x * 1.55f, defaultBodyScale.y * 0.35f, defaultBodyScale.z * 1.55f);
                    break;
            }

            Vector3 localOffset = transform.InverseTransformVector(worldOffset);
            targetBodyPos = defaultBodyPos + localOffset;
            targetBodyRot = defaultBodyRot * rotOffset;
            targetBodyScale = scaleOffset;
        }

        public void ResetDuelStance()
        {
            targetBodyPos = defaultBodyPos;
            targetBodyRot = defaultBodyRot;
            targetBodyScale = defaultBodyScale;
            targetHeadRot = defaultHeadRot;
            if (modelBodyTransform != null)
            {
                modelBodyTransform.localPosition = defaultBodyPos;
                modelBodyTransform.localRotation = defaultBodyRot;
                modelBodyTransform.localScale = defaultBodyScale;
            }
            if (socketHead != null)
            {
                socketHead.localRotation = defaultHeadRot;
            }
        }

        public void TriggerDodge(DuelDirection dir)
        {
            if (!HasAnimator) return;
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
