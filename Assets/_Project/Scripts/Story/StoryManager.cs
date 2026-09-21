using System;
using UnityEngine;
using Karinding.Core;

namespace Karinding.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [SerializeField] private StoryState initialState = StoryState.Frame_RimbaTertidur;

        public StoryState CurrentState { get; private set; }
        public event Action<StoryState, StoryState> OnStateChanged;

        private bool _hasLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load save if exists
            var save = SaveSystem.Load();
            if (save != null)
            {
                CurrentState = (StoryState)save.storyState;
                _hasLoaded = true;
                Debug.Log($"[StoryManager] Loaded save state: {CurrentState}");
                // Apply translation pref from save
                PlayerPrefs.SetInt("dialogue_show_translation", save.showTranslation ? 1 : 0);
            }
            else
            {
                CurrentState = initialState;
                Debug.Log($"[StoryManager] No save, initial state: {CurrentState}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetState(StoryState newState)
        {
            if (CurrentState == newState) return;
            var old = CurrentState;
            CurrentState = newState;
            Debug.Log($"[StoryManager] State {old} -> {newState}");
            OnStateChanged?.Invoke(old, newState);
            // Auto-save
            SaveSystem.Save();
        }

        // For debug / Flow use
        public void ForceState(StoryState state)
        {
            CurrentState = state;
            Debug.Log($"[StoryManager] ForceState {state} (no event, no save)");
        }

        public bool HasLoadedFromSave => _hasLoaded;
    }
}
