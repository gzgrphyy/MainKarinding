using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Tutorial Hari Pertama Scene 3: Belajar Memainkan Karinding.
/// Flow: Prolog -> Fase1 HandPlacement (F,J) -> Fase2 RhythmSabar (3 note) -> Fase3 Resonansi (7 note, gagal dulu) -> Epilog.
/// Modular: tinggal drag prefab TutorialSystem ke scene manapun.
/// </summary>
public class KarindingTutorialController : MonoBehaviour
{
    public static KarindingTutorialController Instance { get; private set; }

    public enum TutorialPhase { Idle, Prolog, HandPlacement, RhythmSabar, Resonansi, Epilog, Done }

    [Header("Charts")]
    [SerializeField] private RhythmChartConfig sabarChart; // Fase 2 - 3-4 note lambat
    [SerializeField] private RhythmChartConfig resonansiChart; // Fase 3 - 7 note
    [SerializeField] private RhythmChartConfig sabarChartFull; // optional: full 10 note for debug

    [Header("References (auto-find if null)")]
    [SerializeField] private RhythmMinigameController rhythmController;
    [SerializeField] private KarindingTutorialOverlay overlay;
    [SerializeField] private DialogueSystem dialogueSystem;
    [Tooltip("Untuk suppress overlay hasil generic selama tutorial & menampilkan SuccessOverlay di epilog.")]
    [SerializeField] private RhythmMissFeedback rhythmFeedback;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "MinigameRhythm";
    [SerializeField] private bool allowSkipTutorial = false;
    [SerializeField] private string playerPrefsKey = "tutorial_karinding_done";

    [Header("Hand Placement")]
    [SerializeField] private LaneSide[] handSequence = new LaneSide[] { LaneSide.LeftOuter, LaneSide.LeftInner, LaneSide.RightInner, LaneSide.RightOuter };

    [Header("Fase 2 Tuning")]
    [SerializeField] private int requiredStreakFase2 = 2;
    [SerializeField] private int maxRushBeforeHint = 2;

    [Header("Fase 3 Forced-Fail-then-Succeed (PLAN E3)")]
    [Tooltip("Attempt 1 selalu di-staging gagal (scripted, bukan vonis), attempt 2 lenient dijamin lolos. No game-over, tanpa klik ULANG.")]
    [SerializeField] private bool forceFailFirstAttempt = true;
    [SerializeField] private float lenientTravelDuration = 1.8f;
    [SerializeField] private float lenientPerfectWindow = 0.15f;
    [SerializeField] private float lenientGoodWindow = 0.30f;
    [SerializeField] private int lenientMaxMisses = 99;

    [Header("Dialog Abah saat Miss (slot teks tutorial)")]
    [Tooltip("Cooldown antar tampil dialog Abah supaya miss beruntun tidak spam teks.")]
    [SerializeField] private float abahMissCooldown = 5f;
    [Tooltip("Durasi tampil dialog Abah di slot InstructionText (auto-hilang, tanpa tap).")]
    [SerializeField] private float abahMissDuration = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool logVerbose = true;

    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Idle;
    public bool IsInHandPlacementPhase => CurrentPhase == TutorialPhase.HandPlacement;
    public bool IsTutorialActive => CurrentPhase != TutorialPhase.Idle && CurrentPhase != TutorialPhase.Done;

    private int _handIndex;
    private int _rushCount;
    private int _streak;
    private bool _fase2FirstHintShown;
    private float _lastAbahMissTime = -999f;
    private const string AbahUlahMaksaLine = "Abah: Ulah maksa, Rimba. Lain kuat na leungeun nu di penta, tapi tenang jeung sabar na haté.";
    private bool _waitingForHandTap;
    private LaneSide _expectedHandLane;
    private System.Action<LaneSide> _onHandTapCallback;

