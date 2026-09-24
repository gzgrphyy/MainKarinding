using UnityEngine;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Trigger pojok kanan: saat Rimba.x >= endX → callback sekali.
    /// Controller pakai ini untuk fade blackscreen akhir Scene 1.
    /// </summary>
    public class WalkOffTrigger : MonoBehaviour
    {
        [Header("Trigger - USER ATUR")]
        [Tooltip("X Rimba saat dianggap sampai pojok kanan (pojok layar ≈ 8.3).")]
        [SerializeField] private float endX = 7f;

        [Header("Runtime (diisi otomatis Beat 8)")]
        [SerializeField] private Transform rimba;

        public event System.Action arrived;

        private bool fired;

        /// <summary>Dipanggil controller saat Beat 8 mulai.</summary>
        public void Configure(Transform rimbaTransform, System.Action onArrived)
        {
            rimba = rimbaTransform;
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
            if (rimba.position.x >= endX)
            {
                fired = true;
                Debug.Log("[WalkOffTrigger] Rimba sampai pojok kanan -> blackscreen");
                arrived?.Invoke();
            }
        }
    }
}
