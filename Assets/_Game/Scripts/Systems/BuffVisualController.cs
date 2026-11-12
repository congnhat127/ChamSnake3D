using UnityEngine;

namespace RongRanChamCham
{
    public class BuffVisualController : MonoBehaviour
    {
        [Header("Character Type")]
        [SerializeField] private bool isMother = true;
        [SerializeField] private bool isConUt = false;

        [Header("Visual Effects")]
        private GameObject shieldSphereObj;
        private GameObject speedAuraObj;
        private Material shieldMat;
        private Material speedMat;

        private void Start()
        {
            CreateVisualObjects();
        }

        private void CreateVisualObjects()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");

            // 1. SHIELD ENERGY BUBBLE (Hình cầu năng lượng bảo vệ trong suốt)
            shieldSphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldSphereObj.name = "FX_Shield_Bubble";
            shieldSphereObj.transform.SetParent(transform, false);
            shieldSphereObj.transform.localPosition = new Vector3(0f, isMother ? 0.95f : 0.55f, 0f);
            float shieldScale = isMother ? 2.1f : 1.35f;
            shieldSphereObj.transform.localScale = Vector3.one * shieldScale;
            Destroy(shieldSphereObj.GetComponent<Collider>());

            shieldMat = new Material(unlitShader);
            shieldMat.SetFloat("_Surface", 1.0f);
            shieldMat.SetFloat("_Blend", 0.0f);
            shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shieldMat.SetInt("_ZWrite", 0);
            shieldMat.SetInt("_Cull", 0);
            shieldMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            shieldMat.SetOverrideTag("RenderType", "Transparent");
            shieldMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Color shieldCol = new Color(0.25f, 0.85f, 1.0f, 0.45f);
            shieldMat.SetColor("_BaseColor", shieldCol);
            shieldMat.SetColor("_Color", shieldCol);
            shieldMat.color = shieldCol;
            shieldSphereObj.GetComponent<Renderer>().sharedMaterial = shieldMat;
            shieldSphereObj.SetActive(false);

            // 2. SPEED AURA RING (Vòng hào quang lướt gió tăng tốc)
            speedAuraObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            speedAuraObj.name = "FX_Speed_Aura";
            speedAuraObj.transform.SetParent(transform, false);
            speedAuraObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            speedAuraObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float speedScale = isMother ? 1.8f : 1.2f;
            speedAuraObj.transform.localScale = Vector3.one * speedScale;
            Destroy(speedAuraObj.GetComponent<Collider>());

            speedMat = new Material(unlitShader);
            speedMat.SetFloat("_Surface", 1.0f);
            speedMat.SetFloat("_Blend", 0.0f);
            speedMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            speedMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            speedMat.SetInt("_ZWrite", 0);
            speedMat.SetInt("_Cull", 0);
            speedMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            speedMat.SetOverrideTag("RenderType", "Transparent");
            speedMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Color speedCol = new Color(0.2f, 0.95f, 0.85f, 0.85f);
            speedMat.SetColor("_BaseColor", speedCol);
            speedMat.SetColor("_Color", speedCol);
            speedMat.color = speedCol;

            Texture2D ringTex = null;
#if UNITY_EDITOR
            ringTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/Items/tex_item_border_ring.png");
#endif
            if (ringTex != null)
            {
                speedMat.SetTexture("_BaseMap", ringTex);
                speedMat.SetTexture("_MainTex", ringTex);
                speedMat.mainTexture = ringTex;
            }
            speedAuraObj.GetComponent<Renderer>().sharedMaterial = speedMat;
            speedAuraObj.SetActive(false);
        }

        private void Update()
        {
            if (BuffSystem.Instance == null) return;

            // Shield status
            bool hasShield = BuffSystem.Instance.SnakeHasShield && (isMother || isConUt);
            if (shieldSphereObj != null)
            {
                if (shieldSphereObj.activeSelf != hasShield) shieldSphereObj.SetActive(hasShield);
                if (hasShield && shieldMat != null)
                {
                    // Gentle energetic pulsation
                    float pulse = Mathf.Sin(Time.time * 6.0f) * 0.12f;
                    float alpha = 0.35f + pulse;
                    Color c = new Color(0.3f, 0.85f, 1.0f, alpha);
                    shieldMat.SetColor("_BaseColor", c);
                    shieldMat.color = c;
                    shieldSphereObj.transform.localScale = Vector3.one * ((isMother ? 2.1f : 1.35f) + pulse * 0.25f);
                }
            }

            // Speed status
            bool hasSpeed = BuffSystem.Instance.IsSnakeSpeedActive && isMother;
            if (speedAuraObj != null)
            {
                if (speedAuraObj.activeSelf != hasSpeed) speedAuraObj.SetActive(hasSpeed);
                if (hasSpeed)
                {
                    // Fast energetic spin
                    speedAuraObj.transform.Rotate(0f, 0f, 280f * Time.deltaTime, Space.Self);
                }
            }
        }
    }
}
