using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RongRanChamCham
{
    /// <summary>
    /// Tạo hiệu ứng nảy đàn hồi (Juice Bounce) mượt mà cho nút bấm uGUI.
    /// Hover: Phóng to nhẹ 1.05x. Click/Hold: Co lại 0.94x. Thả tay: Nảy về 1.0x với dao động tự nhiên.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIJuiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Action OnClickCallback;

        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.93f;
        [SerializeField] private float springSpeed = 16f;

        private Vector3 baseScale;
        private Vector3 targetScale;
        private bool isPointerOver = false;
        private bool isPressed = false;

        private void Awake()
        {
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.001f) baseScale = Vector3.one;
            targetScale = baseScale;
        }

        private void OnEnable()
        {
            targetScale = baseScale;
            transform.localScale = baseScale;
            isPointerOver = false;
            isPressed = false;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * springSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (!isPressed)
            {
                targetScale = baseScale * hoverScale;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            if (!isPressed)
            {
                targetScale = baseScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            targetScale = baseScale * pressScale;
            AudioManager.Instance?.PlaySFX(SoundType.UIClick);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            targetScale = isPointerOver ? baseScale * hoverScale : baseScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickCallback?.Invoke();
        }
    }
}
