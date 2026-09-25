using UnityEngine;

namespace MainKarinding.Core
{
    /// <summary>
    /// Chapter 1 beat enum — sumber kebenaran progres (PLAN Bagian D.6 / F).
    /// Dipakai konsisten Scene 1-9. Scene 2 pakai S2_*.
    /// </summary>
    public enum Ch1Beat
    {
        None = 0,

        // Scene 1
        S1_SpawnEstablishing = 10,
        S1_CuciMuka = 11,
        S1_ShotDuaMenghadapGunung = 12,
        S1_TapMenoleh = 13,
        S1_DialogGunung = 14,
        S1_HandoffKebun = 15,

        // Scene 2 — Pengenalan Karinding
        S2_AbahMainKarinding = 20,
        S2_IntroDialog = 21,
        S2_PilihanOpsi = 22,
        S2_ModelHighlight = 23,
        S2_ClosingFilosofi = 24,

        // Scene 3+
        S3_Handoff = 30,
        S3_TutorialRhythm = 31,
        S3_ClosingDialog = 32,
        S4_FilosofiBigBang = 40,
        S5_AjakHutan = 50,
        S5_SearchBambu = 51,
        S5_KembaliRumah = 52,
        S5_HandoffBambu = 53,
        S6_Assembly = 60,
        S7_RhythmKedua = 70,
        S7_FreePlay = 71,
        S7_PilihanMainLagi = 72,
        S8_SoreAbahPergi = 80,
        S9_RimbaBangun = 90,
    }

    /// <summary>
    /// Singleton DontDestroyOnLoad — progres chapter. Baru (PLAN A.2).
    /// </summary>
    public class StoryState : MonoBehaviour
    {
        public static StoryState Instance { get; private set; }

        [Header("State")]
        [SerializeField] private Ch1Beat currentBeat = Ch1Beat.None;

        public Ch1Beat CurrentBeat => currentBeat;
        public event System.Action<Ch1Beat> beatChanged;

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

        public void SetBeat(Ch1Beat beat)
        {
            currentBeat = beat;
            Debug.Log($"[StoryState] Beat -> {beat}");
            beatChanged?.Invoke(beat);
        }

        public bool HasReached(Ch1Beat beat) => currentBeat >= beat;

        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("StoryState");
            go.AddComponent<StoryState>();
        }
    }
}
