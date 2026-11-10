using UnityEngine;

namespace RongRanChamCham
{
    public class GameplayHUD : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle heartStyle;
        private GUIStyle bannerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle duelButtonStyle;
        private bool stylesInitialized = false;

        private string duelMessage = "";
        private string duelResultMsg = "";
        private float resultMsgTimer = 0f;

        private void Start()
        {
            if (ChamChamDuelManager.Instance != null)
            {
                ChamChamDuelManager.Instance.OnDuelChantStep += msg => duelMessage = msg;
                ChamChamDuelManager.Instance.OnRoundResult += (won, msg) =>
                {
                    duelResultMsg = msg;
                    resultMsgTimer = 3.0f;
                };
            }
        }

        private void Update()
        {
            if (resultMsgTimer > 0f)
            {
                resultMsgTimer -= Time.deltaTime;
            }
        }

        private int lastScreenH = -1;

        private void InitStyles()
        {
            if (stylesInitialized && lastScreenH == Screen.height) return;
            lastScreenH = Screen.height;

            int h = Screen.height;
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.048f), 22, 54),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            headerStyle.normal.textColor = Color.white;

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.034f), 16, 38),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            subHeaderStyle.normal.textColor = Color.yellow;

            heartStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.050f), 24, 56),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            heartStyle.normal.textColor = new Color(1.0f, 0.35f, 0.45f);

            bannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.044f), 20, 50),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bannerStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.038f), 18, 44),
                fontStyle = FontStyle.Bold
            };

            duelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.042f), 20, 48),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            duelButtonStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Top Center: 60s Match Timer
            if (GameManager.Instance != null)
            {
                float time = GameManager.Instance.RemainingTime;
                int mins = Mathf.FloorToInt(time / 60f);
                int secs = Mathf.FloorToInt(time % 60f);
                string timeText = $"⏱️ Thời gian: {mins:00}:{secs:00}";

                GUI.color = Color.black;
                GUI.Label(new Rect((Screen.width / 2f) - 148, 17, 300, 40), timeText, headerStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect((Screen.width / 2f) - 150, 15, 300, 40), timeText, headerStyle);
            }

            // 2. Top Left: 3 Hearts & Remaining Children
            if (GameManager.Instance != null)
            {
                float hearts = GameManager.Instance.CurrentHearts;
                int children = GameManager.Instance.RemainingChildren;

                string heartDisplay = $"❤️ Tim: {hearts:0.0} / 3.0";
                GUI.color = Color.black;
                GUI.Label(new Rect(26, 16, 320, 35), heartDisplay, heartStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(25, 15, 320, 35), heartDisplay, heartStyle);

                string childDisplay = $"👶 Đàn con: {children} bé còn sống";
                GUI.color = Color.black;
                GUI.Label(new Rect(26, 46, 320, 35), childDisplay, subHeaderStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(25, 45, 320, 35), childDisplay, subHeaderStyle);
            }

            // 3. Top Right: Active Food Buffs for Both Sides
            DrawActiveBuffs();

            // 4. Duel Mode Overlay & QTE Buttons
            if (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive)
            {
                DrawDuelOverlay();
            }

            // 5. Result Banner
            if (resultMsgTimer > 0f && !string.IsNullOrEmpty(duelResultMsg))
            {
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                GUI.Box(new Rect((Screen.width / 2f) - 260, 90, 520, 60), duelResultMsg, bannerStyle);
            }

            // 6. Victory & GameOver Modals
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameState.Victory)
                {
                    DrawEndGameModal("🎉 CHIẾN THẮNG!", "Bạn đã bảo vệ thành công đàn con suốt 60 giây!", Color.green);
                }
                else if (GameManager.Instance.CurrentState == GameState.GameOver)
                {
                    DrawEndGameModal("💀 THUA CUỘC!", "Thầy Thuốc đã bắt hết toàn bộ đàn con!", Color.red);
                }
            }
        }

        private void DrawActiveBuffs()
        {
            if (BuffSystem.Instance == null) return;

            float rightX = Screen.width - 250f;
            float startY = 15f;

            // Snake Buffs
            if (BuffSystem.Instance.SnakeHasShield)
            {
                GUI.Label(new Rect(rightX, startY, 240, 25), "🛡️ Rắn: Khiên Bánh Mì (1 lần)", subHeaderStyle);
                startY += 25f;
            }
            if (BuffSystem.Instance.IsSnakeSpeedActive)
            {
                GUI.Label(new Rect(rightX, startY, 240, 25), $"⚡ Rắn: Tốc độ Phở ({BuffSystem.Instance.SnakeSpeedTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }
            if (BuffSystem.Instance.IsSnakeGiantActive)
            {
                GUI.Label(new Rect(rightX, startY, 240, 25), $"⭐ Rắn: Cơm cuộn +30% ({BuffSystem.Instance.SnakeGiantTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }
            if (BuffSystem.Instance.IsSnakeCloneActive)
            {
                GUI.Label(new Rect(rightX, startY, 240, 25), $"👥 Rắn: Phân thân ({BuffSystem.Instance.SnakeCloneTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }

            // Hunter Buffs
            if (BuffSystem.Instance.HunterPierceShield)
            {
                GUI.color = Color.magenta;
                GUI.Label(new Rect(rightX, startY, 240, 25), $"🗡️ Thầy Thuốc: Vồ Xuyên Khiên ({BuffSystem.Instance.HunterPierceTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }
            if (BuffSystem.Instance.IsHunterSpeedActive)
            {
                GUI.color = Color.magenta;
                GUI.Label(new Rect(rightX, startY, 240, 25), $"⚡ Thầy Thuốc: Tốc độ Phở ({BuffSystem.Instance.HunterSpeedTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }
            if (BuffSystem.Instance.IsHunterGiantActive)
            {
                GUI.color = Color.magenta;
                GUI.Label(new Rect(rightX, startY, 240, 25), $"⭐ Thầy Thuốc: Sải tay +30% ({BuffSystem.Instance.HunterGiantTimer:0.0}s)", subHeaderStyle);
                startY += 25f;
            }
            GUI.color = Color.white;
        }

        private void DrawDuelOverlay()
        {
            float boxWidth = Mathf.Min(Screen.width * 0.9f, 640f);
            float boxHeight = 260f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = Screen.height * 0.40f;

            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", bannerStyle);

            GUI.color = Color.yellow;
            GUI.Label(new Rect(boxX, boxY + 15, boxWidth, 40), duelMessage, headerStyle);

            GUI.color = Color.white;
            float remTime = ChamChamDuelManager.Instance.ReactionTimeRemaining;
            if (remTime > 0f)
            {
                GUI.Label(new Rect(boxX, boxY + 55, boxWidth, 30), $"QUAY MẶT TRÁNH TAY THẦY THUỐC! ({remTime:0.0}s)", subHeaderStyle);
            }
            else
            {
                GUI.Label(new Rect(boxX, boxY + 55, boxWidth, 30), "ĐỐI ĐẦU 1 HIỆP QUYẾT ĐỊNH!", subHeaderStyle);
            }

            // 3 Touch Choice Buttons
            float btnW = (boxWidth - 50f) / 3f;
            float btnH = 75f;
            float btnY = boxY + 105f;

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUI.Button(new Rect(boxX + 15, btnY, btnW, btnH), "◄ TRÁI\n[A]", duelButtonStyle))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Left);
            }

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUI.Button(new Rect(boxX + 25 + btnW, btnY, btnW, btnH), "▲ THẲNG\n[W]", duelButtonStyle))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Straight);
            }

            GUI.backgroundColor = new Color(1.0f, 0.5f, 0.2f);
            if (GUI.Button(new Rect(boxX + 35 + btnW * 2, btnY, btnW, btnH), "PHẢI ►\n[D]", duelButtonStyle))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Right);
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawEndGameModal(string title, string desc, Color titleColor)
        {
            float modalW = Mathf.Min(Screen.width * 0.85f, 500f);
            float modalH = 240f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.96f);
            GUI.Box(new Rect(modalX, modalY, modalW, modalH), "", bannerStyle);

            GUI.color = titleColor;
            GUI.Label(new Rect(modalX, modalY + 25, modalW, 45), title, headerStyle);

            GUI.color = Color.white;
            GUI.Label(new Rect(modalX + 20, modalY + 80, modalW - 40, 60), desc, subHeaderStyle);

            if (GUI.Button(new Rect(modalX + (modalW - 200f) * 0.5f, modalY + 155, 200, 55), "Chơi Lại (Restart)", buttonStyle))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
    }
}
