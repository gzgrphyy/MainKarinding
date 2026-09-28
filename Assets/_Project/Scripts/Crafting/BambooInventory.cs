using UnityEngine;
using System.Collections.Generic;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Tas bambu Scene 5 — menyimpan tag hasil pungutan di hutan, terbawa pulang ke teras.
    /// Singleton DontDestroyOnLoad supaya survive LoadScene(Hutan -> Teras).
    /// Maks 3 isi (Tua/Tipis/Pas), tanpa duplikat.
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/Bamboo Inventory")]
    public class BambooInventory : MonoBehaviour
    {
        public static BambooInventory Instance { get; private set; }

        [Header("State (runtime)")]
        [SerializeField] private List<BambooTag> collected = new List<BambooTag>();

        public IReadOnlyList<BambooTag> Collected => collected;
        public int Count => collected.Count;
        public bool HasAll => collected.Count >= 3;

        public event System.Action<BambooTag> added;
        public event System.Action<BambooTag> removed;
        public event System.Action cleared;

        public static void EnsureExists()
        {
            if (Instance != null) return;
            Instance = Object.FindFirstObjectByType<BambooInventory>();
            if (Instance != null) return;
            var go = new GameObject("BambooInventory");
            go.AddComponent<BambooInventory>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool Contains(BambooTag tag) => collected.Contains(tag);

        /// <summary>Tambah hasil pungutan. False kalau sudah ada/penuh.</summary>
        public bool Add(BambooTag tag)
        {
            if (collected.Contains(tag) || collected.Count >= 3) return false;
            collected.Add(tag);
            Debug.Log($"[BambooInventory] +{tag} ({collected.Count}/3)");
            added?.Invoke(tag);
            return true;
        }

        /// <summary>Keluarkan dari tas (dipakai saat drop ke Abah berhasil).</summary>
        public bool Remove(BambooTag tag)
        {
            if (!collected.Remove(tag)) return false;
            Debug.Log($"[BambooInventory] -{tag} ({collected.Count}/3)");
            removed?.Invoke(tag);
            return true;
        }

        public void Clear()
        {
            collected.Clear();
            cleared?.Invoke();
        }
    }
}
