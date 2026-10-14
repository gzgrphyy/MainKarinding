using UnityEngine;
using System.Collections.Generic;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Pengatur search-phase Scene 5 (Beat 2, S5_SearchBambu) — generik, mode Assembly menyusul di Scene 6.
    /// No-decoy: tepat 3 hotspot, semua benar. 3/3 -> OnAllCollected (controller hutan pindah ke mode jalan-pulang).
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/Crafting Puzzle Controller")]
    public class CraftingPuzzleController : MonoBehaviour
    {
        public enum CraftingMode { Search = 0, Assembly = 1 }

        [Header("Mode")]
        [SerializeField] private CraftingMode mode = CraftingMode.Search;

        [Header("Search-phase (Scene 5)")]
        [Tooltip("Kosong = auto-find semua CraftingHotspot anak.")]
        [SerializeField] private List<CraftingHotspot> hotspots = new List<CraftingHotspot>();
        [Tooltip("SFX pungut (TODO_SWAP_S5: sfx_gesek_bambu.wav). Diteruskan ke hotspot yang belum punya clip sendiri.")]
        [SerializeField] private AudioClip collectSfx;

        [Header("Runtime")]
        [SerializeField] private int collectedCount = 0;

        public CraftingMode Mode => mode;
        public int CollectedCount => collectedCount;
        public int TotalCount => hotspots.Count;
        public IReadOnlyList<CraftingHotspot> Hotspots => hotspots;

        public event System.Action allCollected;

        private void Awake()
        {
            if (hotspots.Count == 0)
                hotspots.AddRange(GetComponentsInChildren<CraftingHotspot>(true));
            foreach (var h in hotspots)
                if (h != null) h.Setup(this, collectSfx);
            Debug.Log($"[CraftingPuzzle] Mode={mode}, hotspot={hotspots.Count}");
        }

        /// <summary>Dipanggil CraftingHotspot saat terkumpul.</summary>
        public void NotifyCollected(CraftingHotspot hotspot)
        {
            collectedCount++;
            Debug.Log($"[CraftingPuzzle] Terkumpul {collectedCount}/{hotspots.Count} ({hotspot.Tag}).");
            if (collectedCount >= hotspots.Count && hotspots.Count > 0)
            {
                Debug.Log("[CraftingPuzzle] Semua bambu terkumpul (3/3).");
                allCollected?.Invoke();
            }
        }
    }
}
