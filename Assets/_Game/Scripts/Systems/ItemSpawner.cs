using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Food Prefabs")]
        [SerializeField] private GameObject banhMiPrefab;
        [SerializeField] private GameObject phoPrefab;
        [SerializeField] private GameObject kimbapPrefab;
        [SerializeField] private GameObject tteokbokkiPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int maxItemsOnField = 2;
        [SerializeField] private float spawnInterval = 10f;
        [Tooltip("Bánh Gạo Cay (phân thân) chưa có hiệu ứng trong game; bật khi đã làm xong phân thân")]
        [SerializeField] private bool spawnCloneFood = false;
        [Tooltip("Bán kính chiếm chỗ của vật phẩm, bằng bán kính trigger của pickup (m)")]
        [SerializeField] private float itemBodyRadius = 0.55f;
        [Tooltip("Độ cao lơ lửng của vật phẩm so với mặt sân (m)")]
        [SerializeField] private float itemHoverHeight = 0.45f;

        private float nextSpawnTime = 0f;
        private List<GameObject> activeItems = new List<GameObject>();

        private void Start()
        {
            // Initial spawn
            SpawnRandomFood();
            SpawnRandomFood();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            activeItems.RemoveAll(item => item == null);

            if (activeItems.Count < maxItemsOnField && Time.time >= nextSpawnTime)
            {
                SpawnRandomFood();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        public void SpawnRandomFood()
        {
            // Weighted Random Probability:
            // Banh Mi: 35% (0.00 - 0.35)
            // Pho: 35% (0.35 - 0.70)
            // Kimbap: 20% (0.70 - 0.90)
            // Tteokbokki: 10% (0.90 - 1.00)
            float roll = Random.value;
            FoodType chosenType;
            GameObject prefabToSpawn = null;

            if (roll < 0.35f)
            {
                chosenType = FoodType.BanhMi;
                prefabToSpawn = banhMiPrefab;
            }
            else if (roll < 0.70f)
            {
                chosenType = FoodType.Pho;
                prefabToSpawn = phoPrefab;
            }
            else if (roll < 0.90f)
            {
                chosenType = FoodType.Kimbap;
                prefabToSpawn = kimbapPrefab;
            }
            else if (spawnCloneFood)
            {
                chosenType = FoodType.Tteokbokki;
                prefabToSpawn = tteokbokkiPrefab;
            }
            else
            {
                chosenType = FoodType.Kimbap;
                prefabToSpawn = kimbapPrefab;
            }

            // Vị trí sinh: phân bố đều trong vùng di chuyển được của sân, ngoài vật cản.
            Vector3 spawnPos = ArenaBoundary.RandomPointInside(itemBodyRadius);
            spawnPos.y = itemHoverHeight;

            if (prefabToSpawn == null)
            {
                // Không sinh vật phẩm không có hình: người chơi sẽ nhận buff mà không thấy mình ăn gì.
                Debug.LogWarning($"[ItemSpawner] Chưa gán prefab cho {chosenType}, bỏ qua lần sinh này.");
                return;
            }

            GameObject itemGo = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            if (itemGo != null)
            {
                activeItems.Add(itemGo);
            }
        }
    }
}
