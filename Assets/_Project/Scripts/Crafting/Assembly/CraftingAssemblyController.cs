using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// State machine untuk assembly phase Cut (0-50%) -> Tap (50-100%) -> Completed.
/// Decoupled: hanya fire event, tidak hardcode story/dialogue.
/// </summary>
public class CraftingAssemblyController : MonoBehaviour
{
    public static CraftingAssemblyController Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private CraftingAssemblyConfig config;

    [Header("UI References (optional - auto-find jika null)")]
    [SerializeField] private CraftingCutInputView cutInputView;
    [SerializeField] private CraftingTapInputView tapInputView;
    [SerializeField] private CraftingAssemblyProgressView progressView;
    [SerializeField] private CanvasGroup assemblyRootGroup;

    [Header("Start Cut Button (muncul setelah cutscene)")]
    [SerializeField] private GameObject startCutButtonGO;
    [SerializeField] private UnityEngine.UI.Button startCutButton;

    public event Action<float, AssemblyPhase> OnProgressChanged;
    public event Action<AssemblyPhase> OnPhaseChanged;
    public event Action OnAssemblyComplete;
    public event Action OnAssemblyFailed;
    public event Action<int> OnTapFallCountChanged;

    public float Progress => progress;
    public AssemblyPhase CurrentPhase => currentPhase;
    public float TimeSinceLastTap => timeSinceLastTap;
    public bool IsCompleted => hasCompletedFired;
    public CraftingAssemblyConfig Config => config;
    public int TapFallCount => tapFallCount;

    private float progress;
    private AssemblyPhase currentPhase;
    private float timeSinceLastTap;
    private bool hasCompletedFired;
    private bool isAssemblyActive;
    private int tapFallCount;
    private bool wasAtFloor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (config == null)
            Debug.LogWarning("[CraftingAssemblyController] Config belum di-assign. Pakai fallback default.");

        if (cutInputView == null) cutInputView = GetComponentInChildren<CraftingCutInputView>(true);
        if (tapInputView == null) tapInputView = GetComponentInChildren<CraftingTapInputView>(true);
        if (progressView == null) progressView = GetComponentInChildren<CraftingAssemblyProgressView>(true);

        if (startCutButtonGO == null)
        {
            var found = GameObject.Find("StartCutButton");
            if (found != null) startCutButtonGO = found;
        }
        if (startCutButton == null && startCutButtonGO != null)
            startCutButton = startCutButtonGO.GetComponent<UnityEngine.UI.Button>();

        if (startCutButton != null)
        {
            startCutButton.onClick.RemoveListener(OnStartCutButtonClicked);
            startCutButton.onClick.AddListener(OnStartCutButtonClicked);
        }

