using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            if (LevelManager.Instance == null)
            {
                var existingLm = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                if (existingLm == null)
                {
                    gameObject.AddComponent<LevelManager>();
                }
            }
        }

        public MotherController Mother => mother;
        public HunterAI Hunter => hunter;
        public SnakeChainIK SnakeChain => snakeChain;
        public CameraFollow CamFollow => cameraFollow;

        // Scene vừa được nạp lại để bắt đầu trận ngay (chơi lại, hoặc đổi màn), không dừng ở menu.
        private static bool pendingAutoStart;
        // Trận đã bắt đầu trong scene này: đàn con, buff, vật phẩm không còn ở trạng thái ban đầu.
        private bool matchStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pendingAutoStart = false;
        }

        private void Start()
        {
            if (pendingAutoStart)
            {
                pendingAutoStart = false;
                BeginMatch();
            }
            else
            {
                ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Time.timeScale = 1f;
        }

        /// <summary>Màn mà scene này dựng (theo ArenaDefinition); scene không có thì coi là Màn 1.</summary>
        private static int SceneLevel => ArenaBoundary.ActiveArena != null ? ArenaBoundary.ActiveArena.levelIndex : 1;

        private static string SceneNameForLevel(int level)
        {
            return level == SceneLevel ? SceneManager.GetActiveScene().name : "Man" + level;
        }

        public void ShowMainMenu()
        {
            if (matchStarted)
            {
                // Đã chơi dở: nạp lại scene để menu luôn mở trên trạng thái sạch (đủ con, không buff, không vật phẩm cũ).
                Time.timeScale = 1f;
                pendingAutoStart = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

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

        /// <summary>
        /// Bắt đầu trận ở màn đang chọn. Nếu scene này đã chơi dở hoặc là scene của màn khác
        /// thì nạp scene đúng rồi tự vào trận.
        /// </summary>
        public void StartMatch()
        {
            int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentSelectedLevel : SceneLevel;
            if (matchStarted || level != SceneLevel)
            {
                Time.timeScale = 1f;
                pendingAutoStart = true;
                SceneManager.LoadScene(SceneNameForLevel(level));
                return;
            }
            BeginMatch();
        }

        public void TogglePause()
        {
            if (currentState == GameState.Chasing)
            {
                currentState = GameState.Paused;
                Time.timeScale = 0f;
                AudioManager.Instance?.PauseBGM();
            }
            else if (currentState == GameState.Paused)
            {
                currentState = GameState.Chasing;
                Time.timeScale = 1f;
                AudioManager.Instance?.ResumeBGM();
            }
        }

        private void BeginMatch()
        {
            matchStarted = true;
            Time.timeScale = 1f;
            currentHearts = GameConstants.MaxHearts;
            AudioManager.Instance?.PlayGameplayBGM();
            
            if (LevelManager.Instance != null)
            {
                var lvl = LevelManager.Instance.CurrentLevelData;
                matchTimer = lvl.matchDuration;
            }
            else
            {
                matchTimer = GameConstants.ChaseRoundDuration;
            }

            currentState = GameState.Chasing;
            PlaceActorsAtSpawn();
            if (hunter != null)
            {
                hunter.ResetAfterDuel(GameConstants.RoundStartGrabCooldown, GameConstants.RoundStartHunterFreeze);
            }

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
                        AudioManager.Instance?.StopBGM();
                        AudioManager.Instance?.PlaySFX(SoundType.Victory);
                        LevelManager.Instance?.MarkCurrentLevelCompleted();
                        Vector3 spawnPos = mother != null ? mother.transform.position : Vector3.zero;
                        VictoryCelebrationFX.Spawn(spawnPos);
                        Debug.Log($"[GameManager] CHIẾN THẮNG! Hết thời gian và bạn vẫn giữ được {RemainingChildren} bé con!");
                    }
                    else
                    {
                        currentState = GameState.GameOver;
                        AudioManager.Instance?.StopBGM();
                        AudioManager.Instance?.PlaySFX(SoundType.Defeat);
                        Debug.Log("[GameManager] THUA CUỘC! Hết giờ nhưng không còn bé con nào sống sót!");
                    }
                }
            }
        }

        private void HandleConUtCaught()
        {
            if (currentState != GameState.Chasing) return;

            currentState = GameState.Duel;
            Debug.Log("[GameManager] THẦY THUỐC ĐÃ VỒ TRÚNG ĐUÔI! BẮT ĐẦU ĐỐI ĐẦU CHĂM CHĂM CHĂM (3 HIỆP, THẮNG 2)!");

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
                currentHearts -= GameConstants.HeartLossOnWin;
                Debug.Log($"[GameManager] Thắng Chăm Chăm Chăm! Giữ được bé con, tổng tim còn: {currentHearts:F1}/3.0");

                if (currentHearts <= 0f)
                {
                    EliminateTailChild();
                }
                else
                {
                    StartCoroutine(ResetPositionsAndRestartChaseRoutine("THOÁT HIỂM! TÁI LẬP THẾ TRẬN!"));
                }
            }
            else
            {
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

            // Update Hunter's target to the new Con Ut immediately
            if (hunter != null && snakeChain != null)
            {
                hunter.SetTargets(mother != null ? mother.transform : null, snakeChain.ConUt);
            }

            int count = RemainingChildren;
            Debug.Log($"[GameManager] Đàn con hiện còn: {count} bé.");

            // Tim là sức của Con Út hiện tại; bé Út mới vào vị trí với đủ tim.
            currentHearts = GameConstants.MaxHearts;

            // Lose condition: All children captured, only Mother left!
            if (count <= 0)
            {
                currentState = GameState.GameOver;
                AudioManager.Instance?.StopBGM();
                AudioManager.Instance?.PlaySFX(SoundType.Defeat);
                Debug.Log("[GameManager] THUA CUỘC! Thầy Thuốc đã bắt hết đàn con, chỉ còn lại rắn đầu đàn!");
            }
            else
            {
                StartCoroutine(ResetPositionsAndRestartChaseRoutine("MẤT BÉ ÚT! ĐÀN CON TÁI LẬP ĐỘI HÌNH!"));
            }
        }

        private IEnumerator ResetPositionsAndRestartChaseRoutine(string announcement)
        {
            currentState = GameState.RoundOver;

            // 1-3. Đưa Mẹ, đàn con, Thầy Thuốc về vị trí xuất phát của màn
            PlaceActorsAtSpawn();
            if (hunter != null)
            {
                if (snakeChain != null)
                {
                    hunter.SetTargets(mother != null ? mother.transform : null, snakeChain.ConUt);
                }
                hunter.ResetAfterDuel(GameConstants.RoundStartGrabCooldown, GameConstants.RoundStartHunterFreeze);
            }

            // 4. Update Camera targets
            if (cameraFollow != null && mother != null && snakeChain != null && hunter != null)
            {
                cameraFollow.SetTargets(mother.transform, snakeChain.ConUt != null ? snakeChain.ConUt.transform : null, hunter.transform);
            }

            // 5. Grant temporary protective shield to Snake
            if (BuffSystem.Instance != null)
            {
                BuffSystem.Instance.GrantTemporaryShield();
            }

            // 6. Give player 1.8 seconds to orient and prepare before chase resumes
            yield return new WaitForSeconds(GameConstants.RoundStartPlayerPrepTime);

            currentState = GameState.Chasing;
        }

        /// <summary>
        /// Đặt Mẹ, đàn con và Thầy Thuốc vào vị trí xuất phát của màn (lấy từ ArenaDefinition;
        /// scene không có thì dùng vị trí mặc định của Màn 1).
        /// </summary>
        private void PlaceActorsAtSpawn()
        {
            ArenaDefinition arena = ArenaBoundary.ActiveArena;
            Vector2 motherXZ = arena != null ? arena.motherSpawn : DefaultMotherSpawn;
            Vector2 motherFwd = arena != null ? arena.motherSpawnForward : DefaultMotherForward;
            Vector2 hunterXZ = arena != null ? arena.hunterSpawn : DefaultHunterSpawn;

            Vector3 motherPos = ArenaBoundary.ClampToArena(new Vector3(motherXZ.x, 0f, motherXZ.y), ActorBodyRadius);
            Vector3 hunterPos = ArenaBoundary.ClampToArena(new Vector3(hunterXZ.x, 0f, hunterXZ.y), ActorBodyRadius);

            if (mother != null)
            {
                Vector3 fwd = new Vector3(motherFwd.x, 0f, motherFwd.y);
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                TeleportActor(mother.transform, motherPos, Quaternion.LookRotation(fwd.normalized));
            }

            if (snakeChain != null)
            {
                snakeChain.ResetFormationBehindMother();
            }

            if (hunter != null)
            {
                Vector3 toMother = motherPos - hunterPos;
                toMother.y = 0f;
                if (toMother.sqrMagnitude < 0.0001f) toMother = Vector3.back;
                TeleportActor(hunter.transform, hunterPos, Quaternion.LookRotation(toMother.normalized));
            }
        }

        // CharacterController ghi đè transform.position nếu đang bật, nên tắt nó trong lúc dịch chuyển tức thời.
        private static void TeleportActor(Transform actor, Vector3 pos, Quaternion rot)
        {
            var cc = actor.GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;
            if (wasEnabled) cc.enabled = false;
            actor.SetPositionAndRotation(pos, rot);
            if (wasEnabled) cc.enabled = true;
        }

        // Vị trí xuất phát mặc định của Màn 1 (giữ nguyên giá trị trước đây của hàm reset sau khi đấu).
        private static readonly Vector2 DefaultMotherSpawn = new Vector2(0f, -2.5f);
        private static readonly Vector2 DefaultMotherForward = new Vector2(0f, 1f);
        private static readonly Vector2 DefaultHunterSpawn = new Vector2(0f, 6.0f);
        // Bằng CharacterController.radius của Mẹ và Thầy Thuốc.
        private const float ActorBodyRadius = 0.45f;

        public void ForceVictoryForTesting()
        {
            StopAllCoroutines();
            if (duelManager != null) duelManager.CancelDuel();
            if (cameraFollow != null) cameraFollow.SetDuelMode(false);
            matchTimer = 0f;
            currentState = GameState.Victory;
            AudioManager.Instance?.StopBGM();
            AudioManager.Instance?.PlaySFX(SoundType.Victory);
            LevelManager.Instance?.MarkCurrentLevelCompleted();
            Vector3 spawnPos = mother != null ? mother.transform.position : Vector3.zero;
            VictoryCelebrationFX.Spawn(spawnPos);
            Debug.Log("[GameManager] [TEST] Kích hoạt trạng thái VICTORY ăn mừng chiến thắng!");
        }

        public void ForceGameOverForTesting()
        {
            StopAllCoroutines();
            if (duelManager != null) duelManager.CancelDuel();
            if (cameraFollow != null) cameraFollow.SetDuelMode(false);
            matchTimer = 0f;
            currentState = GameState.GameOver;
            AudioManager.Instance?.StopBGM();
            AudioManager.Instance?.PlaySFX(SoundType.Defeat);
            Debug.Log("[GameManager] [TEST] Kích hoạt trạng thái GAME OVER thua cuộc!");
        }
    }
}
