using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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
        }

        private void Update()
        {
            UpdateInput();
        }

        private void UpdateInput()
        {
            // 1. Check Mobile Multi-touch
            bool hasMobileTouch = ProcessTouches();

            // 2. PC / Keyboard Fallback (if no touch input)
            if (!hasMobileTouch)
            {
                ProcessKeyboardInput();
            }
        }

        private bool ProcessTouches()
        {
            if (Input.touchCount == 0)
            {
                isLeftTouchActive = false;
                return false;
            }

            float screenHalfWidth = Screen.width * 0.5f;

            // Recalculate right-side lock button position
            float btnSize = Mathf.Min(Screen.width, Screen.height) * 0.22f;
            lockButtonRect = new Rect(Screen.width - btnSize - 30f, Screen.height - btnSize - 30f, btnSize, btnSize);

            bool anyTouchHandled = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 screenPos = t.position;
                Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

                // Check Right Side (Lock Button)
                if (lockButtonRect.Contains(guiPos))
                {
                    if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved)
                    {
                        isFacingLocked = true;
                        rightTouchFingerId = t.fingerId;
                        anyTouchHandled = true;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
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
                            // Invert Y for world movement (Up on screen = +Z)
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
            // Draw Virtual Touch Controls on Screen
            float btnSize = Mathf.Min(Screen.width, Screen.height) * 0.22f;
            lockButtonRect = new Rect(Screen.width - btnSize - 35f, Screen.height - btnSize - 35f, btnSize, btnSize);

            // 1. Right Lock-Facing Button
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = isFacingLocked ? new Color(1.0f, 0.3f, 0.3f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.6f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(btnSize * 0.16f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnStyle.normal.textColor = Color.white;

            string btnLabel = isFacingLocked ? "🔒 ĐANG KHÓA\nHƯỚNG ĐẦU" : "🛡️ GIỮ ĐỂ\nKHÓA ĐẦU";
            GUI.Box(lockButtonRect, btnLabel, btnStyle);
            GUI.backgroundColor = prevColor;

            // 2. Left Virtual Joystick Visual
            if (isLeftTouchActive)
            {
                // Outer circle
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.Box(new Rect(joystickCenter.x - joystickRadius, joystickCenter.y - joystickRadius, joystickRadius * 2f, joystickRadius * 2f), "");

                // Handle knob
                GUI.color = new Color(0.2f, 0.8f, 1f, 0.85f);
                GUI.Box(new Rect(joystickCurrentPos.x - 25f, joystickCurrentPos.y - 25f, 50f, 50f), "");
                GUI.color = Color.white;
            }
        }
    }
}
