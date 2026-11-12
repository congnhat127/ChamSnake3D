using UnityEngine;

namespace RongRanChamCham
{
    /// <summary>
    /// Camera của trận đấu: bám đoàn rồng rắn khi rượt đuổi, nhìn ngang cặp Thầy Thuốc - bé Út khi đấu,
    /// và hạ xuống cận cảnh khi kết trận.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform motherTransform;
        [SerializeField] private Transform tailChildTransform;
        [SerializeField] private Transform hunterTransform;

        [Header("Hướng nhìn")]
        [Tooltip("Góc quay quanh trục Y của camera. 180 = đứng phía +Z nhìn về -Z, đúng hướng thiết kế của bối cảnh (công trình nằm phía sau sân, không che màn hình).")]
        [SerializeField] private float viewYaw = 180f;

        [Header("Bám theo khi rượt đuổi")]
        [SerializeField] private float followHeight = 19.5f;
        [SerializeField] private float followBack = 13.0f;
        [SerializeField] private float followPitch = 54f;
        [SerializeField] private float smoothSpeed = 6.0f;
        [Tooltip("Điểm camera nhìn vào không tiến sát mép sân hơn khoảng này (m), để màn hình không bị chiếm bởi vùng ngoài sân.")]
        [SerializeField] private float focusEdgeInset = 3.5f;

        [Header("Đấu Chăm Chăm Chăm")]
        [Tooltip("Khoảng cách ngang từ camera tới cặp Thầy Thuốc - bé Út (m), cộng thêm nửa khoảng cách giữa hai người.")]
        [SerializeField] private float duelDistance = 4.5f;
        [SerializeField] private float duelHeight = 1.6f;
        [SerializeField] private float duelSmoothSpeed = 5f;

        [Header("Kết trận")]
        [Tooltip("Khoảng cách từ camera tới mép nhóm nhân vật đang ăn mừng (m). AGENTS.md mục 6: cận cảnh 1.8m.")]
        [SerializeField] private float endDistance = 1.8f;
        [SerializeField] private float endSmoothSpeed = 4f;

        // Khung hình kết trận tính theo chiều cao thật của model (đo từ Renderer.bounds):
        // camera ở khoảng tầm mắt, nhìn vào giữa thân, và chừa thêm 25% để đầu không bị cắt khi nhân vật nhảy.
        private const float EyeHeightFactor = 0.6f;
        private const float LookHeightFactor = 0.5f;
        private const float FramingMargin = 1.25f;
        private const float FallbackCharacterHeight = 1.7f; // bằng CharacterController.height

        private Camera cachedCamera;
        private Light sunLight;
        private Transform measuredTarget;
        private float measuredHeight;

        // Điểm nhìn vào thân nhân vật: khoảng ngang ngực của chibi cao 1.7m.
        private const float LookHeight = 0.9f;
        // Bằng CharacterController.radius của Thầy Thuốc.
        private const float HunterBodyRadius = 0.45f;

        [Header("Showcase Mode")]
        [SerializeField] private bool isShowcaseMode = false;
        [SerializeField] private Transform showcaseTarget;

        private bool isDuelMode = false;
        private static float activeViewYaw = 0f;

        public float ViewYaw => viewYaw;

        /// <summary>Góc quay quanh Y để nhân vật nhìn thẳng vào camera trận đấu.</summary>
        public static float FacingCameraYaw => activeViewYaw + 180f;

        /// <summary>Đổi hướng joystick (x = phải, y = lên trên màn hình) sang hướng trên mặt sân theo góc nhìn của camera.</summary>
        public static Vector3 InputToWorld(Vector2 input)
        {
            return Quaternion.Euler(0f, activeViewYaw, 0f) * new Vector3(input.x, 0f, input.y);
        }

