using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Item Sprites")]
        [SerializeField] private Sprite banhMiSprite;
        [SerializeField] private Sprite phoSprite;
        [SerializeField] private Sprite kimbapSprite;
        [SerializeField] private Sprite kimchiSprite;

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
            // Clean up nulls
            activeItems.RemoveAll(item => item == null);

            if (activeItems.Count < maxItemsOnField && Time.time >= nextSpawnTime)
            {
                SpawnRandomFood();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        public void SpawnRandomFood()
        {
            FoodType type = (FoodType)Random.Range(0, 4);
            Sprite spriteToUse = null;

            switch (type)
            {
                case FoodType.BanhMi: spriteToUse = banhMiSprite; break;
                case FoodType.Pho: spriteToUse = phoSprite; break;
                case FoodType.Kimbap: spriteToUse = kimbapSprite; break;
                case FoodType.Kimchi: spriteToUse = kimchiSprite; break;
            }

            // Random position in circle
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPos = new Vector3(Mathf.Cos(angle) * r, 0.8f, Mathf.Sin(angle) * r);

            // Create GameObject
            var itemGo = new GameObject($"Pickup_{type}");
            itemGo.transform.position = spawnPos;

            var collider = itemGo.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.8f;

            var sr = itemGo.AddComponent<SpriteRenderer>();
            sr.sprite = spriteToUse;
            itemGo.transform.localScale = Vector3.one * 0.7f;

            var pickup = itemGo.AddComponent<FoodPickup>();
            pickup.Initialize(type, spriteToUse);

            activeItems.Add(itemGo);
        }
    }
}
