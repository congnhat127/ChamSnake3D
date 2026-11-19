using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RongRanChamCham
{
    /// <summary>
    /// Nút Giương Tay / Lá Chắn chuẩn uGUI trên Canvas.
    /// Khi giữ ngón tay/chuột: Mẹ dang tay cản phá đòn vồ của Thầy Thuốc.
    /// </summary>
    public class UIShieldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Image buttonImage;
        private Vector3 normalScale = Vector3.one;

        private void Awake()
        {
            buttonImage = GetComponent<Image>();
            normalScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (buttonImage != null)
            {
                buttonImage.sprite = UIThemeFactory.GetShieldCircleSprite(true);
            }
            transform.localScale = normalScale * 1.08f;
            TouchInputManager.Instance?.SetVirtualShield(true);
            AudioManager.Instance?.PlaySFX(SoundType.UIClick);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (buttonImage != null)
            {
                buttonImage.sprite = UIThemeFactory.GetShieldCircleSprite(false);
            }
            transform.localScale = normalScale;
            TouchInputManager.Instance?.SetVirtualShield(false);
        }

        private void OnDisable()
        {
            if (buttonImage != null)
            {
                buttonImage.sprite = UIThemeFactory.GetShieldCircleSprite(false);
            }
            transform.localScale = normalScale;
            TouchInputManager.Instance?.SetVirtualShield(false);
        }
    }
}
