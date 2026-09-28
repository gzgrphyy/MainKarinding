using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Satu slot tas UI Scene 5 (Beat 4). Menampilkan ikon bambu per-tag, bisa di-drag
    /// ke sprite Abah (NPCHandoffDropTarget). Kalau drop gagal -> ghost hancur, slot utuh.
    /// Dipasang di Image slot; butuh CanvasGroup + raycast aktif.
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/Bamboo Slot UI")]
    [RequireComponent(typeof(Image))]
    public class BambooSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Ikon per-tag (diisi TerasController)")]
        [SerializeField] private Sprite iconTua;
        [SerializeField] private Sprite iconTipis;
        [SerializeField] private Sprite iconPas;
        [SerializeField] private Sprite iconEmpty;

        [Header("Drop")]
        [Tooltip("Target di atas Abah. Kosong = auto-find di scene.")]
        [SerializeField] private NPCHandoffDropTarget dropTarget;

        [Header("Runtime (read-only)")]
        [SerializeField] private BambooTag slotTag;
        [SerializeField] private bool hasItem;

        public bool HasItem => hasItem;
        public BambooTag SlotTag => slotTag;

        private Image image;
        private GameObject ghost;
        private Canvas rootCanvas;

        private void Awake()
        {
            image = GetComponent<Image>();
            rootCanvas = GetComponentInParent<Canvas>();
            if (dropTarget == null) dropTarget = Object.FindFirstObjectByType<NPCHandoffDropTarget>();
            RefreshVisual();
        }

        public void SetIcons(Sprite tua, Sprite tipis, Sprite pas, Sprite empty)
        {
            iconTua = tua; iconTipis = tipis; iconPas = pas; iconEmpty = empty;
            RefreshVisual();
        }

        public void SetItem(BambooTag tag)
        {
            slotTag = tag;
            hasItem = true;
            RefreshVisual();
        }

        public void Clear()
        {
            hasItem = false;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (image == null) return;
            if (!hasItem) { image.sprite = iconEmpty; return; }
            image.sprite = slotTag switch
            {
                BambooTag.Tua => iconTua,
                BambooTag.Tipis => iconTipis,
                _ => iconPas,
            };
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem) return;
            if (dropTarget == null) dropTarget = Object.FindFirstObjectByType<NPCHandoffDropTarget>();
            if (rootCanvas == null) return;
            ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghost.transform.SetParent(rootCanvas.transform, false);
            var gi = ghost.GetComponent<Image>();
            gi.sprite = image.sprite;
            gi.raycastTarget = false;
            var cg = ghost.GetComponent<CanvasGroup>();
            cg.alpha = 0.85f;
            cg.blocksRaycasts = false;
            var rt = ghost.GetComponent<RectTransform>();
            rt.sizeDelta = (transform as RectTransform).sizeDelta;
            rt.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null) ghost.GetComponent<RectTransform>().position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost != null) Object.Destroy(ghost);
            ghost = null;
            if (!hasItem) return;
            if (dropTarget == null) dropTarget = Object.FindFirstObjectByType<NPCHandoffDropTarget>();
            if (dropTarget == null)
            {
                Debug.LogWarning("[BambooSlotUI] NPCHandoffDropTarget tidak ketemu di scene!");
                return;
            }
            if (dropTarget.TryDeliverAt(slotTag, eventData.position, eventData.pressEventCamera))
                Clear(); // sukses -> reaksi dipicu via event delivered di TerasController
            else
                Debug.Log($"[BambooSlotUI] Drop {slotTag} meleset — slot utuh.");
        }
    }
}
