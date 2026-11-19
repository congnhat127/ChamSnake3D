using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TouchPhase = UnityEngine.TouchPhase;

namespace RongRanChamCham
{
    public class ChamChamDuelManager : MonoBehaviour
    {
        public static ChamChamDuelManager Instance { get; private set; }

        [Header("Duel Visuals")]
        [SerializeField] private GameObject duelArenaStage;

        [Header("State")]
        [SerializeField] private bool isDuelActive = false;
        private bool hasUserChosen = false;
        private DuelDirection userClickedChoice = DuelDirection.Straight;
        private DuelDirection currentHunterChoice = DuelDirection.Straight;
        private float reactionTimeRemaining = 0f;
        private int countdownNumber = 0;

        private int currentRound = 1;
        private int playerScore = 0;
        private int hunterScore = 0;

        public event Action<string> OnDuelChantStep; // "CHĂM... CHĂM... CHĂM!"
        public event Action<bool, string> OnRoundResult; // (won, message)
        public event Action<bool> OnDuelFinished; // (conUtSurvived)

        public bool IsDuelActive => isDuelActive;
        public DuelDirection CurrentHunterChoice => currentHunterChoice;
        public float ReactionTimeRemaining => reactionTimeRemaining;
        public int CountdownNumber => countdownNumber;
        public int CurrentRound => currentRound;
        public int PlayerScore => playerScore;
        public int HunterScore => hunterScore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartDuel()
        {
            isDuelActive = true;
            hasUserChosen = false;
            currentRound = 1;
            playerScore = 0;
            hunterScore = 0;

            StageActorsForDuel();

            if (duelArenaStage != null) duelArenaStage.SetActive(true);
            StartCoroutine(DuelRoutine());
        }

