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
    /// Beat 2 S5_SearchBambu: Rimba jalan bebas (analog), 3 titik tak terlihat
    /// (marker kuning kedip, no-decoy) -> tombol Ambil Bambu saat dekat
    /// -> tas 3/3 -> mode jalan-pulang: Rimba ke KIRI -> ujung -> blackscreen -> LoadScene(Teras).
    /// Abah tidak ada di hutan (menunggu di Teras).
    ///
    /// Masuk hutan: scene ini dimuat setelah teras fade-to-black, jadi frame pertama ditahan
    /// hitam lalu fade-in — Rimba sudah berdiri di TEPI KIRI (baru datang dari teras).
    /// </summary>
    public class Scene5HutanController : MonoBehaviour
    {
        [Header("Sprite Sets (Rimba saja)")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;

        [Header("Hutan")]
        [SerializeField] private SpriteRenderer bgRenderer;
        [Tooltip("BG jalan/explore hutan saat load. File: s5_explore_path_hutan_.png")]
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

        [Header("Claim (Beat 2 - tombol Ambil, tanpa PNG bambu)")]
        [Tooltip("Tombol Ambil Bambu, muncul saat Rimba dalam radius titik yang belum di-claim.")]
        [SerializeField] private CutsceneActionButton actionButton;
        [Tooltip("Jarak X Rimba ke titik agar tombol Ambil muncul (gerak walker terkunci sumbu X).")]
        [SerializeField] private float claimRadius = 1.4f;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceHutan;

        [Header("Settings")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float blackscreenDuration = 0.7f;
        [SerializeField] private string terasSceneName = "Ch1_TerasRumahKebun";
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private bool walkHomePhase = false;
        private bool searchDone = false;
        private CraftingHotspot currentTarget;

        private const string HintToast = "Susuri jalan hutan — tekan Ambil Bambu saat tombol muncul!";
        private const string HomeToast = "Tos kenging tilu — balik ka teras, ke kiri!";

        private void Start()
        {
            StoryState.EnsureExists();
            BambooInventory.EnsureExists();
            // Scene ini dimuat tepat setelah teras fade-to-black (LoadScene dari Scene5TerasController).
            // Tahan hitam sejak frame pertama, baru fade-in di IntroRoutine supaya masuk hutan
            // terasa nyambung (Rimba "muncul" di tepi kiri setelah layar terang).
            SetBlackscreenOpaque();
            AutoFind();

            SetBeat(Ch1Beat.S5_SearchBambu);
            if (bgRenderer != null && bgHutanMimpi != null) bgRenderer.sprite = bgHutanMimpi;
            if (audioManager != null && ambienceHutan != null)
                audioManager.PlayAmbient(ambienceHutan, 0.6f);

            if (puzzleController != null) puzzleController.allCollected += OnAllCollected;

            if (actionButton == null) actionButton = Object.FindFirstObjectByType<CutsceneActionButton>();
            if (actionButton != null)
            {
                actionButton.Hide();
                actionButton.pressed += OnClaimPressed;
            }
            else Debug.LogWarning("[Scene5Hutan] CutsceneActionButton belum di-setup! Tambah tombol Ambil Bambu di UI.");

            StartCoroutine(IntroRoutine());
        }

        /// <summary>
        /// Fade-in dari hitam (sisa fade-out teras) -> baru walk phase hidup + toast hint.
        /// Overlay wajib dimatikan lagi setelah fade: Image alpha 0 masih menahan raycast
        /// ke tombol analog di bawahnya.
        /// </summary>
        private IEnumerator IntroRoutine()
        {
            yield return StartCoroutine(FadeBlackscreen(false, blackscreenDuration));
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);

            yield return new WaitForSeconds(openingDelay);

            EnableWalking(true);
            if (toast == null) toast = Object.FindFirstObjectByType<Scene5Toast>();
            if (toast != null) toast.ShowToast(HintToast, 3f);
            else Debug.LogWarning("[Scene5Hutan] Scene5Toast belum di-setup! Tambah ToastRoot di UI_Cutscene.");
        }

        /// <summary>
        /// Tahan layar hitam penuh di frame pertama. Dipakai karena scene ini dimuat tepat
        /// setelah teras fade-to-black, jadi frame pertamanya harus gelap dulu sebelum fade-in
        /// (biar tidak ada 1 frame terang yang "nyeplak" di antara dua fade).
        /// </summary>
        private void SetBlackscreenOpaque()
        {
            if (blackscreenOverlay == null) return;
            blackscreenOverlay.SetActive(true);
            var canvasGroup = blackscreenOverlay.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            var image = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                var c = image.color;
                c.a = 1f;
                image.color = c;
            }
        }

        private void OnDestroy()
        {
            if (puzzleController != null) puzzleController.allCollected -= OnAllCollected;
            if (actionButton != null) actionButton.pressed -= OnClaimPressed;
        }

        /// <summary>
        /// Polling proximity tiap frame: titik tanpa visual, claim hanya lewat tombol.
        /// Jarak X saja (gerak walker terkunci sumbu X, Y hotspot beda ~2.4 dari walker).
        /// </summary>
        private void Update()
        {
            if (searchDone || walkHomePhase) return;
            if (puzzleController == null || rimbaWalker == null || actionButton == null) return;
            var hotspots = puzzleController.Hotspots;
            if (hotspots == null) return;
            CraftingHotspot nearest = null;
            float nearestDx = float.MaxValue;
            float rimbaX = rimbaWalker.transform.position.x;
            foreach (var h in hotspots)
            {
                if (h == null || h.Collected || !h.gameObject.activeInHierarchy) continue;
                float dx = Mathf.Abs(h.transform.position.x - rimbaX);
                if (dx <= claimRadius && dx < nearestDx) { nearestDx = dx; nearest = h; }
            }
            if (nearest != currentTarget)
            {
                currentTarget = nearest;
                if (currentTarget != null) actionButton.Show();
                else actionButton.Hide();
            }
        }

        /// <summary>Tap tombol Ambil -> claim titik terdekat + toast isi tas.</summary>
        private void OnClaimPressed()
        {
            if (currentTarget == null || currentTarget.Collected) return;
            var t = currentTarget;
            currentTarget = null;
            if (actionButton != null) actionButton.Hide();
            t.Collect();
            int count = BambooInventory.Instance != null ? BambooInventory.Instance.Count : 0;
            // Klaim ke-3: lewati toast claim supaya toast pulang (HomeToast dari
            // OnAllCollected, sudah jalan duluan) tidak tertimpa ShowToast berikutnya.
            if (count >= 3) return;
            if (toast == null) toast = Object.FindFirstObjectByType<Scene5Toast>();
            if (toast != null) toast.ShowToast($"Bambu {t.Tag} masuk tas ({count}/3)", 1.6f);
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
            searchDone = true;
            currentTarget = null;
            if (actionButton != null) actionButton.Hide();
            if (toast == null) toast = Object.FindFirstObjectByType<Scene5Toast>();
            if (toast != null) toast.ShowToast(HomeToast, 4f);
            StartCoroutine(DelayedExecute(4f, StartWalkHomePhase));
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
