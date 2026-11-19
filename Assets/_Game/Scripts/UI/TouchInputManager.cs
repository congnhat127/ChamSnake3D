using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using TouchPhase = UnityEngine.TouchPhase;

namespace RongRanChamCham
{
    public class TouchInputManager : MonoBehaviour
    {
        public static TouchInputManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private Vector2 moveDirection = Vector2.zero;
        [SerializeField] private bool isFacingLocked = false;

        [Header("Joystick Settings")]
        [SerializeField] private float joystickRadius = 80f;

        // Virtual Touch Tracking
        private bool isLeftTouchActive = false;
        private int leftTouchFingerId = -1;
        private Vector2 joystickCenter = Vector2.zero;
        private Vector2 joystickCurrentPos = Vector2.zero;

        private int rightTouchFingerId = -1;
        private Rect lockButtonRect;

        // Swipe tracking for Cham Cham Cham QTE
        private Vector2 swipeStartPos;
        private bool isSwipeTracking = false;

        // Professional UI Textures
        private Texture2D texActionNormal;
        private Texture2D texActionPressed;
        private Texture2D texJoystickBase;
        private Texture2D texJoystickKnob;

        public Vector2 MoveDirection => moveDirection;
        public bool IsFacingLocked => isFacingLocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadTextures();
        }

        private void LoadTextures()
        {
            texActionNormal = Resources.Load<Texture2D>("UI/btn_action_circle_normal");
            texActionPressed = Resources.Load<Texture2D>("UI/btn_action_circle_pressed");
            texJoystickBase = Resources.Load<Texture2D>("UI/joystick_base");
            texJoystickKnob = Resources.Load<Texture2D>("UI/joystick_knob");

#if UNITY_EDITOR
            if (texActionNormal == null) texActionNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_action_circle_normal.png");
            if (texActionPressed == null) texActionPressed = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/btn_action_circle_pressed.png");
            if (texJoystickBase == null) texJoystickBase = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/joystick_base.png");
            if (texJoystickKnob == null) texJoystickKnob = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Textures/UI/joystick_knob.png");
#endif
            if (texActionNormal == null) texActionNormal = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_action_circle_normal.png");
            if (texActionPressed == null) texActionPressed = LoadTextureFromDisk("Assets/_Game/Textures/UI/btn_action_circle_pressed.png");
            if (texJoystickBase == null) texJoystickBase = LoadTextureFromDisk("Assets/_Game/Textures/UI/joystick_base.png");
            if (texJoystickKnob == null) texJoystickKnob = LoadTextureFromDisk("Assets/_Game/Textures/UI/joystick_knob.png");

            // Absolute procedural fallback guarantee
            if (texActionNormal == null) texActionNormal = CreateCircleTex(new Color(0.12f, 0.70f, 0.35f, 0.95f), new Color(0.35f, 0.95f, 0.55f));
            if (texActionPressed == null) texActionPressed = CreateCircleTex(new Color(0.08f, 0.45f, 0.22f, 1f), new Color(0.25f, 0.80f, 0.40f));
            if (texJoystickBase == null) texJoystickBase = CreateCircleTex(new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0.40f));
            if (texJoystickKnob == null) texJoystickKnob = CreateCircleTex(new Color(0.2f, 0.65f, 1f, 0.85f), new Color(0.6f, 0.85f, 1f));
        }

        private static Texture2D CreateCircleTex(Color fill, Color border)
        {
            const int s = 64;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] p = new Color[s * s];
            float center = (s - 1) * 0.5f;
            float maxR = center;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d > maxR) p[y * s + x] = Color.clear;
                    else if (d > maxR - 3f) p[y * s + x] = border;
                    else p[y * s + x] = fill;
                }
            }
            t.SetPixels(p);
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
            UpdateInput();
        }

#if UNITY_EDITOR
        // Chỉ dùng cho playtest tự động trong Editor: cầu nối bơm input thay cho ngón tay.
        private bool scriptedInputActive = false;
        private Vector2 scriptedMove = Vector2.zero;
        private bool scriptedLock = false;

        public void SetScriptedInput(bool active, Vector2 move, bool facingLock)
        {
            scriptedInputActive = active;
            scriptedMove = Vector2.ClampMagnitude(move, 1f);
            scriptedLock = facingLock;
            if (!active)
            {
                moveDirection = Vector2.zero;
                isFacingLocked = false;
            }
        }
