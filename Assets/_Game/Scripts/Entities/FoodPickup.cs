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
        private GameObject billboardBadge;
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

            CreateFloatingBillboardBadge();
        }

        private void CreateFloatingBillboardBadge()
        {
            billboardBadge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboardBadge.name = "Floating_Dish_Badge";
            billboardBadge.transform.SetParent(transform, false);
            billboardBadge.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            billboardBadge.transform.localScale = Vector3.one * 0.85f;
            Destroy(billboardBadge.GetComponent<Collider>());

            string texName = foodType switch
            {
                FoodType.Pho => "item_pho",
                FoodType.BanhMi => "item_banh_mi",
                FoodType.Kimbap => "item_kimbap",
                FoodType.Tteokbokki => "item_tteokbokki",
                _ => "item_pho"
            };

            Texture2D tex = Resources.Load<Texture2D>($"Items/{texName}");
#if UNITY_EDITOR
            if (tex == null)
            {
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Game/Textures/Items/{texName}.png");
            }
#endif
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            var mat = new Material(unlitShader);
            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_Blend", 0.0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
                mat.mainTexture = tex;
            }
            mat.SetColor("_BaseColor", Color.white);
            billboardBadge.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void Update()
        {
            float time = Time.time;

            // 1. Nhấp nhô đĩa thức ăn
            if (visualContainer != null)
            {
                float bobY = startContainerPos.y + Mathf.Sin(time * bobFrequency) * bobAmplitude;
                visualContainer.localPosition = new Vector3(startContainerPos.x, bobY, startContainerPos.z);
                float pulse = 1.0f + 0.04f * Mathf.Sin(time * 3.0f);
                visualContainer.localScale = new Vector3(pulse, pulse, pulse);
            }

            // 2. Icon 2.5D bay bổng và luôn xoay mặt về phía Camera
            if (billboardBadge != null)
            {
                float badgeY = 0.95f + Mathf.Sin(time * bobFrequency) * 0.10f;
                billboardBadge.transform.localPosition = new Vector3(0f, badgeY, 0f);
                if (Camera.main != null)
                {
                    billboardBadge.transform.rotation = Camera.main.transform.rotation;
                }
            }

            // 3. Vòng dạ quang sàn phập phồng nhẹ
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
