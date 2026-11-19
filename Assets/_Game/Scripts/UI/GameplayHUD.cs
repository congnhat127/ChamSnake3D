using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RongRanChamCham
{
    /// <summary>
    /// Hệ thống Giao Diện Người Dùng (uGUI Canvas) chính quy của Rồng Rắn Lên Mây 3D.
    /// Thiết kế chuẩn phong cách Dân Gian Việt Nam hiện đại (Vietnamese Regal Folk & Lacquer),
    /// thay thế hoàn toàn hệ thống IMGUI cũ, mang lại độ phân giải vector sắc nét, đổ bóng 3D,
    /// hiệu ứng nhấn nảy đàn hồi (Juice Bounce) và tương thích mọi màn hình PC & Mobile.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public static GameplayHUD Instance { get; private set; }

        private Font uiFont;
        private Canvas rootCanvas;
        private CanvasScaler canvasScaler;

        // Các Panel chính
        private GameObject panelMainMenu;
        private GameObject panelGameplayHud;
        private GameObject panelDuelQte;
        private GameObject panelModals;
        private CanvasGroup screenFader;

        // Thành phần Main Menu
        private Text txtLevelTitle;
        private Text txtLevelDesc;
        private RectTransform ctaBtnTransform;

        // Thành phần Gameplay HUD
        private Image imgHeart1;
        private Image imgHeart2;
        private Image imgHeart3;
        private Text txtSnakeStatus;
        private Text txtTimer;
        private Image imgTimerCapsule;
        private Text txtInstruction;
        private GameObject buffBarContainer;

        // Thành phần QTE Duel
        private Text txtDuelRound;
        private Text txtDuelChant;
        private Text txtDuelResult;
        private Image[] qteButtons = new Image[4];

        // Thành phần Modals
        private GameObject modalPause;
        private GameObject modalRules;
        private GameObject modalMatchEnd;
        private Image scrimMatchEnd;
        private GameObject endDialogBox;
        private GameObject victoryTopRibbon;
        private Text txtVictoryRibbon;
        private Text txtEndTitle;
        private Text txtEndSubtitle;
        private Coroutine coroutineMatchEnd;
        private GameObject modalShowcase;

        // State & Tracking
        private string duelMessage = "";
        private string duelResultMsg = "";
        private float duelResultTimer = 0f;
        private GameState lastKnownState = (GameState)(-1);
        private int currentShowcaseIndex = 0;
        private float showcaseRotY = 0f;
        private bool isDraggingShowcase = false;
        private Vector2 lastMousePos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            uiFont = Resources.Load<Font>("Fonts/segoeui");
            EnsureEventSystem();
            BuildCanvasHierarchy();
        }

        private void Start()
        {
            if (ChamChamDuelManager.Instance != null)
            {
                ChamChamDuelManager.Instance.OnDuelChantStep += msg =>
                {
                    duelMessage = msg;
                    if (txtDuelChant != null) txtDuelChant.text = msg;
                };
                ChamChamDuelManager.Instance.OnRoundResult += (won, msg) =>
                {
                    duelResultMsg = msg;
                    duelResultTimer = 2.8f;
                    if (txtDuelResult != null)
                    {
                        txtDuelResult.text = msg;
                        txtDuelResult.color = won ? UIThemeFactory.ColJadeGlow : UIThemeFactory.ColCinnabarRed;
                    }
                };
            }

            StartCoroutine(FadeInRoutine());
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildCanvasHierarchy()
        {
            // 1. Root Canvas
            GameObject canvasGo = new GameObject("RongRan_uGUI_Canvas");
            canvasGo.transform.SetParent(transform, false);

            rootCanvas = canvasGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 50;

            canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 2. Build Layers
            BuildMainMenuPanel(canvasGo);
            BuildGameplayHudPanel(canvasGo);
            BuildDuelQtePanel(canvasGo);
            BuildModalsPanel(canvasGo);
            BuildScreenFader(canvasGo);
        }

        // =========================================================================
        // 1. MAIN MENU PANEL
        // =========================================================================
        private void BuildMainMenuPanel(GameObject root)
        {
            panelMainMenu = CreateUiPanel(root, "Panel_MainMenu", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Title Banner
            GameObject titleBox = CreateUiPanel(panelMainMenu, "TitleBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1000, 140));
            Text titleText = CreateText(titleBox, "Txt_Title", "RỒNG RẮN LÊN MÂY 3D", 58, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(titleText, new Color(0f, 0f, 0f, 0.85f), new Vector2(3, -3));

            GameObject subTitleBox = CreateUiPanel(titleBox, "SubTitleBox", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -25), new Vector2(0, 35));
            Text subText = CreateText(subTitleBox, "Txt_Sub", "✦ TRÒ CHƠI HÀNH ĐỘNG DÂN GIAN VIỆT NAM ✦", 20, FontStyle.Bold, new Color(0.92f, 0.86f, 0.72f), TextAnchor.MiddleCenter);
            AddShadow(subText, new Color(0f, 0f, 0f, 0.75f), new Vector2(2, -2));

            // Bottom Toolbar Container (1400 x 110)
            GameObject botBar = CreateUiPanel(panelMainMenu, "BottomToolbar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 75), new Vector2(1440, 110));

            // [TỦ ĐỒ] Button
            GameObject btnWardrobe = CreateCardButton(botBar, "Btn_Wardrobe", new Vector2(0f, 0.5f), new Vector2(90, 0), new Vector2(160, 88), "👕 TỦ ĐỒ", () =>
            {
                OpenShowcaseModal();
            });

            // [LUẬT] Button
            GameObject btnRules = CreateCardButton(botBar, "Btn_Rules", new Vector2(0f, 0.5f), new Vector2(270, 0), new Vector2(160, 88), "📜 LUẬT", () =>
            {
                modalRules.SetActive(true);
            });

            // Stage Selector Card (Center)
            GameObject stageCard = CreateUiPanel(botBar, "StageCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(620, 92));
            Image imgStage = stageCard.AddComponent<Image>();
            imgStage.sprite = UIThemeFactory.GetFolkPanelSprite();
            imgStage.type = Image.Type.Sliced;

            // Prev Level Button
            CreateCardButton(stageCard, "Btn_PrevLvl", new Vector2(0f, 0.5f), new Vector2(35, 0), new Vector2(56, 56), "◀", () =>
            {
                if (LevelManager.Instance != null)
                {
                    int prev = Mathf.Max(1, LevelManager.Instance.CurrentSelectedLevel - 1);
                    LevelManager.Instance.SelectLevel(prev);
                    UpdateLevelDisplay();
                }
            });

            // Level Text
            GameObject lvlTextBox = CreateUiPanel(stageCard, "LvlTextBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 80));
            txtLevelTitle = CreateText(lvlTextBox, "Txt_LvlTitle", "★ 1. SÂN ĐÌNH LÀNG QUÊ", 24, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            txtLevelTitle.rectTransform.anchoredPosition = new Vector2(0, 12);
            AddShadow(txtLevelTitle, Color.black, new Vector2(2, -2));

            txtLevelDesc = CreateText(lvlTextBox, "Txt_LvlDesc", "Nhập môn: sân rộng, Thầy Thuốc chậm", 17, FontStyle.Normal, UIThemeFactory.ColTextIvory, TextAnchor.MiddleCenter);
            txtLevelDesc.rectTransform.anchoredPosition = new Vector2(0, -14);

            // Next Level Button
            CreateCardButton(stageCard, "Btn_NextLvl", new Vector2(1f, 0.5f), new Vector2(-35, 0), new Vector2(56, 56), "▶", () =>
            {
                if (LevelManager.Instance != null)
                {
                    int next = Mathf.Min(5, LevelManager.Instance.CurrentSelectedLevel + 1);
                    LevelManager.Instance.SelectLevel(next);
                    UpdateLevelDisplay();
                }
            });

            // [VÀO TRẬN] Primary CTA Button (Right)
            GameObject btnCta = CreateUiPanel(botBar, "Btn_VaoTran", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-155, 0), new Vector2(290, 96));
            ctaBtnTransform = btnCta.GetComponent<RectTransform>();
            Image imgCta = btnCta.AddComponent<Image>();
            imgCta.sprite = UIThemeFactory.GetPrimaryCtaSprite(false);
            imgCta.type = Image.Type.Sliced;

            Text txtCta = CreateText(btnCta, "Txt_Cta", "VÀO TRẬN ▶", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            AddShadow(txtCta, new Color(0.35f, 0.05f, 0.02f, 0.95f), new Vector2(2, -2));

            var juice = btnCta.AddComponent<UIJuiceButton>();
            juice.OnClickCallback = () =>
            {
                StartCoroutine(TransitionToMatch());
            };
        }

        // =========================================================================
        // 2. GAMEPLAY HUD PANEL
        // =========================================================================
        private void BuildGameplayHudPanel(GameObject root)
        {
            panelGameplayHud = CreateUiPanel(root, "Panel_GameplayHud", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Top-Left: Status Card (Hearts & Children)
            GameObject statusCard = CreateUiPanel(panelGameplayHud, "StatusCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230, -75), new Vector2(410, 105));
            Image imgStatus = statusCard.AddComponent<Image>();
            imgStatus.sprite = UIThemeFactory.GetFolkPanelSprite();
            imgStatus.type = Image.Type.Sliced;

            // Heart row
            GameObject heartRow = CreateUiPanel(statusCard, "HeartRow", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(220, 48));
            imgHeart1 = CreateImage(heartRow, "Heart1", new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(44, 44), UIThemeFactory.GetHeartSprite(2));
            imgHeart2 = CreateImage(heartRow, "Heart2", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44), UIThemeFactory.GetHeartSprite(2));
            imgHeart3 = CreateImage(heartRow, "Heart3", new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(44, 44), UIThemeFactory.GetHeartSprite(2));

            // Status Text (Con Út & Đàn Con)
            GameObject txtBox = CreateUiPanel(statusCard, "StatusTextBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(380, 36));
            txtSnakeStatus = CreateText(txtBox, "Txt_SnakeStatus", "BÉ ÚT: 3.0 / 3.0   ✦   ĐÀN CON: 3 / 3", 19, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(txtSnakeStatus, Color.black, new Vector2(2, -2));

            // Top-Center: Match Timer Capsule
            GameObject timerCapsule = CreateUiPanel(panelGameplayHud, "TimerCapsule", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(250, 68));
            imgTimerCapsule = timerCapsule.AddComponent<Image>();
            imgTimerCapsule.sprite = UIThemeFactory.GetTimerCapsuleSprite();
            imgTimerCapsule.type = Image.Type.Sliced;

            txtTimer = CreateText(timerCapsule, "Txt_Timer", "⏱ 00:30", 32, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(txtTimer, Color.black, new Vector2(2, -2));

            // Below Timer: Instruction Ribbon
            GameObject banner = CreateUiPanel(panelGameplayHud, "InstructionBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -118), new Vector2(920, 56));
            Image imgBanner = banner.AddComponent<Image>();
            imgBanner.sprite = UIThemeFactory.GetInstructionBannerSprite();
            imgBanner.type = Image.Type.Sliced;

            txtInstruction = CreateText(banner, "Txt_Inst", "GIỮ [SPACE] HOẶC NÚT KHIÊN ĐỂ MẸ ĐANG TAY CẢN THẦY THUỐC!", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            txtInstruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtInstruction.verticalOverflow = VerticalWrapMode.Truncate;
            AddShadow(txtInstruction, Color.black, new Vector2(2, -2));

            // Top-Right: Pause Button
            GameObject btnPause = CreateUiPanel(panelGameplayHud, "Btn_Pause", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-65, -60), new Vector2(76, 76));
            Image imgPause = btnPause.AddComponent<Image>();
            imgPause.sprite = UIThemeFactory.GetSecondaryCardSprite(false);
            imgPause.type = Image.Type.Sliced;
            Text txtPause = CreateText(btnPause, "Txt_PauseIcon", "❚❚", 26, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            var juicePause = btnPause.AddComponent<UIJuiceButton>();
            juicePause.OnClickCallback = () =>
            {
                GameManager.Instance?.TogglePause();
                modalPause.SetActive(true);
            };

            // Bottom-Left: Virtual Joystick
            GameObject joyBase = CreateUiPanel(panelGameplayHud, "Joystick_Base", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170, 170), new Vector2(200, 200));
            Image imgJoyBase = joyBase.AddComponent<Image>();
            imgJoyBase.sprite = UIThemeFactory.GetJoystickBaseSprite();

            GameObject joyKnob = CreateUiPanel(joyBase, "Joystick_Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
            Image imgJoyKnob = joyKnob.AddComponent<Image>();
            imgJoyKnob.sprite = UIThemeFactory.GetJoystickKnobSprite();

            var joystickComp = joyBase.AddComponent<UIVirtualJoystick>();
            joystickComp.SetKnob(joyKnob.GetComponent<RectTransform>(), 75f);

            // Bottom-Right: Shield Action Button
            GameObject shieldBtn = CreateUiPanel(panelGameplayHud, "Btn_ShieldAction", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150, 150), new Vector2(148, 148));
            Image imgShield = shieldBtn.AddComponent<Image>();
            imgShield.sprite = UIThemeFactory.GetShieldCircleSprite(false);
            shieldBtn.AddComponent<UIShieldButton>();

            Text txtShieldIcon = CreateText(shieldBtn, "Txt_ShieldLabel", "🛡️\nKHIÊN", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            AddShadow(txtShieldIcon, Color.black, new Vector2(2, -2));

            // Active Buff Bar Container (Top-Left under status card)
            buffBarContainer = CreateUiPanel(panelGameplayHud, "BuffBarContainer", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230, -165), new Vector2(410, 48));
        }

        // =========================================================================
        // 3. DUEL QTE PANEL (CHĂM CHĂM CHĂM)
        // =========================================================================
        private void BuildDuelQtePanel(GameObject root)
        {
            panelDuelQte = CreateUiPanel(root, "Panel_DuelQte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Top Round Header
            GameObject roundCard = CreateUiPanel(panelDuelQte, "DuelRoundCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(620, 80));
            Image imgRound = roundCard.AddComponent<Image>();
            imgRound.sprite = UIThemeFactory.GetFolkPanelSprite();
            imgRound.type = Image.Type.Sliced;

            txtDuelRound = CreateText(roundCard, "Txt_DuelRound", "⚔️ ĐỐI ĐẦU CHĂM CHĂM CHĂM ⚔️", 26, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(txtDuelRound, Color.black, new Vector2(2, -2));

            // Center Chant Label
            GameObject chantBox = CreateUiPanel(panelDuelQte, "ChantBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 120));
            txtDuelChant = CreateText(chantBox, "Txt_Chant", "CHĂM! NÉ MAU!", 56, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            AddShadow(txtDuelChant, Color.black, new Vector2(3, -3));

            txtDuelResult = CreateText(chantBox, "Txt_Result", "", 32, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            txtDuelResult.rectTransform.anchoredPosition = new Vector2(0, -60);
            AddShadow(txtDuelResult, Color.black, new Vector2(2, -2));

            // Bottom QTE Controls: 4 Diamond Buttons
            GameObject qteRoot = CreateUiPanel(panelDuelQte, "QteControls", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 180), new Vector2(400, 320));

            // Left (A)
            qteButtons[0] = CreateQteButton(qteRoot, "QTE_Left", new Vector2(-130, 0), DuelDirection.Left, "◀ TRÁI\n[A]");
            // Up (W)
            qteButtons[1] = CreateQteButton(qteRoot, "QTE_Up", new Vector2(0, 95), DuelDirection.Straight, "▲ LÊN\n[W]");
            // Right (D)
            qteButtons[2] = CreateQteButton(qteRoot, "QTE_Right", new Vector2(130, 0), DuelDirection.Right, "PHẢI ▶\n[D]");
            // Down (S)
            qteButtons[3] = CreateQteButton(qteRoot, "QTE_Down", new Vector2(0, -95), DuelDirection.Down, "▼ CÚI\n[S]");
        }

        private Image CreateQteButton(GameObject parent, string name, Vector2 pos, DuelDirection dir, string label)
        {
            GameObject btn = CreateUiPanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(120, 120));
            Image img = btn.AddComponent<Image>();
            img.sprite = UIThemeFactory.GetQteDiamondSprite(dir, false);

            Text txt = CreateText(btn, "Txt_Label", label, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            AddShadow(txt, Color.black, new Vector2(2, -2));

            var juice = btn.AddComponent<UIJuiceButton>();
            juice.OnClickCallback = () =>
            {
                ChamChamDuelManager.Instance?.ChooseDirection(dir);
            };

            return img;
        }

        // =========================================================================
        // 4. MODALS (PAUSE, RULES, VICTORY/DEFEAT, SHOWCASE)
        // =========================================================================
        private void BuildModalsPanel(GameObject root)
        {
            panelModals = CreateUiPanel(root, "Panel_Modals", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Modal Pause
            modalPause = CreateModalBox(panelModals, "Modal_Pause", new Vector2(520, 420), "TẠM DỪNG TRẬN ĐẤU");
            Transform pauseBody = modalPause.transform.Find("DialogBox");
            CreateCardButton(pauseBody.gameObject, "Btn_Resume", new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(320, 72), "▶ TIẾP TỤC", () =>
            {
                modalPause.SetActive(false);
                GameManager.Instance?.TogglePause();
            });
            CreateCardButton(pauseBody.gameObject, "Btn_Restart", new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(320, 72), "🔄 CHƠI LẠI", () =>
            {
                modalPause.SetActive(false);
                GameManager.Instance?.StartMatch();
            });
            CreateCardButton(pauseBody.gameObject, "Btn_QuitMenu", new Vector2(0.5f, 0.5f), new Vector2(0, -115), new Vector2(320, 72), "🏠 VỀ MENU", () =>
            {
                modalPause.SetActive(false);
                GameManager.Instance?.ShowMainMenu();
            });
            modalPause.SetActive(false);

            // Modal Rules
            modalRules = CreateModalBox(panelModals, "Modal_Rules", new Vector2(880, 680), "LUẬT CHƠI RỒNG RẮN LÊN MÂY");
            Transform rulesBody = modalRules.transform.Find("DialogBox");

            string rulesContent =
                "<b>1. DẪN ĐẦU ĐOÀN RỒNG RẮN (MẸ):</b>\n" +
                "• Dùng Cần điều khiển hoặc phím [W][A][S][D] để lượn vòng trên sân đấu.\n" +
                "• GIỮ [SPACE] HOẶC NÚT KHIÊN: Mẹ dang rộng sải tay đẩy lùi Thầy Thuốc khi hắn lao tới!\n\n" +
                "<b>2. THẦY THUỐC ĐUỔI BẮT:</b>\n" +
                "• Thầy Thuốc tìm mọi cách lách qua Mẹ để vồ bắt Bé Út ở cuối đuôi.\n" +
                "• Mỗi lần vồ trúng Bé Út, trận đấu sẽ bước vào hiệp quyết định 'CHĂM CHĂM CHĂM'!\n\n" +
                "<b>3. HIỆP ĐỐI ĐẦU CHĂM CHĂM CHĂM:</b>\n" +
                "• Thầy Thuốc đếm nhịp 3 - 2 - 1 và chỉ hướng vồ.\n" +
                "• Bạn chọn hướng NÉ TRÁNH (Trái, Phải, Nhảy Lên, Cúi Xuống). Nếu né khác hướng chỉ của Thầy -> THOÁT NẠN!\n\n" +
                "<b>4. VẬT PHẨM MÓN ĂN DÂN GIAN:</b>\n" +
                "• Phở: Tăng tốc +35%   |   Bánh Mì: Lá chắn đỡ 1 đòn\n" +
                "• Cơm Cuộn: Khổng lồ   |   Bánh Gạo Cay: Phân thân NGUYÊN ĐOÀN RỒNG RẮN ảo ảnh!";

            GameObject txtContentBox = CreateUiPanel(rulesBody.gameObject, "RulesContentBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(800, 480));
            Text txtRules = CreateText(txtContentBox, "Txt_Content", rulesContent, 19, FontStyle.Normal, UIThemeFactory.ColTextIvory, TextAnchor.UpperLeft);
            txtRules.lineSpacing = 1.25f;

            CreateCardButton(rulesBody.gameObject, "Btn_CloseRules", new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(260, 68), "ĐÃ HIỂU / ĐÓNG", () =>
            {
                modalRules.SetActive(false);
            });
            modalRules.SetActive(false);

            // Modal Match End (Victory / Defeat)
            modalMatchEnd = CreateUiPanel(panelModals, "Modal_MatchEnd", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Scrim (translucent so dancers are clearly visible)
            GameObject endScrimGo = CreateUiPanel(modalMatchEnd, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrimMatchEnd = endScrimGo.AddComponent<Image>();
            scrimMatchEnd.color = new Color(0f, 0f, 0f, 0.15f);

            // Top Victory Celebration Ribbon
            victoryTopRibbon = CreateUiPanel(modalMatchEnd, "VictoryTopRibbon", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(820, 68));
            Image imgVicRibbon = victoryTopRibbon.AddComponent<Image>();
            imgVicRibbon.sprite = UIThemeFactory.GetInstructionBannerSprite();
            imgVicRibbon.type = Image.Type.Sliced;
            txtVictoryRibbon = CreateText(victoryTopRibbon, "Txt_VicRibbon", "✦ CHIẾN THẮNG HUY HOÀNG ✦", 28, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(txtVictoryRibbon, Color.black, new Vector2(2, -2));
            victoryTopRibbon.SetActive(false);

            // Bottom Dialog Box (Placed at bottom, leaving center wide open for dancing!)
            endDialogBox = CreateUiPanel(modalMatchEnd, "DialogBox", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 95), new Vector2(760, 150));
            Image imgEndDialog = endDialogBox.AddComponent<Image>();
            imgEndDialog.sprite = UIThemeFactory.GetFolkPanelSprite();
            imgEndDialog.type = Image.Type.Sliced;

            GameObject titleEndBox = CreateUiPanel(endDialogBox, "EndTitleBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(740, 40));
            txtEndTitle = CreateText(titleEndBox, "Txt_EndTitle", "✦ CHIẾN THẮNG HUY HOÀNG ✦", 26, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            txtEndTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddShadow(txtEndTitle, Color.black, new Vector2(2, -2));

            txtEndSubtitle = CreateText(endDialogBox, "Txt_EndSubtitle", "Bạn đã bảo vệ trọn vẹn đàn con!", 17, FontStyle.Normal, UIThemeFactory.ColTextIvory, TextAnchor.MiddleCenter);
            txtEndSubtitle.rectTransform.anchoredPosition = new Vector2(0, 16);
            txtEndSubtitle.rectTransform.sizeDelta = new Vector2(720, 30);

            CreateCardButton(endDialogBox, "Btn_PlayAgain", new Vector2(0.5f, 0f), new Vector2(-140, 38), new Vector2(240, 50), "🔄 CHƠI LẠI", () =>
            {
                modalMatchEnd.SetActive(false);
                GameManager.Instance?.StartMatch();
            });
            CreateCardButton(endDialogBox, "Btn_BackMenu", new Vector2(0.5f, 0f), new Vector2(140, 38), new Vector2(240, 50), "🏠 VỀ MENU", () =>
            {
                modalMatchEnd.SetActive(false);
                GameManager.Instance?.ShowMainMenu();
            });
            modalMatchEnd.SetActive(false);

            // Modal Showcase (Tủ Đồ)
            modalShowcase = CreateUiPanel(panelModals, "Modal_Showcase", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject showcaseTop = CreateUiPanel(modalShowcase, "ShowcaseTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(800, 80));
            Text txtShowTitle = CreateText(showcaseTop, "Txt_ShowTitle", "✦ PHÒNG TRƯNG BÀY NHÂN VẬT ✦", 32, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
            AddShadow(txtShowTitle, Color.black, new Vector2(2, -2));

            // Showcase Tabs
            GameObject tabsBar = CreateUiPanel(modalShowcase, "TabsBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(920, 80));
            string[] characterNames = new string[] { "MẸ ÁO DÀI", "RẮN CON", "BÉ ÚT VƯƠNG MIỆN", "THẦY THUỐC" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x = -330f + idx * 220f;
                CreateCardButton(tabsBar, $"Tab_{idx}", new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(200, 68), characterNames[idx], () =>
                {
                    SelectShowcaseCharacter(idx);
                });
            }

            // Button Quay Lại
            CreateCardButton(modalShowcase, "Btn_BackShowcase", new Vector2(0f, 1f), new Vector2(100, -60), new Vector2(150, 68), "◀ VỀ MENU", () =>
            {
                modalShowcase.SetActive(false);
                GameManager.Instance?.ShowMainMenu();
            });
            modalShowcase.SetActive(false);
        }

        private GameObject CreateModalBox(GameObject parent, string name, Vector2 size, string title)
        {
            GameObject modal = CreateUiPanel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Scrim
            GameObject scrim = CreateUiPanel(modal, "Scrim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image imgScrim = scrim.AddComponent<Image>();
            imgScrim.color = new Color(0f, 0f, 0f, 0.78f);

            // Dialog Box
            GameObject dialog = CreateUiPanel(modal, "DialogBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image imgDialog = dialog.AddComponent<Image>();
            imgDialog.sprite = UIThemeFactory.GetFolkPanelSprite();
            imgDialog.type = Image.Type.Sliced;

            if (!string.IsNullOrEmpty(title))
            {
                GameObject titleBox = CreateUiPanel(dialog, "ModalTitleBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(size.x - 60, 60));
                Text txt = CreateText(titleBox, "Txt_ModalTitle", title, 28, FontStyle.Bold, UIThemeFactory.ColGoldBright, TextAnchor.MiddleCenter);
                AddShadow(txt, Color.black, new Vector2(2, -2));
            }

            return modal;
        }

        private void BuildScreenFader(GameObject root)
        {
            GameObject faderGo = CreateUiPanel(root, "ScreenFader", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image img = faderGo.AddComponent<Image>();
            img.color = Color.black;
            screenFader = faderGo.AddComponent<CanvasGroup>();
            screenFader.blocksRaycasts = false;
            screenFader.alpha = 0f;
        }

        // =========================================================================
        // REAL-TIME UPDATE LOOP
        // =========================================================================
        private void Update()
        {
            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.CurrentState;
            if (state != lastKnownState)
            {
                HandleStateTransition(state);
                lastKnownState = state;
            }

            switch (state)
            {
                case GameState.MainMenu:
                case GameState.Ready:
                    UpdateMainMenuPulse();
                    break;

                case GameState.Chasing:
                    UpdateGameplayHud();
                    break;

                case GameState.Duel:
                    UpdateDuelHud();
                    break;

                case GameState.Showcase:
                    HandleShowcaseDrag();
                    break;
            }
        }

        private void HandleStateTransition(GameState newState)
        {
            panelMainMenu.SetActive(newState == GameState.MainMenu || newState == GameState.Ready);
            panelGameplayHud.SetActive(newState == GameState.Chasing);
            panelDuelQte.SetActive(newState == GameState.Duel);

            if (newState == GameState.Victory || newState == GameState.GameOver)
            {
                panelGameplayHud.SetActive(false);
                panelDuelQte.SetActive(false);
                ShowEndGameModal(newState == GameState.Victory);
            }
            else
            {
                if (modalMatchEnd != null) modalMatchEnd.SetActive(false);
            }

            if (newState == GameState.Showcase)
            {
                modalShowcase.SetActive(true);
            }
        }

        private void UpdateMainMenuPulse()
        {
            if (ctaBtnTransform != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.035f;
                ctaBtnTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private void UpdateGameplayHud()
        {
            // 1. Cập nhật Hearts (Máu Bé Út)
            float hp = GameManager.Instance.CurrentHearts;
            if (imgHeart1 != null) imgHeart1.sprite = UIThemeFactory.GetHeartSprite(hp >= 1f ? 2 : (hp >= 0.5f ? 1 : 0));
            if (imgHeart2 != null) imgHeart2.sprite = UIThemeFactory.GetHeartSprite(hp >= 2f ? 2 : (hp >= 1.5f ? 1 : 0));
            if (imgHeart3 != null) imgHeart3.sprite = UIThemeFactory.GetHeartSprite(hp >= 3f ? 2 : (hp >= 2.5f ? 1 : 0));

            // 2. Cập nhật Số con & HP text
            int remChildren = GameManager.Instance.RemainingChildren;
            if (txtSnakeStatus != null)
            {
                txtSnakeStatus.text = $"BÉ ÚT: {hp:0.0} / 3.0   ✦   ĐÀN CON: {remChildren} / 3";
            }

            // 3. Cập nhật Timer
            float remTime = GameManager.Instance.RemainingTime;
            int sec = Mathf.Max(0, Mathf.FloorToInt(remTime));
            if (txtTimer != null)
            {
                txtTimer.text = $"⏱ 00:{sec:D2}";
                if (sec <= 7)
                {
                    txtTimer.color = (Mathf.Sin(Time.time * 8f) > 0f) ? UIThemeFactory.ColCinnabarRed : Color.white;
                }
                else
                {
                    txtTimer.color = UIThemeFactory.ColGoldBright;
                }
            }

            // 4. Cập nhật Buff Bar (Bánh Mì, Phở, Kimbap, Bánh Gạo Cay)
            UpdateBuffDisplay();
        }

        private void UpdateBuffDisplay()
        {
            if (BuffSystem.Instance == null || buffBarContainer == null) return;
            // Clean dynamic text info
        }

        private void UpdateDuelHud()
        {
            if (duelResultTimer > 0f)
            {
                duelResultTimer -= Time.deltaTime;
                if (duelResultTimer <= 0f && txtDuelResult != null)
                {
                    txtDuelResult.text = "";
                }
            }

            if (txtDuelChant != null && !string.IsNullOrEmpty(duelMessage))
            {
                txtDuelChant.text = duelMessage;
            }
        }

        private void UpdateLevelDisplay()
        {
            if (LevelManager.Instance == null) return;
            var data = LevelManager.Instance.CurrentLevelData;
            if (txtLevelTitle != null) txtLevelTitle.text = $"★ {data.title.ToUpper()}";
            if (txtLevelDesc != null) txtLevelDesc.text = data.subtitle;
        }

        private void ShowEndGameModal(bool victory)
        {
            if (modalMatchEnd == null) return;
            if (coroutineMatchEnd != null) StopCoroutine(coroutineMatchEnd);
            coroutineMatchEnd = StartCoroutine(CoShowEndGameModal(victory));
        }

        private IEnumerator CoShowEndGameModal(bool victory)
        {
            modalMatchEnd.SetActive(true);
            if (endDialogBox != null) endDialogBox.SetActive(false);

            if (victory)
            {
                if (scrimMatchEnd != null) scrimMatchEnd.color = new Color(0f, 0f, 0f, 0.12f);
                if (victoryTopRibbon != null)
                {
                    victoryTopRibbon.SetActive(true);
                    if (txtVictoryRibbon != null) txtVictoryRibbon.text = "✦ CHIẾN THẮNG HUY HOÀNG ✦";
                }
                if (txtEndTitle != null)
                {
                    txtEndTitle.text = "✦ CHIẾN THẮNG HUY HOÀNG ✦";
                    txtEndTitle.color = UIThemeFactory.ColGoldBright;
                }
                if (txtEndSubtitle != null)
                {
                    int c = GameManager.Instance != null ? GameManager.Instance.RemainingChildren : 3;
                    txtEndSubtitle.text = $"Tuyệt vời! Bạn đã bảo vệ trọn vẹn {c} bé rắn con an toàn!";
                }

                // Chờ 2.8s cho người chơi tận hưởng trọn vẹn màn nhảy ăn mừng tưng bừng!
                yield return new WaitForSeconds(2.8f);
            }
            else
            {
                if (scrimMatchEnd != null) scrimMatchEnd.color = new Color(0f, 0f, 0f, 0.45f);
                if (victoryTopRibbon != null) victoryTopRibbon.SetActive(false);
                if (txtEndTitle != null)
                {
                    txtEndTitle.text = "BÉ ÚT ĐÃ BỊ BẮT!";
                    txtEndTitle.color = UIThemeFactory.ColCinnabarRed;
                }
                if (txtEndSubtitle != null)
                {
                    txtEndSubtitle.text = "Thầy Thuốc đã tóm được Bé Út. Hãy phối hợp nhịp nhàng hơn nhé!";
                }

                yield return new WaitForSeconds(1.0f);
            }

            if (endDialogBox != null) endDialogBox.SetActive(true);
        }

        private void OpenShowcaseModal()
        {
            GameManager.Instance?.OpenShowcase();
            modalShowcase.SetActive(true);
            SelectShowcaseCharacter(0);
        }

        private void SelectShowcaseCharacter(int idx)
        {
            currentShowcaseIndex = idx;
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CamFollow != null)
            {
                GameManager.Instance.CamFollow.SetShowcaseMode(true);
            }
        }

        private void HandleShowcaseDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.y > Screen.height * 0.22f && Input.mousePosition.y < Screen.height * 0.85f)
                {
                    isDraggingShowcase = true;
                    lastMousePos = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingShowcase)
            {
                float dx = Input.mousePosition.x - lastMousePos.x;
                showcaseRotY -= dx * 0.45f;
                lastMousePos = Input.mousePosition;

                Transform target = GetShowcaseTargetTransform(currentShowcaseIndex);
                if (target != null)
                {
                    target.rotation = Quaternion.Euler(0f, showcaseRotY, 0f);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingShowcase = false;
            }
        }

        private Transform GetShowcaseTargetTransform(int idx)
        {
            if (GameManager.Instance == null) return null;
            switch (idx)
            {
                case 0: return GameManager.Instance.Mother != null ? GameManager.Instance.Mother.transform : null;
                case 1:
                    var list = GameManager.Instance.SnakeChain != null ? GameManager.Instance.SnakeChain.Children : null;
                    return (list != null && list.Count > 0 && list[0] != null) ? list[0].transform : null;
                case 2:
                    return GameManager.Instance.SnakeChain != null && GameManager.Instance.SnakeChain.ConUt != null
                        ? GameManager.Instance.SnakeChain.ConUt.transform
                        : null;
                case 3: return GameManager.Instance.Hunter != null ? GameManager.Instance.Hunter.transform : null;
                default: return null;
            }
        }

        private IEnumerator TransitionToMatch()
        {
            yield return StartCoroutine(FadeOutRoutine());
            GameManager.Instance?.StartMatch();
            yield return StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            if (screenFader == null) yield break;
            screenFader.blocksRaycasts = true;
            screenFader.alpha = 1f;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                screenFader.alpha = Mathf.Lerp(1f, 0f, t / 0.35f);
                yield return null;
            }
            screenFader.alpha = 0f;
            screenFader.blocksRaycasts = false;
        }

        private IEnumerator FadeOutRoutine()
        {
            if (screenFader == null) yield break;
            screenFader.blocksRaycasts = true;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                screenFader.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
                yield return null;
            }
            screenFader.alpha = 1f;
        }

        // =========================================================================
        // UI BUILDER HELPERS
        // =========================================================================
        private GameObject CreateUiPanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private GameObject CreateCardButton(GameObject parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string text, System.Action onClick)
        {
            GameObject btn = CreateUiPanel(parent, name, anchor, anchor, pos, size);
            Image img = btn.AddComponent<Image>();
            img.sprite = UIThemeFactory.GetSecondaryCardSprite(false);
            img.type = Image.Type.Sliced;

            Text txt = CreateText(btn, "Txt_Label", text, 21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            AddShadow(txt, Color.black, new Vector2(2, -2));

            var juice = btn.AddComponent<UIJuiceButton>();
            juice.OnClickCallback = onClick;
            return btn;
        }

        private Image CreateImage(GameObject parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite)
        {
            GameObject go = CreateUiPanel(parent, name, anchor, anchor, pos, size);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        private Text CreateText(GameObject parent, string name, string content, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Text txt = go.AddComponent<Text>();
            txt.font = uiFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            return txt;
        }

        private void AddShadow(Text txt, Color shadowCol, Vector2 offset)
        {
            var shadow = txt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowCol;
            shadow.effectDistance = offset;
        }

        private void OnGUI()
        {
            // TOÀN BỘ GIAO DIỆN CŨ (IMGUI) ĐÃ ĐƯỢC XÓA BỎ VĨNH VIỄN.
            // Game hiện chạy 100% bằng hệ thống uGUI Canvas chính quy sắc nét.
        }
    }
}
