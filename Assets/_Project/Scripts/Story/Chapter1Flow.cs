using UnityEngine;
using UnityEngine.Events;
using Karinding.Dialogue;
using Karinding.Core;

#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace Karinding.Story
{
    /// <summary>
    /// Alur dialog Chapter 1 (Fase 3).
    /// Memutar S1 → S2 → onStartRhythmTutorial;
    /// OnRhythmTutorialFinished() memutar S3_Selesai → S4 → onAfterS4.
    /// Antar-modul hanya lewat UnityEvent (konvensi Bagian 2).
    /// </summary>
    public class Chapter1Flow : MonoBehaviour
    {
        [Header("Dialogue Sequences (auto-fill via Karinding → Setup → Chapter1 Scene)")]
        [SerializeField] private DialogueSequence s1_Teras;
        [SerializeField] private DialogueSequence s2_Pengenalan;
        [SerializeField] private DialogueSequence s3_Selesai;
        [SerializeField] private DialogueSequence s4_Filosofi;

        [Header("Events (sambung ke modul lain via Inspector)")]
        public UnityEvent onStartRhythmTutorial;
        public UnityEvent onAfterS4;

        [Header("Debug (Editor only)")]
        [Tooltip("Jika ON, Play di Editor selalu mulai dari S1 (abaikan save). Build tetap resume save.")]
        [SerializeField] private bool alwaysStartFromS1InEditor = true;

        private const string PrefAlwaysS1 = "karinding_always_s1_editor";

        private bool _hasStarted;
        private bool _rhythmFinished;

        private void Start()
        {
#if UNITY_EDITOR
            // DEBUG: hapus nanti - allow toggle via Karinding/Save menu. Default ON sesuai permintaan "play harus langsung S1"
            bool prefOn = PlayerPrefs.GetInt(PrefAlwaysS1, 1) == 1;
            if (prefOn || alwaysStartFromS1InEditor)
            {
                // Jangan hapus file save (biar tetap bisa test resume dengan toggle OFF), cukup abaikan HasLoadedForThisRun
                Debug.Log("[Chapter1Flow] alwaysStartFromS1InEditor ON -> abaikan save, PlayFromStart()->S1 (Scene 1 Teras)");
                if (!_hasStarted) PlayFromStart();
                return;
            }
#endif
            // Fase 4: coba resume dari save jika ada
            if (TryResumeFromSave()) return;
            if (!_hasStarted)
                PlayFromStart();
        }

#if UNITY_EDITOR
        public static void SetAlwaysS1Editor(bool on)
        {
            PlayerPrefs.SetInt(PrefAlwaysS1, on ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Chapter1Flow] alwaysStartFromS1InEditor set to {(on ? "ON (Play selalu S1)" : "OFF (resume save)")} - restart Play untuk efek");
        }
        public static bool GetAlwaysS1Editor() => PlayerPrefs.GetInt(PrefAlwaysS1, 1) == 1;

        // Dipanggil menu tanpa clear progress - cuma replay S1 di run berikutnya tanpa hapus json (opsional)
        public void DebugReplayS1()
        {
            _hasStarted = false;
            _rhythmFinished = false;
            PlayFromStart();
        }
#endif

        private bool TryResumeFromSave()
        {
            var sm = StoryManager.Instance;
            if (sm == null) sm = Object.FindFirstObjectByType<StoryManager>();
            if (sm == null || !sm.HasLoadedFromSave) return false;

            var saved = sm.CurrentState;
            Debug.Log($"[Chapter1Flow] Deteksi save: {saved}, mencoba resume");

            // Jika sudah di KeberangkatanAbah (complete), tidak auto-play, panggil onAfterS4
            if (saved == StoryState.KeberangkatanAbah)
            {
                Debug.Log("[Chapter1Flow] Save di KeberangkatanAbah, chapter selesai. Memanggil onAfterS4");
                _hasStarted = true;
                _rhythmFinished = true;
                onAfterS4?.Invoke();
                return true;
            }
            if (saved == StoryState.MasaKecil_PesanNilaiKehidupan)
            {
                Debug.Log("[Chapter1Flow] Resume di PesanNilaiKehidupan -> PlayS4");
                _hasStarted = true;
                _rhythmFinished = true;
                PlayS4();
                return true;
            }
            if (saved == StoryState.MasaKecil_BelajarMainKarinding)
            {
                Debug.Log("[Chapter1Flow] Resume di BelajarMainKarinding -> menunggu rhythm, invoke onStartRhythmTutorial");
                _hasStarted = true;
                onStartRhythmTutorial?.Invoke();
                int listeners = onStartRhythmTutorial != null ? onStartRhythmTutorial.GetPersistentEventCount() : 0;
                Debug.Log($"[Chapter1Flow] onStartRhythmTutorial listeners: {listeners}");
                if (listeners == 0)
                {
                    Debug.LogWarning("[Chapter1Flow] Tidak ada listener Rhythm (Fase 6 belum disambung). Tekan F1 di Editor untuk lanjut S3 -> S4, atau Karinding/Save/Clear Save untuk ulang dari S1. Jika ingin auto-lanjut, odkomentari baris OnRhythmTutorialFinished() di bawah.");
                    // Opsi auto-lanjut untuk QA tanpa Rhythm (uncomment jika perlu):
                    // OnRhythmTutorialFinished();
                }
                return true;
            }
            if (saved == StoryState.MasaKecil_PengenalanKarinding)
            {
                Debug.Log("[Chapter1Flow] Resume di PengenalanKarinding -> PlayS2");
                _hasStarted = true;
                PlayS2();
                return true;
            }
            if (saved == StoryState.MasaKecil_KesehariaanAbah)
            {
                Debug.Log("[Chapter1Flow] Resume di KesehariaanAbah -> PlayS1");
                _hasStarted = true;
                PlayS1();
                return true;
            }
            if (saved == StoryState.Frame_RimbaTertidur)
            {
                Debug.Log("[Chapter1Flow] Save di Frame_RimbaTertidur -> PlayFromStart");
                return false;
            }
            return false;
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Hook debug: F1 → OnRhythmTutorialFinished() (Input System, bukan legacy)
            // Hanya di Editor, supaya S3/S4 bisa diuji sebelum minigame Rhythm ada.
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                // DEBUG: hapus nanti
                Debug.Log("[Chapter1Flow] F1 ditekan → OnRhythmTutorialFinished()");
                OnRhythmTutorialFinished();
            }
        }
#endif

        public void PlayFromStart()
        {
            if (_hasStarted)
            {
                Debug.LogWarning("[Chapter1Flow] PlayFromStart dipanggil lagi, diabaikan.");
                return;
            }
            _hasStarted = true;
            _rhythmFinished = false;
            Debug.Log("[Chapter1Flow] PlayFromStart → S1");
            // Set initial state
            var sm = StoryManager.Instance != null ? StoryManager.Instance : Object.FindFirstObjectByType<StoryManager>();
            sm?.SetState(StoryState.Frame_RimbaTertidur);
            PlayS1();
        }

        private void PlayS1()
        {
            if (s1_Teras == null) { Debug.LogError("[Chapter1Flow] s1_Teras belum di-assign."); return; }
            if (DialogueManager.Instance == null) { Debug.LogError("[Chapter1Flow] DialogueManager.Instance null. Pastikan DialogueCanvas ada di scene."); return; }

            Debug.Log($"[Chapter1Flow] Memulai {s1_Teras.sequenceId} (S1_Teras)");
            var sm = StoryManager.Instance != null ? StoryManager.Instance : Object.FindFirstObjectByType<StoryManager>();
            sm?.SetState(StoryState.MasaKecil_KesehariaanAbah);
            DialogueManager.Instance.StartDialogue(s1_Teras, () =>
            {
                Debug.Log("[Chapter1Flow] S1 selesai → S2");
                // State setelah S1: siap pengenalan
                sm?.SetState(StoryState.MasaKecil_PengenalanKarinding);
                PlayS2();
            });
        }

        private void PlayS2()
        {
            if (s2_Pengenalan == null) { Debug.LogError("[Chapter1Flow] s2_Pengenalan belum di-assign."); return; }

            Debug.Log($"[Chapter1Flow] Memulai {s2_Pengenalan.sequenceId} (S2_Pengenalan)");
            var sm = StoryManager.Instance != null ? StoryManager.Instance : Object.FindFirstObjectByType<StoryManager>();
            // Pastikan state pengenalan
            sm?.SetState(StoryState.MasaKecil_PengenalanKarinding);
            DialogueManager.Instance.StartDialogue(s2_Pengenalan, () =>
            {
                Debug.Log("[Chapter1Flow] S2 selesai → onStartRhythmTutorial.Invoke()");
                sm?.SetState(StoryState.MasaKecil_BelajarMainKarinding);
                onStartRhythmTutorial?.Invoke();
                Debug.Log($"[Chapter1Flow] onStartRhythmTutorial terpanggil (listeners: {onStartRhythmTutorial?.GetPersistentEventCount()})");
                // Flow berhenti di sini menunggu OnRhythmTutorialFinished() dari modul Rhythm (atau F1 debug).
            });
        }

        /// <summary>
        /// Dipanggil oleh modul Rhythm saat tutorial selesai (atau via F1 debug di Editor).
        /// Memutar S3_Selesai → S4 → onAfterS4.
        /// </summary>
        public void OnRhythmTutorialFinished()
        {
            if (_rhythmFinished)
            {
                Debug.LogWarning("[Chapter1Flow] OnRhythmTutorialFinished dipanggil lagi, diabaikan (sudah selesai).");
                return;
            }
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            {
                Debug.LogWarning("[Chapter1Flow] Dialog masih berjalan, OnRhythmTutorialFinished diabaikan. Tunggu dialog selesai.");
                return;
            }

            _rhythmFinished = true;
            Debug.Log("[Chapter1Flow] OnRhythmTutorialFinished → S3_Selesai");
            PlayS3Selesai();
        }

        private void PlayS3Selesai()
        {
            if (s3_Selesai == null) { Debug.LogError("[Chapter1Flow] s3_Selesai belum di-assign."); return; }

            Debug.Log($"[Chapter1Flow] Memulai {s3_Selesai.sequenceId} (S3_Selesai)");
            var sm = StoryManager.Instance != null ? StoryManager.Instance : Object.FindFirstObjectByType<StoryManager>();
            // State tetap BelajarMainKarinding sampai S3 selesai, lalu ke PesanNilaiKehidupan
            DialogueManager.Instance.StartDialogue(s3_Selesai, () =>
            {
                Debug.Log("[Chapter1Flow] S3_Selesai selesai → S4");
                sm?.SetState(StoryState.MasaKecil_PesanNilaiKehidupan);
                PlayS4();
            });
        }

        private void PlayS4()
        {
            if (s4_Filosofi == null) { Debug.LogError("[Chapter1Flow] s4_Filosofi belum di-assign."); return; }

            Debug.Log($"[Chapter1Flow] Memulai {s4_Filosofi.sequenceId} (S4_Filosofi)");
            DialogueManager.Instance.StartDialogue(s4_Filosofi, () =>
            {
                Debug.Log("[Chapter1Flow] S4 selesai → onAfterS4.Invoke()");
                var sm = StoryManager.Instance != null ? StoryManager.Instance : Object.FindFirstObjectByType<StoryManager>();
                sm?.SetState(StoryState.KeberangkatanAbah);
                onAfterS4?.Invoke();
                Debug.Log($"[Chapter1Flow] Flow Chapter1 (Fase 3-4) selesai. IsPlaying={DialogueManager.Instance.IsPlaying} State={sm?.CurrentState}");
            });
        }
    }
}
