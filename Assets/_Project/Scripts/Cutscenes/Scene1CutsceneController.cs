using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MainKarinding.Core;
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
        [Header("Dialogue Boxes Per-Shot (hierarchy-driven)")]
        [Tooltip("Shot1 follow kepala (Beat2). Drag DialogueBox_Shot1.")]
        [SerializeField] private FloatingDialogueController floatingShot1;
        [Tooltip("Shot2 statis bawah (Beat7). Drag DialogueBox_Shot2.")]
        [SerializeField] private CutsceneDialogueDisplay displayShot2;
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
        [Tooltip("Beat6 swap via Inspector (hierarchy-driven). Isi misal [abah2, mountain2]. Kosong = default 2-frame dari SpriteSet tanpa noleh.")]
        [SerializeField] private Sprite[] beat6SwapSprites = new Sprite[0];
        [Tooltip("Timing per-frame Beat6. Kosong/tidak cocok = default [0.15, 0.3].")]
        [SerializeField] private float[] beat6SwapTimings = new float[0];

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

        [Header("Toast hint jalan")]
        [SerializeField] private Scene5Toast followToast;

        [Header("Beat 8 - Abah keluar frame & kedip analog")]
        [Tooltip("Abah di-set inactive saat posisi X-nya lewat nilai ini (keluar frame kanan).")]
        [SerializeField] private float abahExitX = 10.8f;
        [Tooltip("Amplitudo kedip analog (0.1 = pulse 10% dari scale normal). Wajib diisi Scene1CutsceneController di Scene1.")]
        [SerializeField] private float analogPulseAmount = 0.1f;
        [Tooltip("Periode kedip analog (detik).")]
        [SerializeField] private float analogPulsePeriod = 0.5f;

        [Header("Exploration - BATAS KANAN (USER ATUR)")]
        [Tooltip("Spawn X Rimba saat Beat 8 dimulai. Harus < endX WalkOffTrigger, dan < maxX RimbaWalker supaya tidak ter-clamp di luar frame.")]
        [SerializeField] private float spawnRimbaX = 8.5f;
        [Tooltip("Spawn X Abah saat Beat 8 dimulai. Harus terlihat di frame (tepi layar ≈ ±8.3).")]
        [SerializeField] private float spawnAbahX = 10.2f;

        private Vector3 spawnRimbaPos;
        private Vector3 spawnAbahPos;
        private bool spawnCaptured;

        [Header("Settings")]
        [SerializeField] private float blackscreenDuration = 0.7f;
        [Tooltip("Kalau true, Beat 8 loop balik ke Beat 1 buat testing. Matikan kalau exploration sudah siap.")]
        [SerializeField] private bool loopForTesting = true;

        [Header("Handoff ke Scene 2")]
        [Tooltip("Drag Scene2_Cutscene. Diaktifkan otomatis setelah blackscreen Beat 8 (loopForTesting=false).")]
        [SerializeField] private GameObject scene2Root;
        [Tooltip("Jeda (detik) sebelum tirai Scene 1 dibuka setelah Scene 2 aktif. Scene 2 menampilkan blackscreen sendiri dulu.")]
        [SerializeField] private float scene2HandoffDelay = 0.5f;

        private int currentBeat = 0;
        private bool cutsceneActive = false;

        private Coroutine analogBlinkRoutine;
        private Vector3 analogRestScale;
        private bool analogRestCaptured;

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
            AutoFindFollowToast();

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

        private void AutoFindFollowToast()
        {
            if (followToast != null) return;
            var uiAnalog = explorationRoot != null ? explorationRoot.transform.Find("UI_Analog") : null;
            if (uiAnalog != null)
                followToast = uiAnalog.GetComponentInChildren<Scene5Toast>(true);
            if (followToast == null)
                Debug.LogWarning("[Scene1Cutscene] ToastRoot belum di-setup di UI_Analog! Copy ToastRoot dari Scene8.");
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

            // Tahap 1 hierarchy-driven: sprite awal ngikut Inspector
            // (pasang pintu1 di Shot1/Char_Rimba via drag-drop), jangan overwrite dari SpriteSet.

            // Play ambient audio
            if (audioManager != null && ambientMorning != null)
                audioManager.PlayAmbient(ambientMorning, 0.6f);

            // Dua tahap sebelum Beat 2 (dialog Abah)
            StartCoroutine(Beat1TwoStage());
        }

        private IEnumerator Beat1TwoStage()
        {
            yield return new WaitForSeconds(2f);
            if (shot1Rimba != null && rimbaSpriteSet != null && rimbaSpriteSet.idlePintu != null)
                shot1Rimba.sprite = rimbaSpriteSet.idlePintu;
            yield return new WaitForSeconds(1f);
            ExecuteBeat2();
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

            // Switch sudah terjadi di balik hitam (SwitchToShot2UnderBlack).
            // Idempoten: amankan state kalau Beat 4 dipanggil langsung.
            if (shot1Root != null && shot1Root.activeSelf) shot1Root.SetActive(false);
            if (shot2Root != null && !shot2Root.activeSelf) shot2Root.SetActive(true);

            // Box standby nyala duluan ("..."), dialog asli mulai setelah tombol Lihat (Beat 7).
            if (displayShot2 != null)
                displayShot2.ShowStandby("...");
            else if (dialogueDisplay != null)
                dialogueDisplay.ShowStandby("...");

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
            // Hierarchy-driven: isi beat6SwapSprites/Timings di Inspector. Kosong = default 2-frame tanpa noleh.
            if (rimbaSpriteSwapper != null && rimbaSpriteSet != null)
            {
                Sprite[] swapSequence;
                float[] timings;
                if (beat6SwapSprites != null && beat6SwapSprites.Length > 0)
                {
                    swapSequence = beat6SwapSprites;
                    timings = (beat6SwapTimings != null && beat6SwapTimings.Length == swapSequence.Length)
                        ? beat6SwapTimings
                        : null;
                }
                else
                {
                    swapSequence = new[]
                    {
                        rimbaSpriteSet.lookoutFacingAbah,
                        rimbaSpriteSet.lookoutFacingMountain
                    };
                    timings = new[] { 0.15f, 0.3f };
                }

                Debug.Log($"[Scene1Cutscene] Beat 6 swap start ({swapSequence.Length} frame)");
                rimbaSpriteSwapper.PlaySwapSequence(swapSequence, timings, () =>
                {
                    Debug.Log("[Scene1Cutscene] Beat 6 swap complete -> Beat 7");
                    ExecuteBeat7();
                });
            }
            else
            {
                // Hierarchy-driven: jangan ganti sprite, langsung lanjut.
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
            if (displayShot2 != null)
                displayShot2.Hide();
            if (floatingShot1 != null)
                floatingShot1.Hide();
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
                // Tangkap Y/Z scene sebagai acuan, tapi X dipaksa dari field spawn
                // (harus >= maxX Walker) supaya keduanya MUNCUL dari tepi kanan.
                spawnRimbaPos = rimbaWalker.transform.position;
                spawnAbahPos = abahWalker.transform.position;
                spawnRimbaPos.x = spawnRimbaX;
                spawnAbahPos.x = spawnAbahX;
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

            if (followToast != null)
                followToast.ShowToast("Ikuti Abah pakai analog!", 3f);

            StartAbahExit();
            StartAnalogBlink();

            Debug.Log("[Scene1Cutscene] Walk off mulai - gerakkan Rimba dengan analog, ikuti Abah ke kanan");
        }

        private void StartAbahExit()
        {
            if (abahWalker == null)
            {
                Debug.LogWarning("[Scene1Cutscene] abahWalker belum di-assign! Abah tidak keluar frame otomatis.");
                return;
            }
            abahWalker.ResumeWalking(abahExitX);
            StartCoroutine(AbahExitRoutine());
        }

        private IEnumerator AbahExitRoutine()
        {
            if (abahWalker == null) yield break;
            while (abahWalker != null && abahWalker.transform.position.x < abahExitX)
                yield return null;
            if (abahWalker != null)
                abahWalker.gameObject.SetActive(false);
        }

        private void StartAnalogBlink()
        {
            StopAnalogBlink(false);
            if (analogStick == null || rimbaWalker == null)
            {
                Debug.LogWarning("[Scene1Cutscene] analogStick/rimbaWalker belum di-assign! Kedip analog dilewati.");
                return;
            }
            if (!analogRestCaptured)
            {
                analogRestScale = analogStick.transform.localScale;
                analogRestCaptured = true;
            }
            analogStick.SetForceShow(true);
            analogBlinkRoutine = StartCoroutine(AnalogBlinkRoutine());
        }

        private IEnumerator AnalogBlinkRoutine()
        {
            // Kedip jalan terus sampai analog BENAR-BENAR disentuh pertama kali —
            // gerakan lewat keyboard tidak menghentikan hint.
            float t = 0f;
            while (analogStick != null && !analogStick.IsDragging)
            {
                t += Time.deltaTime;
                float pulse = 1f + analogPulseAmount * Mathf.Sin(t / Mathf.Max(0.05f, analogPulsePeriod) * 2f * Mathf.PI);
                analogStick.transform.localScale = analogRestScale * pulse;
                yield return null;
            }
            StopAnalogBlink(true);
        }

        private void StopAnalogBlink(bool restoreScale)
        {
            if (analogBlinkRoutine != null)
            {
                StopCoroutine(analogBlinkRoutine);
                analogBlinkRoutine = null;
            }
            if (restoreScale && analogRestCaptured && analogStick != null)
            {
                analogStick.transform.localScale = analogRestScale;
                analogStick.SetForceShow(false);
            }
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
            StopAnalogBlink(true);

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
                Debug.Log("[Scene1Cutscene] Scene 1 selesai - handoff ke Scene 2");

                // EntryPoint HARUS diset SEBELUM Scene 2 aktif — SceneEntryGate Scene 2
                // baru jalan saat aktivasi dan akan menutup lagi kalau entry belum Scene2.
                if (StoryState.Instance != null)
                    StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene2);

                if (scene2Root != null)
                    scene2Root.SetActive(true);
                else
                    Debug.LogError("[Scene1Cutscene] scene2Root belum di-assign! Drag Scene2_Cutscene di Inspector.");

                // Beri waktu Scene 2 siap (Start + Beat 1) dengan blackscreen miliknya.
                yield return new WaitForSeconds(scene2HandoffDelay);

                // Dunia Scene 1 (termasuk UI_Analog) mati di balik hitam.
                if (explorationRoot != null)
                    explorationRoot.SetActive(false);

                // Tirai Scene 1 ditutup; reveal pakai blackscreen milik Scene 2.
                if (blackscreenOverlay != null)
                    blackscreenOverlay.SetActive(false);

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

                // Shot1 (Beat2) = follow kepala, Shot2 (Beat7+) = statis bawah.
                bool isShot1 = currentBeat == 2;
                var floating = isShot1 ? (floatingShot1 != null ? floatingShot1 : floatingDialogue) : null;
                var display = !isShot1 ? (displayShot2 != null ? displayShot2 : dialogueDisplay) : null;
                // Fallback lama kalau ref per-shot kosong.
                if (isShot1 && floating == null) display = dialogueDisplay;
                if (!isShot1 && display == null) floating = floatingDialogue;

                if (isShot1 && floating != null && anchor != null)
                {
                    floating.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => dialogueFinished = true);
                }
                else if (!isShot1 && display != null)
                {
                    display.ShowDialogue(line.textSunda, line.speaker, () => dialogueFinished = true);
                }
                else if (floatingDialogue != null && anchor != null)
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
            if (displayShot2 != null)
                displayShot2.Hide();
            if (floatingShot1 != null)
                floatingShot1.Hide();
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

                    // Ganti shot selagi hitam penuh — yang tersingkap nanti langsung Shot 2.
                    SwitchToShot2UnderBlack();

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

        /// <summary>
        /// Ganti Shot 1 -> Shot 2 selagi layar hitam penuh (dipanggil dari tengah transisi).
        /// </summary>
        private void SwitchToShot2UnderBlack()
        {
            if (shot1Root != null) shot1Root.SetActive(false);
            if (shot2Root != null) shot2Root.SetActive(true);
            // Pastikan sprite Shot 2 tampil penuh, bukan sisa alpha dari fade.
            foreach (var sr in new[] { shot2Abah, shot2Rimba })
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f;
                    sr.color = c;
                }
            }
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
