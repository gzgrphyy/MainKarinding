using UnityEngine;
using System.Collections;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 7 — Main Karinding Buatan Sendiri (PLAN E7).
    /// Opening: dialog "Sok cobaan..." (reuse s3 pegangCanggung) -> LoadScene(MinigameRhythm).
    /// Resume (beat >= S7_RhythmKedua, balik dari minigame): closing (reuse s3 bangga) -> freeze ke Scene 8.
    /// Free-play terjadi DI DALAM MinigameRhythm via StandardRhythmLauncher (freePlayMode),
    /// bukan di teras. Sprite s7_* di-skip, reuse art S3 (keputusan reuse).
    /// TODO Beat3 (S7_PilihanMainLagi): choice "Main Lagi (Hard)" / "Lanjut" via DialogueChoicePrompt
    /// setelah hard chart tersedia. Sementara langsung freeze (non-blocking per plan).
    /// </summary>
    public class Scene7CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Siang (reuse milik Scene 6)")]
        [SerializeField] private GameObject shotRoot;
        [Tooltip("UI root yang ikut di-hide oleh gate Scene 6 (entry != Scene6) — diaktifkan lagi di sini.")]
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSiang;

        [Header("UI Components (reuse)")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;

        [Header("Head Anchors")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceSiang;

        [Header("Settings")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float blackscreenDuration = 0.7f;
        [SerializeField] private string minigameSceneName = "MinigameRhythm";
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private readonly DialogueLine[] openingDialogue = new[]
        {
            new DialogueLine("Abah", "Sok cobaan, Ba. Ieu mah benteun, jieunan Rimba sorangan.", "Coba, Rimba. Ini mah beda, buatan Rimba sendiri."),
        };

        private readonly DialogueLine[] closingDialogue = new[]
        {
            new DialogueLine("Abah", "Ayeuna Rimba tos gaduh jeung tiasa maenkeun Karinding. Tapi Rimba ge kudu emut eta lain ngan saukur sora ti Rimba.", "Sekarang Rimba udah punya Karinding sendiri dan bisa memainkannya. Tapi Rimba juga harus ingat kalau itu bukan cuma suara dari Rimba."),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene7);

            if (StoryState.Instance.HasReached(Ch1Beat.S7_RhythmKedua))
            {
                Debug.Log("[Scene7Cutscene] Resume: minigame selesai -> closing");
                ExecuteClosing();
            }
            else
            {
                ExecuteOpening();
            }
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene7Cutscene] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene7Cutscene] abahSpriteSet belum di-assign!");
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
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene7Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        private void ExecuteOpening()
        {
            Debug.Log("[Scene7Cutscene] Opening - S7_RhythmKedua");
            SetBeat(Ch1Beat.S7_RhythmKedua);
            if (shotRoot != null) shotRoot.SetActive(true);
            if (uiRoot != null) uiRoot.SetActive(true);
            // Reuse art S2: duduk berdua, santai sebelum minigame.
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.duduk : null, "RimbaSpriteSet.duduk");
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.duduk : null, "AbahSpriteSet.duduk");
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);
            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(openingDialogue, () => StartCoroutine(LoadMinigameRoutine())))));
        }

        private void ExecuteClosing()
        {
            Debug.Log("[Scene7Cutscene] Closing - free-play selesai di minigame");
            SetBeat(Ch1Beat.S7_PilihanMainLagi);
            if (shotRoot != null) shotRoot.SetActive(true);
            if (uiRoot != null) uiRoot.SetActive(true);
            // Reuse art S2: duduk berdua, santai setelah minigame.
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.duduk : null, "RimbaSpriteSet.duduk");
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.duduk : null, "AbahSpriteSet.duduk");
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);
            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(closingDialogue, OnScene7Complete))));
        }

        private void OnScene7Complete()
        {
            Debug.Log("[Scene7Cutscene] Scene 7 selesai — lanjut Scene 8 (S8_SoreAbahPergi). Freeze sampai Scene 8 dibangun.");
        }

        private IEnumerator LoadMinigameRoutine()
        {
            StandardRhythmLauncher.requestedChartId = "standard";
            if (audioManager != null) audioManager.FadeOutAmbient(0.35f);
            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration));
            if (!SceneExists(minigameSceneName))
            {
                Debug.LogError($"[Scene7Cutscene] Scene '{minigameSceneName}' tidak ada di Build Settings! Tambahkan MinigameRhythm.");
                yield break;
            }
            Debug.Log($"[Scene7Cutscene] LoadScene({minigameSceneName}).");
            UnityEngine.SceneManagement.SceneManager.LoadScene(minigameSceneName);
        }

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shotRimba == null) return;
            if (sprite != null) shotRimba.sprite = sprite;
            else Debug.LogWarning($"[Scene7Cutscene] {fieldName} belum di-assign!");
        }

        private void ApplyAbahSprite(Sprite sprite, string fieldName)
        {
            if (shotAbah == null) return;
            if (sprite != null) shotAbah.sprite = sprite;
            else Debug.LogWarning($"[Scene7Cutscene] {fieldName} belum di-assign!");
        }

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
                    Debug.LogWarning("[Scene7Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
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
                Debug.Log("[Scene7Cutscene] Auto-lanjut (timeout, player tidak tap).");
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
                Debug.LogWarning("[Scene7Cutscene] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
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
    }
}
