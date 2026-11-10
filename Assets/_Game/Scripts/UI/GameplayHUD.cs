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
        private GUIStyle infoBoxStyle;
        private GUIStyle tabButtonStyle;
        private bool stylesInitialized = false;

        private string duelMessage = "";
        private string duelResultMsg = "";
        private float resultMsgTimer = 0f;

        // Menu & Showcase state
        private bool showRulesModal = false;
        private int showcaseIndex = 0; // 0 = Mother, 1 = Child, 2 = Hunter
        private float showcaseRotationY = 180f;
        private Vector2 lastMousePos;
        private bool isDraggingRotation = false;

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

            // In Showcase mode, handle character rotation & camera focus
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Showcase)
            {
                HandleShowcaseInteraction();
            }
        }

        private void HandleShowcaseInteraction()
        {
            // Touch / Mouse drag to rotate character 360 degrees
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.y < Screen.height * 0.70f && Input.mousePosition.y > Screen.height * 0.20f)
                {
                    isDraggingRotation = true;
                    lastMousePos = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingRotation)
            {
                float deltaX = Input.mousePosition.x - lastMousePos.x;
                showcaseRotationY -= deltaX * 0.5f;
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingRotation = false;
            }

            // Apply rotation and camera framing
            Transform currentTarget = GetCurrentShowcaseTransform();
            if (currentTarget != null)
            {
                currentTarget.rotation = Quaternion.Euler(0f, showcaseRotationY, 0f);

                if (GameManager.Instance.CamFollow != null)
                {
                    GameManager.Instance.CamFollow.SetShowcaseMode(true, currentTarget);
                }
            }
        }

        private Transform GetCurrentShowcaseTransform()
        {
            if (GameManager.Instance == null) return null;

            switch (showcaseIndex)
            {
                case 0:
                    return GameManager.Instance.Mother != null ? GameManager.Instance.Mother.transform : null;
                case 1:
                    if (GameManager.Instance.SnakeChain != null && GameManager.Instance.SnakeChain.ConUt != null)
                        return GameManager.Instance.SnakeChain.ConUt.transform;
                    return null;
                case 2:
                    return GameManager.Instance.Hunter != null ? GameManager.Instance.Hunter.transform : null;
                default:
                    return null;
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
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            duelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.042f), 20, 48),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            duelButtonStyle.normal.textColor = Color.white;

            infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.030f), 14, 28),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            infoBoxStyle.normal.textColor = Color.white;

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(h * 0.034f), 16, 36),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.CurrentState;

            // --- 1. MAIN MENU SCREEN ---
            if (state == GameState.MainMenu)
            {
                DrawMainMenu();
                return;
            }

            // --- 2. CHARACTER SHOWCASE SCREEN ---
            if (state == GameState.Showcase)
            {
                DrawShowcase();
                return;
            }

            // --- 3. IN-GAME CHASE & DUEL ---
            DrawInGameHUD();
        }

        private void DrawMainMenu()
        {
            float modalW = Mathf.Min(Screen.width * 0.90f, 620f);
            float modalH = Mathf.Min(Screen.height * 0.82f, 440f);
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            GUI.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.94f);
            GUI.Box(new Rect(modalX, modalY, modalW, modalH), "", bannerStyle);

            // Title
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(modalX, modalY + 20, modalW, 45), "🐉 RỒNG RẮN LÊN MÂY 3D 🐍", headerStyle);

            GUI.color = new Color(0.8f, 0.9f, 1.0f);
            GUI.Label(new Rect(modalX, modalY + 65, modalW, 30), "Trò Chơi Dân Gian Đương Đại • Chibi Casual Battle", headerStyle);

            if (showRulesModal)
            {
                DrawRulesModal(modalX, modalY, modalW, modalH);
                return;
            }

            // Action Buttons
            float btnW = Mathf.Min(modalW * 0.80f, 380f);
            float btnH = Mathf.Clamp(Screen.height * 0.085f, 48f, 65f);
            float btnX = (Screen.width - btnW) * 0.5f;
            float startY = modalY + 115f;

            GUI.color = Color.white;

            // Play Match Button
            GUI.backgroundColor = new Color(0.15f, 0.75f, 0.35f, 1.0f);
            if (GUI.Button(new Rect(btnX, startY, btnW, btnH), "🎮 VÀO TRẬN ĐẤU (PLAY MATCH)", buttonStyle))
            {
                GameManager.Instance.StartMatch();
            }

            // Character Showcase Button
            GUI.backgroundColor = new Color(0.20f, 0.55f, 0.95f, 1.0f);
            if (GUI.Button(new Rect(btnX, startY + btnH + 15, btnW, btnH), "👘 THIẾT KẾ NHÂN VẬT (SHOWCASE)", buttonStyle))
            {
                GameManager.Instance.OpenShowcase();
            }

            // Rules & Items Button
            GUI.backgroundColor = new Color(0.95f, 0.60f, 0.15f, 1.0f);
            if (GUI.Button(new Rect(btnX, startY + (btnH + 15) * 2, btnW, btnH), "📖 LUẬT CHƠI & 4 VẬT PHẨM", buttonStyle))
            {
                showRulesModal = true;
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawRulesModal(float modalX, float modalY, float modalW, float modalH)
        {
            string rulesText =
                "🎯 ĐIỀU KIỆN THẮNG/THUA:\n" +
                "• Hết 60 giây giữ được ít nhất 1 rắn con -> CHIẾN THẮNG!\n" +
                "• Thầy thuốc vồ bắt hết chỉ còn lại Rắn Mẹ -> THUA CUỘC!\n\n" +
                "❤️ CƠ CHẾ SỐNG SÓT (CHĂM CHĂM CHĂM - 1 HIỆP QUYẾT ĐỊNH):\n" +
                "• Mỗi lần bị vồ: Đấu 1 hiệp Chăm Chăm Chăm quyết định.\n" +
                "• Rắn con THẮNG: Trừ 0.5 tim (từ 3.0 -> 2.5 tim) và đẩy lùi Thầy thuốc.\n" +
                "• Rắn con THUA: Bị loại khỏi cuộc chơi ngay lập tức!\n\n" +
                "🥘 4 VẬT PHẨM DÂN TỘC:\n" +
                "• 🥖 Bánh Mì: Khiên 1 lần đòn cho Rắn / Thầy thuốc Vồ Xuyên Khiên (7s).\n" +
                "• 🍜 Phở: Tăng tốc chạy +35% (7s).\n" +
                "• 🍙 Cơm Cuộn: Khổng lồ hóa +30% kích thước & tầm với (7s).\n" +
                "• 🥘 Bánh Gạo Cay: Tạo phân thân ảo ảnh (5s, mờ sau 2s, cực hiếm 10%).";

            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.98f);
            GUI.Box(new Rect(modalX + 15, modalY + 60, modalW - 30, modalH - 125), rulesText, infoBoxStyle);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
            if (GUI.Button(new Rect(modalX + (modalW - 180) * 0.5f, modalY + modalH - 55, 180, 45), "ĐÃ HIỂU (CLOSE)", buttonStyle))
            {
                showRulesModal = false;
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawShowcase()
        {
            float topY = 15f;
            float pad = 15f;

            // 1. Top Header Title
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, topY, Screen.width, 40), "👘 THIẾT KẾ NHÂN VẬT 3D (CHARACTER SHOWCASE)", headerStyle);

            // 2. Character Tabs
            float tabW = Mathf.Clamp(Screen.width * 0.26f, 110f, 210f);
            float tabH = 46f;
            float totalTabsW = tabW * 3 + pad * 2;
            float tabStartX = (Screen.width - totalTabsW) * 0.5f;
            float tabY = topY + 48f;

            string[] tabNames = new string[] { "🐍 RẮN MẸ", "👶 RẮN CON", "🧙‍♂️ THẦY THUỐC" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUI.backgroundColor = (showcaseIndex == i) ? new Color(0.2f, 0.75f, 0.4f, 1.0f) : new Color(0.2f, 0.25f, 0.35f, 0.85f);
                if (GUI.Button(new Rect(tabStartX + i * (tabW + pad), tabY, tabW, tabH), tabNames[i], tabButtonStyle))
                {
                    showcaseIndex = i;
                    showcaseRotationY = 180f; // Reset face view
                }
            }

            // 3. Instruction hint
            GUI.color = new Color(0.75f, 0.85f, 1.0f);
            GUI.Label(new Rect(0, tabY + 52f, Screen.width, 28), "🖐️ Chạm & Kéo vuốt màn hình để xoay 360° xem chi tiết ngoại hình", headerStyle);

            // 4. Quick Rotation Buttons on sides
            float rotBtnY = Screen.height * 0.45f;
            GUI.backgroundColor = new Color(0.15f, 0.2f, 0.3f, 0.85f);
            if (GUI.Button(new Rect(20, rotBtnY, 90, 50), "◄ QUAY", buttonStyle))
            {
                showcaseRotationY += 45f;
            }
            if (GUI.Button(new Rect(Screen.width - 110, rotBtnY, 90, 50), "QUAY ►", buttonStyle))
            {
                showcaseRotationY -= 45f;
            }

            // 5. Bottom Info Box
            float infoW = Mathf.Min(Screen.width * 0.92f, 750f);
            float infoH = Mathf.Clamp(Screen.height * 0.26f, 110f, 170f);
            float infoX = (Screen.width - infoW) * 0.5f;
            float infoY = Screen.height - infoH - 75f;

            string charDesc = "";
            if (showcaseIndex == 0)
            {
                charDesc = "🐍 RẮN MẸ (ĐẦU ĐÀN):\n" +
                           "• Trang phục: Áo bà ba vàng nghệ truyền thống, nón lá quai thao mây tre đan.\n" +
                           "• Thiết kế sửa lỗi: Hàng cúc áo ngực phía trước chuẩn xác (+Z), đôi tay ôm cong về phía trước che chở đàn con.\n" +
                           "• Kỹ năng: Dẫn đường luồn lách, dang tay cản phá và xô đẩy Thầy Thuốc.";
            }
            else if (showcaseIndex == 1)
            {
                charDesc = "👶 RẮN CON (CON ÚT & ĐÀN CON):\n" +
                           "• Trang phục: Tóc chỏm dân gian Việt Nam, dây buộc đỏ may mắn, áo yếm nâu đỏ.\n" +
                           "• Thiết kế: Đôi tay vươn cong ra trước ôm chặt eo bạn trước. Con Út đội vương miện lá sen viền vàng.\n" +
                           "• Kỹ năng: Quyết đấu 1 hiệp Chăm Chăm Chăm khi bị vồ bắt (Thắng -0.5 tim, Thua bị loại).";
            }
            else
            {
                charDesc = "🧙‍♂️ THẦY THUỐC (THỢ SĂN):\n" +
                           "• Trang phục: Áo dài gấm truyền thống, đội khăn đóng sẫm màu, túi thuốc bắc gia truyền đeo bên hông.\n" +
                           "• Thiết kế: Tư thế rình rập, bàn tay vồ linh hoạt, ánh mắt láu lỉnh rình bắt con út.\n" +
                           "• Kỹ năng: Vồ bắt con út, ăn Bánh Mì để Vồ Xuyên Khiên, ăn Phở để siêu tốc độ.";
            }

            GUI.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.94f);
            GUI.Box(new Rect(infoX, infoY, infoW, infoH), charDesc, infoBoxStyle);

            // 6. Navigation Buttons at Bottom
            float bottomBtnW = Mathf.Clamp(Screen.width * 0.38f, 150f, 260f);
            float bottomBtnH = 50f;
            float bottomY = Screen.height - 60f;

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1.0f);
            if (GUI.Button(new Rect(infoX, bottomY, bottomBtnW, bottomBtnH), "⬅️ VỀ MENU", buttonStyle))
            {
                GameManager.Instance.ShowMainMenu();
            }

            GUI.backgroundColor = new Color(0.15f, 0.75f, 0.35f, 1.0f);
            if (GUI.Button(new Rect(infoX + infoW - bottomBtnW, bottomY, bottomBtnW, bottomBtnH), "🎮 VÀO TRẬN ĐẤU", buttonStyle))
            {
                GameManager.Instance.StartMatch();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawInGameHUD()
        {
            // 1. Top Center: 60s Match Timer
            float time = GameManager.Instance.RemainingTime;
            int mins = Mathf.FloorToInt(time / 60f);
            int secs = Mathf.FloorToInt(time % 60f);
            string timeText = $"⏱️ Thời gian: {mins:00}:{secs:00}";

            GUI.color = Color.black;
            GUI.Label(new Rect((Screen.width / 2f) - 148, 17, 300, 40), timeText, headerStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect((Screen.width / 2f) - 150, 15, 300, 40), timeText, headerStyle);

            // 2. Top Left: 3 Hearts & Remaining Children
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

            // Top Menu shortcut button
            GUI.backgroundColor = new Color(0.2f, 0.25f, 0.35f, 0.85f);
            if (GUI.Button(new Rect(Screen.width - 110, 15, 95, 38), "⏸️ Menu", buttonStyle))
            {
                GameManager.Instance.ShowMainMenu();
            }
            GUI.backgroundColor = Color.white;

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
            if (GameManager.Instance.CurrentState == GameState.Victory)
            {
                DrawEndGameModal("🎉 CHIẾN THẮNG!", "Bạn đã bảo vệ thành công đàn con suốt 60 giây!", Color.green);
            }
            else if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                DrawEndGameModal("💀 THUA CUỘC!", "Thầy Thuốc đã bắt hết toàn bộ đàn con!", Color.red);
            }
        }

        private void DrawActiveBuffs()
        {
            if (BuffSystem.Instance == null) return;

            float rightX = Screen.width - 250f;
            float startY = 65f;

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
