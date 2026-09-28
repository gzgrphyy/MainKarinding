using UnityEngine;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Satu rumpun bambu di hutan Scene 5 (Beat 2, S5_SearchBambu).
    /// No-decoy: semua 3 hotspot wajib diambil, tidak ada jebakan/salah.
    /// Klik/tap (OnMouseDown, butuh Collider2D) -> masuk tas + lapor controller.
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/Crafting Hotspot")]
    [RequireComponent(typeof(Collider2D))]
    public class CraftingHotspot : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Tag bambu rumpun ini: Tua/Tipis/Pas.")]
        [SerializeField] private BambooTag bambuTag = BambooTag.Pas;

        [Header("Audio (TODO_SWAP_S5: sfx_gesek_bambu.wav belum ada, pakai tap_confirm sementara)")]
        [SerializeField] private AudioClip collectSfx;
        [SerializeField] private AudioSource audioSource;

        public BambooTag Tag => bambuTag;
        public bool Collected { get; private set; }

        private CraftingPuzzleController controller;

        public void Setup(CraftingPuzzleController owner, AudioClip sfx)
        {
            controller = owner;
            if (collectSfx == null) collectSfx = sfx;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void OnMouseDown()
        {
            Collect();
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;

            BambooInventory.EnsureExists();
            if (BambooInventory.Instance != null && !BambooInventory.Instance.Add(bambuTag))
                Debug.LogWarning($"[CraftingHotspot] {name}: tas menolak tag {bambuTag} (duplikat/penuh).");

            if (audioSource != null && collectSfx != null)
                audioSource.PlayOneShot(collectSfx);
            else if (collectSfx == null)
                Debug.Log("[CraftingHotspot] collectSfx belum di-assign (TODO_SWAP_S5: sfx_gesek_bambu.wav).");

            Debug.Log($"[CraftingHotspot] {name} ({bambuTag}) terkumpul.");
            if (controller != null) controller.NotifyCollected(this);

            gameObject.SetActive(false);
        }
    }
}