        // Pastikan assemblyRoot & start button hidden awal
        if (assemblyRootGroup != null) assemblyRootGroup.gameObject.SetActive(false);
        if (startCutButtonGO != null) startCutButtonGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (startCutButton != null)
            startCutButton.onClick.RemoveListener(OnStartCutButtonClicked);
    }

    private void Update()
    {
        if (!isAssemblyActive || hasCompletedFired) return;
        if (currentPhase != AssemblyPhase.Tapping) return;

        timeSinceLastTap += Time.deltaTime;

        float grace = config != null ? config.tapDecayGraceTime : 0.25f;
        float decayRate = config != null ? config.tapDecayRate : 15f;

        if (timeSinceLastTap > grace)
        {
            float minClamp = 50f;
            if (config != null) minClamp = config.cuttingToTappingThreshold;

            float newProgress = progress - decayRate * Time.deltaTime;
            newProgress = Mathf.Max(minClamp, newProgress);

            // Deteksi transisi ke floor (50%) — fall counting
            bool hitFloor = progress > minClamp + 0.01f && newProgress <= minClamp + 0.01f;
            if (hitFloor && !wasAtFloor)
            {
                wasAtFloor = true;
                tapFallCount++;
                Debug.Log($"[CraftingAssemblyController] Fall {tapFallCount}/{GetMaxFalls()} -> balik ke 50%");
                OnTapFallCountChanged?.Invoke(tapFallCount);
                if (progressView != null) progressView.ShowToast($"Jatuh {tapFallCount}/{GetMaxFalls()}", 1f);
                if (progressView != null) progressView.PlayFallFeedback();

                if (tapFallCount >= GetMaxFalls())
                {
                    Debug.Log("[CraftingAssemblyController] Failed 2 falls -> reset ke Cutting");
                    FailAssembly();
                    return;
                }
            }
            else if (newProgress > minClamp + 0.5f)
            {
                wasAtFloor = false;
            }

            if (!Mathf.Approximately(newProgress, progress))
            {
                progress = newProgress;
                OnProgressChanged?.Invoke(progress, currentPhase);
                if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);
            }
        }
        else
        {
            if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);
        }
    }

    private int GetMaxFalls()
    {
        if (config != null) return Mathf.Max(1, config.maxTapFallsBeforeFail);
        return 2;
    }

    public void StartAssembly()
    {
        progress = 0f;
        currentPhase = AssemblyPhase.Cutting;
        timeSinceLastTap = 0f;
        hasCompletedFired = false;
        isAssemblyActive = true;
        tapFallCount = 0;
        wasAtFloor = false;

        Debug.Log("[CraftingAssemblyController] StartAssembly - phase Cutting, progress 0");

        UpdateUIVisibility();
        OnProgressChanged?.Invoke(progress, currentPhase);
        OnPhaseChanged?.Invoke(currentPhase);
        if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);

        // DIVERGENSI main: blok tray dihapus — CraftingTrayController tidak dibawa ke main
        // (tray hanya dipakai fase search yang sudah dihapus; tidak ada tray di scene assembly).

        if (assemblyRootGroup != null)
        {
            assemblyRootGroup.alpha = 1f;
            assemblyRootGroup.blocksRaycasts = true;
            assemblyRootGroup.interactable = true;
            assemblyRootGroup.gameObject.SetActive(true);
        }

        // Preview mini pojok kanan atas dihapus permanen — pastikan tetap mati
        var miniGO = GameObject.Find("Canvas/AssemblyRoot/KarindingPreviewMini");
        if (miniGO != null) miniGO.SetActive(false);
        var prevCamGO = GameObject.Find("KarindingPreviewCamera_Selesai");
        if (prevCamGO != null) prevCamGO.GetComponent<Camera>().enabled = true;
    }

    public void AddCutProgress(float straightnessRatio = 1f)
    {
        if (!isAssemblyActive || hasCompletedFired) return;
        if (currentPhase != AssemblyPhase.Cutting) return; // silent ignore cross-phase

        float inc = config != null ? config.cutIncrement : 10f;
        float bonus = 0f;
        if (config != null && config.cutStraightnessBonus > 0f && straightnessRatio > 0.95f)
            bonus = config.cutStraightnessBonus;

        progress += inc + bonus;

        float threshold = config != null ? config.cuttingToTappingThreshold : 50f;
        if (progress >= threshold)
        {
            progress = threshold; // clamp exactly 50 on transition
            OnProgressChanged?.Invoke(progress, currentPhase);
            if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);
            SwitchToTapping();
            return;
        }

        progress = Mathf.Clamp(progress, 0f, 100f);
        OnProgressChanged?.Invoke(progress, currentPhase);
        if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);

        Debug.Log($"[CraftingAssemblyController] Cut valid straightness={straightnessRatio:F2} +{inc + bonus} -> progress={progress:F1}");

        CheckCompletion();
    }

    public void AddTapProgress()
    {
        if (!isAssemblyActive || hasCompletedFired) return;
        if (currentPhase != AssemblyPhase.Tapping) return;

        float inc = config != null ? config.tapIncrement : 2.5f;
        timeSinceLastTap = 0f;

        float newProg = Mathf.Min(config != null ? config.completionThreshold : 100f, progress + inc);
        bool leftFloor = progress <= 50.5f && newProg > 50.5f;
        progress = newProg;
        if (leftFloor) wasAtFloor = false;

        OnProgressChanged?.Invoke(progress, currentPhase);
        if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);

        Debug.Log($"[CraftingAssemblyController] Tap +{inc} -> progress={progress:F1} fall {tapFallCount}/{GetMaxFalls()}");

        CheckCompletion();
    }

    private void SwitchToTapping()
    {
        currentPhase = AssemblyPhase.Tapping;
        timeSinceLastTap = 0f;
        Debug.Log("[CraftingAssemblyController] Switch to Tapping (50%)");
        UpdateUIVisibility();
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void CheckCompletion()
    {
        float threshold = config != null ? config.completionThreshold : 100f;
        if (progress >= threshold && !hasCompletedFired)
        {
            CompleteAssembly();
        }
    }

    private void CompleteAssembly()
    {
        if (hasCompletedFired) return;
        hasCompletedFired = true;
        currentPhase = AssemblyPhase.Completed;
        progress = config != null ? config.completionThreshold : 100f;
        isAssemblyActive = false;

        Debug.Log("[CraftingAssemblyController] Completed! progress=100");

        OnProgressChanged?.Invoke(progress, currentPhase);
        OnPhaseChanged?.Invoke(currentPhase);
        if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);
        if (progressView != null) progressView.PlayCompleteFeedback();

        UpdateUIVisibility();

        if (assemblyRootGroup != null)
        {
            assemblyRootGroup.blocksRaycasts = false;
            assemblyRootGroup.interactable = false;
        }

        // DIVERGENSI main: reveal via CraftingPuzzleController dihapus — controller search
        // tidak dibawa ke main dan OnPuzzleComplete tidak ada subscriber di sini.
        // UI akhir ditangani PlayCompleteFeedback() -> SuccessOverlay di atas.

        OnAssemblyComplete?.Invoke();
    }

    private void UpdateUIVisibility()
    {
        bool isCutting = currentPhase == AssemblyPhase.Cutting && isAssemblyActive;
        bool isTapping = currentPhase == AssemblyPhase.Tapping && isAssemblyActive;
        bool isCompleted = currentPhase == AssemblyPhase.Completed || hasCompletedFired;

        if (cutInputView != null) cutInputView.SetActive(isCutting);
        if (tapInputView != null) tapInputView.SetActive(isTapping);

        if (progressView != null) progressView.SetPhaseVisible(isAssemblyActive || isCompleted);

        // Saat completed, progress tetap visible untuk reveal; assemblyRoot tetap active tapi tidak interaktif
        // Pemanggil bisa hide manual setelah delay jika mau: assemblyRootGroup.DOFade(0,0.5f).OnComplete(()=>gameObject.SetActive(false))
    }

    private void FailAssembly()
    {
        Debug.Log("[CraftingAssemblyController] FailAssembly - 2 falls, reset ke Cutting 0%");
        isAssemblyActive = false;
        hasCompletedFired = false;
        wasAtFloor = false;
        tapFallCount = 0;

        // Toast sudah di-trigger di Update sebelum fail, tambah toast gagal
        if (progressView != null) progressView.ShowToast("Gagal! Ulangi potong", 1.5f);
        if (progressView != null) progressView.PlayFailFeedback();

        // Delay sedikit biar feedback keliatan, lalu reset ke Cutting
        DOVirtual.DelayedCall(0.9f, () =>
        {
            progress = 0f;
            currentPhase = AssemblyPhase.Cutting;
            timeSinceLastTap = 0f;
            isAssemblyActive = true;
            hasCompletedFired = false;
            UpdateUIVisibility();
            OnProgressChanged?.Invoke(progress, currentPhase);
            OnPhaseChanged?.Invoke(currentPhase);
            if (progressView != null) progressView.SetProgress(progress, currentPhase, timeSinceLastTap);
            OnAssemblyFailed?.Invoke();
            Debug.Log("[CraftingAssemblyController] Reset to Cutting after fail");
        });
    }

    /// <summary> Untuk debug / testing - reset tanpa trigger complete. </summary>
    public void DebugReset()
    {
        hasCompletedFired = false;
        isAssemblyActive = false;
        progress = 0f;
        currentPhase = AssemblyPhase.Cutting;
        timeSinceLastTap = 0f;
        tapFallCount = 0;
        wasAtFloor = false;
        UpdateUIVisibility();
        OnProgressChanged?.Invoke(progress, currentPhase);
        OnPhaseChanged?.Invoke(currentPhase);
    }

    public void SetConfig(CraftingAssemblyConfig newConfig)
    {
        config = newConfig;
    }

    /// <summary>
    /// Dipanggil oleh CraftingAssemblyTransitionController setelah cutscene.
    /// Munculkan tombol MULAI MEMOTONG, belum langsung mulai Cut.
    /// </summary>
    public void ShowStartCutButton()
    {
        // DIVERGENSI main: blok tray dihapus (alasan sama seperti di StartAssembly).

        progress = 0f;
        currentPhase = AssemblyPhase.Cutting;
        timeSinceLastTap = 0f;
        hasCompletedFired = false;
        isAssemblyActive = false;
        tapFallCount = 0;
        wasAtFloor = false;

        if (assemblyRootGroup != null)
            assemblyRootGroup.gameObject.SetActive(false);

        UpdateUIVisibility();

        // Cari ulang jika null (scene load)
        if (startCutButtonGO == null)
        {
            var found = GameObject.Find("StartCutButton");
            if (found != null) startCutButtonGO = found;
        }
        if (startCutButton == null && startCutButtonGO != null)
            startCutButton = startCutButtonGO.GetComponent<UnityEngine.UI.Button>();

        if (startCutButtonGO != null)
        {
            startCutButtonGO.SetActive(true);
            // Punch anim
            try
            {
                DOTween.Kill(startCutButtonGO.transform);
                startCutButtonGO.transform.localScale = Vector3.one;
                startCutButtonGO.transform.DOPunchScale(Vector3.one * 0.08f, 0.4f, 4, 0.6f);
            }
            catch { }
        }

        Debug.Log("[CraftingAssemblyController] ShowStartCutButton - menunggu player klik MULAI MEMOTONG");
    }

    public void OnStartCutButtonClicked()
    {
        Debug.Log("[CraftingAssemblyController] StartCutButton clicked -> StartAssembly");
        if (startCutButtonGO != null) startCutButtonGO.SetActive(false);
        StartAssembly();
    }

    /// <summary> Cek apakah assembly sudah siap tapi menunggu tombol start. </summary>
    public bool IsWaitingForStartButton => startCutButtonGO != null && startCutButtonGO.activeSelf;
}
