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
    /// Mode Auto-Hide: saat diam analog tidak kelihatan, saat area sentuh dipencet
    /// analog muncul di titik jari, lalu hilang lagi setelah dilepas.
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

        [Header("Auto-Hide - USER ATUR")]
        [Tooltip("Kalau aktif: diam = analog hilang, dipencet = analog muncul.")]
        [SerializeField] private bool hideWhenIdle = false;
        [Tooltip("Analog pindah ke titik jari/tap (floating). Kalau mati, tetap di posisi awal.")]
        [SerializeField] private bool appearAtPressPosition = false;
        [Tooltip("Area sentuh yang nerima tap/drag (mis. 1/2 layar kiri). Kosong = objek ini sendiri.")]
        [SerializeField] private RectTransform touchArea;
        [Tooltip("Analog ditahan supaya seluruh lingkaran tetap di dalam rect ini. Kosong = canvas-nya.")]
        [SerializeField] private RectTransform keepInsideRect;
        [Tooltip("Lama fade muncul/hilang (detik). 0 = instan.")]
        [SerializeField] private float fadeDuration = 0.12f;
        [Tooltip("Kosong = diambil otomatis dari objek ini.")]
        [SerializeField] private CanvasGroup canvasGroup;

        public Vector2 Value { get; private set; }

        private bool dragging;
        private bool restCaptured;
        private Vector2 restPosition;
        private Camera uiCamera;
        private float targetAlpha = 1f;

        private void Awake()
        {
            if (baseRect == null)
                baseRect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            var canvas = GetComponentInParent<Canvas>();
            uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;

            if (baseRect != null && !restCaptured)
            {
                restPosition = baseRect.anchoredPosition;
                restCaptured = true;
            }

            if (knobRect != null)
                knobRect.anchoredPosition = Vector2.zero;

            targetAlpha = hideWhenIdle ? 0f : 1f;
            if (canvasGroup != null)
                canvasGroup.alpha = targetAlpha;
        }

        private void Update()
        {
            // Fade show/hide. Komponen TETAP aktif (cuma alpha) supaya Value tetap
            // terbaca RimbaWalker walau analog sedang tidak kelihatan.
            if (canvasGroup != null)
            {
                if (fadeDuration <= 0f)
                    canvasGroup.alpha = targetAlpha;
                else if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime / fadeDuration);
            }

            if (dragging) return;
            if (keyboardFallback)
                Value = ReadKeyboard();
            else if (Value != Vector2.zero)
                Value = Vector2.zero;
        }

        /// <summary>Area sentuh dipencet: analog muncul (dan pindah ke titik jari kalau floating).</summary>
        public void BeginPress(PointerEventData eventData)
        {
            if (eventData == null) return;

            dragging = true;
            targetAlpha = 1f;

            if (appearAtPressPosition)
                MoveBaseToScreenPoint(eventData.position, eventData.pressEventCamera);

            UpdateKnob(eventData);
        }

        /// <summary>Jari digeser.</summary>
        public void DragTo(PointerEventData eventData)
        {
            if (eventData == null) return;
            if (appearAtPressPosition && !dragging)
                MoveBaseToScreenPoint(eventData.position, eventData.pressEventCamera);
            UpdateKnob(eventData);
        }

        /// <summary>Jari dilepas: analog hilang lagi.</summary>
        public void EndPress(PointerEventData eventData)
        {
            dragging = false;
            Value = Vector2.zero;
            if (knobRect != null)
                knobRect.anchoredPosition = Vector2.zero;
            if (!appearAtPressPosition && baseRect != null && restCaptured)
                baseRect.anchoredPosition = restPosition;
            if (hideWhenIdle)
                targetAlpha = 0f;
        }

        // Kalau objek ini sendiri yang jadi area sentuh (tanpa AnalogTouchArea).
        public void OnPointerDown(PointerEventData eventData) { BeginPress(eventData); }
        public void OnDrag(PointerEventData eventData) { DragTo(eventData); }
        public void OnPointerUp(PointerEventData eventData) { EndPress(eventData); }

        /// <summary>Pindahkan base ke titik screen (mode floating). Hasil di-clamp di dalam keepInsideRect.</summary>
        private void MoveBaseToScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransform parent = baseRect != null ? baseRect.parent as RectTransform : null;
            if (parent == null) return;

            Camera cam = uiCamera != null ? uiCamera : eventCamera;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out local))
                return;

            local = ClampInsideBounds(local, parent);
            baseRect.anchoredPosition = local - AnchorReference(parent, baseRect);
        }

        /// <summary>Tahan supaya seluruh lingkaran base tetap di dalam keepInsideRect.</summary>
        private Vector2 ClampInsideBounds(Vector2 localPoint, RectTransform parent)
        {
            RectTransform bounds = keepInsideRect != null ? keepInsideRect : parent;
            if (bounds == null) return localPoint;

            Vector3[] corners = new Vector3[4];
            bounds.GetWorldCorners(corners);
            Vector2 bMin = parent.InverseTransformPoint(corners[0]);
            Vector2 bMax = parent.InverseTransformPoint(corners[2]);

            Vector2 half = baseRect.rect.size * 0.5f;
            float minX = bMin.x + half.x;
            float maxX = bMax.x - half.x;
            float minY = bMin.y + half.y;
            float maxY = bMax.y - half.y;
            if (minX > maxX) minX = maxX = (bMin.x + bMax.x) * 0.5f;
            if (minY > maxY) minY = maxY = (bMin.y + bMax.y) * 0.5f;

            return new Vector2(Mathf.Clamp(localPoint.x, minX, maxX), Mathf.Clamp(localPoint.y, minY, maxY));
        }

        /// <summary>Titik acuan anchoredPosition di dalam parent (untuk anchor non-stretch).</summary>
        private static Vector2 AnchorReference(RectTransform parent, RectTransform child)
        {
            return (Vector2)parent.rect.min + Vector2.Scale(child.anchorMin, parent.rect.size);
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
