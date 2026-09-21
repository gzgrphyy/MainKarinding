using System;
using System.Collections.Generic;
using UnityEngine;
using Karinding.Dialogue;
using Karinding.Core;

namespace Karinding.Codex
{
    public class CodexManager : MonoBehaviour
    {
        public static CodexManager Instance { get; private set; }

        [Tooltip("Daftar semua entry Codex (assign di Inspector atau via CodexGenerator)")]
        [SerializeField] private List<CodexEntry> entries = new List<CodexEntry>();

        private readonly HashSet<string> _unlockedIds = new HashSet<string>();

        public event Action<CodexEntry> OnEntryUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Subscribe ke DialogueManager jika sudah ada, atau tunggu
            TrySubscribe();
        }

        private void OnDisable()
        {
            var dm = DialogueManager.Instance;
            if (dm != null) dm.OnDialogueEvent -= HandleDialogueEvent;
        }

        private void Start()
        {
            TrySubscribe();
            LoadFromSave();
        }

        private void TrySubscribe()
        {
            var dm = DialogueManager.Instance;
            if (dm != null)
            {
                dm.OnDialogueEvent -= HandleDialogueEvent;
                dm.OnDialogueEvent += HandleDialogueEvent;
            }
        }

        private void HandleDialogueEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            // Hanya proses CODEX_* dan CHAPTER1_END
            if (!eventId.StartsWith("CODEX_") && eventId != "CHAPTER1_END" && eventId != "ABAH_IKAT_BEDOG") return;
            if (eventId.StartsWith("CODEX_"))
            {
                Unlock(eventId);
            }
            else
            {
                Debug.Log($"[CodexManager] Event non-codex diterima: {eventId}");
            }
        }

        public void Unlock(string entryId)
        {
            if (_unlockedIds.Contains(entryId))
            {
                // Sudah unlock, tidak duplikat
                return;
            }
            CodexEntry entry = entries.Find(e => e.entryId == entryId);
            if (entry == null)
            {
                // Entry belum terdaftar - tetap simpan id untuk save, log warning
                Debug.LogWarning($"[CodexManager] Entry {entryId} belum ada di list entries, tetap di-unlock (fallback).");
                _unlockedIds.Add(entryId);
                Debug.Log($"[CodexManager] Unlocked (fallback): {entryId}");
                SaveSystem.Save();
                return;
            }
            _unlockedIds.Add(entryId);
            entry.unlocked = true;
            Debug.Log($"[CodexManager] Unlocked: {entry.title} ({entry.entryId})");
            OnEntryUnlocked?.Invoke(entry);
            SaveSystem.Save();
        }

        public bool IsUnlocked(string entryId) => _unlockedIds.Contains(entryId);

        public IReadOnlyCollection<string> GetUnlockedIds() => _unlockedIds;

        public List<CodexEntry> GetAllEntries() => entries;

        public void RegisterEntries(List<CodexEntry> newEntries)
        {
            entries = newEntries ?? new List<CodexEntry>();
            // Re-apply unlocked state after register
            foreach (var id in _unlockedIds)
            {
                var e = entries.Find(x => x.entryId == id);
                if (e != null) e.unlocked = true;
            }
        }

        private void LoadFromSave()
        {
            var save = SaveSystem.Load();
            if (save == null || save.codexUnlockedIds == null) return;
            foreach (var id in save.codexUnlockedIds)
            {
                if (_unlockedIds.Contains(id)) continue;
                _unlockedIds.Add(id);
                var e = entries.Find(x => x.entryId == id);
                if (e != null) e.unlocked = true;
                Debug.Log($"[CodexManager] Loaded unlock: {id}");
            }
            if (save.codexUnlockedIds.Count > 0)
                Debug.Log($"[CodexManager] Loaded {save.codexUnlockedIds.Count} codex entries from save");
        }

        // Untuk editor generator: assign entries via code
        public void SetEntries(List<CodexEntry> list)
        {
            entries = list;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Pastikan entryId unik
            var seen = new HashSet<string>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (string.IsNullOrEmpty(e.entryId))
                    Debug.LogWarning($"[CodexManager] Entry {e.name} entryId kosong", e);
                else if (!seen.Add(e.entryId))
                    Debug.LogWarning($"[CodexManager] Duplicate entryId {e.entryId}", e);
            }
        }
#endif
    }
}
