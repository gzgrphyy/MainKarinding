using UnityEngine;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Target drop Beat 4 (S5_HandoffBambu) — dipasang di atas sprite Abah di Teras.
    /// Slot tas UI memanggil TryDeliverAt saat drag dilepas; kalau titik layar jatuh di
    /// collider ini DAN tag masih ada di tas -> keluarkan dari tas + event delivered
    /// (TerasController memicu reaksi sprite + dialog per-tag). Urutan drag bebas.
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/NPC Handoff Drop Target")]
    [RequireComponent(typeof(Collider2D))]
    public class NPCHandoffDropTarget : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Kosong = pakai Collider2D di GO ini.")]
        [SerializeField] private Collider2D targetCollider;

        /// <summary>Tag yang berhasil diserahkan ke Abah.</summary>
        public event System.Action<BambooTag> delivered;

        private void Awake()
        {
            if (targetCollider == null) targetCollider = GetComponent<Collider2D>();
        }

        /// <summary>Coba serahkan tag di posisi layar tertentu. True = berhasil.</summary>
        public bool TryDeliverAt(BambooTag tag, Vector2 screenPosition, Camera cam)
        {
            if (targetCollider == null)
            {
                Debug.LogError("[NPCHandoffDropTarget] targetCollider NULL! Pasang BoxCollider2D di sprite Abah.");
                return false;
            }
            Camera c = cam != null ? cam : Camera.main;
            if (c == null)
            {
                Debug.LogError("[NPCHandoffDropTarget] Camera NULL!");
                return false;
            }
            Vector3 world = c.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(c.transform.position.z - transform.position.z)));
            if (!targetCollider.OverlapPoint(world))
                return false;

            BambooInventory.EnsureExists();
            if (BambooInventory.Instance == null || !BambooInventory.Instance.Remove(tag))
            {
                Debug.LogWarning($"[NPCHandoffDropTarget] Tag {tag} tidak ada di tas — drop ditolak.");
                return false;
            }
            Debug.Log($"[NPCHandoffDropTarget] {tag} diserahkan ke Abah.");
            delivered?.Invoke(tag);
            return true;
        }
    }
}
