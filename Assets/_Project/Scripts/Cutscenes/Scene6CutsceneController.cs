using UnityEngine;
using System.Collections;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 6 — Teras side (PLAN E6).
    /// Opening: dialog "Bagian-bagian na tos aya..." (sprite dari SpriteRenderer di scene) -> LoadScene(MinigameCrafting).
    /// Resume (beat >= S6_Assembly, balik dari minigame): closing -> freeze ke Scene 7.
    /// Bark encourage/koreksi tampil DI DALAM MinigameCrafting via Scene6AbahBark (bukan di sini).
    /// </summary>
    public class Scene6CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Siang (reuse)")]
        [SerializeField] private GameObject shotRoot;
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
        [SerializeField] private string craftingSceneName = "MinigameCrafting";
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private readonly DialogueLine[] openingDialogue = new[]
        {
            new DialogueLine("Abah", "Bagian-bagian na tos aya, Ba. Ayeuna kari nyusun jeng ngantelkeun na. nepi ka ngabentuk Karinding.", "Bagian-bagiannya udah ada, Ba. Sekarang tinggal susun dan tempelin, sampai jadi bentuk Karinding."),
        };

        private readonly DialogueLine[] closingDialogue = new[]
        {
            new DialogueLine("Abah", "Tah, jadi Karindingna. Ieu jieunan Rimba sorangan.", "Nah, jadi Karindingnya. Ini buatan Rimba sendiri."),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene6);

            if (StoryState.Instance.HasReached(Ch1Beat.S6_Assembly))
            {
                Debug.Log("[Scene6Cutscene] Resume: assembly selesai -> closing");
                ExecuteClosing();
            }
            else
            {
                ExecuteOpening();
            }
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene6Cutscene] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene6Cutscene] abahSpriteSet belum di-assign!");
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
                Debug.LogError($"[Scene6Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        private void ExecuteOpening()
        {
            Debug.Log("[Scene6Cutscene] Opening - S6_Assembly");
            if (shotRoot != null) shotRoot.SetActive(true);
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);
            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(openingDialogue, () => StartCoroutine(LoadCraftingRoutine())))));
        }

        private void ExecuteClosing()
        {
            Debug.Log("[Scene6Cutscene] Closing - assembly selesai");
            if (shotRoot != null) shotRoot.SetActive(true);
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);
            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(closingDialogue, OnScene6Complete))));
        }

        private void OnScene6Complete()
        {
            Debug.Log("[Scene6Cutscene] Scene 6 selesai — lanjut Scene 7 (S7_RhythmKedua). Freeze sampai Scene 7 dibangun.");
        }

        private IEnumerator LoadCraftingRoutine()
        {
            if (audioManager != null) audioManager.FadeOutAmbient(0.35f);
            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration));
            if (!SceneExists(craftingSceneName))
            {
                Debug.LogError($"[Scene6Cutscene] Scene '{craftingSceneName}' tidak ada di Build Settings! Tambahkan MinigameCrafting.");
                yield break;
            }
            Debug.Log($"[Scene6Cutscene] LoadScene({craftingSceneName}).");
            UnityEngine.SceneManagement.SceneManager.LoadScene(craftingSceneName);
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
                    Debug.LogWarning("[Scene6Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
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
                Debug.Log("[Scene6Cutscene] Auto-lanjut (timeout, player tidak tap).");
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
                Debug.LogWarning("[Scene6Cutscene] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
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