        /// <summary>Góc quay để một nhân vật ở <paramref name="from"/> quay mặt về camera, cúi đầu thêm <paramref name="pitch"/> độ.</summary>
        public static Quaternion FacingCamera(Vector3 from, float pitch = 0f)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - from;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f)
                {
                    return Quaternion.LookRotation(toCam.normalized) * Quaternion.Euler(pitch, 0f, 0f);
                }
            }
            return Quaternion.Euler(pitch, activeViewYaw + 180f, 0f);
        }

        private void OnEnable()
        {
            activeViewYaw = viewYaw;
        }

        private void Start()
        {
            cachedCamera = GetComponent<Camera>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type == LightType.Directional) { sunLight = light; break; }
            }
            EnsureTargets();
        }

        /// <summary>Chiều cao model của nhân vật, đo một lần cho mỗi mục tiêu.</summary>
        private float HeightOf(Transform target)
        {
            if (target == measuredTarget) return measuredHeight;

            measuredTarget = target;
            measuredHeight = FallbackCharacterHeight;
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float top = renderers[0].bounds.max.y;
                for (int i = 1; i < renderers.Length; i++) top = Mathf.Max(top, renderers[i].bounds.max.y);
                measuredHeight = Mathf.Max(top - target.position.y, 0.5f);
            }
            return measuredHeight;
        }

        /// <summary>Khoảng cách để một vật cao <paramref name="height"/> nằm trọn trong khung hình theo chiều dọc.</summary>
        private float DistanceToFit(float height)
        {
            float fov = cachedCamera != null ? cachedCamera.fieldOfView : 42f;
            return height * 0.5f * FramingMargin / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }

        public void SetTargets(Transform mother, Transform tail, Transform hunter)
        {
            motherTransform = mother;
            tailChildTransform = tail;
            hunterTransform = hunter;
        }

        private void EnsureTargets()
        {
            if (motherTransform == null)
            {
                var mother = Object.FindAnyObjectByType<MotherController>();
                if (mother != null) motherTransform = mother.transform;
            }

            if (tailChildTransform == null)
            {
                var snake = Object.FindAnyObjectByType<SnakeChainIK>();
                if (snake != null && snake.ConUt != null) tailChildTransform = snake.ConUt.transform;
            }

            if (hunterTransform == null)
            {
                var hunter = Object.FindAnyObjectByType<HunterAI>();
                if (hunter != null) hunterTransform = hunter.transform;
            }
        }

        public void SetShowcaseMode(bool enabled, Transform focusTarget = null)
        {
            isShowcaseMode = enabled;
            showcaseTarget = focusTarget;
        }

        public void SetDuelMode(bool enabled)
        {
            isDuelMode = enabled;
        }

        private void LateUpdate()
        {
            EnsureTargets();

            if (isShowcaseMode && showcaseTarget != null)
            {
                UpdateShowcase();
                return;
            }

            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Chasing;

            if (state == GameState.Victory && motherTransform != null)
            {
                UpdateVictory();
                return;
            }

            if (state == GameState.GameOver && hunterTransform != null)
            {
                // Đứng phía bên kia Thầy Thuốc so với đoàn rồng rắn: Thầy đắc thắng ở tiền cảnh, Mẹ và con ủ rũ phía sau.
                Vector3 hunterPos = hunterTransform.position;
                Vector3 dir = motherTransform != null ? Flat(hunterPos - motherTransform.position) : Flat(transform.position - hunterPos);
                dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Quaternion.Euler(0f, viewYaw, 0f) * Vector3.back;

                float height = HeightOf(hunterTransform);
                float distance = Mathf.Max(HunterBodyRadius + endDistance, DistanceToFit(height));
                MoveTo(hunterPos + dir * distance + Vector3.up * (height * EyeHeightFactor), hunterPos + Vector3.up * (height * LookHeightFactor), endSmoothSpeed);
                return;
            }

            if (isDuelMode && hunterTransform != null && tailChildTransform != null)
            {
                UpdateDuel();
                return;
            }

            if (motherTransform == null) return;
            UpdateFollow();
        }

        private void UpdateFollow()
        {
            Vector3 center = motherTransform.position;
            if (tailChildTransform != null)
            {
                center = (motherTransform.position * 0.6f) + (tailChildTransform.position * 0.4f);
            }

            // Giữ điểm nhìn cách mép sân một khoảng để khung hình luôn chủ yếu là mặt sân.
            Vector2 focus = ArenaBoundary.ClampToShape(ArenaBoundary.ActiveArena, new Vector2(center.x, center.z), focusEdgeInset);

            Quaternion yawRot = Quaternion.Euler(0f, viewYaw, 0f);
            Vector3 target = new Vector3(focus.x, 0f, focus.y) + Vector3.up * followHeight + yawRot * Vector3.back * followBack;

            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(followPitch, viewYaw, 0f), Time.deltaTime * smoothSpeed);
        }

        private void UpdateDuel()
        {
            // Đứng ngang hông cặp đấu để thấy cả Thầy Thuốc lẫn bé Út.
            Vector3 hunterPos = hunterTransform.position;
            Vector3 tailPos = tailChildTransform.position;
            Vector3 mid = (hunterPos + tailPos) * 0.5f;
            Vector3 axis = Flat(tailPos - hunterPos);

            Vector3 side = SideToward(mid, axis);
            float dist = duelDistance + axis.magnitude * 0.5f;
            MoveTo(mid + side * dist + Vector3.up * duelHeight, mid + Vector3.up * LookHeight, duelSmoothSpeed);
        }

        private void UpdateVictory()
        {
            // Nhóm ăn mừng gồm Mẹ và các con còn lại: đứng ngang hàng để không ai che ai.
            Vector3 center = motherTransform.position;
            int count = 1;
            var chain = GameManager.Instance.SnakeChain;
            if (chain != null)
            {
                var children = chain.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] == null) continue;
                    center += children[i].transform.position;
                    count++;
                }
            }
            center /= count;

            float radius = Flat(motherTransform.position - center).magnitude;
            Vector3 axis = tailChildTransform != null ? Flat(motherTransform.position - tailChildTransform.position) : Vector3.zero;
            Vector3 side = SideToward(center, axis);

            // Hàng nhân vật rộng 2 x radius; lùi thêm một lần bề rộng đó để cả hàng nằm trong khung hình.
            float height = HeightOf(motherTransform);
            float distance = Mathf.Max(endDistance + HunterBodyRadius + radius * 2f, DistanceToFit(height));

            // Ưu tiên đứng phía có nắng chiếu vào mặt nhân vật.
            if (sunLight != null && Vector3.Dot(side, Flat(-sunLight.transform.forward)) < 0f) side = -side;

            // Nhưng nếu Thầy Thuốc đang đứng chắn giữa camera và nhóm ăn mừng thì sang phía bên kia.
            if (hunterTransform != null)
            {
                Vector3 toHunter = Flat(hunterTransform.position - center);
                float along = Vector3.Dot(side, toHunter);
                float across = (toHunter - side * along).magnitude;
                if (along > 0f && along < distance && across < radius + HunterBodyRadius * 2f) side = -side;
            }

            MoveTo(center + side * distance + Vector3.up * (height * EyeHeightFactor), center + Vector3.up * (height * LookHeightFactor), endSmoothSpeed);
        }

        private void UpdateShowcase()
        {
            // Cùng phía với camera trận đấu (phía có nắng), lùi vừa đủ để thấy trọn nhân vật.
            float height = HeightOf(showcaseTarget);
            Vector3 back = Quaternion.Euler(0f, viewYaw, 0f) * Vector3.back;
            MoveTo(showcaseTarget.position + back * DistanceToFit(height) + Vector3.up * (height * EyeHeightFactor),
                showcaseTarget.position + Vector3.up * (height * LookHeightFactor), 6f);
        }

        /// <summary>Hướng vuông góc với <paramref name="axis"/>, chọn phía đang gần camera hơn để camera không phải vòng qua nhân vật.</summary>
        private Vector3 SideToward(Vector3 pivot, Vector3 axis)
        {
            if (axis.sqrMagnitude < 0.01f)
            {
                Vector3 fallback = Flat(transform.position - pivot);
                return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Quaternion.Euler(0f, viewYaw, 0f) * Vector3.back;
            }

            Vector3 side = Vector3.Cross(Vector3.up, axis.normalized);
            if (Vector3.Dot(side, Flat(transform.position - pivot)) < 0f) side = -side;
            return side;
        }

        private void MoveTo(Vector3 position, Vector3 lookAt, float speed)
        {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * speed);
            Vector3 toTarget = lookAt - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * speed);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
