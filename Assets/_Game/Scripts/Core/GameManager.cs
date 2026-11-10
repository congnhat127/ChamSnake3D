using UnityEngine;

namespace RongRanChamCham
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private MotherController mother;
        [SerializeField] private SnakeChainIK snakeChain;
        [SerializeField] private HunterAI hunter;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private ChamChamDuelManager duelManager;
        [SerializeField] private ItemSpawner itemSpawner;

        [Header("Match State")]
        [SerializeField] private GameState currentState = GameState.Ready;
        [SerializeField] private float matchTimer = GameConstants.ChaseRoundDuration;
        [SerializeField] private float currentHearts = GameConstants.MaxHearts;

        public GameState CurrentState => currentState;
        public float RemainingTime => matchTimer;
        public float CurrentHearts => currentHearts;
        public int RemainingChildren => snakeChain != null ? snakeChain.RemainingChildrenCount : 0;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public MotherController Mother => mother;
        public HunterAI Hunter => hunter;
        public SnakeChainIK SnakeChain => snakeChain;
        public CameraFollow CamFollow => cameraFollow;

        private void Start()
        {
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            currentState = GameState.MainMenu;
            if (cameraFollow != null)
            {
                cameraFollow.SetShowcaseMode(false);
                cameraFollow.SetDuelMode(false);
            }
        }

        public void OpenShowcase()
        {
            currentState = GameState.Showcase;
        }

        public void StartMatch()
        {
            currentHearts = GameConstants.MaxHearts;
            matchTimer = GameConstants.ChaseRoundDuration;
            currentState = GameState.Chasing;

            if (cameraFollow != null)
            {
                cameraFollow.SetShowcaseMode(false);
                cameraFollow.SetDuelMode(false);
            }

            if (hunter != null)
            {
                hunter.OnCaughtConUt -= HandleConUtCaught;
                hunter.OnCaughtConUt += HandleConUtCaught;
            }

            if (duelManager != null)
            {
                duelManager.OnDuelFinished -= HandleDuelFinished;
                duelManager.OnDuelFinished += HandleDuelFinished;
            }

            if (cameraFollow != null && mother != null && snakeChain != null && hunter != null)
            {
                cameraFollow.SetTargets(mother.transform, snakeChain.ConUt != null ? snakeChain.ConUt.transform : null, hunter.transform);
            }
        }

        private void Update()
        {
            if (currentState == GameState.Chasing)
            {
                matchTimer -= Time.deltaTime;
                if (matchTimer <= 0f)
                {
                    matchTimer = 0f;
                    // Win condition: time expired with at least 1 child remaining!
                    if (RemainingChildren >= 1)
                    {
                        currentState = GameState.Victory;
                        Debug.Log($"[GameManager] CHIẾN THẮNG! Hết 60s và bạn vẫn giữ được {RemainingChildren} bé con!");
                    }
                    else
                    {
                        currentState = GameState.GameOver;
                        Debug.Log("[GameManager] THUA CUỘC! Hết giờ nhưng không còn bé con nào sống sót!");
                    }
                }
            }
        }

        private void HandleConUtCaught()
        {
            if (currentState != GameState.Chasing) return;

            currentState = GameState.Duel;
            Debug.Log("[GameManager] THẦY THUỐC ĐÃ VỒ TRÚNG ĐUÔI! BẮT ĐẦU ĐỐI ĐẦU CHĂM CHĂM CHĂM (1 HIỆP QUYẾT ĐỊNH)!");

            if (cameraFollow != null)
            {
                cameraFollow.SetDuelMode(true);
            }

            if (duelManager != null)
            {
                duelManager.StartDuel();
            }
        }

        private void HandleDuelFinished(bool conUtWon)
        {
            if (cameraFollow != null) cameraFollow.SetDuelMode(false);

            if (conUtWon)
            {
                // Won Cham Cham Cham: Keep child, lose 0.5 heart!
                currentHearts -= GameConstants.HeartLossOnWin;
                Debug.Log($"[GameManager] Thắng Chăm Chăm Chăm! Giữ được bé con, tổng tim còn: {currentHearts:F1}/3.0");

                if (currentHearts <= 0f)
                {
                    // Hearts depleted -> Child out
                    EliminateTailChild();
                }
                else
                {
                    // Repel Hunter
                    if (hunter != null && mother != null)
                    {
                        Vector3 pushDir = (hunter.transform.position - mother.transform.position).normalized;
                        hunter.GetShovedByMother(pushDir, 9.0f);
                    }
                    currentState = GameState.Chasing;
                }
            }
            else
            {
                // Lost Cham Cham Cham: That child is OUT!
                Debug.Log("[GameManager] Thua Chăm Chăm Chăm! Bé Út bị loại khỏi cuộc chơi!");
                EliminateTailChild();
            }
        }

        private void EliminateTailChild()
        {
            if (snakeChain != null)
            {
                snakeChain.RemoveTailChild();
            }

            int count = RemainingChildren;
            Debug.Log($"[GameManager] Đàn con hiện còn: {count} bé.");

            // Lose condition: All children captured, only Mother left!
            if (count <= 0)
            {
                currentState = GameState.GameOver;
                Debug.Log("[GameManager] THUA CUỘC! Thầy Thuốc đã bắt hết đàn con, chỉ còn lại rắn đầu đàn!");
            }
            else
            {
                // Hunter gets brief delay before next chase
                if (hunter != null && mother != null)
                {
                    Vector3 pushDir = (hunter.transform.position - mother.transform.position).normalized;
                    hunter.GetShovedByMother(pushDir, 5.0f);
                }
                currentState = GameState.Chasing;
            }
        }
    }
}
