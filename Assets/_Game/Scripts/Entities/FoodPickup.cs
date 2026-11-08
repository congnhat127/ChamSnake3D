using UnityEngine;

namespace RongRanChamCham
{
    public class FoodPickup : MonoBehaviour
    {
        [Header("Food Configuration")]
        [SerializeField] private FoodType foodType;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float rotateSpeed = 80f;
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobAmplitude = 0.25f;

        private Vector3 startPosition;

        public FoodType Type => foodType;

        public void Initialize(FoodType type, Sprite sprite)
        {
            foodType = type;
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            // Rotate continuously
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Bob up and down gently
            float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Mother or Children can collect the food
            if (other.CompareTag("Player") || other.GetComponent<MotherController>() != null || other.GetComponent<ChildFollower>() != null)
            {
                if (BuffSystem.Instance != null)
                {
                    BuffSystem.Instance.ApplyBuff(foodType);
                }

                // TODO: Play pickup SFX & spawn sparkle particles
                Destroy(gameObject);
            }
        }
    }
}
