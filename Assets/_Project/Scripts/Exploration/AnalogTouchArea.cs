using UnityEngine;
using UnityEngine.EventSystems;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Area sentuh analog (mis. separuh layar kiri).
    /// Tap di area ini => analog muncul di titik jari (AnalogStickController.BeginPress),
    /// geser => knob ikut, lepas => analog hilang lagi.
    /// Komponen ini cuma jembatan event; semua logika tetap di AnalogStickController.
    /// </summary>
    public class AnalogTouchArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("Kosong = cari otomatis AnalogStickController di Canvas atasnya.")]
        [SerializeField] private AnalogStickController controller;

        private void Awake()
        {
            if (controller == null)
                controller = ResolveController();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (controller != null)
                controller.BeginPress(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (controller != null)
                controller.DragTo(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (controller != null)
                controller.EndPress(eventData);
        }

        private AnalogStickController ResolveController()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var found = canvas.GetComponentInChildren<AnalogStickController>(true);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
