using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RongRanChamCham
{
    public class GameplayHUD : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle heartStyle;
        private GUIStyle bannerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle ctaButtonStyle;
        private GUIStyle secondaryBtnStyle;
        private GUIStyle infoBoxStyle;
        private GUIStyle cardBtnStyle;
        private GUIStyle cardBtnActiveStyle;
        private GUIStyle subHeaderCenterStyle;
        private GUIStyle iconStyle;
        private GUIStyle bigIconStyle;

        // Bố cục HUD tính trên khung ảo cao 1080 đơn vị; R() đổi sang pixel thật theo chiều cao màn hình.
        private const float VH = 1080f;
        private static float UiScale => Screen.height / VH;
        private static float VW => Screen.width / UiScale;

        private static Rect R(float x, float y, float w, float h)
        {
            float s = UiScale;
            return new Rect(x * s, y * s, w * s, h * s);
        }
        private bool stylesInitialized = false;

        private string duelMessage = "";
        private string duelResultMsg = "";
        private float resultMsgTimer = 0f;

        // Menu & Showcase state
        private bool showRulesModal = false;
        private int showcaseIndex = 0; // 0 = Mother, 1 = Child, 2 = Con Ut Crown, 3 = Hunter
        private float showcaseRotationY = 0f;
        private Vector2 lastMousePos;
        private bool isDraggingRotation = false;

        // Professional UI Textures
        private Texture2D texCtaNormal;
        private Texture2D texCtaPressed;
        private Texture2D texSecCardNormal;
        private Texture2D texSecCardHover;
        private Texture2D texQteLeft;
        private Texture2D texQteUp;
        private Texture2D texQteRight;
        private Texture2D texQteDown;
        private GUIStyle countdownStyle;
        private float celebrationTimer = 0f;
        private Font customFont;

        private void Start()
        {
            LoadTextures();

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

        private void LoadTextures()
        {
            // 1. Try Resources (Works 100% in WebGL and standalone builds)
            texCtaNormal = Resources.Load<Texture2D>("UI/btn_primary_cta_normal");
            texCtaPressed = Resources.Load<Texture2D>("UI/btn_primary_cta_pressed");
            texSecCardNormal = Resources.Load<Texture2D>("UI/btn_secondary_card_normal");
            texSecCardHover = Resources.Load<Texture2D>("UI/btn_secondary_card_hover");
            texQteLeft = Resources.Load<Texture2D>("UI/btn_qte_left");
            texQteUp = Resources.Load<Texture2D>("UI/btn_qte_up");
            texQteRight = Resources.Load<Texture2D>("UI/btn_qte_right");
            texQteDown = Resources.Load<Texture2D>("UI/btn_qte_down");

#if UNITY_EDITOR
            if (texCtaNormal == null) texCtaNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_primary_cta_normal.png");
            if (texCtaPressed == null) texCtaPressed = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_primary_cta_pressed.png");
            if (texSecCardNormal == null) texSecCardNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_secondary_card_normal.png");
            if (texSecCardHover == null) texSecCardHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_secondary_card_hover.png");
            if (texQteLeft == null) texQteLeft = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_qte_left.png");
            if (texQteUp == null) texQteUp = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_qte_up.png");
            if (texQteRight == null) texQteRight = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_qte_right.png");
            if (texQteDown == null) texQteDown = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_qte_down.png");
#endif
            if (texCtaNormal == null) texCtaNormal = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_primary_cta_normal.png");
            if (texCtaPressed == null) texCtaPressed = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_primary_cta_pressed.png");
            if (texSecCardNormal == null) texSecCardNormal = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_secondary_card_normal.png");
            if (texSecCardHover == null) texSecCardHover = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_secondary_card_hover.png");
            if (texQteLeft == null) texQteLeft = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_qte_left.png");
            if (texQteUp == null) texQteUp = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_qte_up.png");
            if (texQteRight == null) texQteRight = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_qte_right.png");
            if (texQteDown == null) texQteDown = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_qte_down.png");

            // Absolute procedural fallback guarantee (never allows invisible buttons!)
            if (texCtaNormal == null) texCtaNormal = CreateProceduralButtonTex(new Color(1f, 0.52f, 0.05f), new Color(1f, 0.72f, 0.20f));
            if (texCtaPressed == null) texCtaPressed = CreateProceduralButtonTex(new Color(0.85f, 0.38f, 0.02f), new Color(0.95f, 0.50f, 0.10f));
            if (texSecCardNormal == null) texSecCardNormal = CreateProceduralButtonTex(new Color(0.12f, 0.16f, 0.24f, 0.95f), new Color(0.35f, 0.45f, 0.60f, 0.95f));
            if (texSecCardHover == null) texSecCardHover = CreateProceduralButtonTex(new Color(0.20f, 0.28f, 0.42f, 0.98f), new Color(0.50f, 0.65f, 0.85f, 0.98f));
        }

        private static Texture2D CreateProceduralButtonTex(Color baseColor, Color borderColor)
        {
            const int w = 64;
            const int h = 64;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3);
                    pixels[y * w + x] = isBorder ? borderColor : baseColor;
                }
            }
            t.SetPixels(pixels);
            t.Apply();
            return t;
        }

        private Texture2D LoadTextureFromDisk(string assetRelPath)
        {
            string fullPath = Path.Combine(Application.dataPath, assetRelPath.Replace("Assets/", ""));
            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes)) return tex;
            }
            return null;
        }

        private void Update()
        {
            if (resultMsgTimer > 0f)
            {
                resultMsgTimer -= Time.deltaTime;
            }

            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.Victory || GameManager.Instance.CurrentState == GameState.GameOver))
            {
                celebrationTimer += Time.deltaTime;
            }
            else
            {
                celebrationTimer = 0f;
            }

            UpdateShowcaseVisibility();

            // In Showcase mode, handle character rotation & camera focus
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Showcase)
            {
                HandleShowcaseInteraction();
            }
        }

        private bool wasShowcaseLastFrame = false;

        private void UpdateShowcaseVisibility()
        {
            if (GameManager.Instance == null) return;

            bool isShowcase = GameManager.Instance.CurrentState == GameState.Showcase;

            if (isShowcase)
            {
                wasShowcaseLastFrame = true;

                // 1. Mother
                if (GameManager.Instance.Mother != null)
                {
                    bool showMother = (showcaseIndex == 0);
                    GameManager.Instance.Mother.gameObject.SetActive(showMother);
                    if (showMother)
                    {
                        GameManager.Instance.Mother.transform.position = Vector3.zero;
                    }
                }

                // 2. Snake Children (Completely isolate one child at a time)
                if (GameManager.Instance.SnakeChain != null)
                {
                    var children = GameManager.Instance.SnakeChain.Children;
                    bool inChildMode = (showcaseIndex == 1 || showcaseIndex == 2);
                    GameManager.Instance.SnakeChain.gameObject.SetActive(inChildMode);

                    if (inChildMode && children != null)
                    {
                        for (int i = 0; i < children.Count; i++)
                        {
                            var ch = children[i];
                            if (ch == null) continue;

                            bool isCurrentTarget = (showcaseIndex == 1 && i == 0) ||
                                                   (showcaseIndex == 2 && i == children.Count - 1);

                            ch.gameObject.SetActive(isCurrentTarget);

                            if (isCurrentTarget)
                            {
                                ch.transform.position = Vector3.zero;
                                Transform crown = ch.transform.Find("Socket_Head/Socket_Crown/Crown_ConUt");
                                if (crown != null)
                                {
                                    crown.gameObject.SetActive(showcaseIndex == 2);
                                }
                            }
                        }
                    }
                }

                // 3. Hunter
                if (GameManager.Instance.Hunter != null)
                {
                    bool showHunter = (showcaseIndex == 3);
                    GameManager.Instance.Hunter.gameObject.SetActive(showHunter);
                    if (showHunter)
                    {
                        GameManager.Instance.Hunter.transform.position = Vector3.zero;
                    }
                }
            }
            else if (wasShowcaseLastFrame)
            {
                wasShowcaseLastFrame = false;

                // Restore all entities to active state for gameplay
                if (GameManager.Instance.Mother != null)
                    GameManager.Instance.Mother.gameObject.SetActive(true);

                if (GameManager.Instance.SnakeChain != null)
                {
                    GameManager.Instance.SnakeChain.gameObject.SetActive(true);
                    var children = GameManager.Instance.SnakeChain.Children;
                    if (children != null)
                    {
                        for (int i = 0; i < children.Count; i++)
                        {
                            if (children[i] != null)
                            {
                                children[i].gameObject.SetActive(true);
                                Transform crown = children[i].transform.Find("Socket_Head/Socket_Crown/Crown_ConUt");
                                if (crown != null)
                                {
                                    crown.gameObject.SetActive(i == children.Count - 1);
                                }
                            }
                        }
                    }
                }

                if (GameManager.Instance.Hunter != null)
                    GameManager.Instance.Hunter.gameObject.SetActive(true);
            }
        }

        private void HandleShowcaseInteraction()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.y < Screen.height * 0.75f && Input.mousePosition.y > Screen.height * 0.22f)
                {
                    isDraggingRotation = true;
                    lastMousePos = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingRotation)
            {
                float deltaX = Input.mousePosition.x - lastMousePos.x;
                showcaseRotationY -= deltaX * 0.45f;
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingRotation = false;
            }

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
                    if (GameManager.Instance.SnakeChain != null && GameManager.Instance.SnakeChain.Children != null && GameManager.Instance.SnakeChain.Children.Count > 0)
                        return GameManager.Instance.SnakeChain.Children[0] != null ? GameManager.Instance.SnakeChain.Children[0].transform : null;
                    return null;
                case 2:
                    if (GameManager.Instance.SnakeChain != null && GameManager.Instance.SnakeChain.ConUt != null)
                        return GameManager.Instance.SnakeChain.ConUt.transform;
                    return null;
                case 3:
                    return GameManager.Instance.Hunter != null ? GameManager.Instance.Hunter.transform : null;
                default:
                    return null;
            }
        }

        private int lastScreenH = -1;

        private void InitStyles()
        {
            if (customFont == null)
            {
                customFont = Resources.Load<Font>("Fonts/segoeui");
#if UNITY_EDITOR
                if (customFont == null)
                    customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/segoeui.ttf");
#endif
            }
            if (customFont != null)
            {
                GUI.skin.font = customFont;
            }

            if (stylesInitialized && lastScreenH == Screen.height) return;
            lastScreenH = Screen.height;

            int h = Screen.height;
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.044f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.88f, 0.25f);

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.030f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            subHeaderStyle.normal.textColor = Color.white;

            heartStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.040f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            heartStyle.normal.textColor = new Color(1.0f, 0.35f, 0.45f);

            bannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(h * 0.036f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bannerStyle.normal.textColor = Color.white;

            ctaButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.036f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            ctaButtonStyle.normal.textColor = Color.white;

            secondaryBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.028f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            secondaryBtnStyle.normal.textColor = new Color(0.95f, 0.90f, 0.75f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(h * 0.032f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            cardBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.026f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            cardBtnStyle.normal.textColor = new Color(0.85f, 0.85f, 0.90f);

            cardBtnActiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.028f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            cardBtnActiveStyle.normal.textColor = new Color(1.0f, 0.90f, 0.20f);

            infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(h * 0.028f),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            infoBoxStyle.normal.textColor = Color.white;

            subHeaderCenterStyle = new GUIStyle(subHeaderStyle) { alignment = TextAnchor.UpperCenter };

            // Icon nằm chính giữa nút
            iconStyle = new GUIStyle(headerStyle) { alignment = TextAnchor.MiddleCenter };
            bigIconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.085f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bigIconStyle.normal.textColor = Color.white;

            // Bảng luật và thẻ giới thiệu chứa nhiều chữ: cỡ nhỏ hơn để vừa khung
            infoBoxStyle.fontSize = Mathf.RoundToInt(h * 0.024f);

            if (customFont != null)
            {
                headerStyle.font = customFont;
                subHeaderStyle.font = customFont;
                heartStyle.font = customFont;
                bannerStyle.font = customFont;
                ctaButtonStyle.font = customFont;
                secondaryBtnStyle.font = customFont;
                buttonStyle.font = customFont;
                cardBtnStyle.font = customFont;
                cardBtnActiveStyle.font = customFont;
                infoBoxStyle.font = customFont;
                subHeaderCenterStyle.font = customFont;
                iconStyle.font = customFont;
                bigIconStyle.font = customFont;
            }

            // Số đếm ngược lớn trực quan cho QTE Chăm Chăm Chăm
            countdownStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(h * 0.080f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            countdownStyle.normal.textColor = new Color(1.0f, 0.92f, 0.20f);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.CurrentState;

            if (state == GameState.MainMenu)
            {
                DrawMainMenu();
                return;
            }

            if (state == GameState.Showcase)
            {
                DrawShowcase();
                return;
            }

            DrawInGameHUD();
        }

        // ==============================================================
        // 1. MAIN MENU (BRAWL STARS / NINTENDO STYLE DOCK COMPOSITION)
        // ==============================================================
        private void DrawMainMenu()
        {
            // Subtle top/bottom vignette
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(R(0, 0, VW, 135), Texture2D.whiteTexture);
            GUI.DrawTexture(R(0, VH - 130, VW, 130), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Top Header: Game Title
            GUI.Label(R(0, 14, VW, 66), "RỒNG RẮN LÊN MÂY 3D", headerStyle);

            GUI.color = new Color(0.85f, 0.92f, 1.0f, 0.85f);
            GUI.Label(R(0, 82, VW, 44), "Trò Chơi Dân Gian Đương Đại", subHeaderCenterStyle);
            GUI.color = Color.white;

            if (showRulesModal)
            {
                DrawRulesModal();
                return;
            }

            // Level Selector in Main Menu
            if (LevelManager.Instance != null)
            {
                float lvlBoxW = Mathf.Min(VW * 0.46f, 880f);
                float lvlBoxH = 130f;
                float lvlBoxX = (VW - lvlBoxW) * 0.5f;
                float lvlBoxY = VH - lvlBoxH - 50f;
                float arrowW = 80f;

                GUI.backgroundColor = new Color(0.08f, 0.10f, 0.16f, 0.94f);
                GUI.Box(R(lvlBoxX, lvlBoxY, lvlBoxW, lvlBoxH), "", bannerStyle);
                GUI.backgroundColor = Color.white;

                int curLvl = LevelManager.Instance.CurrentSelectedLevel;
                var lvlData = LevelManager.Instance.CurrentLevelData;
                bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(curLvl);

                Rect prevRect = R(lvlBoxX + 12f, lvlBoxY + 15f, arrowW, lvlBoxH - 30f);
                if (curLvl > 1 && DrawProButton(prevRect, texSecCardNormal, texSecCardHover, "<", iconStyle))
                {
                    LevelManager.Instance.SelectLevel(curLvl - 1);
                }

                float textX = lvlBoxX + arrowW + 24f;
                float textW = lvlBoxW - 2f * (arrowW + 24f);
                string lockTag = isUnlocked ? "" : "[KHÓA] ";
                GUI.color = isUnlocked ? new Color(1.0f, 0.88f, 0.25f) : new Color(0.6f, 0.65f, 0.70f);
                GUI.Label(R(textX, lvlBoxY + 14f, textW, 48f), lockTag + lvlData.title, subHeaderCenterStyle);

                GUI.color = new Color(0.85f, 0.90f, 0.95f, 0.85f);
                GUI.Label(R(textX, lvlBoxY + 68f, textW, 44f), lvlData.subtitle, cardBtnStyle);
                GUI.color = Color.white;

                Rect nextRect = R(lvlBoxX + lvlBoxW - arrowW - 12f, lvlBoxY + 15f, arrowW, lvlBoxH - 30f);
                if (curLvl < 5 && DrawProButton(nextRect, texSecCardNormal, texSecCardHover, ">", iconStyle))
                {
                    LevelManager.Instance.SelectLevel(curLvl + 1);
                }
            }

            // Bottom-Right: PRIMARY CTA PLAY BUTTON (Tam giác Play được khắc trực tiếp trên texture)
            const float refScale = 1f; // số đo dưới đây là đơn vị ảo 1080p
            float ctaSize = VH * 0.20f;
            float ctaX = VW - ctaSize - 45f * refScale;
            float ctaY = VH - ctaSize - 50f * refScale;
            Rect ctaRect = R(ctaX, ctaY, ctaSize, ctaSize);

            if (DrawProButton(ctaRect, texCtaNormal, texCtaPressed, "", bigIconStyle))
            {
                GameManager.Instance.StartMatch();
            }

            // Bottom-Left: SECONDARY ICON DOCK (Showcase & Rules)
            float secSize = VH * 0.12f;
            float secSpacing = 18f * refScale;
            float secX = 45f * refScale;
            float secY = VH - secSize - 50f * refScale;

            // Button 1: 3D Character Showcase (Tủ Đồ)
            Rect secRect1 = R(secX, secY, secSize, secSize);
            if (DrawProButton(secRect1, texSecCardNormal, texSecCardHover, "TỦ ĐỒ", iconStyle))
            {
                showcaseRotationY = CameraFollow.FacingCameraYaw;
                GameManager.Instance.OpenShowcase();
            }

            // Button 2: Rules (?)
            Rect secRect2 = R(secX + secSize + secSpacing, secY, secSize, secSize);
            if (DrawProButton(secRect2, texSecCardNormal, texSecCardHover, "?", iconStyle))
            {
                showRulesModal = true;
            }
        }

        private void DrawRulesModal()
        {
            float modalW = Mathf.Min(VW * 0.90f, 1300f);
            float modalH = VH * 0.80f;
            float modalX = (VW - modalW) * 0.5f;
            float modalY = (VH - modalH) * 0.5f;

            GUI.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            GUI.Box(R(modalX, modalY, modalW, modalH), "", bannerStyle);

            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(R(modalX, modalY + 18, modalW, 66), "LUẬT CHƠI", headerStyle);
            GUI.color = Color.white;

            // Close button (Top-Right X Icon)
            float closeSize = 100f;
            Rect closeRect = R(modalX + modalW - closeSize - 12f, modalY + 12f, closeSize, closeSize);
            if (DrawProButton(closeRect, texSecCardNormal, texSecCardHover, "X", iconStyle))
            {
                showRulesModal = false;
            }

            string rulesText =
                "MỤC TIÊU CHIẾN THẮNG:\n" +
                "• RẮN MẸ: Luồn lách né tránh Thầy Thuốc đến hết giờ, bảo vệ đàn con phía sau.\n" +
                "• THẦY THUỐC: Vồ bắt các bé rắn con. Bắt hết con -> Thầy Thuốc thắng!\n\n" +
                "QUYẾT ĐẤU CHĂM CHĂM CHĂM (KHI BỊ VỒ BẮT):\n" +
                "• Bé Út đấu 3 hiệp với Thầy Thuốc, thắng 2 hiệp là thoát.\n" +
                "• THOÁT: Bé Út mất 0.5 tim; hết tim thì bé bị loại, bé kế tiếp vào thay với đủ tim.\n" +
                "• THUA ĐẤU: Bé Út bị loại ngay.\n\n" +
                "4 MÓN ĂN VIỆT - HÀN TIẾP SỨC:\n" +
                "• Bánh Mì: Khiên 1 lần đòn cho Rắn (10s) / Thầy thuốc Vồ Xuyên Khiên (10s).\n" +
                "• Phở: Tăng tốc bứt phá +35% (10s).\n" +
                "• Cơm Cuộn: Khổng lồ hóa +30% kích thước & sải tay cản phá (10s).\n" +
                "• Bánh Gạo Cay: Tạo phân thân ảo ảnh đánh lừa (5s).";

            GUI.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.98f);
            GUI.Box(R(modalX + 30, modalY + 125, modalW - 60, modalH - 155), rulesText, infoBoxStyle);
            GUI.backgroundColor = Color.white;
        }

        // ==============================================================
        // 2. CHARACTER SHOWCASE (BOTTOM DOCK CAROUSEL & 360 ROTATION)
        // ==============================================================
        private void DrawShowcase()
        {
            const float refScale = 1f; // số đo dưới đây là đơn vị ảo 1080p
            float topBarH = 120f;

            // Top Bar
            GUI.color = new Color(0f, 0f, 0f, 0.40f);
            GUI.DrawTexture(R(0, 0, VW, topBarH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Back to Main Menu Button
            float iconBtnSize = 100f;
            Rect backRect = R(25f * refScale, 10f * refScale, iconBtnSize, iconBtnSize);
            if (DrawProButton(backRect, texSecCardNormal, texSecCardHover, "MENU", iconStyle))
            {
                GameManager.Instance.ShowMainMenu();
            }

            // Title
            GUI.Label(R(140f, 26f, VW - 280f, 66f), "NHÂN VẬT", headerStyle);

            // Play CTA at top right (Play Icon)
            Rect playRect = R(VW - iconBtnSize - 25f * refScale, 10f * refScale, iconBtnSize, iconBtnSize);
            if (DrawProButton(playRect, texCtaNormal, texCtaPressed, "", ctaButtonStyle))
            {
                GameManager.Instance.StartMatch();
            }

            // Left Floating Lore Card
            DrawShowcaseLoreCard();

            // Center Rotation Hint
            GUI.color = new Color(1f, 1f, 1f, 0.70f);
            GUI.Label(R(0, VH * 0.70f, VW, 44), "< Vuốt để xoay >", subHeaderCenterStyle);
            GUI.color = Color.white;

            // Bottom Dock Carousel (Icon Only Tabs)
            DrawShowcaseBottomDock(refScale);
        }

        private void DrawShowcaseLoreCard()
        {
            float cardW = 560f;
            float cardH = 130f;
            float cardX = 25f;
            float cardY = 145f;

            string lore = "";
            switch (showcaseIndex)
            {
                case 0:
                    lore = "RẮN MẸ (ĐẦU ĐÀN)\nVai trò: Dẫn dắt & Giương sải tay cản phá";
                    break;
                case 1:
                    lore = "RẮN CON (THÂN RỒNG)\nVai trò: Nối đuôi liên kết & Chống văng ly tâm";
                    break;
                case 2:
                    lore = "CON ÚT (MỤC TIÊU)\nVai trò: Khúc đuôi sống còn • Đấu trí Chăm Chăm Chăm";
                    break;
                case 3:
                    lore = "THẦY THUỐC (THỢ SĂN)\nVai trò: Luồn lách bọc sườn & Vồ bắt Con Út";
                    break;
            }

            GUI.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            GUI.Box(R(cardX, cardY, cardW, cardH), lore, infoBoxStyle);
            GUI.backgroundColor = Color.white;
        }

        private void DrawShowcaseBottomDock(float refScale)
        {
            float dockH = 130f;
            float dockY = VH - dockH - 20f * refScale;
            int numTabs = 4;
            float tabSize = dockH;
            float totalW = tabSize * numTabs + (numTabs - 1) * 16f * refScale;
            float startX = (VW - totalW) * 0.5f;

            string[] tabIcons = { "MẸ", "CON", "ÚT", "THẦY" };

            for (int i = 0; i < numTabs; i++)
            {
                Rect tabRect = R(startX + i * (tabSize + 16f * refScale), dockY, tabSize, tabSize);
                bool isActive = (showcaseIndex == i);

                Texture2D cardTex = isActive ? texSecCardHover : texSecCardNormal;
                GUIStyle style = isActive ? cardBtnActiveStyle : cardBtnStyle;

                // Elevated position for active tab
                if (isActive)
                {
                    tabRect.y -= 10f * refScale;
                    tabRect.height += 10f * refScale;
                }

                if (DrawProButton(tabRect, cardTex, cardTex, tabIcons[i], iconStyle))
                {
                    showcaseIndex = i;
                    showcaseRotationY = CameraFollow.FacingCameraYaw;
                }
            }
        }

        // ==============================================================
        // 3. IN-GAME HUD (SLEEK CAPSULE TIMER, STATUS BADGES & CRYSTAL QTE)
        // ==============================================================
        private void DrawInGameHUD()
        {
            const float refScale = 1f; // số đo dưới đây là đơn vị ảo 1080p

            // 1. Top Center: Match Timer Capsule
            float time = GameManager.Instance.RemainingTime;
            int mins = Mathf.FloorToInt(time / 60f);
            int secs = Mathf.FloorToInt(time % 60f);
            string timeText = $"{mins:00}:{secs:00}";

            float timerW = 200f * refScale;
            float timerH = 55f * refScale;
            float timerX = (VW - timerW) * 0.5f;
            GUI.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.92f);
            GUI.Box(R(timerX, 15f * refScale, timerW, timerH), timeText, bannerStyle);

            // Tutorial Banner if on Level 1
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelData.hasTutorialGuide
                && GameManager.Instance.CurrentState == GameState.Chasing)
            {
                float tutW = 560f;
                float tutH = 42f * refScale;
                float tutX = (VW - tutW) * 0.5f;
                GUI.backgroundColor = new Color(0.06f, 0.12f, 0.22f, 0.92f);
                GUI.Box(R(tutX, 75f * refScale, tutW, tutH), "Giữ nút Khiên để cản Thầy Thuốc", cardBtnActiveStyle);
                GUI.backgroundColor = Color.white;
            }

            // 2. Top Left: Status Badges (Hearts & Remaining Children)
            float hearts = GameManager.Instance.CurrentHearts;
            int children = GameManager.Instance.RemainingChildren;

            // Translucent backing panel for contrast
            float badgeW = 280f * refScale;
            float badgeH = 118f * refScale;
            float badgeX = 20f * refScale;
            float badgeY = 15f * refScale;
            GUI.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            GUI.Box(R(badgeX, badgeY, badgeW, badgeH), "", bannerStyle);
            GUI.backgroundColor = Color.white;

            GUI.Label(R(badgeX + 18f, badgeY + 6f, badgeW - 36f, 56f), $"HP: {hearts:0.0} / 3.0", heartStyle);
            GUI.Label(R(badgeX + 18f, badgeY + 64f, badgeW - 36f, 46f), $"CON: {children}", subHeaderStyle);

            // Top Right: Pause Icon (Clean square icon button - ZERO TEXT)
            float pauseSize = 90f;
            Rect pauseRect = R(VW - pauseSize - 20f * refScale, 15f * refScale, pauseSize, pauseSize);
            GameState hudState = GameManager.Instance.CurrentState;
            if ((hudState == GameState.Chasing || hudState == GameState.Paused)
                && DrawProButton(pauseRect, texSecCardNormal, texSecCardHover, hudState == GameState.Paused ? "TIẾP" : "II", iconStyle))
            {
                GameManager.Instance.TogglePause();
            }

            if (GameManager.Instance.CurrentState == GameState.Paused)
            {
                DrawPauseOverlay(refScale);
                return;
            }

            // Active Food Buffs
            DrawActiveBuffs(refScale);

            // 3. QTE Duel Overlay (Zero Ugly Text on Buttons - 100% Crystal Directional Gems!)
            if (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive)
            {
                DrawDuelOverlay(refScale);
            }

            // 4. Round Result Banner (chỉ hiện giữa các hiệp khi không đang đếm lùi né đòn)
            bool isCountingDown = ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive && ChamChamDuelManager.Instance.ReactionTimeRemaining > 0f;
            if (resultMsgTimer > 0f && !string.IsNullOrEmpty(duelResultMsg) && !isCountingDown)
            {
                float resW = 760f;
                float resH = 70f;
                GUI.backgroundColor = new Color(0.06f, 0.09f, 0.14f, 0.94f);
                GUI.Box(R((VW - resW) * 0.5f, 215f, resW, resH), duelResultMsg, bannerStyle);
                GUI.backgroundColor = Color.white;
            }

            // RoundOver Reset Banner
            if (GameManager.Instance.CurrentState == GameState.RoundOver)
            {
                float roW = 460f;
                float roH = 70f;
                float roX = (VW - roW) * 0.5f;
                GUI.backgroundColor = new Color(0.10f, 0.16f, 0.26f, 0.96f);
                GUI.Box(R(roX, 90f, roW, roH), "XẾP LẠI ĐỘI HÌNH", bannerStyle);
                GUI.backgroundColor = Color.white;
            }

            // 5. Kết trận: băng tiêu đề ngắn ở trên; sau khoảng lặng điện ảnh mới hiện hai nút icon ở mép dưới,
            //    để cảnh ăn mừng 3D ở giữa màn hình không bị che.
            GameState endState = GameManager.Instance.CurrentState;
            if (endState == GameState.Victory || endState == GameState.GameOver)
            {
                bool won = endState == GameState.Victory;
                float banW = 560f;
                float banH = 80f;
                GUI.backgroundColor = won ? new Color(0.06f, 0.14f, 0.24f, 0.92f) : new Color(0.25f, 0.08f, 0.10f, 0.92f);
                GUI.Box(R((VW - banW) * 0.5f, 90f, banW, banH), "", bannerStyle);
                GUI.backgroundColor = Color.white;
                GUI.Label(R((VW - banW) * 0.5f, 100f, banW, 66f), won ? "CHIẾN THẮNG!" : "THUA CUỘC!", headerStyle);

                if (celebrationTimer >= EndCinemaSeconds)
                {
                    DrawEndGameButtons();
                }
            }
        }

        // AGENTS.md mục 6: khoảng lặng điện ảnh 2.5-3 giây trước khi hiện nút kết quả.
        private const float EndCinemaSeconds = 2.5f;

        private void DrawEndGameButtons()
        {
            float btnSize = VH * 0.18f;
            float spacing = btnSize * 0.4f;
            float startX = (VW - (btnSize * 2f + spacing)) * 0.5f;
            float btnY = VH - btnSize - 50f;

            if (DrawProButton(R(startX, btnY, btnSize, btnSize), texCtaNormal, texCtaPressed, "CHƠI LẠI", cardBtnStyle))
            {
                GameManager.Instance.StartMatch();
            }
            if (DrawProButton(R(startX + btnSize + spacing, btnY, btnSize, btnSize), texSecCardNormal, texSecCardHover, "MENU", cardBtnStyle))
            {
                GameManager.Instance.ShowMainMenu();
            }
        }

        private void DrawPauseOverlay(float refScale)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(R(0, 0, VW, VH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Hai nút icon to giữa màn hình: tiếp tục và về menu
            float btnSize = VH * 0.20f;
            float spacing = btnSize * 0.35f;
            float startX = (VW - (btnSize * 2f + spacing)) * 0.5f;
            float btnY = (VH - btnSize) * 0.5f;

            if (DrawProButton(R(startX, btnY, btnSize, btnSize), texCtaNormal, texCtaPressed, "", bigIconStyle))
            {
                GameManager.Instance.TogglePause();
            }
            if (DrawProButton(R(startX + btnSize + spacing, btnY, btnSize, btnSize), texSecCardNormal, texSecCardHover, "MENU", cardBtnStyle))
            {
                GameManager.Instance.ShowMainMenu();
            }
        }

        private void DrawActiveBuffs(float refScale)
        {
            if (BuffSystem.Instance == null) return;
            float buffW = 320f * refScale;
            float rightX = VW - buffW - 20f * refScale;
            float startY = 130f; // dưới nút tạm dừng (cao 100, cách mép trên 15)

            if (BuffSystem.Instance.SnakeHasShield)
            {
                GUI.Label(R(rightX, startY, buffW, 30f * refScale), $"KHIÊN: Bánh Mì ({BuffSystem.Instance.SnakeShieldTimer:0.0}s)", subHeaderStyle);
                startY += 30f * refScale;
            }
            if (BuffSystem.Instance.IsSnakeSpeedActive)
            {
                GUI.Label(R(rightX, startY, buffW, 30f * refScale), $"TỐC ĐỘ: Phở ({BuffSystem.Instance.SnakeSpeedTimer:0.0}s)", subHeaderStyle);
                startY += 30f * refScale;
            }
            if (BuffSystem.Instance.IsSnakeGiantActive)
            {
                GUI.Label(R(rightX, startY, buffW, 30f * refScale), $"KHỔNG LỒ: Kimbap ({BuffSystem.Instance.SnakeGiantTimer:0.0}s)", subHeaderStyle);
                startY += 30f * refScale;
            }
            if (BuffSystem.Instance.IsSnakeCloneActive)
            {
                GUI.Label(R(rightX, startY, buffW, 30f * refScale), $"PHÂN THÂN: Bánh Gạo ({BuffSystem.Instance.SnakeCloneTimer:0.0}s)", subHeaderStyle);
                startY += 30f * refScale;
            }
        }

        // ==============================================================
        // 4. QTE CHĂM CHĂM CHĂM (CRYSTAL ARROWS & SWIPE REFLEX)
        // ==============================================================
        private void DrawDuelOverlay(float refScale)
        {
            // Lời hô và tỉ số ở trên, bốn viên ngọc ở mép dưới: giữa màn hình để trống cho Thầy Thuốc và bé Út.
            float topW = Mathf.Min(960f, VW * 0.92f);
            float topX = (VW - topW) * 0.5f;
            float topY = 85f;

            GUI.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 0.92f);
            GUI.Box(R(topX, topY, topW, 115f), "", bannerStyle);
            GUI.backgroundColor = Color.white;

            GUI.color = new Color(1.0f, 0.88f, 0.25f);
            GUI.Label(R(topX, topY + 6f, topW, 58f), duelMessage, headerStyle);

            GUI.color = Color.white;
            float remTime = ChamChamDuelManager.Instance.ReactionTimeRemaining;
            int pScore = ChamChamDuelManager.Instance.PlayerScore;
            int hScore = ChamChamDuelManager.Instance.HunterScore;
            int round = Mathf.Min(ChamChamDuelManager.Instance.CurrentRound, 3);
            string subPrompt = $"HIỆP {round}/3   •   RỒNG RẮN {pScore} – {hScore} THẦY THUỐC";
            GUI.Label(R(topX, topY + 66f, topW, 42f), subPrompt, subHeaderCenterStyle);

            // Đồng hồ đếm lùi số lớn trực quan 3 - 2 - 1 và thời gian né đòn
            int cdNum = ChamChamDuelManager.Instance.CountdownNumber;
            if (cdNum > 0)
            {
                float cdW = 160f;
                float cdH = 160f;
                float cdX = (VW - cdW) * 0.5f;
                float cdY = topY + 125f;

                GUI.backgroundColor = new Color(0.05f, 0.08f, 0.15f, 0.94f);
                GUI.Box(R(cdX, cdY, cdW, cdH), "", bannerStyle);
                GUI.backgroundColor = Color.white;

                GUI.color = new Color(1.0f, 0.90f, 0.22f);
                GUI.Label(R(cdX, cdY + 12f, cdW, cdH - 45f), cdNum.ToString(), countdownStyle);
                GUI.color = new Color(0.85f, 0.90f, 1.0f, 0.90f);
                GUI.Label(R(cdX, cdY + cdH - 38f, cdW, 30f), "CHUẨN BỊ...", subHeaderCenterStyle);
                GUI.color = Color.white;
            }
            else if (remTime > 0f)
            {
                int countSec = Mathf.CeilToInt(remTime);
                float cdW = 140f;
                float cdH = 140f;
                float cdX = (VW - cdW) * 0.5f;
                float cdY = topY + 125f;

                GUI.backgroundColor = new Color(0.05f, 0.08f, 0.15f, 0.92f);
                GUI.Box(R(cdX, cdY, cdW, cdH), "", bannerStyle);
                GUI.backgroundColor = Color.white;

                GUI.color = new Color(1.0f, 0.90f, 0.22f);
                GUI.Label(R(cdX, cdY + 10f, cdW, cdH - 42f), countSec.ToString(), countdownStyle);
                GUI.color = new Color(0.85f, 0.90f, 1.0f, 0.90f);
                GUI.Label(R(cdX, cdY + cdH - 34f, cdW, 28f), "GIÂY NÉ", subHeaderCenterStyle);
                GUI.color = Color.white;

                // Chỉ dẫn hướng rõ ràng theo đòn chỉ của Thầy Thuốc
                DuelDirection hDir = ChamChamDuelManager.Instance.CurrentHunterChoice;
                string hDirName = hDir switch
                {
                    DuelDirection.Left => "◀ TRÁI",
                    DuelDirection.Right => "▶ PHẢI",
                    DuelDirection.Down => "▼ CÚI",
                    _ => "▲ LÊN"
                };

                float guideW = 600f;
                float guideH = 46f;
                float guideX = (VW - guideW) * 0.5f;
                float guideY = cdY + cdH + 12f;

                GUI.backgroundColor = new Color(0.12f, 0.05f, 0.22f, 0.88f);
                GUI.Box(R(guideX, guideY, guideW, guideH), "", bannerStyle);
                GUI.backgroundColor = Color.white;

                GUI.color = new Color(1.0f, 0.85f, 0.25f);
                GUI.Label(R(guideX, guideY + 4f, guideW, guideH), $"THẦY THUỐC CHỈ: {hDirName}  —  HÃY NÉ HƯỚNG KHÁC!", subHeaderCenterStyle);
                GUI.color = Color.white;
            }

            // Bốn viên ngọc hướng (Trái, Lên, Xuống, Phải): vừa vặn hai ngón cái trên di động
            float gemSize = VH * 0.175f;
            float gemSpacing = gemSize * 0.22f;
            float totalW = 4f * gemSize + 3f * gemSpacing;
            float gemStartX = (VW - totalW) * 0.5f;
            float gemY = VH - gemSize - 40f;

            if (DrawTextureButton(R(gemStartX, gemY, gemSize, gemSize), texQteLeft))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Left);
            }
            if (DrawTextureButton(R(gemStartX + 1f * (gemSize + gemSpacing), gemY, gemSize, gemSize), texQteUp))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Straight);
            }
            if (DrawTextureButton(R(gemStartX + 2f * (gemSize + gemSpacing), gemY, gemSize, gemSize), texQteDown))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Down);
            }
            if (DrawTextureButton(R(gemStartX + 3f * (gemSize + gemSpacing), gemY, gemSize, gemSize), texQteRight))
            {
                ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Right);
            }
        }

        // ==============================================================
        // HELPER: DRAW PRO BUTTON WITH 3D FEEDBACK & CANONICAL IMGUI EVENT HANDLING
        // ==============================================================
        private bool DrawProButton(Rect rect, Texture2D normalTex, Texture2D pressedTex, string label, GUIStyle textStyle)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            bool clicked = false;

            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        if (rect.Contains(evt.mousePosition))
                        {
                            clicked = true;
                        }
                    }
                    break;

                case EventType.Repaint:
                    bool isHover = rect.Contains(evt.mousePosition);
                    bool isHot = (GUIUtility.hotControl == controlID);
                    Texture2D tex = (isHot ? (pressedTex ?? normalTex) : (isHover ? (pressedTex ?? normalTex) : normalTex));

                    Rect drawRect = rect;
                    if (isHot)
                    {
                        drawRect.y += 3f;
                    }

                    if (tex != null)
                    {
                        GUI.DrawTexture(drawRect, tex, ScaleMode.StretchToFill, true);
                    }

                    if (!string.IsNullOrEmpty(label) && textStyle != null)
                    {
                        GUI.Label(drawRect, label, textStyle);
                    }
                    break;
            }

            if (clicked)
            {
                AudioManager.Instance?.PlaySFX(SoundType.UIClick);
            }

            return clicked;
        }

        private bool DrawTextureButton(Rect rect, Texture2D tex)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            bool clicked = false;

            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        if (rect.Contains(evt.mousePosition))
                        {
                            clicked = true;
                        }
                    }
                    break;

                case EventType.Repaint:
                    bool isHot = (GUIUtility.hotControl == controlID);
                    Rect drawRect = rect;
                    if (isHot)
                    {
                        drawRect.y += 4f;
                        drawRect.x += 2f;
                        drawRect.width -= 4f;
                        drawRect.height -= 4f;
                    }

                    if (tex != null)
                    {
                        GUI.DrawTexture(drawRect, tex, ScaleMode.ScaleToFit, true);
                    }
                    break;
            }

            if (clicked)
            {
                AudioManager.Instance?.PlaySFX(SoundType.UIClick);
            }

            return clicked;
        }
    }
}
