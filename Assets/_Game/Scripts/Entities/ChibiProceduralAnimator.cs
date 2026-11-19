using UnityEngine;

namespace RongRanChamCham
{
    /// <summary>
    /// Procedural Chibi stepping and bounce motor.
    /// Drives cute pitter-patter footsteps and body bobbing based on movement velocity.
    /// Zero GC allocation in hot loops.
    /// </summary>
    public class ChibiProceduralAnimator : MonoBehaviour
    {
        [Header("Rig & Sockets")]
        [SerializeField] private Transform socketFootL;
        [SerializeField] private Transform socketFootR;
        [SerializeField] private Transform socketHandL;
        [SerializeField] private Transform socketHandR;
        [SerializeField] private Transform bodyVisual;

        public Transform BodyVisual => bodyVisual;

        [Header("Pitter-Patter Step Parameters")]
        [Tooltip("Step cycle frequency (steps per second multiplier)")]
        [SerializeField] private float stepFrequency = 14f;
        [Tooltip("Forward-backward foot stride swing distance (meters)")]
        [SerializeField] private float strideLength = 0.22f;
        [Tooltip("Foot lift height above ground during stride (meters)")]
        [SerializeField] private float stepLiftHeight = 0.12f;

        [Header("Body Bobbing & Wobble")]
        [Tooltip("Vertical body bounce height (meters)")]
        [SerializeField] private float bodyBobHeight = 0.05f;
        [Tooltip("Side-to-side roll wobble angle (degrees)")]
        [SerializeField] private float bodyWobbleAngle = 4.5f;

        private CharacterController characterController;
        private Animator animator;
        private Vector3 defaultFootLPos;
        private Vector3 defaultFootRPos;
        private Vector3 defaultHandLPos;
        private Vector3 defaultHandRPos;
        private Vector3 defaultBodyPos;
        private Quaternion defaultBodyRot;
        private Vector3 defaultScale;
        private float celebrationSeed = 0f;

        private float stepCycle = 0f;
        private float currentSpeedRatio = 0f;

        // Expressive Juice Timers & States
        private float powerUpTimer = 0f;
        private float powerUpDuration = 0.6f;
        private bool isVictoryCelebrating = false;
        private bool isDefeatedFrustration = false;

        public void TriggerPowerUpPulse(float duration = 0.6f)
        {
            powerUpDuration = duration;
            powerUpTimer = duration;
        }

        public void SetCelebrationMode(bool celebrating, bool defeated)
        {
            isVictoryCelebrating = celebrating;
            isDefeatedFrustration = defeated;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            if (animator != null && (animator.runtimeAnimatorController == null || !animator.isInitialized))
            {
                animator.enabled = false;
                animator = null;
            }
            EnsureSockets();
        }

        private void Start()
        {
            defaultScale = transform.localScale;
            if (socketFootL != null) defaultFootLPos = socketFootL.localPosition;
            if (socketFootR != null) defaultFootRPos = socketFootR.localPosition;
            if (socketHandL != null) defaultHandLPos = socketHandL.localPosition;
            if (socketHandR != null) defaultHandRPos = socketHandR.localPosition;
            celebrationSeed = UnityEngine.Random.Range(0f, 2f);

            if (bodyVisual != null)
            {
                defaultBodyPos = bodyVisual.localPosition;
                defaultBodyRot = bodyVisual.localRotation;
            }
        }

