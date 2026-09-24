using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MainKarinding.Exploration;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Main controller for Scene 1 Cutscene (Teras Rumah, Pagi).
    /// Orchestrates 8 beats following "A Space for the Unbound" style.
    /// SEPARATED from PlayerMovementController/Exploration Camera.
    /// </summary>
    public class Scene1CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot 1 - Wide Establishing")]
        [SerializeField] private GameObject shot1Root;
        [SerializeField] private SpriteRenderer shot1Rimba;
        [SerializeField] private SpriteRenderer shot1Abah;

        [Header("Shot 2 - Look-out")]
        [SerializeField] private GameObject shot2Root;
        [SerializeField] private SpriteRenderer shot2Rimba;
        [SerializeField] private SpriteRenderer shot2Abah;

        [Header("UI Components")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private CutsceneActionButton actionButton;
        [SerializeField] private GameObject blackscreenOverlay;
        [Tooltip("Tap fullscreen dialog. Auto-find kalau kosong. Dimatikan paksa saat Beat 5 biar tidak menelan tap Lihat.")]
        [SerializeField] private GameObject tapDetector;

        [Header("Head Anchors (floating bubble follow - USER bisa geser di Scene View)")]
        [Tooltip("Shot1: kepala Rimba & Abah. Auto-find kalau kosong.")]
        [SerializeField] private Transform headAnchorShot1Rimba;
        [SerializeField] private Transform headAnchorShot1Abah;
        [Tooltip("Shot2: kepala Rimba & Abah.")]
        [SerializeField] private Transform headAnchorShot2Rimba;
        [SerializeField] private Transform headAnchorShot2Abah;

        [Header("Animation")]
        [SerializeField] private CutsceneSpriteSwapper rimbaSpriteSwapper;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambientMorning;
        [SerializeField] private AudioClip sfxTransitionBlackscreen;

        [Header("Exploration Handoff")]
        [SerializeField] private GameObject explorationRoot;
        [Tooltip("Diisi otomatis saat setup scene Beat 8 (boleh kosong).")]
        [SerializeField] private RimbaWalker rimbaWalker;
        [SerializeField] private AbahAutoWalker abahWalker;
        [SerializeField] private WalkOffTrigger walkTrigger;
        [SerializeField] private AnalogStickController analogStick;

        private Vector3 spawnRimbaPos;
        private Vector3 spawnAbahPos;
        private bool spawnCaptured;

        [Header("Settings")]
        [SerializeField] private float blackscreenDuration = 0.7f;
        [Tooltip("Kalau true, Beat 8 loop balik ke Beat 1 buat testing. Matikan kalau exploration sudah siap.")]
        [SerializeField] private bool loopForTesting = true;

        private int currentBeat = 0;
        private bool cutsceneActive = false;

        // Dialogue data for Beat 2 & Beat 7
        private readonly DialogueLine[] beat2Dialogue = new[]
        {
            new DialogueLine("Abah", "Baba tos gugah gening ? Sok gera wawasuh heula", "Baba udah bangun ternyata? Sana cuci muka dulu")
        };

        private readonly DialogueLine[] beat7Dialogue = new[]
        {
            new DialogueLine("Abah", "Aya jalan nu kudu dilakonan ku nyalira, Ba. Abah keur ngora ge kitu", "Ada jalan yang harus ditempuh sendiri, Ba. Ketika Abah muda juga begitu"),
            new DialogueLine("Rimba", "Maksudna kumaha bah ?", "Maksudnya gimana bah?"),
            new DialogueLine("Abah", "Moal karasa ayeuna. Engke ge ngarti sorangan", "Ga bakal kerasa sekarang. Nanti juga ngerti sendiri."),
            new DialogueLine("Rimba", "", ""), // Kebingungan, no dialogue - hanya reaksi visual
            new DialogueLine("Abah", "Tos tong di pikiran teuing. Ayeuna mah urang ka kebon we hayu, bisi kaburu panas", "Udah jangan terlalu dipikirin. Sekarang kita ke kebon aja, takutnya keburu panas"),
            new DialogueLine("Rimba", "Muhun bah, Hayu", "Iya bah, Ayo")
        };

        private void Start()
        {
            // Initial setup
            if (blackscreenOverlay != null)
                blackscreenOverlay.SetActive(false);

            if (explorationRoot != null)
                explorationRoot.SetActive(false);

            AutoFindHeadAnchors();

            // Subscribe Beat 5 sekali di sini (event permanen, anti callback-null)
            if (actionButton != null)
            {
                actionButton.pressed -= ExecuteBeat6;
                actionButton.pressed += ExecuteBeat6;
            }

            // Start cutscene
            StartCutscene();
        }

        private void OnDestroy()
        {
            if (actionButton != null)
                actionButton.pressed -= ExecuteBeat6;
        }

        private void AutoFindHeadAnchors()
        {
            if (headAnchorShot1Rimba == null)
                headAnchorShot1Rimba = transform.Find("Shot1_WideEstablishing/Char_Rimba/HeadAnchor");
            if (headAnchorShot1Abah == null)
                headAnchorShot1Abah = transform.Find("Shot1_WideEstablishing/Char_Abah/HeadAnchor");
            if (headAnchorShot2Rimba == null)
                headAnchorShot2Rimba = transform.Find("Shot2_Lookout/Char_Rimba/HeadAnchor");
            if (headAnchorShot2Abah == null)
                headAnchorShot2Abah = transform.Find("Shot2_Lookout/Char_Abah/HeadAnchor");
            if (tapDetector == null)
            {
                Transform tap = transform.Find("UI_Cutscene/TapDetector");
                if (tap != null)
                    tapDetector = tap.gameObject;
            }
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLower().Contains("rimba");
            // Beat 2 pakai Shot1, Beat 7 pakai Shot2
            Transform anchor = currentBeat == 2
                ? (isRimba ? headAnchorShot1Rimba : headAnchorShot1Abah)
                : (isRimba ? headAnchorShot2Rimba : headAnchorShot2Abah);

            if (anchor == null)
                Debug.LogError($"[Scene1Cutscene] Anchor NULL untuk speaker '{speaker}' (Beat {currentBeat})! Assign di Inspector Scene1_Cutscene.");
            else
                Debug.Log($"[Scene1Cutscene] Dialog '{speaker}' anchor={anchor.name} pos={anchor.position}");

            return anchor;
        }

        /// <summary>
        /// Start Scene 1 cutscene from Beat 1.
        /// </summary>
        public void StartCutscene()
        {
            cutsceneActive = true;
            currentBeat = 1;

            // Lock player movement (placeholder - will integrate with PlayerMovementController later)
            // PlayerMovementController.Instance?.LockMovement();

            ExecuteBeat1();
        }

        #region Beat Execution

        /// <summary>
        /// Beat 1 - Spawn characters in Shot 1.
        /// </summary>
        private void ExecuteBeat1()
        {
            Debug.Log("[Scene1Cutscene] Beat 1 - Spawn");

            // Activate Shot 1, deactivate Shot 2
            if (shot1Root != null) shot1Root.SetActive(true);
            if (shot2Root != null) shot2Root.SetActive(false);

            // Set initial sprites
            if (shot1Rimba != null && rimbaSpriteSet != null)
                shot1Rimba.sprite = rimbaSpriteSet.idlePintu;

            if (shot1Abah != null && abahSpriteSet != null)
                shot1Abah.sprite = abahSpriteSet.idleTerasTalking;

            // Play ambient audio
            if (audioManager != null && ambientMorning != null)
                audioManager.PlayAmbient(ambientMorning, 0.6f);

            // Auto-advance to Beat 2 after 1 second
            StartCoroutine(DelayedExecute(1f, ExecuteBeat2));
        }

        /// <summary>
        /// Beat 2 - Dialog pertama dari Abah.
        /// </summary>
        private void ExecuteBeat2()
        {
            Debug.Log("[Scene1Cutscene] Beat 2 - Dialog Pertama");

            currentBeat = 2;

            // Show dialogue
            StartCoroutine(PlayDialogueSequence(beat2Dialogue, ExecuteBeat3));
        }

        /// <summary>
        /// Beat 3 - Blackscreen transition (hard cut).
        /// </summary>
        private void ExecuteBeat3()
        {
            Debug.Log("[Scene1Cutscene] Beat 3 - Blackscreen Transition");

            currentBeat = 3;

            StartCoroutine(PlayBlackscreenTransition());
        }

        /// <summary>
        /// Beat 4 - Reveal Shot 2 (Look-out).
        /// </summary>
        private void ExecuteBeat4()
        {
            Debug.Log("[Scene1Cutscene] Beat 4 - Reveal Shot 2");

            currentBeat = 4;

            // Switch shots
            if (shot1Root != null) shot1Root.SetActive(false);
            if (shot2Root != null) shot2Root.SetActive(true);

            // Set Shot 2 sprites (berdampingan dari awal)
            if (shot2Abah != null && abahSpriteSet != null)
                shot2Abah.sprite = abahSpriteSet.lookoutFacingMountain;

            if (shot2Rimba != null && rimbaSpriteSet != null)
                shot2Rimba.sprite = rimbaSpriteSet.lookoutFacingAbah;

            // Fade in Shot 2 sprites for smooth reveal
            StartCoroutine(FadeSpriteRenderers(new[] { shot2Abah, shot2Rimba }, 0.5f));

            // Auto-advance to Beat 5 (increased delay for breathing room)
            StartCoroutine(DelayedExecute(1.0f, ExecuteBeat5));
        }

        /// <summary>
        /// Beat 5 - Action Button (STRICT tap required).
        /// </summary>
        private void ExecuteBeat5()
        {
            Debug.Log("[Scene1Cutscene] Beat 5 - Action Button");

            currentBeat = 5;

            // Matikan tap fullscreen biar tidak menelan tap tombol Lihat (lapisan paling atas).
            if (tapDetector != null && tapDetector.activeSelf)
                tapDetector.SetActive(false);

            // Show action button and wait for tap (callback via event di Start)
            if (actionButton != null)
            {
                actionButton.Show();
                Debug.Log("[Scene1Cutscene] Beat 5 ready - tap tombol Lihat");
            }
            else
            {
                Debug.LogError("[Scene1Cutscene] actionButton belum di-assign! Assign di Inspector Scene1_Cutscene.");
            }
        }

        /// <summary>
        /// Beat 6 - Rimba menoleh (sprite swap animation).
        /// </summary>
        private void ExecuteBeat6()
        {
            Debug.Log("[Scene1Cutscene] Beat 6 - Rimba Menoleh");

            currentBeat = 6;

            // Play sprite swap sequence with variable frame timing
            if (rimbaSpriteSwapper != null && rimbaSpriteSet != null)
            {
                Sprite[] swapSequence = new[]
                {
                    rimbaSpriteSet.lookoutFacingAbah,
                    rimbaSpriteSet.lookoutNoleh1,
                    rimbaSpriteSet.lookoutNoleh2,
                    rimbaSpriteSet.lookoutFacingMountain
                };

                // Timing: jelas terlihat noleh (±0.15s/frame, hold akhir)
                float[] timings = new[] { 0.15f, 0.15f, 0.15f, 0.25f };

                Debug.Log("[Scene1Cutscene] Beat 6 swap start (4 frame)");
                rimbaSpriteSwapper.PlaySwapSequence(swapSequence, timings, () =>
                {
                    Debug.Log("[Scene1Cutscene] Beat 6 swap complete -> Beat 7");
                    ExecuteBeat7();
                });
            }
            else
            {
                // Fallback: direct sprite change
                if (shot2Rimba != null && rimbaSpriteSet != null)
                    shot2Rimba.sprite = rimbaSpriteSet.lookoutFacingMountain;

                ExecuteBeat7();
            }
        }

        /// <summary>
        /// Beat 7 - Dialog lanjutan (6 baris).
        /// </summary>
        private void ExecuteBeat7()
        {
            Debug.Log("[Scene1Cutscene] Beat 7 - Dialog Lanjutan");

            currentBeat = 7;

            // Reuse Shot 2 sprites (sudah set di Beat 6)
            // Show dialogue sequence
            StartCoroutine(PlayDialogueSequence(beat7Dialogue, ExecuteBeat8));
        }

        /// <summary>
        /// Beat 8 - Walk off ke kebun: Abah auto-jalan kanan, Rimba via analog.
        /// Sampai pojok kanan → blackscreen → loop (testing).
        /// </summary>
        private void ExecuteBeat8()
        {
            Debug.Log("[Scene1Cutscene] Beat 8 - Walk Off ke kebun");

            currentBeat = 8;

            // Hide cutscene UI
            if (dialogueDisplay != null)
                dialogueDisplay.Hide();
            if (floatingDialogue != null)
                floatingDialogue.Hide();

            // Deactivate cutscene shots
            if (shot1Root != null) shot1Root.SetActive(false);
            if (shot2Root != null) shot2Root.SetActive(false);

            // Kalau exploration belum di-setup, fallback loop lama
            if (explorationRoot == null || rimbaWalker == null || abahWalker == null || walkTrigger == null)
            {
                Debug.LogWarning("[Scene1Cutscene] Exploration belum setup, pakai loop lama.");
                StartCoroutine(ShowEndingAndLoop());
                return;
            }

            StartWalkOff();
        }

        private void StartWalkOff()
        {
            explorationRoot.SetActive(true);

            if (!spawnCaptured && rimbaWalker != null && abahWalker != null)
            {
                spawnRimbaPos = rimbaWalker.transform.position;
                spawnAbahPos = abahWalker.transform.position;
                spawnCaptured = true;
            }
            if (rimbaWalker != null)
                rimbaWalker.transform.position = spawnRimbaPos;
            if (abahWalker != null)
                abahWalker.transform.position = spawnAbahPos;

            if (rimbaSpriteSet != null && rimbaWalker != null)
                rimbaWalker.Configure(analogStick, rimbaSpriteSet.walkRight, rimbaSpriteSet.idleRight);
            if (abahSpriteSet != null && abahWalker != null)
                abahWalker.Configure(abahSpriteSet.walkRight, abahSpriteSet.idleRight);
            if (walkTrigger != null && rimbaWalker != null)
                walkTrigger.Configure(rimbaWalker.transform, OnWalkOffArrived);

            if (rimbaWalker != null)
                rimbaWalker.Locked = false;

            Debug.Log("[Scene1Cutscene] Walk off mulai - gerakkan Rimba dengan analog, ikuti Abah ke kanan");
        }

        private void OnWalkOffArrived()
        {
            if (rimbaWalker != null)
                rimbaWalker.Locked = true;

            if (audioManager != null)
                audioManager.FadeOutAmbient(1.5f);

            StartCoroutine(FinishWalkOff());
        }

        /// <summary>
        /// Blackscreen akhir Scene 1, lalu loop ke Beat 1 (testing).
        /// </summary>
        private IEnumerator FinishWalkOff()
        {
            if (blackscreenOverlay != null)
            {
                Image blackImage = blackscreenOverlay.GetComponent<Image>();
                if (blackscreenOverlay.GetComponent<Canvas>() == null)
                    blackscreenOverlay.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                blackscreenOverlay.SetActive(true);
                if (blackImage != null)
                    yield return StartCoroutine(FadeImage(blackImage, 0f, 1f, 1.0f));
                else
                    yield return new WaitForSeconds(1.0f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (!loopForTesting)
            {
                cutsceneActive = false;
                Debug.Log("[Scene1Cutscene] Scene 1 selesai - freeze di black (tunggu Scene 2)");
                yield break;
            }

            Debug.Log("[Scene1Cutscene] Loop testing - balik ke Beat 1");
            yield return new WaitForSeconds(1.0f);

            if (explorationRoot != null)
                explorationRoot.SetActive(false);

            if (blackscreenOverlay != null)
            {
                Image blackImage = blackscreenOverlay.GetComponent<Image>();
                if (blackImage != null)
                    yield return StartCoroutine(FadeImage(blackImage, 1f, 0f, 0.5f));
                blackscreenOverlay.SetActive(false);
            }

            StartCutscene();
        }

        /// <summary>
        /// Ending: fade to black 1 detik, lalu loop ke Beat 1 (testing).
        /// Kalau loopForTesting=false, freeze di black (tunggu exploration).
        /// </summary>
        private IEnumerator ShowEndingAndLoop()
        {
            if (blackscreenOverlay != null)
            {
                Image blackImage = blackscreenOverlay.GetComponent<Image>();
                if (blackImage != null)
                {
                    blackscreenOverlay.SetActive(true);
                    yield return StartCoroutine(FadeImage(blackImage, 0f, 1f, 0.5f));
                }
                else
                {
                    blackscreenOverlay.SetActive(true);
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (!loopForTesting)
            {
                cutsceneActive = false;
                Debug.Log("[Scene1Cutscene] Cutscene Complete - Freeze (exploration belum siap)");
                yield break;
            }

            Debug.Log("[Scene1Cutscene] Loop testing - balik ke Beat 1");
            yield return new WaitForSeconds(1.0f);

            // Reset shots + audio untuk loop
            if (blackscreenOverlay != null)
            {
                Image blackImage = blackscreenOverlay.GetComponent<Image>();
                if (blackImage != null)
                    yield return StartCoroutine(FadeImage(blackImage, 1f, 0f, 0.5f));
                blackscreenOverlay.SetActive(false);
            }

            StartCutscene();
        }

        #endregion

        #region Helper Methods

        private IEnumerator PlayDialogueSequence(DialogueLine[] lines, System.Action onComplete)
        {
            foreach (var line in lines)
            {
                // Skip empty dialogue lines (e.g., Beat 7 line 4 - visual reaction only)
                if (string.IsNullOrEmpty(line.textSunda))
                {
                    yield return new WaitForSeconds(0.8f); // Brief pause for visual reaction
                    continue;
                }

                bool dialogueFinished = false;
                Transform anchor = ResolveAnchor(line.speaker);

                // Prioritas: floating bubble (Scene 1-8 reusable) kalau sudah di-assign
                if (floatingDialogue != null && anchor != null)
                {
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => dialogueFinished = true);
                }
                else if (dialogueDisplay != null)
                {
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => dialogueFinished = true);
                }
                else
                {
                    Debug.LogWarning("[Scene1Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
                    dialogueFinished = true;
                }

                // Wait for dialogue to finish (player tap)
                yield return new WaitUntil(() => dialogueFinished);
                yield return new WaitForSeconds(0.2f); // Small delay between lines
            }

            // Hide dialogue box after sequence
            if (dialogueDisplay != null)
                dialogueDisplay.Hide();
            if (floatingDialogue != null)
                floatingDialogue.Hide();

            onComplete?.Invoke();
        }

        private IEnumerator PlayBlackscreenTransition()
        {
            // Play transition SFX
            if (audioManager != null && sfxTransitionBlackscreen != null)
                audioManager.PlaySFX(sfxTransitionBlackscreen);

            // Fade in/out blackscreen with smooth transitions
            if (blackscreenOverlay != null)
            {
                Image blackImage = blackscreenOverlay.GetComponent<Image>();
                if (blackImage != null)
                {
                    blackscreenOverlay.SetActive(true);
                    
                    // Fade in (0 → 1) over 0.3s
                    yield return StartCoroutine(FadeImage(blackImage, 0f, 1f, 0.3f));
                    
                    // Hold black screen
                    float holdDuration = blackscreenDuration - 0.6f; // Subtract fade in/out time
                    if (holdDuration > 0)
                        yield return new WaitForSeconds(holdDuration);
                    
                    // Fade out (1 → 0) over 0.3s
                    yield return StartCoroutine(FadeImage(blackImage, 1f, 0f, 0.3f));
                    
                    blackscreenOverlay.SetActive(false);
                }
                else
                {
                    // Fallback to instant on/off if no Image component
                    blackscreenOverlay.SetActive(true);
                    yield return new WaitForSeconds(blackscreenDuration);
                    blackscreenOverlay.SetActive(false);
                }
            }
            else
            {
                yield return new WaitForSeconds(blackscreenDuration);
            }

            // Proceed to Beat 4
            ExecuteBeat4();
        }

        private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>
        /// Helper: Fade Image alpha from 'from' to 'to' over duration.
        /// </summary>
        private IEnumerator FadeImage(Image image, float from, float to, float duration)
        {
            float elapsed = 0f;
            Color color = image.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(from, to, t);
                image.color = color;
                yield return null;
            }

            color.a = to;
            image.color = color;
        }

        /// <summary>
        /// Helper: Fade in SpriteRenderers from alpha 0 to 1 over duration.
        /// </summary>
        private IEnumerator FadeSpriteRenderers(SpriteRenderer[] renderers, float duration)
        {
            // Set all to alpha 0
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 0f;
                    sr.color = c;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);

                foreach (var sr in renderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }

                yield return null;
            }

            // Ensure full alpha at end
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f;
                    sr.color = c;
                }
            }
        }

        #endregion

        #region Dialogue Data Structure

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
