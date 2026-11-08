using UnityEngine;

namespace RongRanChamCham
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform motherTransform;
        [SerializeField] private Transform tailChildTransform;
        [SerializeField] private Transform hunterTransform;

        [Header("Camera Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 20f, -13f);
        [SerializeField] private float smoothSpeed = 6.0f;

        [Header("Duel POV Mode")]
        [SerializeField] private bool isDuelMode = false;
        [SerializeField] private Vector3 duelCameraPos = new Vector3(0f, 2.2f, -3.5f);
        [SerializeField] private Vector3 duelCameraEuler = new Vector3(10f, 0f, 0f);

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private void Start()
        {
            EnsureTargets();
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
                var mother = UnityEngine.Object.FindAnyObjectByType<MotherController>();
                if (mother != null) motherTransform = mother.transform;
            }

            if (tailChildTransform == null)
            {
                var snake = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
                if (snake != null && snake.ConUt != null) tailChildTransform = snake.ConUt.transform;
            }

            if (hunterTransform == null)
            {
                var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
                if (hunter != null) hunterTransform = hunter.transform;
            }
        }

        public void SetDuelMode(bool enabled, Vector3? customPos = null, Quaternion? customRot = null)
        {
            isDuelMode = enabled;
            if (customPos.HasValue) duelCameraPos = customPos.Value;
            if (customRot.HasValue) targetRotation = customRot.Value;
        }

        private void LateUpdate()
        {
            EnsureTargets();

            if (isDuelMode)
            {
                transform.position = Vector3.Lerp(transform.position, duelCameraPos, Time.deltaTime * 5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(duelCameraEuler), Time.deltaTime * 5f);
                return;
            }

            if (motherTransform == null) return;

            Vector3 center = motherTransform.position;
            if (tailChildTransform != null)
            {
                center = (motherTransform.position * 0.6f) + (tailChildTransform.position * 0.4f);
            }

            // Smooth camera offset
            targetPosition = center + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }
    }
}
