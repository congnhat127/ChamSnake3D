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
        [SerializeField] private Transform hunterDuelPos;
        [SerializeField] private Transform conUtDuelPos;
        [SerializeField] private Transform hunterHand;

        [Header("State")]
        [SerializeField] private bool isDuelActive = false;
        [SerializeField] private int currentRound = 1;
        [SerializeField] private int maxRounds = 3;
        [SerializeField] private int conUtHealth = GameConstants.ConUtMaxHealth;

        public event Action<string> OnDuelChantStep; // "Rồng...", "Rắn...", "Lên Mây...", "CHĂM!"
        public event Action<bool, string> OnRoundResult; // (won, message)
        public event Action<bool> OnDuelFinished; // (playerSurvived)

        public bool IsDuelActive => isDuelActive;
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

        public void StartDuel(int startingHealth)
        {
            conUtHealth = startingHealth;
            isDuelActive = true;
            currentRound = 1;

            if (duelArenaStage != null) duelArenaStage.SetActive(true);
            StartCoroutine(DuelRoutine());
        }

        private IEnumerator DuelRoutine()
        {
            while (currentRound <= maxRounds && conUtHealth > 0)
            {
                // Chant sequence
                OnDuelChantStep?.Invoke("RỒNG RẮN LÊN MÂY...");
                yield return new WaitForSeconds(1.2f);

                OnDuelChantStep?.Invoke("THẤY CÂY THỤC BÌNH...");
                yield return new WaitForSeconds(1.0f);

                OnDuelChantStep?.Invoke("CHĂM... CHĂM... CHĂM!");
                yield return new WaitForSeconds(0.6f);

                // Wait for player reaction (0.8s reaction window)
                DuelDirection playerChoice = DuelDirection.Straight;
                float timer = 0.8f;

                while (timer > 0)
                {
                    timer -= Time.deltaTime;

#if ENABLE_INPUT_SYSTEM
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                        {
                            playerChoice = DuelDirection.Left;
                            break;
                        }
                        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                        {
                            playerChoice = DuelDirection.Right;
                            break;
                        }
                        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                        {
                            playerChoice = DuelDirection.Straight;
                            break;
                        }
                    }
#else
                    if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    {
                        playerChoice = DuelDirection.Left;
                        break;
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    {
                        playerChoice = DuelDirection.Right;
                        break;
                    }
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    {
                        playerChoice = DuelDirection.Straight;
                        break;
                    }
#endif
                    yield return null;
                }

                // Hunter chooses random direction
                DuelDirection hunterChoice = (DuelDirection)UnityEngine.Random.Range(0, 3);

                // Check outcome
                // Rule: If Con Út looks SAME direction as Hunter's hand chop -> HIT (Lose health)
                // If Con Út looks DIFFERENT direction -> DODGED! (Safe)
                if (playerChoice == hunterChoice)
                {
                    conUtHealth--;
                    OnRoundResult?.Invoke(false, $"BỊ BẮT! Thợ săn chém trúng hướng ({hunterChoice})! Mất 1 máu!");
                }
                else
                {
                    OnRoundResult?.Invoke(true, $"NÉ THÀNH CÔNG! Thợ săn chém ({hunterChoice}), Con Út quay ({playerChoice})!");
                }

                yield return new WaitForSeconds(2.0f);
                currentRound++;
            }

            isDuelActive = false;
            if (duelArenaStage != null) duelArenaStage.SetActive(false);

            bool survived = conUtHealth > 0;
            OnDuelFinished?.Invoke(survived);
        }
    }
}
