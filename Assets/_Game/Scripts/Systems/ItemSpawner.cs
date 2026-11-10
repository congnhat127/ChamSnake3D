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
        [SerializeField] private float minSpawnRadius = 3.5f;
        [SerializeField] private float maxSpawnRadius = 11.5f;

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
            else
            {
                chosenType = FoodType.Tteokbokki;
                prefabToSpawn = tteokbokkiPrefab;
            }

            // Spawn Position in Arena Circle
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = new Vector3(Mathf.Cos(angle) * r, 0.45f, Mathf.Sin(angle) * r);

            GameObject itemGo = null;
            if (prefabToSpawn != null)
            {
                itemGo = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: create token runtime
                itemGo = new GameObject($"Pickup_{chosenType}");
                itemGo.transform.position = spawnPos;
                var col = itemGo.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.45f;
                var pickup = itemGo.AddComponent<FoodPickup>();
                pickup.Initialize(chosenType);
            }

            if (itemGo != null)
            {
                activeItems.Add(itemGo);
            }
        }
    }
}