#endif

        private Vector2 virtualJoystickInput = Vector2.zero;
        private bool virtualShieldInput = false;

        public void SetVirtualJoystick(Vector2 dir)
        {
            virtualJoystickInput = dir;
        }

        public void SetVirtualShield(bool active)
        {
            virtualShieldInput = active;
        }

        private void UpdateInput()
        {
#if UNITY_EDITOR
            if (scriptedInputActive)
            {
                moveDirection = scriptedMove;
                isFacingLocked = scriptedLock;
                return;
            }
#endif
            // Nếu có input từ uGUI Canvas
            if (virtualJoystickInput.sqrMagnitude > 0.001f)
            {
                moveDirection = virtualJoystickInput;
            }
            else
            {
                // Fallback bàn phím / chuột
                ProcessKeyboardInput();
            }

            if (virtualShieldInput)
            {
                isFacingLocked = true;
            }
            else
            {
                isFacingLocked = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1);
            }
        }

        private bool ProcessTouches()
        {
            if (Input.touchCount == 0)
            {
                if (isLeftTouchActive)
                {
                    isLeftTouchActive = false;
                    leftTouchFingerId = -1;
                    moveDirection = Vector2.zero;
                }
                isFacingLocked = false;
                rightTouchFingerId = -1;
                return false;
            }

            float screenHalfWidth = Screen.width * 0.5f;
            bool anyTouchHandled = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 screenPos = t.position;
                Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

                // Swipe Gesture Detection for QTE Duel
                if (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive)
                {
                    if (t.phase == TouchPhase.Began)
                    {
                        swipeStartPos = screenPos;
                        isSwipeTracking = true;
                    }
                    else if (t.phase == TouchPhase.Ended && isSwipeTracking)
                    {
                        Vector2 delta = screenPos - swipeStartPos;
                        if (delta.magnitude > 45f)
                        {
                            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                            {
                                if (delta.x < 0) ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Left);
                                else ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Right);
                            }
                            else
                            {
                                if (delta.y > 0) ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Straight);
                                else ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Down);
                            }
                        }
                        isSwipeTracking = false;
                    }
                }

                // Check Right Side (Action Shield Button)
                if (screenPos.x >= screenHalfWidth)
                {
                    if (lockButtonRect.Contains(guiPos))
                    {
                        if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved)
                        {
                            isFacingLocked = true;
                            rightTouchFingerId = t.fingerId;
                            anyTouchHandled = true;
                        }
                    }
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        if (t.fingerId == rightTouchFingerId)
                        {
                            isFacingLocked = false;
                            rightTouchFingerId = -1;
                        }
                    }
                }

                // Check Left Side (Virtual Joystick)
                if (screenPos.x < screenHalfWidth)
                {
                    if (t.phase == TouchPhase.Began)
                    {
                        isLeftTouchActive = true;
                        leftTouchFingerId = t.fingerId;
                        joystickCenter = guiPos;
                        joystickCurrentPos = guiPos;
                        moveDirection = Vector2.zero;
                        anyTouchHandled = true;
                    }
                    else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && t.fingerId == leftTouchFingerId)
                    {
                        joystickCurrentPos = guiPos;
                        Vector2 diff = joystickCurrentPos - joystickCenter;
                        float dist = diff.magnitude;

                        if (dist > 5.0f)
                        {
                            Vector2 dir = diff.normalized;
                            float clampedDist = Mathf.Min(dist, joystickRadius);
                            moveDirection = new Vector2(dir.x, -dir.y) * (clampedDist / joystickRadius);
                        }
                        else
                        {
                            moveDirection = Vector2.zero;
                        }
                        anyTouchHandled = true;
                    }
                    else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && t.fingerId == leftTouchFingerId)
                    {
                        isLeftTouchActive = false;
                        leftTouchFingerId = -1;
                        moveDirection = Vector2.zero;
                    }
                }
            }

            return anyTouchHandled;
        }

        private bool ProcessMouseAsTouch()
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonUp(0))
            {
                if (isLeftTouchActive)
                {
                    isLeftTouchActive = false;
                    moveDirection = Vector2.zero;
                }
                return false;
            }

            Vector2 mouseScreen = Input.mousePosition;
            Vector2 mouseGui = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);
            float screenHalfWidth = Screen.width * 0.5f;

            // Swipe detection for Mouse
            if (ChamChamDuelManager.Instance != null && ChamChamDuelManager.Instance.IsDuelActive)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    swipeStartPos = mouseScreen;
                    isSwipeTracking = true;
                }
                else if (Input.GetMouseButtonUp(0) && isSwipeTracking)
                {
                    Vector2 delta = mouseScreen - swipeStartPos;
                    if (delta.magnitude > 45f)
                    {
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            if (delta.x < 0) ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Left);
                            else ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Right);
                        }
                        else
                        {
                            if (delta.y > 0) ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Straight);
                            else ChamChamDuelManager.Instance.ChooseDirection(DuelDirection.Down);
                        }
                    }
                    isSwipeTracking = false;
                }
            }

            // Check Right Lock Button
            if (lockButtonRect.Contains(mouseGui))
            {
                isFacingLocked = Input.GetMouseButton(0);
                return true;
            }

            // Check Left Virtual Joystick
            if (mouseScreen.x < screenHalfWidth)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    isLeftTouchActive = true;
                    joystickCenter = mouseGui;
                    joystickCurrentPos = mouseGui;
                    moveDirection = Vector2.zero;
                    return true;
                }
                else if (Input.GetMouseButton(0) && isLeftTouchActive)
                {
                    joystickCurrentPos = mouseGui;
                    Vector2 diff = joystickCurrentPos - joystickCenter;
                    float dist = diff.magnitude;
                    if (dist > 5.0f)
                    {
                        Vector2 dir = diff.normalized;
                        float clampedDist = Mathf.Min(dist, joystickRadius);
                        moveDirection = new Vector2(dir.x, -dir.y) * (clampedDist / joystickRadius);
                    }
                    else
                    {
                        moveDirection = Vector2.zero;
                    }
                    return true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isLeftTouchActive = false;
                moveDirection = Vector2.zero;
            }

            return false;
        }

        private void ProcessKeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                float h = 0f, v = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;

                moveDirection = new Vector2(h, v).normalized;
                isFacingLocked = kb.spaceKey.isPressed;
                return;
            }
#endif
            float rawH = Input.GetAxisRaw("Horizontal");
            float rawV = Input.GetAxisRaw("Vertical");
            moveDirection = new Vector2(rawH, rawV).normalized;
            isFacingLocked = Input.GetKey(KeyCode.Space);
        }

        private void OnGUI()
        {
            // Legacy IMGUI đã được thay thế hoàn toàn bằng hệ thống uGUI Canvas chính quy sắc nét.
        }
    }
}
