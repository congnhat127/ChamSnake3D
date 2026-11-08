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
        [SerializeField] private int conUtHealth = GameConstants.ConUtMaxHealth;

        public GameState CurrentState => currentState;
        public float RemainingTime => matchTimer;
        public int ConUtHealth => conUtHealth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartMatch();
        }

        public void StartMatch()
        {
            conUtHealth = GameConstants.ConUtMaxHealth;
            matchTimer = GameConstants.ChaseRoundDuration;
            currentState = GameState.Chasing;

            if (hunter != null)
            {
                hunter.OnCaughtConUt += HandleConUtCaught;
            }

            if (duelManager != null)
            {
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
                if (matchTimer <= 0)
                {
                    matchTimer = 0;
                    currentState = GameState.Victory;
                    Debug.Log("[GameManager] CHIẾN THẮNG! Gia đình Rồng Rắn đã bảo vệ thành công Con Út!");
                }
            }
        }

        private void HandleConUtCaught()
        {
            if (currentState != GameState.Chasing) return;

            currentState = GameState.Duel;
            Debug.Log("[GameManager] CHUYỂN SANG MÀN ĐỐI ĐẦU CHĂM CHĂM CHĂM!");

            if (cameraFollow != null)
            {
                cameraFollow.SetDuelMode(true);
            }

            if (duelManager != null)
            {
                duelManager.StartDuel(conUtHealth);
            }
        }

        private void HandleDuelFinished(bool conUtSurvived)
        {
            if (conUtSurvived)
            {
                currentState = GameState.Chasing;
                if (cameraFollow != null) cameraFollow.SetDuelMode(false);
                Debug.Log("[GameManager] Con Út thoát hiểm ngoạn mục! Tiếp tục cuộc rượt đuổi!");
            }
            else
            {
                currentState = GameState.GameOver;
                Debug.Log("[GameManager] THUA CUỘC! Thợ Săn đã bắt được Con Út!");
            }
        }
    }
}
