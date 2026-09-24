using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Analog 4 arah (UGUI). Output Value -1..1 per sumbu.
    /// Mouse (Editor) + sentuh (Android) via EventSystem.
    /// Keyboard WASD/panah sebagai fallback via Input System.
    /// Layout base/knob 100% user-atur di Inspector.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class AnalogStickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;

        [Header("Layout - USER ATUR")]
        [Tooltip("Jari max (px) dari tengah base.")]
        [SerializeField] private float maxRadius = 110f;
        [Tooltip("Di bawah ini dianggap nol.")]
        [SerializeField] private float deadzone = 0.15f;
        [Tooltip("Keyboard fallback aktif (buat testing Editor).")]
        [SerializeField] private bool keyboardFallback = true;

        public Vector2 Value { get; private set; }

        private bool dragging;
        private Camera uiCamera;

        private void Awake()
        {
            if (baseRect == null)
                baseRect = GetComponent<RectTransform>();
            var canvas = GetComponentInParent<Canvas>();
            uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            if (knobRect != null)
                knobRect.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (dragging) return;
            if (keyboardFallback)
                Value = ReadKeyboard();
            else if (Value != Vector2.zero)
                Value = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            UpdateKnob(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateKnob(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            Value = Vector2.zero;
            if (knobRect != null)
                knobRect.anchoredPosition = Vector2.zero;
        }

        private void UpdateKnob(PointerEventData eventData)
        {
            if (baseRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    baseRect, eventData.position, uiCamera ?? eventData.pressEventCamera, out Vector2 local))
                return;
            Vector2 clamped = Vector2.ClampMagnitude(local, maxRadius);
            if (knobRect != null)
                knobRect.anchoredPosition = clamped;
            Vector2 v = clamped / maxRadius;
            Value = v.magnitude < deadzone ? Vector2.zero : v;
        }

        private static Vector2 ReadKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            Vector2 v = new Vector2(x, y);
            return v.magnitude > 1f ? v.normalized : v;
#else
            return Vector2.zero;
#endif
        }
    }
}
