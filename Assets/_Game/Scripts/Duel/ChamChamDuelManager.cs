using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        private float reactionTimeRemaining = 0f;

        public event Action<string> OnDuelChantStep; // "CHĂM... CHĂM... CHĂM!"
        public event Action<bool, string> OnRoundResult; // (won, message)
        public event Action<bool> OnDuelFinished; // (conUtSurvived)

        public bool IsDuelActive => isDuelActive;
        public float ReactionTimeRemaining => reactionTimeRemaining;

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

            if (duelArenaStage != null) duelArenaStage.SetActive(true);
            StartCoroutine(DuelRoutine());
        }

        public void ChooseDirection(DuelDirection dir)
        {
            if (!isDuelActive || hasUserChosen) return;
            userClickedChoice = dir;
            hasUserChosen = true;
        }

        private IEnumerator DuelRoutine()
        {
            // 1. Chant sequence
            OnDuelChantStep?.Invoke("RỒNG RẮN LÊN MÂY...");
            yield return new WaitForSeconds(1.0f);

            OnDuelChantStep?.Invoke("CHĂM... CHĂM... CHĂM!");
            yield return new WaitForSeconds(0.4f);

            // 2. Wait for player reaction (1.2s reaction window)
            DuelDirection playerChoice = DuelDirection.Straight;
            reactionTimeRemaining = 1.2f;
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

                // Mobile Touch Swipe Detection (Left / Right / Up)
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
                            else if (delta.y > 0)
                            {
                                playerChoice = DuelDirection.Straight;
                            }
                            hasUserChosen = true;
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
                        playerChoice = DuelDirection.Left;
                        hasUserChosen = true;
                        break;
                    }
                    if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    {
                        playerChoice = DuelDirection.Right;
                        hasUserChosen = true;
                        break;
                    }
                    if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                    {
                        playerChoice = DuelDirection.Straight;
                        hasUserChosen = true;
                        break;
                    }
                }
#else
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    playerChoice = DuelDirection.Left;
                    hasUserChosen = true;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    playerChoice = DuelDirection.Right;
                    hasUserChosen = true;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    playerChoice = DuelDirection.Straight;
                    hasUserChosen = true;
                    break;
                }
#endif
                yield return null;
            }

            // 3. Hunter chooses random direction
            DuelDirection hunterChoice = (DuelDirection)UnityEngine.Random.Range(0, 3);

            // 4. Check outcome (1 Decisive Round):
            // Rule: If Con Út looks DIFFERENT direction than Hunter's hand -> WON (Safe, lose 0.5 heart)
            // If Con Út looks SAME direction -> LOST (Con is eliminated / OUT)
            bool won = (playerChoice != hunterChoice) && hasUserChosen;

            if (won)
            {
                OnRoundResult?.Invoke(true, $"CHIẾN THẮNG! Thầy Thuốc chỉ ({hunterChoice}), Con Út quay ({playerChoice})! (Mất 0.5 tim)");
            }
            else
            {
                string reason = hasUserChosen ? $"Thầy Thuốc chém trúng hướng ({hunterChoice})" : "Quá thời gian phản xạ";
                OnRoundResult?.Invoke(false, $"THUA CUỘC! {reason}! Con Út bị bắt!");
            }

            yield return new WaitForSeconds(2.0f);

            isDuelActive = false;
            if (duelArenaStage != null) duelArenaStage.SetActive(false);

            OnDuelFinished?.Invoke(won);
        }
    }
}
