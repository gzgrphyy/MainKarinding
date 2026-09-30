using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Launcher alur produksi MinigameRhythm.unity untuk Scene 7 (PLAN E7).
/// Beda dengan DebugRhythmTester (klik manual untuk debug):
/// auto-start 1 ronde standar -> overlay Success (dengan skor) / Fail (retry).
/// Ulang = main lagi, Lanjut = balik ke teras (closing Scene 7).
/// Story beats: lolos -> S7_RhythmKedua + EntryPoint Scene7 (sebelum user klik Lanjut).
/// </summary>
public class StandardRhythmLauncher : MonoBehaviour
{
    /// <summary>Chart yang dimainkan: "standard" (default) atau "hard". Di-set Scene7CutsceneController sebelum LoadScene.</summary>
    public static string requestedChartId = "standard";

    [Header("Charts")]
    [SerializeField] private RhythmChartConfig standardChart;
    [SerializeField] private RhythmChartConfig hardChart; // opsional, boleh null (fallback ke standar)

    [Header("Return")]
    [SerializeField] private string returnSceneName = "Ch1_TerasRumahKebun";

    [Header("References (auto-find bila null)")]
    [SerializeField] private RhythmMinigameController rhythm;
    [SerializeField] private RhythmMissFeedback feedback;
    [SerializeField] private KarindingResultController resultController;
    [SerializeField] private Button debugStartButton; // Btn_StartTest — disembunyikan di alur produksi

    private void Start()
    {
        if (rhythm == null) rhythm = RhythmMinigameController.Instance;
        if (rhythm == null) rhythm = FindFirstObjectByType<RhythmMinigameController>();
        if (feedback == null) feedback = FindFirstObjectByType<RhythmMissFeedback>();
        if (resultController == null) resultController = FindFirstObjectByType<KarindingResultController>();
        if (debugStartButton == null)
        {
            var go = GameObject.Find("Btn_StartTest");
            if (go != null) debugStartButton = go.GetComponent<Button>();
        }
        if (debugStartButton != null) debugStartButton.gameObject.SetActive(false);

        if (rhythm == null)
        {
            Debug.LogError("[S7Launcher] RhythmMinigameController tidak ditemukan — auto-start dibatalkan.");
            return;
        }

        if (resultController != null && !string.IsNullOrEmpty(returnSceneName))
            resultController.SetNextScene(returnSceneName);

        // Ronde normal: skor + overlay tampil apa adanya (Success/Fail via RhythmMissFeedback).
        rhythm.FreePlayMode = false;
        if (feedback != null) feedback.SuppressSuccessOverlay = false;
        rhythm.OnMinigameComplete += HandleComplete;

        RhythmChartConfig chart = ResolveChart(requestedChartId);
        if (chart == null)
        {
            Debug.LogError("[S7Launcher] Chart '" + requestedChartId + "' tidak tersedia — auto-start dibatalkan.");
            return;
        }
        Debug.Log("[S7Launcher] Auto-start ronde (" + chart.chartId + ")");
        rhythm.StartMinigame(chart, true);
    }

    private void OnDestroy()
    {
        if (rhythm != null) rhythm.OnMinigameComplete -= HandleComplete;
    }

    private RhythmChartConfig ResolveChart(string id)
    {
        if (id == "hard")
        {
            if (hardChart != null) return hardChart;
            Debug.LogWarning("[S7Launcher] Hard chart belum ada — fallback ke standar.");
        }
        return standardChart;
    }

    private void HandleComplete(RhythmMinigameResult result)
    {
        if (result == null) return;
        if (!result.passed)
        {
            Debug.Log("[S7Launcher] Ronde gagal — menunggu retry manual (overlay Fail).");
            return;
        }
        // Siapkan state balik ke teras. Overlay Success (dengan skor) tampil via RhythmMissFeedback;
        // Ulang = main lagi, Lanjut = balik teras (closing Scene 7).
        MainKarinding.Core.StoryState.EnsureExists();
        var story = MainKarinding.Core.StoryState.Instance;
        if (story != null)
        {
            story.SetBeat(MainKarinding.Core.Ch1Beat.S7_RhythmKedua);
            story.SetEntryPoint(MainKarinding.Core.Ch1EntryPoint.Scene7);
        }
        Debug.Log("[S7Launcher] Ronde lolos, skor=" + result.totalScore + " — overlay Success tampil (Ulang/Lanjut).");
    }
}
