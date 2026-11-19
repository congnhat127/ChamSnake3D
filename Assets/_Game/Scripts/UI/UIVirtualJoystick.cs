using UnityEngine;
using UnityEngine.EventSystems;

namespace RongRanChamCham
{
    /// <summary>
    /// Cần điều khiển ảo chuẩn uGUI (Virtual Joystick) trên Canvas.
    /// Hỗ trợ kéo vuốt mượt mà, tự động hồi tâm đàn hồi, gửi tín hiệu trực tiếp sang TouchInputManager.
    /// </summary>
    public class UIVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private float handleRange = 75f;

        private RectTransform baseRect;
        private Canvas parentCanvas;
        private Vector2 inputVector = Vector2.zero;

        private void Awake()
        {
            baseRect = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void SetKnob(RectTransform knob, float range)
        {
            knobRect = knob;
            handleRange = range;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? parentCanvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, cam, out Vector2 localPoint))
            {
                inputVector = Vector2.ClampMagnitude(localPoint / handleRange, 1.0f);
                if (knobRect != null)
                {
                    knobRect.anchoredPosition = inputVector * handleRange;
                }
                TouchInputManager.Instance?.SetVirtualJoystick(inputVector);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;
            if (knobRect != null)
            {
                knobRect.anchoredPosition = Vector2.zero;
            }
            TouchInputManager.Instance?.SetVirtualJoystick(Vector2.zero);
        }

        private void OnDisable()
        {
            inputVector = Vector2.zero;
            if (knobRect != null)
            {
                knobRect.anchoredPosition = Vector2.zero;
            }
            TouchInputManager.Instance?.SetVirtualJoystick(Vector2.zero);
        }
    }
}
