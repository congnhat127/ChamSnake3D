#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using RongRanChamCham;

namespace RongRanChamCham.Editor
{
    public static class BuildEnhancedFoodPrefabs
    {
        private const string ModelsDir = "Assets/_Game/Models";
        private const string TexturesDir = "Assets/_Game/Textures";
        private const string MaterialsDir = "Assets/_Game/Materials";
        private const string PrefabsDir = "Assets/_Game/Prefabs";

        [MenuItem("Tools/Rong Ran/Build Enhanced High-Visibility Food Prefabs")]
        public static void Execute()
        {
            var tokenModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Items/Food_Plate_Token.obj");
            if (tokenModel == null)
            {
                Debug.LogError("[BuildEnhancedFoodPrefabs] Food_Plate_Token.obj not found!");
                return;
            }

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");

            // Ensure texture importer is configured for crisp transparency
            string ringTexPath = $"{TexturesDir}/Items/tex_item_border_ring.png";
            var importer = AssetImporter.GetAtPath(ringTexPath) as TextureImporter;
            if (importer != null)
            {
                if (!importer.alphaIsTransparency || importer.mipmapEnabled)
                {
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            // Soft border ring texture (hollow transparent center, colored border ring)
            var glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ringTexPath);

            var items = new (FoodType type, string texName, string matName, string prefabName, Color auraColor)[]
            {
                (FoodType.BanhMi, "item_banh_mi.png", "Mat_Item_BanhMi.mat", "Food_BanhMi.prefab", new Color(1.0f, 0.75f, 0.20f)), // Warm Golden
                (FoodType.Pho, "item_pho.png", "Mat_Item_Pho.mat", "Food_Pho.prefab", new Color(0.20f, 0.80f, 1.0f)),             // Soft Sky Blue
                (FoodType.Kimbap, "item_kimbap.png", "Mat_Item_Kimbap.mat", "Food_Kimbap.prefab", new Color(0.30f, 0.90f, 0.50f)),         // Soft Emerald
                (FoodType.Tteokbokki, "item_tteokbokki.png", "Mat_Item_Tteokbokki.mat", "Food_Tteokbokki.prefab", new Color(1.0f, 0.35f, 0.35f)) // Soft Coral
            };

            foreach (var item in items)
            {
                // 1. Food Plate Material (Subtle, elegant sheen)
                string matPath = $"{MaterialsDir}/Items/{item.matName}";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(litShader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Items/{item.texName}");
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                    mat.mainTexture = tex;
                }
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", item.auraColor * 0.12f);
                EditorUtility.SetDirty(mat);

                // 2. Soft Feathered Transparent Ground Aura Material (Hollow Ring)
                string auraMatPath = $"{MaterialsDir}/Items/Mat_Aura_{item.type}.mat";
                var auraMat = AssetDatabase.LoadAssetAtPath<Material>(auraMatPath);
                if (auraMat == null)
                {
                    auraMat = new Material(unlitShader);
                    AssetDatabase.CreateAsset(auraMat, auraMatPath);
                }
                if (glowTex != null)
                {
                    auraMat.SetTexture("_BaseMap", glowTex);
                    auraMat.SetTexture("_MainTex", glowTex);
                    auraMat.mainTexture = glowTex;
                }

                // Setup Transparent Alpha Blending in URP
                auraMat.SetFloat("_Surface", 1.0f); // 1 = Transparent
                auraMat.SetFloat("_Blend", 0.0f);   // 0 = Alpha blend
                auraMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                auraMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                auraMat.SetInt("_ZWrite", 0);
                auraMat.SetInt("_Cull", 0);         // Double-sided
                auraMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                Color colVal = new Color(item.auraColor.r, item.auraColor.g, item.auraColor.b, 0.85f);
                auraMat.SetColor("_BaseColor", colVal);
                auraMat.SetColor("_Color", colVal);
                auraMat.color = colVal;
                auraMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                EditorUtility.SetDirty(auraMat);

                // 3. Build Prefab Root
                string prefabPath = $"{PrefabsDir}/Items/{item.prefabName}";
                GameObject root = new GameObject(item.prefabName.Replace(".prefab", ""));

                // Sphere Collider for pickup (rộng rãi 0.75m radius để đón bắt mượt mà)
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.75f;
                col.center = new Vector3(0f, 0.48f, 0f);

                // Visual Container (animated by FoodPickup)
                var visualContainer = new GameObject("Visual_Container");
                visualContainer.transform.SetParent(root.transform, false);

                // 3a. Clean 3D Food Plate (Tăng kích thước to rõ 2.5x, góc ngửa do FoodPickup điều khiển)
                var tokenInst = (GameObject)PrefabUtility.InstantiatePrefab(tokenModel, visualContainer.transform);
                tokenInst.name = "Visual_Plate";
                tokenInst.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                tokenInst.transform.localRotation = Quaternion.identity;
                tokenInst.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

                foreach (var r in tokenInst.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                // 4. Soft Feathered Ground Glow (Flat Quad on floor)
                var auraGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                auraGo.name = "Ground_Aura_Ring";
                auraGo.transform.SetParent(root.transform, false);
                auraGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                auraGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                auraGo.transform.localScale = new Vector3(2.3f, 2.3f, 2.3f);
                var auraRend = auraGo.GetComponent<Renderer>();
                auraRend.sharedMaterial = auraMat;
                auraRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                UnityEngine.Object.DestroyImmediate(auraGo.GetComponent<Collider>());

                // 5. Soft Point Light (Delicate tint, not blinding)
                var lightGo = new GameObject("Point_Light_Glow");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                var pLight = lightGo.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = item.auraColor;
                pLight.range = 2.2f;
                pLight.intensity = 0.55f;
                pLight.shadows = LightShadows.None;

                // 6. FoodPickup Logic Component
                var pickup = root.AddComponent<FoodPickup>();
                pickup.Initialize(item.type);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                Debug.Log($"[BuildEnhancedFoodPrefabs] Đã tạo Prefab vật phẩm tinh tế: {prefabPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
