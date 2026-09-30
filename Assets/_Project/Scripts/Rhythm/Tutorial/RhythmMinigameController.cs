using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmMinigameController : MonoBehaviour
{
    public static RhythmMinigameController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NoteView noteViewPrefab;
    [SerializeField] private Transform notesContainer;
    [SerializeField] private AudioSource audioSource;

    [Header("Track Geometry (UI anchored positions)")]
    [Tooltip("Spawn X (anchored) untuk note, biasanya +canvasWidth/2. Semua note spawn di kanan track.")]
    [SerializeField] private float noteSpawnX = 960f;

    [Tooltip("Hit X (anchored) untuk note, biasanya 0 (tengah canvas).")]
    [SerializeField] private float noteHitX = 0f;

    [Tooltip("Y (anchored) track — semua note jalan pada Y ini.")]
    [SerializeField] private float noteTrackY = 0f;

    public event Action<NoteData> OnNoteSpawned;
    public event Action<NoteData, HitGrade> OnNoteJudged;
    public event Action<RhythmMinigameResult> OnMinigameComplete;

    public int CurrentScore => _result != null ? _result.totalScore : 0;
    public int CurrentCombo => _combo;
    public float SongTime => _songTime;
    public bool IsPlaying => _isPlaying;
    public bool IsCountingDown => _isCountingDown;
    public bool IsPaused => _isPaused;

    public event Action<int> OnCountdownTick; // 3,2,1,0(GO)

    public float TotalDuration
    {
        get
        {
            if (_activeConfig != null && _activeConfig.backgroundMusic != null)
                return _activeConfig.backgroundMusic.length;
            if (_pendingSpawns.Count > 0)
                return _pendingSpawns[_pendingSpawns.Count - 1].spawnTime + _activeConfig.noteTravelDuration;
            return 0f;
        }
    }

    [Header("Countdown")]
    [Tooltip("Durasi countdown detik sebelum mulai (3 = 3,2,1,GO). 0 = tanpa countdown.")]
    [SerializeField] private int countdownSeconds = 3;

    [Header("Free Play (Scene 7)")]
    [Tooltip("True = tap bebas tanpa skor/miss: note yang lewat despawn diam-diam, judgement hanya visual+suara, ronde selalu passed. Di-set StandardRhythmLauncher.")]
    [SerializeField] private bool freePlayMode = false;

    /// <summary>Mode tap bebas Scene 7. Persistent — tidak di-reset oleh StartMinigame, jadi RetryLast ikut mode yang sama.</summary>
    public bool FreePlayMode { get { return freePlayMode; } set { freePlayMode = value; } }

    private RhythmChartConfig _activeConfig;
    private float _songTime;
    private bool _isPlaying;
    private bool _isCountingDown;
    private bool _isPaused;
    private Coroutine _countdownCoroutine;
    private RhythmMinigameResult _result;
    private int _combo;

    private struct ActiveNote
    {
        public NoteData data;
        public NoteView view;
    }

    private readonly List<ActiveNote> _activeNotes = new List<ActiveNote>();
    private readonly List<NoteData> _pendingSpawns = new List<NoteData>();
    private int _spawnCursor;
    private int _judgedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Anti auto-loop: setelah 1 ronde selesai, StartMinigame DITOLAK sampai ada ULANG manual
    // (PrepareManualRestart / RetryLast) atau start ter-script eksplisit (allowDuringResult = true).
    private string _lastStartStack = "";
    private bool _allowManualRestart = false;
    private bool _awaitingManualRestart = false;

    /// <summary>True = ronde sudah selesai dan sedang menunggu keputusan manual (klik ULANG / LANJUT).</summary>
    public bool IsAwaitingManualRestart => _awaitingManualRestart;

    public void PrepareManualRestart() { _allowManualRestart = true; }

    /// <summary>Start normal. Ditolak bila ronde sebelumnya sudah selesai dan belum ada ULANG manual.</summary>
    public void StartMinigame(RhythmChartConfig config) { StartMinigame(config, false); }

    /// <summary>
    /// Start eksplisit dari skrip (mis. rangkaian tutorial Fase 2 -> Fase 3) supaya tidak diblok
    /// aturan "harus klik ULANG dulu" setelah ronde sebelumnya selesai.
    /// </summary>
    public void StartMinigame(RhythmChartConfig config, bool allowDuringResult)
    {
        if (config == null)
        {
            Debug.LogError("[RhythmMinigameController] StartMinigame called with null config.");
            return;
        }

        // Guard deterministik (bukan berbasis waktu): setelah complete, ronde baru hanya boleh
        // dari ULANG manual atau start ter-script — jadi tidak ada "otomatis ngulang".
        if (_awaitingManualRestart && !_allowManualRestart && !allowDuringResult)
        {
            Debug.LogWarning("[RhythmMinigameController] BLOCKED StartMinigame - ronde sudah selesai, tunggu klik ULANG. caller=" + System.Environment.StackTrace.Split('\n')[1].Trim());
            return;
        }
        _allowManualRestart = false;
        _awaitingManualRestart = false;
        _lastStartStack = System.Environment.StackTrace;

        ClearActiveNotes();
        _activeConfig = config;
        _songTime = 0f;
        _isPlaying = false;
        _isCountingDown = false;
        _isPaused = false;
        _spawnCursor = 0;
        _judgedCount = 0;
        _combo = 0;
        _result = new RhythmMinigameResult();

        _pendingSpawns.Clear();
        _pendingSpawns.AddRange(config.notes);
        _pendingSpawns.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        Time.timeScale = 1f;
        if (_countdownCoroutine != null) { StopCoroutine(_countdownCoroutine); _countdownCoroutine = null; }

        // Countdown before actually playing
        if (countdownSeconds > 0)
        {
            _isCountingDown = true;
            _countdownCoroutine = StartCoroutine(CountdownRoutine(countdownSeconds));
        }
        else
        {
            BeginPlaying();
        }

        Debug.Log("[RhythmMinigameController] StartMinigame chartId=" + config.chartId +
                  " notes=" + _pendingSpawns.Count +
                  " travelDuration=" + config.noteTravelDuration + " countdown=" + countdownSeconds + " caller=" + _lastStartStack.Split('\n')[1].Trim());
    }

    private void BeginPlaying()
    {
        _isPlaying = true;
        _isCountingDown = false;
        if (audioSource != null && _activeConfig.backgroundMusic != null)
        {
            audioSource.clip = _activeConfig.backgroundMusic;
            audioSource.loop = false;
            audioSource.Play();
        }
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSecondsRealtime(1f);
        }
        OnCountdownTick?.Invoke(0); // 0 = GO
        // small extra frame for GO visibility
        yield return new WaitForSecondsRealtime(0.5f);
        _countdownCoroutine = null;
        BeginPlaying();
    }

    public bool RetryLast()
    {
        if (_activeConfig == null) return false;
        _allowManualRestart = true;
        Debug.Log("[RhythmMinigameController] RetryLast dipanggil manual (klik ULANG) stack=" + System.Environment.StackTrace.Split('\n')[1].Trim());
        StartMinigame(_activeConfig);
        return true;
    }

    public bool IsInputBlocked => _isCountingDown || _isPaused;

    // Returns TapResult for differentiated feedback; Hit cases invoke RegisterJudgement internally.
    public TapResult HandleLaneButtonPressed(LaneSide lane)
    {
        if (!_isPlaying || _isCountingDown || _isPaused) return TapResult.EmptyNoNote;
        if (_activeConfig == null) return TapResult.EmptyNoNote;

        float now = _songTime;
        float goodWin = _activeConfig.goodWindow;
        float perfectWin = _activeConfig.perfectWindow;

        int bestIndex = -1;
        float bestAbsDelta = float.MaxValue;

        for (int i = 0; i < _activeNotes.Count; i++)
        {
            var an = _activeNotes[i];
            if (an.view == null || an.view.Judged) continue;
            if (an.data.lane != lane) continue;

            float delta = Mathf.Abs(now - an.data.spawnTime);
            if (delta > goodWin) continue;
            if (delta < bestAbsDelta)
            {
                bestAbsDelta = delta;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            var hit = _activeNotes[bestIndex];
            HitGrade grade = bestAbsDelta <= perfectWin ? HitGrade.Perfect : HitGrade.Good;
            RegisterJudgement(hit, grade);
            return grade == HitGrade.Perfect ? TapResult.HitPerfect : TapResult.HitGood;
        }

        // No hit: differentiate WrongLane vs EmptyNoNote
        bool anyNoteInWindow = false;
        bool noteInOtherLane = false;
        for (int i = 0; i < _activeNotes.Count; i++)
        {
            var an = _activeNotes[i];
            if (an.view == null || an.view.Judged) continue;
            float delta = Mathf.Abs(now - an.data.spawnTime);
            if (delta <= goodWin)
            {
                anyNoteInWindow = true;
                if (an.data.lane != lane) noteInOtherLane = true;
            }
        }
        if (noteInOtherLane)
        {
            Debug.Log("[RhythmMinigameController] tap wrong lane=" + lane + " (note in other lane within window)");
            return TapResult.WrongLane;
        }
        Debug.Log("[RhythmMinigameController] tap ignored lane=" + lane + " (no note in window)");
        return anyNoteInWindow ? TapResult.EmptyNoNote : TapResult.EmptyNoNote;
    }

    // Legacy bool wrapper for callers that only need hit/empty
    public bool HandleLaneButtonPressedLegacy(LaneSide lane)
    {
        var r = HandleLaneButtonPressed(lane);
        return r == TapResult.HitPerfect || r == TapResult.HitGood;
    }

    public void NotifyNoteReachedHitZone(NoteView view)
    {
        if (view == null) return;
        if (view.Judged) return;

        for (int i = 0; i < _activeNotes.Count; i++)
        {
            if (_activeNotes[i].view == view)
            {
                if (freePlayMode)
                {
                    // Free-play: note lewat tanpa penalti — despawn diam-diam, tetap hitung selesai.
                    var silent = _activeNotes[i];
                    if (silent.view != null) silent.view.MarkJudged();
                    _activeNotes.Remove(silent);
                    _judgedCount++;
                    if (silent.view != null) Destroy(silent.view.gameObject);
                    TryCompleteMinigame();
                    return;
                }
                RegisterJudgement(_activeNotes[i], HitGrade.Miss);
                return;
            }
        }
    }

    private void RegisterJudgement(ActiveNote an, HitGrade grade)
    {
        if (an.view != null) an.view.MarkJudged();
        _activeNotes.Remove(an);
        _judgedCount++;

        if (freePlayMode)
        {
            // PLAN E7: tap bebas — hanya visual + suara, tanpa skor/kombo/miss.
            OnNoteJudged?.Invoke(an.data, grade);
            if (an.view != null)
            {
                if (grade == HitGrade.Perfect) an.view.PlayPerfectFeedback();
                else if (grade == HitGrade.Good) an.view.PlayGoodFeedback();
                else Destroy(an.view.gameObject);
            }
            TryCompleteMinigame();
            return;
        }

        if (grade == HitGrade.Perfect) _result.perfectCount++;
        else if (grade == HitGrade.Good) _result.goodCount++;
        else _result.missCount++;

        if (grade == HitGrade.Miss) _combo = 0;
        else _combo++;

        int baseScore = 0;
        if (grade == HitGrade.Perfect) baseScore = _activeConfig.scorePerPerfect;
        else if (grade == HitGrade.Good) baseScore = _activeConfig.scorePerGood;

        int scoreAdd = baseScore;
        if (grade != HitGrade.Miss && _activeConfig.comboStep > 0 && _activeConfig.comboMultiplier > 0f)
        {
            int steps = _combo / _activeConfig.comboStep;
            float mult = 1f + steps * _activeConfig.comboMultiplier;
            scoreAdd = Mathf.RoundToInt(baseScore * mult);
        }
        _result.totalScore += scoreAdd;

        Debug.Log("[RhythmMinigameController] judged lane=" + an.data.lane +
                  " grade=" + grade + " score+=" + scoreAdd +
                  " total=" + _result.totalScore);

        OnNoteJudged?.Invoke(an.data, grade);

        if (an.view != null)
        {
            switch (grade)
            {
                case HitGrade.Miss:
                    an.view.PlayMissFeedback();
                    break;
                case HitGrade.Perfect:
                    an.view.PlayPerfectFeedback();
                    break;
                case HitGrade.Good:
                    an.view.PlayGoodFeedback();
                    break;
                default:
                    Destroy(an.view.gameObject);
                    break;
            }
        }

        TryCompleteMinigame();
    }

    // Pause API - uses Time.timeScale for simple freeze, audio handled separately
    public void PauseMinigame()
    {
        if (!_isPlaying || _isPaused || _isCountingDown) return;
        _isPaused = true;
        Time.timeScale = 0f;
        if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
    }

    public void ResumeMinigame()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        if (audioSource != null && _activeConfig != null && _activeConfig.backgroundMusic != null) audioSource.UnPause();
    }

    private void Update()
    {
        if (!_isPlaying || _isCountingDown || _isPaused || _activeConfig == null) return;

        _songTime += Time.deltaTime;

        while (_spawnCursor < _pendingSpawns.Count)
        {
            var note = _pendingSpawns[_spawnCursor];
            float spawnAt = note.spawnTime - _activeConfig.noteTravelDuration;
            if (_songTime < spawnAt) break;
            SpawnNote(note);
            _spawnCursor++;
        }

        if (_judgedCount >= _pendingSpawns.Count && _pendingSpawns.Count > 0)
        {
            CompleteMinigame();
        }
    }

    private void SpawnNote(NoteData note)
    {
        if (noteViewPrefab == null)
        {
            Debug.LogError("[RhythmMinigameController] noteViewPrefab not assigned.");
            return;
        }

        var view = Instantiate(noteViewPrefab, notesContainer != null ? notesContainer : transform);
        view.Setup(note, _activeConfig.noteTravelDuration, this);
        view.BeginTravel(new Vector2(noteSpawnX, noteTrackY), new Vector2(noteHitX, noteTrackY));
        _activeNotes.Add(new ActiveNote { data = note, view = view });

        OnNoteSpawned?.Invoke(note);
    }

    private void TryCompleteMinigame()
    {
        if (_pendingSpawns.Count == 0) return;
        if (_judgedCount >= _pendingSpawns.Count)
        {
            CompleteMinigame();
        }
    }

    private void CompleteMinigame()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        _isPaused = false;
        _isCountingDown = false;
        Time.timeScale = 1f;

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        int total = _pendingSpawns.Count;
        _result.accuracy = _result.GetAccuracy(total);
        _result.rank = RhythmMinigameResult.GetRank(_result.accuracy);
        _result.passed = freePlayMode ? true : _result.missCount < _activeConfig.maxMisses;
        // Ronde selesai -> kunci start otomatis. Ronde baru hanya via klik ULANG (RetryLast)
        // atau start ter-script (tutorial Fase 3).
        _awaitingManualRestart = true;
        Debug.Log("[RhythmMinigameController] complete score=" + _result.totalScore +
                  " perfect=" + _result.perfectCount +
                  " good=" + _result.goodCount +
                  " miss=" + _result.missCount +
                  " accuracy=" + _result.accuracy.ToString("F1") + "% rank=" + _result.rank +
                  " passed=" + _result.passed + " (overlay manual, tunggu klik Ulang)");
        OnMinigameComplete?.Invoke(_result);
    }

    private void ClearActiveNotes()
    {
        for (int i = 0; i < _activeNotes.Count; i++)
        {
            if (_activeNotes[i].view != null) Destroy(_activeNotes[i].view.gameObject);
        }
        _activeNotes.Clear();
    }
}