        private void EnsureSockets()
        {
            if (socketFootL == null) socketFootL = transform.Find("Socket_Foot_L");
            if (socketFootR == null) socketFootR = transform.Find("Socket_Foot_R");
            if (socketHandL == null) socketHandL = transform.Find("Socket_Hand_L");
            if (socketHandR == null) socketHandR = transform.Find("Socket_Hand_R");
            if (bodyVisual == null)
            {
                var body = transform.Find("Model_Body");
                if (body == null)
                {
                    var rend = GetComponentInChildren<Renderer>();
                    if (rend != null && rend.transform != transform) body = rend.transform;
                }
                if (body == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        var child = transform.GetChild(i);
                        if (!child.name.StartsWith("Socket_") && !child.name.Contains("VFX") && !child.name.Contains("Aura"))
                        {
                            body = child;
                            break;
                        }
                    }
                }
                bodyVisual = body;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Calculate horizontal movement speed
            Vector3 vel = characterController != null ? characterController.velocity : Vector3.zero;
            vel.y = 0f;
            float speed = vel.magnitude;

            // Determine if moving forward or backward relative to transform facing
            float forwardDot = speed > 0.05f ? Vector3.Dot(vel.normalized, transform.forward) : 0f;
            float stepDirection = forwardDot >= -0.1f ? 1.0f : -1.0f;

            // Target speed ratio (normalized around 5 m/s)
            float targetRatio = Mathf.Clamp01(speed / 5.0f);
            currentSpeedRatio = Mathf.MoveTowards(currentSpeedRatio, targetRatio, dt * 8f);

            // 2. Advance step cycle when moving
            if (currentSpeedRatio > 0.02f)
            {
                stepCycle += dt * stepFrequency * (speed / 3.5f) * stepDirection;
            }
            else
            {
                // Smoothly decay step cycle back towards integer phase (feet resting)
                stepCycle = Mathf.MoveTowards(stepCycle, Mathf.Round(stepCycle / Mathf.PI) * Mathf.PI, dt * 10f);
            }

            // 3. Procedural Foot Step Calculations
            // Left foot: sin(cycle), Right foot: sin(cycle + PI)
            float phaseL = Mathf.Sin(stepCycle);
            float phaseR = Mathf.Sin(stepCycle + Mathf.PI);

            // Forward-backward swing
            float offsetZL = phaseL * strideLength * currentSpeedRatio;
            float offsetZR = phaseR * strideLength * currentSpeedRatio;

            // Vertical foot lift (only lift on forward swing, max(0, sin))
            float liftL = Mathf.Max(0f, Mathf.Cos(stepCycle)) * stepLiftHeight * currentSpeedRatio;
            float liftR = Mathf.Max(0f, Mathf.Cos(stepCycle + Mathf.PI)) * stepLiftHeight * currentSpeedRatio;

            if (socketFootL != null)
            {
                Vector3 targetFootL = defaultFootLPos + new Vector3(0f, liftL, offsetZL);
                socketFootL.localPosition = Vector3.Lerp(socketFootL.localPosition, targetFootL, dt * 25f);
            }

            if (socketFootR != null)
            {
                Vector3 targetFootR = defaultFootRPos + new Vector3(0f, liftR, offsetZR);
                socketFootR.localPosition = Vector3.Lerp(socketFootR.localPosition, targetFootR, dt * 25f);
            }

            // 4. Body Motion & Expressive Modes (Luôn chạy nhảy ăn mừng tưng bừng khi Thắng!)
            if (bodyVisual != null)
            {
                if (isVictoryCelebrating)
                {
                    // Cheerful celebratory bounce & joyful sway (Nhảy cẫng lên ăn mừng cực vui nhộn!)
                    float phase = Time.time * 9.0f + celebrationSeed;
                    float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.42f;
                    float wobble = Mathf.Sin(phase * 0.9f) * 12f;
                    bodyVisual.localPosition = defaultBodyPos + new Vector3(0f, bob, 0f);
                    bodyVisual.localRotation = defaultBodyRot * Quaternion.Euler(-12f, 0f, wobble);

                    // Arms raised waving high in celebration! (Vung tay chào reo hò)
                    if (socketHandL != null)
                    {
                        float waveL = Mathf.Sin(phase * 1.5f) * 0.12f;
                        socketHandL.localPosition = defaultHandLPos + new Vector3(0f, 0.35f + waveL, 0.10f);
                        socketHandL.localRotation = Quaternion.Euler(0f, 0f, 35f + Mathf.Sin(phase) * 20f);
                    }
                    if (socketHandR != null)
                    {
                        float waveR = Mathf.Cos(phase * 1.5f) * 0.12f;
                        socketHandR.localPosition = defaultHandRPos + new Vector3(0f, 0.35f + waveR, 0.10f);
                        socketHandR.localRotation = Quaternion.Euler(0f, 0f, -35f - Mathf.Sin(phase) * 20f);
                    }

                    // Feet lift when jumping
                    if (socketFootL != null) socketFootL.localPosition = defaultFootLPos + new Vector3(0f, bob * 0.6f, 0f);
                    if (socketFootR != null) socketFootR.localPosition = defaultFootRPos + new Vector3(0f, bob * 0.6f, 0f);
                }
                else if (isDefeatedFrustration)
                {
                    // Frustrated stomping / dejected trembling
                    float phase = Time.time * 16f;
                    float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.08f;
                    float wobble = Mathf.Sin(phase * 1.5f) * 8f;
                    bodyVisual.localPosition = defaultBodyPos + new Vector3(0f, bob, 0f);
                    bodyVisual.localRotation = defaultBodyRot * Quaternion.Euler(16f, 0f, wobble);
                }
                else if (animator == null || !animator.enabled || animator.runtimeAnimatorController == null)
                {
                    // Bounce twice per step cycle
                    float bob = Mathf.Abs(Mathf.Sin(stepCycle)) * bodyBobHeight * currentSpeedRatio;
                    // Idle breathing when stationary
                    if (currentSpeedRatio <= 0.05f)
                    {
                        bob = Mathf.Sin(Time.time * 2.5f) * 0.015f;
                    }

                    // Side-to-side wobble
                    float wobble = phaseL * bodyWobbleAngle * currentSpeedRatio;

                    bodyVisual.localPosition = defaultBodyPos + new Vector3(0f, bob, 0f);
                    bodyVisual.localRotation = defaultBodyRot * Quaternion.Euler(0f, 0f, wobble);
                }
            }

            // 6. Power-Up Squash & Stretch Pulse (Gồng lên khi ăn buff)
            if (powerUpTimer > 0f)
            {
                powerUpTimer -= dt;
                float progress = Mathf.Clamp01(1f - (powerUpTimer / powerUpDuration));
                float pulse = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = defaultScale * (1.0f + pulse * 0.35f);
            }
        }
    }
}

