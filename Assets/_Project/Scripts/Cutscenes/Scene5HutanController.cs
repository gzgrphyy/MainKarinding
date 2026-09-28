using UnityEngine;
using System.Collections;
using MainKarinding.Core;
using MainKarinding.Crafting;
using MainKarinding.Exploration;
using MainKarinding.Cutscenes;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 5 — Hutan side (PLAN E5 revisi), tinggal di Ch1_Hutan.unity.
    /// Beat 2 S5_SearchBambu: Rimba jalan bebas (analog), klik 3 rumpun bambu (no-decoy)
    /// -> tas 3/3 -> mode jalan-pulang: Rimba ke KIRI -> ujung -> blackscreen -> LoadScene(Teras).
    /// Abah tidak ada di hutan (menunggu di Teras).
    /// </summary>
    public class Scene5HutanController : MonoBehaviour
    {
        [Header("Sprite Sets (Rimba saja)")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;

        [Header("Hutan")]
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgHutanMimpi;
        [SerializeField] private GameObject explorationRoot;
        [SerializeField] private RimbaWalker rimbaWalker;
        [SerializeField] private AnalogStickController analogStick;
        [SerializeField] private WalkOffTrigger walkHomeTrigger;

        [Header("Search")]
        [SerializeField] private CraftingPuzzleController puzzleController;

        [Header("UI Components")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;
        [SerializeField] private Transform headAnchorRimba;
        [Tooltip("Toast kecil non-blocking pengganti bubble besar (bubble menghalangi layar saat cari bambu).")]
        [SerializeField] private Scene5Toast toast;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceHutan;

        [Header("Settings")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float blackscreenDuration = 0.7f;
        [SerializeField] private string terasSceneName = "Ch1_TerasRumahKebun";
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private bool walkHomePhase = false;

        private const string HintToast = "Milarian awi nu sae — ketuk rumpun bambu!";
        private const string HomeToast = "Tos kenging tilu — balik ka teras, ke kiri!";

        private void Start()
        {
            StoryState.EnsureExists();
            BambooInventory.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            AutoFind();

            SetBeat(Ch1Beat.S5_SearchBambu);
            if (bgRenderer != null && bgHutanMimpi != null) bgRenderer.sprite = bgHutanMimpi;
            if (audioManager != null && ambienceHutan != null)
                audioManager.PlayAmbient(ambienceHutan, 0.6f);

            if (puzzleController != null) puzzleController.allCollected += OnAllCollected;

            StartCoroutine(DelayedExecute(openingDelay, () =>
            {
                EnableWalking(true);
                if (toast == null) toast = Object.FindFirstObjectByType<Scene5Toast>();
                if (toast != null) toast.ShowToast(HintToast, 3f);
                else Debug.LogWarning("[Scene5Hutan] Scene5Toast belum di-setup! Tambah ToastRoot di UI_Cutscene.");
            }));
        }

        private void OnDestroy()
        {
            if (puzzleController != null) puzzleController.allCollected -= OnAllCollected;
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene5Hutan] rimbaSpriteSet belum di-assign!");
            if (puzzleController == null) puzzleController = Object.FindFirstObjectByType<CraftingPuzzleController>();
            if (puzzleController == null) Debug.LogWarning("[Scene5Hutan] CraftingPuzzleController tidak ketemu di scene!");
            if (rimbaWalker == null) rimbaWalker = Object.FindFirstObjectByType<RimbaWalker>();
            if (analogStick == null) analogStick = Object.FindFirstObjectByType<AnalogStickController>();
            if (walkHomeTrigger == null) walkHomeTrigger = Object.FindFirstObjectByType<WalkOffTrigger>();
            if (headAnchorRimba == null && rimbaWalker != null)
            {
                var t = rimbaWalker.transform.Find("HeadAnchor");
                if (t != null) headAnchorRimba = t;
            }
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        private void EnableWalking(bool enabled)
        {
            if (explorationRoot != null && !explorationRoot.activeSelf)
                explorationRoot.SetActive(true);
            if (rimbaWalker == null) return;
            if (rimbaSpriteSet != null)
                rimbaWalker.Configure(analogStick, rimbaSpriteSet.walkRight, rimbaSpriteSet.idleRight);
            rimbaWalker.Locked = !enabled;
        }

        private void OnAllCollected()
        {
            Debug.Log("[Scene5Hutan] 3/3 terkumpul -> mode jalan-pulang (ke KIRI).");
            if (toast == null) toast = Object.FindFirstObjectByType<Scene5Toast>();
            if (toast != null) toast.ShowToast(HomeToast, 2.5f);
            StartCoroutine(DelayedExecute(2.6f, StartWalkHomePhase));
        }

        private void StartWalkHomePhase()
        {
            walkHomePhase = true;
            EnableWalking(true);
            if (walkHomeTrigger != null && rimbaWalker != null)
                walkHomeTrigger.Configure(rimbaWalker.transform, OnWalkLeftArrived);
            else
                Debug.LogError("[Scene5Hutan] walkHomeTrigger/rimbaWalker belum di-setup! Set WalkOffTrigger arah Left di Inspector.");
        }

        private void OnWalkLeftArrived()
        {
            if (!walkHomePhase) return;
            walkHomePhase = false;
            if (rimbaWalker != null) rimbaWalker.Locked = true;
            Debug.Log("[Scene5Hutan] Rimba sampai ujung kiri -> balik teras.");
            StartCoroutine(LoadTerasRoutine());
        }

        private IEnumerator LoadTerasRoutine()
        {
            if (audioManager != null) audioManager.FadeOutAmbient(0.35f);
            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration));
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            bool exists = false;
            for (int i = 0; i < count; i++)
            {
                if (UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i).Contains(terasSceneName))
                { exists = true; break; }
            }
            if (!exists)
            {
                Debug.LogError($"[Scene5Hutan] Scene '{terasSceneName}' tidak ada di Build Settings!");
                yield break;
            }
            Debug.Log($"[Scene5Hutan] LoadScene({terasSceneName}) — tas terbawa (DontDestroyOnLoad).");
            UnityEngine.SceneManagement.SceneManager.LoadScene(terasSceneName);
        }

        #region Helpers (pola Scene 1-4)

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
                if (floatingDialogue != null && headAnchorRimba != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, headAnchorRimba, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene5Hutan] Tidak ada dialogue display! Skip: " + line.textSunda);
                    finished = true;
                }
                float elapsed = 0f;
                while (!finished && elapsed < dialogueTimeoutSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                yield return new WaitForSeconds(0.2f);
            }
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            onComplete?.Invoke();
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
            if (canvasGroup == null && image == null) yield break;
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

        #endregion
    }
}
