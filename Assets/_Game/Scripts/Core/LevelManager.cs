using System;
using UnityEngine;

namespace RongRanChamCham
{
    [System.Serializable]
    public class LevelData
    {
        public int levelIndex;          // 1 to 5
        public string title;            // e.g. "Đình Làng (Hướng Dẫn)"
        public string subtitle;         // e.g. "Làm quen cách di chuyển & phòng thủ"
        public float matchDuration;     // 60s
        public int childCount;          // 3, 4, 5
        public float hunterSpeedMult;   // 0.70x, 1.0x, 1.15x, 1.30x
        public float qteReactionWindow; // 1.4s, 1.1s, 0.95s, 0.85s
        public bool hasTutorialGuide;   // true for Level 1
    }

    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        private const string PrefUnlockedKey = "RR_UnlockedLevel";

        // Không [SerializeField]: bảng này là nguồn duy nhất, scene không được giữ bản sao riêng.
        private readonly LevelData[] levels = new LevelData[]
        {
            new LevelData
            {
                levelIndex = 1,
                title = "1. Sân Đình Làng Quê",
                subtitle = "Nhập môn: sân trống, Thầy Thuốc chậm",
                matchDuration = 90f,
                childCount = 3,
                hunterSpeedMult = 0.72f,
                qteReactionWindow = 1.4f,
                hasTutorialGuide = true
            },
            new LevelData
            {
                levelIndex = 2,
                title = "2. Làng Hanok Bukchon",
                subtitle = "Năm vật cản đứng yên giữa sân",
                matchDuration = 90f,
                childCount = 4,
                hunterSpeedMult = 1.0f,
                qteReactionWindow = 1.1f,
                hasTutorialGuide = false
            },
            new LevelData
            {
                levelIndex = 3,
                title = "3. Phố Cổ Hội An",
                subtitle = "Sân đêm đèn lồng, Thầy Thuốc nhanh hơn",
                matchDuration = 90f,
                childCount = 5,
                hunterSpeedMult = 1.15f,
                qteReactionWindow = 1.0f,
                hasTutorialGuide = false
            },
            new LevelData
            {
                levelIndex = 4,
                title = "4. Cung Gyeongbokgung",
                subtitle = "Ba quả cầu tuyết lăn qua sân",
                matchDuration = 90f,
                childCount = 5,
                hunterSpeedMult = 1.25f,
                qteReactionWindow = 0.9f,
                hasTutorialGuide = false
            },
            new LevelData
            {
                levelIndex = 5,
                title = "5. Cầu Rồng Đà Nẵng",
                subtitle = "Ba xe điện chạy vòng, Thầy Thuốc nhanh nhất",
                matchDuration = 90f,
                childCount = 5,
                hunterSpeedMult = 1.35f,
                qteReactionWindow = 0.85f,
                hasTutorialGuide = false
            }
        };

        // Giữ qua các lần nạp scene để scene của màn mới biết mình đang chơi màn nào.
        private static int selectedLevel = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            selectedLevel = 1;
        }

        public int CurrentSelectedLevel
        {
            get => selectedLevel;
            private set => selectedLevel = value;
        }

        public int HighestUnlockedLevel
        {
            get => PlayerPrefs.GetInt(PrefUnlockedKey, 5); // Mặc định mở cả 5 màn để trải nghiệm & kiểm thử Vertical Slice
            private set
            {
                PlayerPrefs.SetInt(PrefUnlockedKey, Mathf.Clamp(value, 1, 5));
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Đồng bộ cấp độ theo ArenaDefinition trong scene hiện tại
            var arena = UnityEngine.Object.FindAnyObjectByType<ArenaDefinition>();
            if (arena != null && arena.levelIndex >= 1 && arena.levelIndex <= 5)
            {
                selectedLevel = arena.levelIndex;
            }
            else
            {
                selectedLevel = PlayerPrefs.GetInt("RongRan_SelectedLevel", 1);
            }
        }

        public LevelData GetLevelData(int levelIndex)
        {
            int idx = Mathf.Clamp(levelIndex - 1, 0, levels.Length - 1);
            return levels[idx];
        }

        public LevelData CurrentLevelData => GetLevelData(CurrentSelectedLevel);

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= HighestUnlockedLevel;
        }

        public void SelectLevel(int levelIndex)
        {
            int targetLevel = Mathf.Clamp(levelIndex, 1, 5);
            CurrentSelectedLevel = targetLevel;
            PlayerPrefs.SetInt("RongRan_SelectedLevel", targetLevel);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] Đã chọn Màn {targetLevel}: {CurrentLevelData.title}");

            // Nếu đang ở MainMenu, chuyển ngay scene 3D để cập nhật toàn bộ bối cảnh & ánh sáng trước mắt người chơi
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                string targetScene = "Man" + targetLevel;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != targetScene)
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
                }
            }
        }

        public void MarkCurrentLevelCompleted()
        {
            if (CurrentSelectedLevel >= HighestUnlockedLevel && HighestUnlockedLevel < 5)
            {
                HighestUnlockedLevel = CurrentSelectedLevel + 1;
                Debug.Log($"[LevelManager] CHÚC MỪNG! Đã mở khóa Màn {HighestUnlockedLevel}!");
            }
        }

        public void ResetProgress()
        {
            HighestUnlockedLevel = 1;
            CurrentSelectedLevel = 1;
        }
    }
}