    // For sabar chart slicing - we use runtime copy
    private RhythmChartConfig _runtimeSabarChart;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(WaitAndInit());
    }

    private IEnumerator WaitAndInit()
    {
        // Tunggu controller siap
        yield return null;
        if (rhythmController == null) rhythmController = RhythmMinigameController.Instance;
        if (rhythmController == null) rhythmController = FindFirstObjectByType<RhythmMinigameController>();
        if (overlay == null) overlay = FindFirstObjectByType<KarindingTutorialOverlay>();
        if (rhythmFeedback == null) rhythmFeedback = FindFirstObjectByType<RhythmMissFeedback>();
        if (dialogueSystem == null) dialogueSystem = DialogueSystem.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<DialogueSystem>();

        // Jika DialogueSystem belum ada di scene, buat GO baru dan attach (akan BuildUI di Awake)
        if (dialogueSystem == null)
        {
            var dsGO = new GameObject("DialogueSystem");
            // Parent ke Canvas jika ada
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) dsGO.transform.SetParent(canvas.transform, false);
            dialogueSystem = dsGO.AddComponent<DialogueSystem>();
            // Wait a frame for Awake
            yield return null;
            dialogueSystem = DialogueSystem.Instance;
        }

        // Jika overlay belum ada, buat juga
        if (overlay == null)
        {
            var ovGO = new GameObject("TutorialOverlay");
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) ovGO.transform.SetParent(canvas.transform, false);
            overlay = ovGO.AddComponent<KarindingTutorialOverlay>();
            yield return null;
        }

        // Load charts if not assigned (fallback ke Resources/TestChart_01)
        if (sabarChart == null) sabarChart = Resources.Load<RhythmChartConfig>("TutorialChart_HariPertama_S3");
        // Also try direct asset path via Resources.Load is fallback, but we have ScriptableObject in _Project/ScriptableObjects not in Resources.
        // So fallback: find by name via asset database? Use FindFirstObject? Instead we try to load via addressable path not available.
        // Last fallback: use test chart or create runtime.
        if (sabarChart == null)
        {
            // Try find any RhythmChartConfig in scene via DebugRhythmTester
            var tester = FindFirstObjectByType<DebugRhythmTester>();
            if (tester != null)
            {
                // Use reflection to get testConfig
                var field = typeof(DebugRhythmTester).GetField("testConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null) sabarChart = field.GetValue(tester) as RhythmChartConfig;
            }
        }

        // If still null, create runtime sabar chart (3 notes)
        if (sabarChart == null)
        {
            sabarChart = CreateRuntimeSabarChart();
            Log("SabarChart auto-created runtime fallback");
        }
        if (resonansiChart == null)
        {
            resonansiChart = CreateRuntimeResonansiChart();
            Log("ResonansiChart auto-created runtime fallback");
        }

        // Check PlayerPrefs skip
        if (PlayerPrefs.GetInt(playerPrefsKey, 0) == 1 && allowSkipTutorial)
        {
            Log("Tutorial sudah pernah selesai, skip (allowSkip=true). Langsung main.");
            // Optionally auto-start main chart if exists, else do nothing
            yield break;
        }

        if (autoStart) StartTutorial();
    }

    public void StartTutorial()
    {
        // Hanya boleh mulai dari Idle. Setelah Done, tutorial TIDAK bisa jalan/ulang sendiri —
        // harus klik ULANG (RestartTutorial) yang me-reset state ke Idle dulu.
        if (CurrentPhase != TutorialPhase.Idle) return;
        Log("StartTutorial");
        StartCoroutine(RunTutorialRoutine());
    }

    // Called by LaneButton when tutorial hand phase active
    public void NotifyHandTap(LaneSide lane, TapResult result)
    {
        if (!IsInHandPlacementPhase) return;
        if (!_waitingForHandTap) return;
        if (lane == _expectedHandLane)
        {
            Log($"Hand tap CORRECT lane={lane}");
            _waitingForHandTap = false;
            // Clear highlight quickly
            overlay?.ClearHighlight(lane);
            overlay?.ShowHand(false);
            overlay?.HideInstruction(0.15f);
        }
        else
        {
            Log($"Hand tap WRONG lane={lane} expected={_expectedHandLane}");
            // Feedback: shake wrong lane
            var btns = FindObjectsByType<LaneButton>(FindObjectsSortMode.None);
            foreach (var b in btns) if (b.Lane == lane) { b.PlayWrongLaneFeedback(); break; }
            // Android-friendly: toast, not dialog (no pause)
            overlay?.ShowToast("Salah tombol! Perhatikan tombol yang berkedip.", new Color(1f, 0.7f, 0.2f, 1f), 1.0f);
#if UNITY_ANDROID
            if (SystemInfo.supportsVibration) Handheld.Vibrate();
#endif
        }
    }

    // Called by LaneButton for any tap to track rush in Fase2 - Android: no pause, just toast
    public void NotifyAnyTap(LaneSide lane, TapResult result)
    {
        if (CurrentPhase != TutorialPhase.RhythmSabar) return;
        if (result == TapResult.EmptyNoNote || result == TapResult.WrongLane)
        {
            _rushCount++;
            Log($"Rush tap lane={lane} result={result} rushCount={_rushCount}");
            if (_rushCount >= maxRushBeforeHint && !_fase2FirstHintShown)
            {
                _fase2FirstHintShown = true;
                // No pause, just toast (Android friendly)
                overlay?.ShowToast("Terlalu cepat! Sabar, ikuti ritmenya...", new Color(1f, 0.85f, 0.2f, 1f), 1.2f);
                // Subtle hint without blocking gameplay
                StartCoroutine(ShowRushToastHint());
            }
            else if (_rushCount == 1)
            {
                overlay?.ShowToast("Pelan... tunggu note sampai lingkaran", new Color(1f, 1f, 1f, 0.95f), 0.9f);
            }
        }
        else if (result == TapResult.HitPerfect || result == TapResult.HitGood)
        {
            _streak++;
            Log($"Hit streak={_streak}");
        }
        else
        {
            _streak = 0;
        }
    }

    /// <summary>
    /// Dipanggil saat note di-judge Miss selama Fase 3 (Resonansi) saja — Fase 2
    /// (guided) sengaja tidak di-hook supaya slot teks tutorial tidak ketabrak hint.
    /// Tampil di slot InstructionText yang sudah ada (auto-hilang) — bukan toast/popup
    /// baru, tanpa tap. Dibatasi cooldown supaya miss beruntun tidak spam.
    /// </summary>
    private void HandleMissAbahLine(NoteData data, HitGrade grade)
    {
        if (grade != HitGrade.Miss) return;
        if (CurrentPhase != TutorialPhase.Resonansi) return;
        if (Time.time - _lastAbahMissTime < Mathf.Max(0f, abahMissCooldown)) return;
        _lastAbahMissTime = Time.time;
        Log("Miss -> dialog Abah di slot teks tutorial");
        overlay?.ShowInstruction(AbahUlahMaksaLine, Mathf.Max(0.5f, abahMissDuration));
    }

    private IEnumerator ShowRushToastHint()
    {
        // Brief non-blocking hint - no pause, keeps gameplay running (Android requirement)
        // We already showed toast above, optionally pulse the track
        yield return new WaitForSeconds(0.1f);
        overlay?.ShowInstruction("Tenang... ikuti dengungnya", 1.4f);
    }

    private IEnumerator ShowHandHintAfterDelay(float delay, string msg)
    {
        // Deprecated for Android: use toast via NotifyHandTap directly
        yield return new WaitForSeconds(delay);
        // No dialog during gameplay - handled by overlay toast
    }

    private IEnumerator RunTutorialRoutine()
    {
        CurrentPhase = TutorialPhase.Prolog;

        // Selama tutorial, overlay hasil generic (Success/Fail) di-suppress: UI akhir hanya di epilog
        // supaya tidak ada 2 set tombol ULANG/LANJUT bertumpuk dan tidak ada restart tak sengaja.
        if (rhythmFeedback == null) rhythmFeedback = FindFirstObjectByType<RhythmMissFeedback>();
        if (rhythmFeedback != null) rhythmFeedback.SuppressResultOverlay = true;

        // Hide start button if exists (DebugRhythmTester)
        var startBtn = GameObject.Find("Btn_StartTest");
        if (startBtn != null) startBtn.SetActive(false);

        // Prolog - tanpa dialog, langsung overlay jelas & menarik (Android, cepat paham)
        Log("Phase: Prolog");
        // No dialogue - just brief overlay intro
        overlay?.ShowDim(true, 0.25f);
        overlay?.ShowInstruction("Belajar tombol Karinding — tap yang berkedip", 1.6f);
        overlay?.SetProgressDotsVisible(true);
        overlay?.UpdateProgressDots(0, 4);
        yield return new WaitForSeconds(0.6f);

        // Fase 1 Hand Placement - 4 tombol kiri→kanan, warna per tombol, progress dots, hand gerak smooth
        CurrentPhase = TutorialPhase.HandPlacement;
        Log("Phase: HandPlacement");

        for (_handIndex = 0; _handIndex < handSequence.Length; _handIndex++)
        {
            LaneSide target = handSequence[_handIndex];
            string laneName = LaneToDisplayAndroid(target);
            Color laneColor = GetLaneColor(target);
            // Progress dots update
            overlay?.UpdateProgressDots(_handIndex, 4);
            // Colored instruction persistent sampai tap benar - tidak hilang biar tidak lupa
            string coloredName = $"<color=#{ColorUtility.ToHtmlStringRGB(laneColor)}>{laneName.ToUpper()}</color>";
            overlay?.ShowInstruction($"Tap {coloredName} yang berkedip  ({_handIndex+1}/4)", -1);
            _expectedHandLane = target;
            _waitingForHandTap = true;
            overlay?.HighlightLane(target, true);
            if (_handIndex == 0) overlay?.ShowHand(target);
            else overlay?.MoveHandTo(target, 0.35f); // smooth left→right, tidak teleport - menarik & cepat paham
            // Wait for correct tap
            yield return new WaitUntil(() => !_waitingForHandTap);
            // Success feedback - dot punch + toast + quick scale
            overlay?.UpdateProgressDots(_handIndex + 1, 4);
            overlay?.ShowToast($"Bagus! {laneName} benar", new Color(0.4f, 1f, 0.5f, 1f), 0.85f);
            // Brief highlight pulse already, no long wait - cepat
            yield return new WaitForSeconds(0.45f);
        }
        overlay?.ClearAllHighlights();
        overlay?.ShowHand(false);
        overlay?.UpdateProgressDots(4, 4);
        // Menarik: semua tombol pulse bersama setelah 4 selesai
        foreach (var ls in handSequence) overlay?.HighlightLane(ls, true);
        yield return new WaitForSeconds(0.4f);
        foreach (var ls in handSequence) overlay?.ClearHighlight(ls);
        // Aba-aba jelas sebelum tutorial sambil bermain - dim tetap, teks persistent tidak ke-timpa
        overlay?.ShowInstruction("Oke Rimba, sekarang coba <b>sambil bermain</b> — ikuti tombol yang berkedip!", -1);
        yield return new WaitForSeconds(1.8f);
        overlay?.HideInstruction(0.15f);
        yield return new WaitForSeconds(0.2f);
        overlay?.ShowInstruction("Ikuti ritme sambil main — tap pas di lingkaran", -1);
        yield return new WaitForSeconds(1.4f);
        overlay?.HideInstruction(0.15f);
        overlay?.SetProgressDotsVisible(false);
        overlay?.ShowDim(false, 0.2f);
        yield return new WaitForSeconds(0.25f);

        // Fase 2 Rhythm Sabar - langsung countdown setelah aba-aba
        CurrentPhase = TutorialPhase.RhythmSabar;
        Log("Phase: RhythmSabar");
        _rushCount = 0;
        _streak = 0;
        _fase2FirstHintShown = false;

        // Fase2 single-run: 4 note guided mudah, langsung selesai tanpa ngulang (sesuai request: jangan auto retry)
        Log("Fase2 single attempt - guided 4 note mudah, langsung selesai");
        {
            var chartToPlay = PrepareSabarChart();
            // Guided 4 note sambil bermain (mudah) - semua 4 dituntun
            System.Action<NoteData> onSpawned = null;
            System.Action<NoteData, HitGrade> onJudged = null;
            System.Action<RhythmMinigameResult> onComplete = null;
            bool chartDone = false;
            RhythmMinigameResult lastResult = null;
            int guidedCount = 0;

            onSpawned = (note) =>
            {
                if (guidedCount < 4)
                {
                    Color c = GetLaneColor(note.lane);
                    string colored = $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{LaneToDisplayAndroid(note.lane).ToUpper()}</color>";
                    overlay?.ShowInstruction($"Tap {colored} saat di lingkaran", -1);
                    overlay?.HighlightLane(note.lane, true);
                    if (guidedCount == 0) overlay?.ShowHand(note.lane);
                    else overlay?.MoveHandTo(note.lane, 0.3f);
                    guidedCount++;
                }
            };
            onJudged = (data, grade) =>
            {
                overlay?.ClearHighlight(data.lane);
                overlay?.HideInstruction(0.15f);
                if (grade == HitGrade.Perfect || grade == HitGrade.Good) { }
                else { _streak = 0; }
            };
            onComplete = (res) => { lastResult = res; chartDone = true; };

            rhythmController.OnNoteSpawned += onSpawned;
            rhythmController.OnNoteJudged += onJudged;
            rhythmController.OnMinigameComplete += onComplete;

            rhythmController.StartMinigame(chartToPlay, true); // start ter-script (bukan auto-loop)
            yield return new WaitUntil(() => chartDone);
            rhythmController.OnNoteSpawned -= onSpawned;
            rhythmController.OnNoteJudged -= onJudged;
            rhythmController.OnMinigameComplete -= onComplete;
            overlay?.HideInstruction(0.2f);
            overlay?.ShowHand(false);
            overlay?.ClearAllHighlights();

            int hits = lastResult != null ? (lastResult.perfectCount + lastResult.goodCount) : 0;
            Log($"Fase2 result hits={hits} streak={_streak} rush={_rushCount} miss={lastResult?.missCount}");
            // Single-run: tidak ada retry, langsung lanjut walau miss - cuma toast beda
            if (hits >= 2 && _streak >= requiredStreakFase2)
            {
                overlay?.ShowToast("Bagus! Dengungnya terdengar", new Color(0.4f, 1f, 0.5f, 1f), 1.2f);
            }
            else
            {
                overlay?.ShowToast("Sudah dicoba — bagus!", new Color(1f, 0.85f, 0.2f, 1f), 1.1f);
            }
            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(0.5f);

        // Fase 3 scripted 2-pass (PLAN E3: forced-fail-then-succeed, no game-over).
        // Attempt 1 selalu di-staging "gagal" (dengung pecah) apa pun skor player,
        // lalu attempt 2 lenient yang dijamin sampai Epilog. Otomatis, tanpa klik ULANG.
        CurrentPhase = TutorialPhase.Resonansi;
        Log("Phase: Resonansi - forced-fail-then-succeed (2 pass)");
        overlay?.ShowInstruction("Fase 3 — Coba rangkaian lengkap!", 1.4f);
        yield return new WaitForSeconds(1.6f);
        overlay?.HideInstruction(0.15f);
        RhythmMinigameResult fase3Result = null;
        if (forceFailFirstAttempt)
        {
            var chart3 = resonansiChart != null ? resonansiChart : CreateRuntimeResonansiChart();
            yield return StartCoroutine(PlayChartOnce(chart3, (r) => fase3Result = r));
            Log($"Fase3 attempt1 passed={fase3Result?.passed} miss={fase3Result?.missCount} accuracy={fase3Result?.accuracy}");

            // Scripted fail staging — narasi "gagal dulu", bukan vonis ke player.
            var karindingImg = GameObject.Find("Karinding") ?? GameObject.Find("karinding2d");
            if (karindingImg != null) karindingImg.transform.DOPunchScale(Vector3.one * 0.45f, 0.7f, 8, 0.9f);
            else
            {
                var hitRing = GameObject.Find("HitRing");
                if (hitRing != null) hitRing.transform.DOPunchScale(Vector3.one * 0.5f, 0.6f, 6, 1f);
            }
            overlay?.ShowToast("Dengungna pecah... deui, Ba, sing sabar.", new Color(1f, 0.85f, 0.2f, 1f), 1.6f);
            yield return new WaitForSeconds(1.8f);
        }
        {
            // Attempt final — lenient: window besar + miss longgar, dijamin sampai Epilog.
            var chartLenient = PrepareLenientResonansiChart();
            yield return StartCoroutine(PlayChartOnce(chartLenient, (r) => fase3Result = r));
            Log($"Fase3 attempt-final passed={fase3Result?.passed} miss={fase3Result?.missCount} accuracy={fase3Result?.accuracy}");
            var karindingImg2 = GameObject.Find("Karinding") ?? GameObject.Find("karinding2d");
            if (karindingImg2 != null) karindingImg2.transform.DOPunchScale(Vector3.one * 0.35f, 0.6f, 8, 0.9f);
            else
            {
                var hitRing2 = GameObject.Find("HitRing");
                if (hitRing2 != null) hitRing2.transform.DOPunchScale(Vector3.one * 0.4f, 0.5f, 6, 1f);
            }
            overlay?.ShowToast("Dengung jelas! Mantap", new Color(0.4f, 1f, 0.5f, 1f), 1.3f);
            yield return new WaitForSeconds(1.2f);
        }

        // Epilog - UI akhir TUNGGAL: FX_SuccessOverlay (KarindingResultController) yang ada di scene.
        // ULANG = ulangi tutorial dari Fase 1 (hanya via klik), LANJUT = placeholder scene berikutnya.
        // Tidak ada auto-ngulang: checklist "jangan otomatis ngulang permainan" terpenuhi di sini.
        CurrentPhase = TutorialPhase.Epilog;
        Log("Phase: Epilog - pakai SuccessOverlay scene (tombol ULANG/LANJUT manual)");
        overlay?.ShowToast("Mantap! Semua tombol dikuasai", new Color(0.4f, 1f, 0.5f, 1f), 1.2f);
        yield return new WaitForSeconds(1.0f);
        PlayerPrefs.SetInt(playerPrefsKey, 1);
        PlayerPrefs.Save();
        Log($"Tutorial complete, saved {playerPrefsKey}=1");
        var storyListener = FindFirstObjectByType<StoryProgressListener>();
        if (storyListener != null) storyListener.AdvanceStoryState(StoryProgressListener.StoryState.HariPertama_BelajarKarinding_Selesai);

        CurrentPhase = TutorialPhase.Done;
        // Tutup overlay tutorial dulu: dulu di sini muncul 2 set tombol bertumpuk (ULANG dobel)
        // sehingga klik bisa "nyasar" dan permainan terasa ngulang sendiri.
        overlay?.HideInstruction(0.2f);
        overlay?.ClearAllHighlights();
        overlay?.SetProgressDotsVisible(false);
        overlay?.ShowDim(false, 0.2f);
        overlay?.HideAll(0.15f);

        if (rhythmFeedback == null) rhythmFeedback = FindFirstObjectByType<RhythmMissFeedback>();
        if (rhythmFeedback != null)
        {
            rhythmFeedback.SuppressResultOverlay = false;
            rhythmFeedback.ShowSuccessResult(fase3Result);
        }
        else
        {
            LogWarning("RhythmMissFeedback tidak ada di scene - SuccessOverlay akhir tidak tampil");
        }
        // Idle sampai player pilih ULANG/LANJUT - jangan auto Load, biar tidak ngulang tiba-tiba
        yield break;
    }

    private RhythmChartConfig PrepareSabarChart()
    {
        if (sabarChart == null) return CreateRuntimeSabarChart();
        // Jika chart punya banyak note, slice ke 4 pertama untuk Fase2 - easy tuning
        if (sabarChart.notes.Count > 4)
        {
            var clone = Instantiate(sabarChart);
            clone.name = sabarChart.name + "_RuntimeFase2";
            var sliced = new List<NoteData>();
            for (int i = 0; i < Mathf.Min(4, sabarChart.notes.Count); i++) sliced.Add(sabarChart.notes[i]);
            clone.notes = sliced;
            clone.noteTravelDuration = 2.0f; // mudah: lambat
            clone.maxMisses = 5;
            clone.perfectWindow = 0.15f; // mudah: window besar
            clone.goodWindow = 0.28f;
            return clone;
        }
        // Even for 4, enforce easy tuning
        sabarChart.noteTravelDuration = 2.0f;
        sabarChart.perfectWindow = 0.15f;
        sabarChart.goodWindow = 0.28f;
        return sabarChart;
    }

    /// <summary>Mainkan 1 chart sampai OnMinigameComplete, hasil via callback. Dipakai Fase 3 2-pass.</summary>
    private IEnumerator PlayChartOnce(RhythmChartConfig chart, System.Action<RhythmMinigameResult> onDone)
    {
        bool done = false;
        RhythmMinigameResult res = null;
        System.Action<RhythmMinigameResult> handler = (r) => { res = r; done = true; };
        rhythmController.OnMinigameComplete += handler;
        rhythmController.OnNoteJudged += HandleMissAbahLine;
        rhythmController.StartMinigame(chart, true); // start ter-script (bukan auto-loop)
        yield return new WaitUntil(() => done);
        rhythmController.OnMinigameComplete -= handler;
        rhythmController.OnNoteJudged -= HandleMissAbahLine;
        onDone?.Invoke(res);
    }

    /// <summary>Clone chart Resonansi dengan tuning lenient (attempt final dijamin sampai Epilog).</summary>
    private RhythmChartConfig PrepareLenientResonansiChart()
    {
        var src = resonansiChart != null ? resonansiChart : CreateRuntimeResonansiChart();
        var clone = Instantiate(src);
        clone.name = src.name + "_RuntimeLenient";
        clone.noteTravelDuration = Mathf.Max(src.noteTravelDuration, lenientTravelDuration);
        clone.perfectWindow = Mathf.Max(src.perfectWindow, lenientPerfectWindow);
        clone.goodWindow = Mathf.Max(src.goodWindow, lenientGoodWindow);
        clone.maxMisses = Mathf.Max(src.maxMisses, lenientMaxMisses);
        return clone;
    }

    private RhythmChartConfig CreateRuntimeSabarChart()
    {
        var cfg = ScriptableObject.CreateInstance<RhythmChartConfig>();
        cfg.chartId = "runtime_sabar_fase2";
        cfg.noteTravelDuration = 2.0f; // mudah
        cfg.perfectWindow = 0.15f;
        cfg.goodWindow = 0.28f;
        cfg.scorePerPerfect = 100;
        cfg.scorePerGood = 50;
        cfg.maxMisses = 5;
        cfg.notes = new List<NoteData>
        {
            new NoteData{ spawnTime = 2.0f, lane = LaneSide.LeftInner },
            new NoteData{ spawnTime = 4.5f, lane = LaneSide.RightInner },
            new NoteData{ spawnTime = 7.0f, lane = LaneSide.LeftInner },
            new NoteData{ spawnTime = 9.5f, lane = LaneSide.RightInner },
        };
        return cfg;
    }

    private RhythmChartConfig CreateRuntimeResonansiChart()
    {
        var cfg = ScriptableObject.CreateInstance<RhythmChartConfig>();
        cfg.chartId = "runtime_resonansi_fase3";
        cfg.noteTravelDuration = 1.4f;
        cfg.perfectWindow = 0.08f;
        cfg.goodWindow = 0.18f;
        cfg.scorePerPerfect = 100;
        cfg.scorePerGood = 50;
        cfg.maxMisses = 2;
        cfg.notes = new List<NoteData>
        {
            new NoteData{ spawnTime = 1.5f, lane = LaneSide.LeftInner },
            new NoteData{ spawnTime = 2.8f, lane = LaneSide.RightInner },
            new NoteData{ spawnTime = 4.0f, lane = LaneSide.LeftInner },
            new NoteData{ spawnTime = 5.2f, lane = LaneSide.RightInner },
            new NoteData{ spawnTime = 6.5f, lane = LaneSide.LeftOuter },
            new NoteData{ spawnTime = 7.8f, lane = LaneSide.RightOuter },
            new NoteData{ spawnTime = 9.0f, lane = LaneSide.LeftInner },
        };
        return cfg;
    }

    private string LaneToDisplay(LaneSide lane)
    {
        switch (lane)
        {
            case LaneSide.LeftOuter: return "kiri luar (D)";
            case LaneSide.LeftInner: return "kiri dalam (F)";
            case LaneSide.RightInner: return "kanan dalam (J)";
            case LaneSide.RightOuter: return "kanan luar (K)";
            default: return lane.ToString();
        }
    }

    private string LaneToDisplayAndroid(LaneSide lane)
    {
        // Android: pakai warna tombol (lebih intuitif touch) + tanpa D/F/J/K kecuali debug
        switch (lane)
        {
            case LaneSide.LeftOuter: return "tombol MERAH kiri";
            case LaneSide.LeftInner: return "tombol KUNING kiri-dalam";
            case LaneSide.RightInner: return "tombol HIJAU kanan-dalam";
            case LaneSide.RightOuter: return "tombol BIRU kanan";
            default: return lane.ToString();
        }
    }

    private Color GetLaneColor(LaneSide lane)
    {
        switch (lane)
        {
            case LaneSide.LeftOuter: return new Color(1f, 0.38f, 0.38f, 1f); // merah
            case LaneSide.LeftInner: return new Color(1f, 0.85f, 0.25f, 1f); // kuning
            case LaneSide.RightInner: return new Color(0.18f, 0.78f, 0.38f, 1f); // hijau
            case LaneSide.RightOuter: return new Color(0.22f, 0.65f, 0.92f, 1f); // biru
            default: return Color.white;
        }
    }

    private string LaneToKey(LaneSide lane)
    {
        switch (lane)
        {
            case LaneSide.LeftOuter: return "D";
            case LaneSide.LeftInner: return "F";
            case LaneSide.RightInner: return "J";
            case LaneSide.RightOuter: return "K";
            default: return "?";
        }
    }

    private IEnumerator ShowDialogue(string text)
    {
        if (dialogueSystem == null)
        {
            dialogueSystem = DialogueSystem.Instance;
            if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<DialogueSystem>();
        }
        if (dialogueSystem == null)
        {
            Debug.LogWarning("[TutorialController] DialogueSystem not found, log: " + text);
            yield return new WaitForSeconds(1.2f);
            yield break;
        }
        bool done = false;
        System.Action handler = null;
        handler = () => done = true;
        dialogueSystem.OnDialogueComplete += handler;
        dialogueSystem.ShowDialogue(text);
        yield return new WaitUntil(() => done);
        dialogueSystem.OnDialogueComplete -= handler;
        // Small pause after dialogue
        yield return new WaitForSeconds(0.15f);
    }

    public void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Log("LoadNextScene: nextSceneName empty, stay.");
            return;
        }
        // Check if scene in build
        bool exists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(nextSceneName)) { exists = true; break; }
        }
        if (!exists) LogWarning($"Scene {nextSceneName} not in BuildSettings, stay.");
        else SceneManager.LoadScene(nextSceneName);
    }

    public void SkipTutorial()
    {
        if (!allowSkipTutorial) return;
        StopAllCoroutines();
        overlay?.HideAll();
        dialogueSystem?.ForceDismiss();
        PlayerPrefs.SetInt(playerPrefsKey, 1);
        CurrentPhase = TutorialPhase.Done;
        LoadNextScene();
    }

    public void RestartTutorial()
    {
        Log("RestartTutorial - manual Coba Lagi kiri");
        StopAllCoroutines();
        if (rhythmFeedback == null) rhythmFeedback = FindFirstObjectByType<RhythmMissFeedback>();
        if (rhythmFeedback != null) rhythmFeedback.SuppressResultOverlay = true;
        overlay?.HideAll(0.2f);
        overlay?.ShowEpilogButtons(false);
        dialogueSystem?.ForceDismiss();
        // Reset state
        _handIndex = 0;
        _rushCount = 0;
        _streak = 0;
        _fase2FirstHintShown = false;
        _waitingForHandTap = false;
        _lastAbahMissTime = -999f;
        // Coroutine bisa mati sebelum unsubscribe (StopAllCoroutines di atas) —
        // cabut handler miss di sini supaya tidak ganda di run berikutnya.
        if (rhythmController == null) rhythmController = RhythmMinigameController.Instance;
        if (rhythmController == null) rhythmController = FindFirstObjectByType<RhythmMinigameController>();
        if (rhythmController != null) rhythmController.OnNoteJudged -= HandleMissAbahLine;
        CurrentPhase = TutorialPhase.Idle;
        StartCoroutine(DelayedRestart());
    }

    private System.Collections.IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(0.4f);
        StartTutorial();
    }

    private void Log(string msg) { if (logVerbose) Debug.Log("[TutorialController] " + msg); }
    private void LogWarning(string msg) { Debug.LogWarning("[TutorialController] " + msg); }
}
