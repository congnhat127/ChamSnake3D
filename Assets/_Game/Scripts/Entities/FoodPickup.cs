using UnityEngine;

namespace RongRanChamCham
{
    public class FoodPickup : MonoBehaviour
    {
        [Header("Food Configuration")]
        [SerializeField] private FoodType foodType;

        [Header("Animation")]
        [SerializeField] private float rotateSpeed = 65f;
        [SerializeField] private float bobFrequency = 2.0f;
        [SerializeField] private float bobAmplitude = 0.12f;

        private Vector3 startPosition;

        public FoodType Type => foodType;

        public void Initialize(FoodType type)
        {
            foodType = type;
        }

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            // Rotate smoothly around Y axis
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Gentle floating bob
            float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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
