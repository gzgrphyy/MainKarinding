using UnityEngine;
using System.Collections;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 3 — Belajar Memainkan Karinding (PLAN E3).
    /// Scene file: Ch1_TerasRumahKebun.unity, pakai Shot_TerasSiang (sama dengan Scene 2).
    /// 3 beat: S3_Handoff > S3_TutorialRhythm > S3_ClosingDialog.
    ///
    /// Perpindahan ke minigame pakai LoadScene (replace penuh, bukan additive):
    /// setelah Beat 2, scene ini dibongkar dan TutorialMinigameRhythm jadi scene utama.
    /// StoryState (DontDestroyOnLoad) membawa currentBeat + EntryPoint, jadi saat teras
    /// di-load ulang controller ini langsung rute ke Beat 3 tanpa mengulang Beat 1-2.
    /// </summary>
    public class Scene3CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Siang (reuse Shot_TerasSiang milik Scene 2)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSiang;

        [Header("UI Components (reuse milik Scene 2)")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;

        [Header("Head Anchors")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceSiang;
        [Tooltip("Clip dengung karinding. Hanya _low yang tersedia; vary volume/pitch, lihat beatNoteClipOverride.")]
        [SerializeField] private AudioClip dengungLow;
        [Tooltip("Loop dengung pelan selama Beat 1. Auto-add kalau kosong.")]
        [SerializeField] private AudioSource karindingLoopSource;

        [Header("Scene 3 Settings")]
        [SerializeField] private float openingDelay = 1f;
        [Tooltip("Fade ambient sebelum pindah ke scene tutorial.")]
        [SerializeField] private float ambientFadeOutSeconds = 0.35f;
        [Tooltip("Durasi blackscreen sebelum LoadScene.")]
        [SerializeField] private float blackscreenDuration = 0.7f;
        [Tooltip("Nama scene minigame. Harus ada di Build Settings.")]
        [SerializeField] private string tutorialSceneName = "TutorialMinigameRhythm";
        [Tooltip("Anti-stuck: player dianggap selesai beat dialog meski tidak tap.")]
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private int currentBeat = 0;
        private bool cutsceneActive = false;

        // ---- Dialog verbatim PLAN E3 (3 baris Abah + 2 narasi) ----

        /// Beat 1 — handoff, 1 baris baru (naskah asli cuma punya 2 baris).
        private readonly DialogueLine[] beat1Dialogue = new[]
        {
            new DialogueLine("Abah", "Sok cepeung, Ba. Ieu karinding Abah, ayeuna giliran Rimba nyobaan.", "Peganglah. Ini karinding Abah, sekarang giliran Rimba mencoba."),
            new DialogueLine("", "", ""), // narasi: Abah menyodorkan karinding ke arah Rimba
        };

        /// Beat 2 — tampil SEBELUM minigame sebagai encouragement, bukan interupsi mid-gameplay.
        private readonly DialogueLine[] beat2Dialogue = new[]
        {
            new DialogueLine("Abah", "Ulah maksa, Rimba. Lain kuat na leungeun nu di penta, tapi tenang jeung sabar na haté.", "Jangan maksa, Rimba. Bukan kekuatan tangan yang diminta, tapi sabar dan ketenangan hati."),
        };

        /// Beat 3 — setelah tutorial selesai, back di teras.
        private readonly DialogueLine[] beat3Dialogue = new[]
        {
            new DialogueLine("", "", ""), // narasi: Rimba tersenyum lebar, bangga dengan pencapaiannya sendiri
            new DialogueLine("Abah", "Omat eta lain sora Rimba, tapi hasil tina getaran nu di barengan ku resonansi.", "Ingat, itu bukan suara Rimba, tapi hasil dari getaran yang dibarengi dengan resonansi."),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            AutoFind();

            // Tandai entry point supaya gate Scene 1/2 tetap menutup saat scene ini di-load ulang.
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene3);

            cutsceneActive = true;
            // Beat 3 kalau tutorial sudah pernah selesai (currentBeat >= S3_TutorialRhythm).
            if (StoryState.Instance.HasReached(Ch1Beat.S3_TutorialRhythm))
            {
                Debug.Log("[Scene3Cutscene] Resume: tutorial sudah selesai -> langsung Beat 3");
                StartCoroutine(DelayedExecute(openingDelay, ExecuteBeat3));
            }
            else
            {
                StartCutscene();
            }
        }

        public void StartCutscene()
        {
            ExecuteBeat1();
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene3Cutscene] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene3Cutscene] abahSpriteSet belum di-assign!");

            if (headAnchorRimba == null)
            {
                var t = transform.Find("Shot_TerasSiang/Char_Rimba/HeadAnchor");
                if (t != null) headAnchorRimba = t;
            }
            if (headAnchorAbah == null)
            {
                var t = transform.Find("Shot_TerasSiang/Char_Abah/HeadAnchor");
                if (t != null) headAnchorAbah = t;
            }
            if (karindingLoopSource == null)
            {
                karindingLoopSource = GetComponent<AudioSource>();
                if (karindingLoopSource == null) karindingLoopSource = gameObject.AddComponent<AudioSource>();
                karindingLoopSource.playOnAwake = false;
            }
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene3Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            currentBeat = (int)beat;
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        // Beat 1 — Abah menyerahkan karinding (dialog/narasi only, tanpa sprite handoff)
        private void ExecuteBeat1()
        {
            Debug.Log("[Scene3Cutscene] Beat 1 - S3_Handoff");
            SetBeat(Ch1Beat.S3_Handoff);

            if (shotRoot != null) shotRoot.SetActive(true);
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.duduk : null, "RimbaSpriteSet.duduk");
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.duduk : null, "AbahSpriteSet.duduk");
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);

            // Dengung khas mengalun pelan, loop sampai pindah ke minigame.
            if (karindingLoopSource != null && dengungLow != null)
            {
                karindingLoopSource.clip = dengungLow;
                karindingLoopSource.volume = 0.35f;
                karindingLoopSource.loop = true;
                karindingLoopSource.Play();
            }
            else if (dengungLow == null)
            {
                Debug.LogWarning("[Scene3Cutscene] dengungLow belum di-assign! Assign sfx_karinding_dengung_low.wav");
            }

            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(beat1Dialogue, ExecuteBeat2))));
        }

        // Beat 2 — swap ke pose pegang karinding, dialog encouragement, lalu pindah ke scene tutorial
        private void ExecuteBeat2()
        {
            Debug.Log("[Scene3Cutscene] Beat 2 - S3_TutorialRhythm");
            SetBeat(Ch1Beat.S3_TutorialRhythm);
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.pegangCanggung : null, "RimbaSpriteSet.pegangCanggung");
            StartCoroutine(PlayDialogueSequence(beat2Dialogue, () => StartCoroutine(LoadTutorialRoutine())));
        }

        // Beat 3 — kembali dari tutorial, closing dialog
        private void ExecuteBeat3()
        {
            Debug.Log("[Scene3Cutscene] Beat 3 - S3_ClosingDialog");
            SetBeat(Ch1Beat.S3_ClosingDialog);
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.bangga : null, "RimbaSpriteSet.bangga");
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.senyumTipis : null, "AbahSpriteSet.senyumTipis");
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            if (shotRoot != null) shotRoot.SetActive(true);

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);
            if (karindingLoopSource != null && dengungLow != null)
            {
                karindingLoopSource.volume = 0.22f;
                if (!karindingLoopSource.isPlaying) karindingLoopSource.Play();
            }

            StartCoroutine(PlayDialogueSequence(beat3Dialogue, OnScene3Complete));
        }

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shotRimba == null) return;
            if (sprite != null) shotRimba.sprite = sprite;
            else Debug.LogWarning($"[Scene3Cutscene] {fieldName} belum di-assign!");
        }

        private void ApplyAbahSprite(Sprite sprite, string fieldName)
        {
            if (shotAbah == null) return;
            if (sprite != null) shotAbah.sprite = sprite;
            else Debug.LogWarning($"[Scene3Cutscene] {fieldName} belum di-assign!");
        }

        // Fade audio + blackscreen, lalu LoadScene (replace penuh)
        private IEnumerator LoadTutorialRoutine()
        {
            cutsceneActive = false;

            if (karindingLoopSource != null && karindingLoopSource.isPlaying)
                karindingLoopSource.Stop();
            if (audioManager != null) audioManager.FadeOutAmbient(ambientFadeOutSeconds);

            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration));

            if (string.IsNullOrEmpty(tutorialSceneName))
            {
                Debug.LogError("[Scene3Cutscene] tutorialSceneName kosong — stuck di Beat 2. Assign 'TutorialMinigameRhythm'.");
                yield break;
            }
            if (!SceneExists(tutorialSceneName))
            {
                Debug.LogError($"[Scene3Cutscene] Scene '{tutorialSceneName}' tidak ada di Build Settings — stuck di Beat 2. Tambah lewat File > Build Settings.");
                yield break;
            }

            Debug.Log($"[Scene3Cutscene] LoadScene({tutorialSceneName}) — StoryState tetap hidup (DontDestroyOnLoad).");
            UnityEngine.SceneManagement.SceneManager.LoadScene(tutorialSceneName);
        }

        private void OnScene3Complete()
        {
            cutsceneActive = false;
            if (karindingLoopSource != null && karindingLoopSource.isPlaying)
                karindingLoopSource.Stop();
            Debug.Log("[Scene3Cutscene] Scene 3 selesai — lanjut Scene 4 (S4_FilosofiBigBang). Freeze di sini sampai Scene 4 dibangun.");
        }

        #region Helpers (pola Scene 1 & 2)

        private static bool SceneExists(string name)
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains(name)) return true;
            }
            return false;
        }

        private IEnumerator PlayDialogueSequence(DialogueLine[] lines, System.Action onComplete)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.textSunda))
                {
                    yield return new WaitForSeconds(0.8f);
                    continue;
                }
                bool finished = false;
                Transform anchor = ResolveAnchor(line.speaker);
                if (floatingDialogue != null && anchor != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene3Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
                    finished = true;
                }
                yield return StartCoroutine(WaitDialogueOrTimeout(() => finished, dialogueTimeoutSeconds));
                yield return new WaitForSeconds(0.2f);
            }
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            onComplete?.Invoke();
        }

        private IEnumerator WaitDialogueOrTimeout(System.Func<bool> isDone, float timeout)
        {
            float elapsed = 0f;
            while (!isDone() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!isDone())
                Debug.Log("[Scene3Cutscene] Auto-lanjut (timeout, player tidak tap).");
        }

        private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private IEnumerator FadeBlackscreen(bool fadeIn, float duration)
        {
            if (blackscreenOverlay == null) yield break;

            var canvasGroup = blackscreenOverlay.GetComponent<CanvasGroup>();
            var image = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
            if (canvasGroup == null && image == null)
            {
                Debug.LogWarning("[Scene3Cutscene] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
                yield break;
            }

            blackscreenOverlay.SetActive(true);
            float start = fadeIn ? 0f : 1f;
            float end = fadeIn ? 1f : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, end, Mathf.Clamp01(t / Mathf.Max(0.01f, duration)));
                if (canvasGroup != null) canvasGroup.alpha = a;
                if (image != null)
                {
                    var c = image.color;
                    c.a = a;
                    image.color = c;
                }
                yield return null;
            }
            // Selesai di alpha 1 (fade in) atau 0 (fade out). Objek dibiarkan sesuai alpha terakhir.
        }

        [System.Serializable]
        private struct DialogueLine
        {
            public string speaker;
            public string textSunda;
            public string textIndo;
            public DialogueLine(string speaker, string sunda, string indo)
            {
                this.speaker = speaker;
                this.textSunda = sunda;
                this.textIndo = indo;
            }
        }

        #endregion
    }
}
