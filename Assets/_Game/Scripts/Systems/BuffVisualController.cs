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
        private TrailRenderer speedTrail;
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

            // 2. SPEED DASH WIND TRAILS (Vệt lướt gió khí động học cuốn phía sau khi tăng tốc)
            GameObject trailObj = new GameObject("FX_Speed_Dash_Trail");
            trailObj.transform.SetParent(transform, false);
            trailObj.transform.localPosition = new Vector3(0f, isMother ? 0.55f : 0.35f, 0f);

            speedTrail = trailObj.AddComponent<TrailRenderer>();
            speedTrail.time = 0.38f;
            speedTrail.minVertexDistance = 0.08f;
            speedTrail.startWidth = isMother ? 0.85f : 0.50f;
            speedTrail.endWidth = 0.02f;
            speedTrail.autodestruct = false;
            speedTrail.emitting = false;

            speedMat = new Material(unlitShader);
            speedMat.SetFloat("_Surface", 1.0f);
            speedMat.SetFloat("_Blend", 0.0f);
            speedMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            speedMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            speedMat.SetInt("_ZWrite", 0);
            speedMat.SetInt("_Cull", 0);
            speedMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            speedMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Color speedCol = new Color(0.35f, 0.95f, 1.0f, 0.85f);
            speedMat.SetColor("_BaseColor", speedCol);
            speedTrail.material = speedMat;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.85f, 1f, 1f), 0.0f), new GradientColorKey(new Color(0.2f, 0.8f, 1f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            speedTrail.colorGradient = gradient;
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

            // Speed status: emit wind dash trails behind character
            bool hasSpeed = BuffSystem.Instance.IsSnakeSpeedActive;
            if (speedTrail != null)
            {
                if (speedTrail.emitting != hasSpeed)
                {
                    speedTrail.emitting = hasSpeed;
                }
            }
        }
    }
}
