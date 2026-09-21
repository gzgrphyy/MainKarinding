using System;
using System.Collections.Generic;
using UnityEngine;
using Karinding.Dialogue;
using Karinding.Core;

namespace Karinding.Core
{
    public class AffinityManager : MonoBehaviour
    {
        public static AffinityManager Instance { get; private set; }

        // TODO: nilai tier final belum ditentukan, angka sementara
        public const int TierAsing = 0;
        public const int TierKenal = 5;
        public const int TierDekat = 10;

        [Tooltip("Skor affinity per npcId")]
        private readonly Dictionary<string, int> _scores = new Dictionary<string, int>();

        public event Action<string, int> OnAffinityUpdated; // npcId, newValue

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => TrySubscribe();
        private void OnDisable()
        {
            var dm = DialogueManager.Instance;
            if (dm != null) dm.OnAffinityChanged -= HandleAffinityChanged;
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
                dm.OnAffinityChanged -= HandleAffinityChanged;
                dm.OnAffinityChanged += HandleAffinityChanged;
            }
        }

        private void HandleAffinityChanged(string npcId, int delta)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            if (!_scores.ContainsKey(npcId)) _scores[npcId] = 0;
            _scores[npcId] += delta;
            Debug.Log($"[Affinity] {npcId} {(delta>=0?"+":"")}{delta} -> {_scores[npcId]} (tier:{GetTier(npcId)})");
            OnAffinityUpdated?.Invoke(npcId, _scores[npcId]);
            SaveSystem.Save();
        }

        public int GetScore(string npcId) => _scores.TryGetValue(npcId, out int v) ? v : 0;

        public string GetTier(string npcId)
        {
            int s = GetScore(npcId);
            if (s >= TierDekat) return "Dekat";
            if (s >= TierKenal) return "Kenal";
            return "Asing";
        }

        // TODO: angka tier final belum ditentukan
        public IReadOnlyDictionary<string,int> GetAll() => _scores;

        public void SetScore(string npcId, int value)
        {
            _scores[npcId] = value;
            OnAffinityUpdated?.Invoke(npcId, value);
            SaveSystem.Save();
        }

        private void LoadFromSave()
        {
            var save = SaveSystem.Load();
            if (save == null || save.affinity == null) return;
            foreach (var e in save.affinity)
            {
                _scores[e.npcId] = e.value;
                Debug.Log($"[Affinity] Loaded {e.npcId} = {e.value}");
            }
        }
    }
}