        private void StageActorsForDuel()
        {
            var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
            var snakeChain = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
            var mother = UnityEngine.Object.FindAnyObjectByType<MotherController>();

            if (hunter == null || snakeChain == null || snakeChain.ConUt == null) return;

            // Tâm điểm sân khấu đối kháng 3D: trung tâm sân đấu
            Vector3 center = Vector3.zero;
            if (ArenaBoundary.ActiveArena != null)
            {
                float mx = (ArenaBoundary.ActiveArena.motherSpawn.x + ArenaBoundary.ActiveArena.hunterSpawn.x) * 0.5f;
                float mz = (ArenaBoundary.ActiveArena.motherSpawn.y + ArenaBoundary.ActiveArena.hunterSpawn.y) * 0.5f;
                center = new Vector3(mx, 0f, mz);
            }
            center.y = 0f;

            // Thầy Thuốc đứng bên phải (x = +1.3m), quay mặt sang trái hướng về bé Út
            Vector3 hunterDuelPos = center + new Vector3(1.3f, 0f, 0f);
            hunter.transform.position = hunterDuelPos;
            hunter.transform.rotation = Quaternion.LookRotation(Vector3.left);

            // Bé Út đứng bên trái (x = -1.3m), quay mặt sang phải hướng về Thầy Thuốc
            Vector3 utDuelPos = center + new Vector3(-1.3f, 0f, 0f);
            snakeChain.ConUt.transform.position = utDuelPos;
            snakeChain.ConUt.transform.rotation = Quaternion.LookRotation(Vector3.right);

            // Mẹ và các con khác đứng lùi lại phía sau (z = +2.0m) làm hậu phương cổ vũ
            if (mother != null)
            {
                mother.transform.position = center + new Vector3(-3.2f, 0f, 1.8f);
                mother.transform.rotation = Quaternion.LookRotation(new Vector3(1.0f, 0f, -0.35f));
            }

            var children = snakeChain.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null || children[i] == snakeChain.ConUt) continue;
                children[i].transform.position = center + new Vector3(-4.0f - i * 0.85f, 0f, 2.0f);
                children[i].transform.rotation = Quaternion.LookRotation(new Vector3(1.0f, 0f, -0.35f));
            }
        }

        // Dùng khi scene không có LevelManager; bằng cửa sổ phản xạ của Màn 2.
        private const float DefaultReactionWindow = 1.35f;

        private static string DirectionName(DuelDirection dir)
        {
            switch (dir)
            {
                case DuelDirection.Left: return "Trái";
                case DuelDirection.Right: return "Phải";
                case DuelDirection.Down: return "Cúi";
                default: return "Lên";
            }
        }

        public void CancelDuel()
        {
            StopAllCoroutines();
            isDuelActive = false;
            countdownNumber = 0;
            if (duelArenaStage != null) duelArenaStage.SetActive(false);
        }

        public void ChooseDirection(DuelDirection dir)
        {
            // Chỉ chấp nhận bấm nút khi đang trong cửa sổ phản xạ (sau khi đếm 3-2-1 xong)
            if (!isDuelActive || countdownNumber > 0 || reactionTimeRemaining <= 0f || hasUserChosen) return;
            userClickedChoice = dir;
            hasUserChosen = true;

            // Phản xạ tức thì: Bé Út né và quay đầu ngay khi nhấn nút
            var snakeChain = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
            if (snakeChain != null && snakeChain.ConUt != null)
            {
                snakeChain.ConUt.SetDuelDodge(dir);
                AudioManager.Instance?.PlaySFX(SoundType.Dodge);
            }
        }

        private IEnumerator DuelRoutine()
        {
            // Best of 3 System: Tối đa 3 hiệp, ai thắng 2 trước là giành phần thắng
            while (playerScore < 2 && hunterScore < 2 && currentRound <= 3)
            {
                hasUserChosen = false;
                userClickedChoice = DuelDirection.Straight;

                // Reset thế đứng của cả Bé Út và Thầy Thuốc trước mỗi hiệp
                var snakeChain = UnityEngine.Object.FindAnyObjectByType<SnakeChainIK>();
                var hunter = UnityEngine.Object.FindAnyObjectByType<HunterAI>();
                if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.ResetDuelStance();
                if (hunter != null) hunter.ResetDuelStance();

                // ĐẾM LÙI 3 - 2 - 1 ĐỀU ĐẶN Ở MỌI HIỆP ĐẤU (Nhún nhảy & giậm chân theo nhịp Chăm)
                countdownNumber = 3;
                OnDuelChantStep?.Invoke("3");
                AudioManager.Instance?.PlaySFX(SoundType.ChamBeat);
                if (hunter != null) hunter.PlayDuelChantStomp(3);
                if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.PlayDuelAnticipation(3);
                yield return new WaitForSeconds(0.68f);

                countdownNumber = 2;
                OnDuelChantStep?.Invoke("2");
                AudioManager.Instance?.PlaySFX(SoundType.ChamBeat);
                if (hunter != null) hunter.PlayDuelChantStomp(2);
                if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.PlayDuelAnticipation(2);
                yield return new WaitForSeconds(0.68f);

                countdownNumber = 1;
                OnDuelChantStep?.Invoke("1");
                AudioManager.Instance?.PlaySFX(SoundType.ChamBeat);
                if (hunter != null) hunter.PlayDuelChantStomp(1);
                if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.PlayDuelAnticipation(1);
                yield return new WaitForSeconds(0.68f);

                countdownNumber = 0;
                OnDuelChantStep?.Invoke("CHĂM! NÉ MAU!");
                AudioManager.Instance?.PlaySFX(SoundType.ChamBeat);

                // Thầy Thuốc chọn hướng và ra đòn chỉ hướng rõ ràng
                DuelDirection hunterChoice = (DuelDirection)UnityEngine.Random.Range(0, 4);
                currentHunterChoice = hunterChoice;
                if (hunter != null) hunter.TriggerDuelPoint(hunterChoice);

                // Cửa sổ thời gian phản xạ né đòn (tối thiểu 1.35 giây)
                hasUserChosen = false;
                userClickedChoice = (DuelDirection)(-1);
                DuelDirection playerChoice = (DuelDirection)(-1);
                reactionTimeRemaining = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelData.qteReactionWindow : DefaultReactionWindow;
                if (reactionTimeRemaining < 1.35f) reactionTimeRemaining = 1.35f;

                Vector2 touchStartPos = Vector2.zero;
                bool isTrackingSwipe = false;

                while (reactionTimeRemaining > 0f)
                {
                    reactionTimeRemaining -= Time.deltaTime;

                    if (hasUserChosen)
                    {
                        playerChoice = userClickedChoice;
                        break;
                    }

                    // Mobile Touch Swipe Detection (Left / Right / Up / Down)
                    if (Input.touchCount > 0)
                    {
                        Touch t = Input.GetTouch(0);
                        if (t.phase == TouchPhase.Began)
                        {
                            touchStartPos = t.position;
                            isTrackingSwipe = true;
                        }
                        else if (isTrackingSwipe && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Ended))
                        {
                            Vector2 delta = t.position - touchStartPos;
                            if (delta.magnitude > 45f)
                            {
                                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                                {
                                    playerChoice = delta.x < 0 ? DuelDirection.Left : DuelDirection.Right;
                                }
                                else
                                {
                                    playerChoice = delta.y > 0 ? DuelDirection.Straight : DuelDirection.Down;
                                }
                                ChooseDirection(playerChoice);
                                break;
                            }
                        }
                    }

#if ENABLE_INPUT_SYSTEM
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                        {
                            ChooseDirection(DuelDirection.Left);
                            playerChoice = DuelDirection.Left;
                            break;
                        }
                        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                        {
                            ChooseDirection(DuelDirection.Right);
                            playerChoice = DuelDirection.Right;
                            break;
                        }
                        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                        {
                            ChooseDirection(DuelDirection.Straight);
                            playerChoice = DuelDirection.Straight;
                            break;
                        }
                        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                        {
                            ChooseDirection(DuelDirection.Down);
                            playerChoice = DuelDirection.Down;
                            break;
                        }
                    }
#else
                    if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    {
                        ChooseDirection(DuelDirection.Left);
                        playerChoice = DuelDirection.Left;
                        break;
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    {
                        ChooseDirection(DuelDirection.Right);
                        playerChoice = DuelDirection.Right;
                        break;
                    }
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    {
                        ChooseDirection(DuelDirection.Straight);
                        playerChoice = DuelDirection.Straight;
                        break;
                    }
                    if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                    {
                        ChooseDirection(DuelDirection.Down);
                        playerChoice = DuelDirection.Down;
                        break;
                    }
#endif
                    yield return null;
                }

                // Giữ nguyên tư thế chỉ hướng và né đòn 0.75s để người chơi thấy rõ mồn một chuyển động
                yield return new WaitForSeconds(0.75f);

                // Kiểm tra kết quả hiệp: BẮT BUỘC người chơi phải chủ động bấm né và hướng né KHÁC hướng Thầy Thuốc
                bool roundWon = hasUserChosen && (playerChoice != hunterChoice);

                if (roundWon)
                {
                    playerScore++;
                    AudioManager.Instance?.PlaySFX(SoundType.Pickup);
                    OnRoundResult?.Invoke(true, $"HIỆP {currentRound}: NÉ ĐƯỢC!  {playerScore} – {hunterScore}");
                    if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.PlayDuelVictoryHop();
                    if (hunter != null) hunter.PlayDuelFrustration();
                }
                else
                {
                    hunterScore++;
                    AudioManager.Instance?.PlaySFX(SoundType.Defeat);
                    string failReason = hasUserChosen ? $"Trùng hướng {DirectionName(hunterChoice)}" : "Chưa bấm né (Quá giờ)";
                    OnRoundResult?.Invoke(false, $"HIỆP {currentRound}: {failReason}!  {playerScore} – {hunterScore}");
                    if (snakeChain != null && snakeChain.ConUt != null) snakeChain.ConUt.PlayDuelHurtStumble();
                    if (hunter != null) hunter.PlayDuelTriumphSwipe();
                }

                yield return new WaitForSeconds(1.3f);
                currentRound++;
            }

            // 5. Final Best of 3 Outcome (Win 2/3 rounds)
            bool overallWon = (playerScore >= 2);

            if (overallWon)
            {
                OnRoundResult?.Invoke(true, $"BÉ ÚT THOÁT!  {playerScore} – {hunterScore}");
            }
            else
            {
                OnRoundResult?.Invoke(false, $"BÉ ÚT BỊ BẮT!  {playerScore} – {hunterScore}");
            }

            yield return new WaitForSeconds(1.8f);

            isDuelActive = false;
            if (duelArenaStage != null) duelArenaStage.SetActive(false);

            OnDuelFinished?.Invoke(overallWon);
        }
    }
}
