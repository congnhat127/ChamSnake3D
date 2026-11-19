using UnityEngine;

namespace RongRanChamCham
{
    public class FoodPickup : MonoBehaviour
    {
        [Header("Food Configuration")]
        [SerializeField] private FoodType foodType;

        [Header("Animation")]
        [SerializeField] private float rotateSpeed = 40f;
        [SerializeField] private float bobFrequency = 1.8f;
        [SerializeField] private float bobAmplitude = 0.06f;

        private Transform visualContainer;
        private Transform groundAura;
        private Vector3 startContainerPos;
        private Vector3 startAuraScale;

        public FoodType Type => foodType;

        public void Initialize(FoodType type)
        {
            foodType = type;
        }

        private void Start()
        {
            visualContainer = transform.Find("Visual_Container");
            groundAura = transform.Find("Ground_Aura_Ring");

            if (visualContainer != null)
            {
                startContainerPos = visualContainer.localPosition;
                // Nghiêng đĩa -34 độ ngửa mặt trực diện về phía camera góc nhìn trên cao
                visualContainer.localRotation = Quaternion.Euler(-34f, 0f, 0f);
            }
            else
            {
                startContainerPos = transform.position;
            }

            if (groundAura != null)
            {
                startAuraScale = groundAura.localScale;
            }
        }

        private void Update()
        {
            float time = Time.time;

            // 1. Nhấp nhô nhẹ đĩa thức ăn 3D
            if (visualContainer != null)
            {
                float bobY = startContainerPos.y + Mathf.Sin(time * bobFrequency) * bobAmplitude;
                visualContainer.localPosition = new Vector3(startContainerPos.x, bobY, startContainerPos.z);
                float pulse = 1.0f + 0.05f * Mathf.Sin(time * 3.0f);
                visualContainer.localScale = new Vector3(pulse, pulse, pulse);
            }
            else
            {
                float newY = startContainerPos.y + Mathf.Sin(time * bobFrequency) * bobAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // 2. Vòng dạ quang sàn phập phồng nhẹ
            if (groundAura != null)
            {
                float auraPulse = 1.0f + 0.10f * Mathf.Sin(time * 4.0f);
                groundAura.localScale = new Vector3(startAuraScale.x * auraPulse, startAuraScale.y, startAuraScale.z * auraPulse);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Case 1: Snake picks up food (Mother or any Child)
            if (other.CompareTag("Player") || other.GetComponent<MotherController>() != null || other.GetComponent<ChildFollower>() != null)
            {
                if (BuffSystem.Instance != null)
                {
                    BuffSystem.Instance.ApplyBuff(foodType, true);
                }
                Destroy(gameObject);
                return;
            }

            // Case 2: Hunter picks up food (Thầy Thuốc)
            if (other.GetComponent<HunterAI>() != null)
            {
                if (BuffSystem.Instance != null)
                {
                    BuffSystem.Instance.ApplyBuff(foodType, false);
                }
                Destroy(gameObject);
                return;
            }
        }
    }
}
