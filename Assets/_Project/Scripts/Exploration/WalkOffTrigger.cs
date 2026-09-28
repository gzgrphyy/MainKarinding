using UnityEngine;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Trigger ujung layar: saat Rimba.x >= endX (kanan) atau <= endX (kiri) → callback sekali.
    /// Controller pakai ini untuk fade blackscreen akhir Scene 1 (kanan) dan Scene 5 (kanan brangkat, kiri pulang).
    /// </summary>
    public class WalkOffTrigger : MonoBehaviour
    {
        public enum ExitDirection { Right = 0, Left = 1 }

        [Header("Trigger - USER ATUR")]
        [Tooltip("Arah keluar yang dipantau.")]
        [SerializeField] private ExitDirection direction = ExitDirection.Right;
        [Tooltip("X Rimba saat dianggap sampai ujung (pojok layar ≈ ±8.3).")]
        [SerializeField] private float endX = 7f;

        [Header("Runtime (diisi otomatis Beat 8)")]
        [SerializeField] private Transform rimba;

        public event System.Action arrived;

        private bool fired;

        /// <summary>
        /// Dipanggil controller saat Beat 8 mulai. Pakai arah &amp; endX yang di-set di Inspector.
        /// </summary>
        public void Configure(Transform rimbaTransform, System.Action onArrived)
        {
            Configure(rimbaTransform, onArrived, direction, endX);
        }

        /// <summary>
        /// Varian runtime: controller menentukan sendiri arah keluar &amp; titik berhentinya.
        /// Dipakai Scene 5 Beat 3 (pulang dari hutan): Rimba masuk dari kanan, lalu trigger
        /// arah Left yang sama dipakai ulang untuk menandai dia sudah sampai di Abah.
        /// Nilai serialized cuma berubah di memory Play Mode (otomatis revert saat exit).
        /// </summary>
        public void Configure(Transform rimbaTransform, System.Action onArrived, ExitDirection exitDirection, float exitX)
        {
            rimba = rimbaTransform;
            direction = exitDirection;
            endX = exitX;
            if (onArrived != null)
            {
                arrived -= onArrived;
                arrived += onArrived;
            }
            fired = false;
        }

        private void Update()
        {
            if (fired || rimba == null) return;
            bool reached = direction == ExitDirection.Right
                ? rimba.position.x >= endX
                : rimba.position.x <= endX;
            if (reached)
            {
                fired = true;
                Debug.Log($"[WalkOffTrigger] Rimba sampai titik {direction} (x={endX}).");
                arrived?.Invoke();
            }
        }
    }
}
