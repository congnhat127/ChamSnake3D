using System;
using UnityEngine;

namespace RongRanChamCham
{
    public class BuffSystem : MonoBehaviour
    {
        public static BuffSystem Instance { get; private set; }

        public event Action<FoodType, float> OnBuffActivated;
        public event Action<FoodType> OnBuffExpired;
        public event Action<int> OnConUtHealed;

        [Header("Active Status")]
        public bool IsShieldActive { get; private set; }
        public bool IsSpeedActive { get; private set; }
        public bool IsGiantActive { get; private set; }

        public float RemainingBuffTime { get; private set; }
        public FoodType? CurrentBuffType { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (RemainingBuffTime > 0)
            {
                RemainingBuffTime -= Time.deltaTime;
                if (RemainingBuffTime <= 0)
                {
                    ExpireCurrentBuff();
                }
            }
        }

        public void ApplyBuff(FoodType type)
        {
            CurrentBuffType = type;
            RemainingBuffTime = GameConstants.BuffDuration;

            switch (type)
            {
                case FoodType.BanhMi:
                    IsShieldActive = true;
                    break;

                case FoodType.Pho:
                    IsShieldActive = true;
                    OnConUtHealed?.Invoke(1);
                    break;

                case FoodType.Kimbap:
                    IsSpeedActive = true;
                    break;

                case FoodType.Kimchi:
                    IsGiantActive = true;
                    break;
            }

            OnBuffActivated?.Invoke(type, GameConstants.BuffDuration);
            Debug.Log($"[BuffSystem] Activated {type} for {GameConstants.BuffDuration}s!");
        }

        private void ExpireCurrentBuff()
        {
            if (CurrentBuffType.HasValue)
            {
                var expired = CurrentBuffType.Value;
                IsShieldActive = false;
                IsSpeedActive = false;
                IsGiantActive = false;
                CurrentBuffType = null;
                RemainingBuffTime = 0f;
                OnBuffExpired?.Invoke(expired);
                Debug.Log($"[BuffSystem] Buff {expired} expired.");
            }
        }
    }
}
