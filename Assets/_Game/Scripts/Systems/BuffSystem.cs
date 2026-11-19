using System;
using UnityEngine;

namespace RongRanChamCham
{
    public class BuffSystem : MonoBehaviour
    {
        public static BuffSystem Instance { get; private set; }

        public event Action<FoodType, bool> OnBuffApplied; // (foodType, isSnake)
        public event Action<FoodType, bool> OnBuffExpired;

        [Header("Snake Active Buffs")]
        public bool SnakeHasShield { get; private set; }
        public float SnakeShieldTimer { get; private set; }
        public float SnakeSpeedTimer { get; private set; }
        public float SnakeGiantTimer { get; private set; }
        public float SnakeCloneTimer { get; private set; }

        [Header("Hunter Active Buffs")]
        public bool HunterPierceShield { get; private set; }
        public float HunterPierceTimer { get; private set; }
        public float HunterSpeedTimer { get; private set; }
        public float HunterGiantTimer { get; private set; }
        public float HunterCloneTimer { get; private set; }

        public bool IsSnakeSpeedActive => SnakeSpeedTimer > 0f;
        public bool IsSnakeGiantActive => SnakeGiantTimer > 0f;
        public bool IsSnakeCloneActive => SnakeCloneTimer > 0f;

        public bool IsHunterSpeedActive => HunterSpeedTimer > 0f;
        public bool IsHunterGiantActive => HunterGiantTimer > 0f;
        public bool IsHunterCloneActive => HunterCloneTimer > 0f;

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
            float dt = Time.deltaTime;

            // Snake timers
            if (SnakeShieldTimer > 0f)
            {
                SnakeShieldTimer -= dt;
                if (SnakeShieldTimer <= 0f)
                {
                    SnakeHasShield = false;
                    OnBuffExpired?.Invoke(FoodType.BanhMi, true);
                }
            }
            if (SnakeSpeedTimer > 0f)
            {
                SnakeSpeedTimer -= dt;
                if (SnakeSpeedTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Pho, true);
            }
            if (SnakeGiantTimer > 0f)
            {
                SnakeGiantTimer -= dt;
                if (SnakeGiantTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Kimbap, true);
            }
            if (SnakeCloneTimer > 0f)
            {
                SnakeCloneTimer -= dt;
                if (SnakeCloneTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Tteokbokki, true);
            }

            // Hunter timers
            if (HunterPierceTimer > 0f)
            {
                HunterPierceTimer -= dt;
                if (HunterPierceTimer <= 0f)
                {
                    HunterPierceShield = false;
                    OnBuffExpired?.Invoke(FoodType.BanhMi, false);
                }
            }
            if (HunterSpeedTimer > 0f)
            {
                HunterSpeedTimer -= dt;
                if (HunterSpeedTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Pho, false);
            }
            if (HunterGiantTimer > 0f)
            {
                HunterGiantTimer -= dt;
                if (HunterGiantTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Kimbap, false);
            }
            if (HunterCloneTimer > 0f)
            {
                HunterCloneTimer -= dt;
                if (HunterCloneTimer <= 0f) OnBuffExpired?.Invoke(FoodType.Tteokbokki, false);
            }
        }

        public void ConsumeSnakeShield()
        {
            if (SnakeHasShield)
            {
                SnakeHasShield = false;
                SnakeShieldTimer = 0f;
                Debug.Log("[BuffSystem] Khiên Bánh Mì của Rồng Rắn đã vỡ (đỡ thành công 1 đòn vồ)!");
                OnBuffExpired?.Invoke(FoodType.BanhMi, true);
            }
        }

        public void GrantTemporaryShield()
        {
            SnakeHasShield = true;
            SnakeShieldTimer = 3.0f; // Bảo hộ tạm thời 3s sau đối đầu
            Debug.Log("[BuffSystem] Cấp lá chắn bảo hộ tạm thời 3s cho đàn con sau pha đối đầu!");
        }

        public void ApplyBuff(FoodType type, bool isSnake)
        {
            float normalDuration = GameConstants.BuffDuration;
            float cloneDuration = GameConstants.CloneDuration;

            AudioManager.Instance?.PlaySFX(SoundType.Pickup);

            if (isSnake)
            {
                switch (type)
                {
                    case FoodType.BanhMi:
                        SnakeHasShield = true;
                        SnakeShieldTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Rồng Rắn nhận LÁ CHẮN BÁNH MÌ trong {normalDuration}s (đỡ 1 lần vồ)!");
                        break;
                    case FoodType.Pho:
                        SnakeSpeedTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Rồng Rắn ăn PHỞ: Tăng tốc +35% trong {normalDuration}s!");
                        break;
                    case FoodType.Kimbap:
                        SnakeGiantTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Rồng Rắn ăn CƠM CUỘN: Tăng kích thước +30% trong {normalDuration}s!");
                        break;
                    case FoodType.Tteokbokki:
                        SnakeCloneTimer = cloneDuration;
                        // Sinh ra ĐOÀN RỒNG RẮN ẢO ẢNH toàn diện (Mẹ + Đàn Con Út đội vương miện) tách ra từ đàn rắn thật
                        var snake = GameManager.Instance != null ? GameManager.Instance.SnakeChain : null;
                        var mother = GameManager.Instance != null ? GameManager.Instance.Mother : null;
                        if (mother != null)
                        {
                            DecoyCloneActor.Spawn(mother, snake);
                        }
                        Debug.Log($"<color=cyan>[BuffSystem]</color> Rồng Rắn ăn BÁNH GẠO CAY: Tạo NGUYÊN ĐOÀN RỒNG RẮN PHÂN THÂN trong {cloneDuration}s!");
                        break;
                }
                OnBuffApplied?.Invoke(type, true);
            }
            else
            {
                switch (type)
                {
                    case FoodType.BanhMi:
                        HunterPierceShield = true;
                        HunterPierceTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Thầy Thuốc ăn BÁNH MÌ: VỒ XUYÊN KHIÊN trong {normalDuration}s!");
                        break;
                    case FoodType.Pho:
                        HunterSpeedTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Thầy Thuốc ăn PHỞ: Tăng tốc +35% trong {normalDuration}s!");
                        break;
                    case FoodType.Kimbap:
                        HunterGiantTimer = normalDuration;
                        Debug.Log($"[BuffSystem] Thầy Thuốc ăn CƠM CUỘN: Tăng kích thước +30% trong {normalDuration}s!");
                        break;
                    case FoodType.Tteokbokki:
                        HunterCloneTimer = cloneDuration;
                        Debug.Log($"[BuffSystem] Thầy Thuốc ăn BÁNH GẠO CAY: Tạo PHÂN THÂN trong {cloneDuration}s!");
                        break;
                }
                OnBuffApplied?.Invoke(type, false);
            }
        }
    }
}
