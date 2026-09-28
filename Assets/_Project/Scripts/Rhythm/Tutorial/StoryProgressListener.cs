using UnityEngine;

/// <summary>
/// Bridge story progress untuk TUTORIAL RHYTHM + ASSEMBLY CRAFTING di project main.
/// DIVERGENSI dari project sumber: versi sumber subscribe ke CraftingAssemblyController
/// (sistem crafting S6). Versi ini meneruskan progres tutorial ke beat S3_TutorialRhythm
/// dan progres assembly crafting ke beat yang bisa dikonfigurasi (default S6_Assembly).
/// Jangan timpa file ini saat copy ulang script dari project sumber.
/// Dipakai (null-guarded) oleh KarindingTutorialController & KarindingResultController.
/// </summary>
public class StoryProgressListener : MonoBehaviour
{
    // Enum dipertahankan apa adanya: KarindingTutorialController memakai
    // StoryProgressListener.StoryState.HariPertama_BelajarKarinding_Selesai
    public enum StoryState
    {
        None,
        MasaKecil_MenyelesaikanKarinding,
        HariPertama_BelajarKarinding_Selesai
    }

    [Header("Assembly Crafting (Scene 6)")]
    [Tooltip("Beat yang ditandai saat assembly crafting selesai. Hanya dipakai bila ada CraftingAssemblyController di scene (scene tutorial tidak ada -> otomatis skip).")]
    [SerializeField] private MainKarinding.Core.Ch1Beat assemblyCompleteBeat = MainKarinding.Core.Ch1Beat.S6_Assembly;

    [Header("Debug")]
    [SerializeField] private bool logOnAdvance = true;

    private CraftingAssemblyController _asm;

    private void OnEnable()
    {
        // Langganan opsional: hanya ada di scene crafting (S6). Di scene tutorial
        // CraftingAssemblyController tidak ada -> lewati otomatis.
        _asm = CraftingAssemblyController.Instance;
        if (_asm == null) _asm = FindFirstObjectByType<CraftingAssemblyController>();
        if (_asm != null) _asm.OnAssemblyComplete += HandleAssemblyComplete;
    }

    private void OnDisable()
    {
        if (_asm != null) _asm.OnAssemblyComplete -= HandleAssemblyComplete;
        _asm = null;
    }

    private void HandleAssemblyComplete()
    {
        AdvanceStoryState(StoryState.MasaKecil_MenyelesaikanKarinding);
    }

    public void AdvanceStoryState(StoryState newState)
    {
        if (logOnAdvance)
            Debug.Log($"[StoryProgressListener] AdvanceStoryState: {newState}");

        // Teruskan ke sistem beat milik main (MainKarinding.Core.StoryState).
        // Fully-qualified: nama StoryState di scope ini = enum nested di atas.
        MainKarinding.Core.StoryState.EnsureExists();
        var story = MainKarinding.Core.StoryState.Instance;
        if (story == null)
        {
            Debug.LogWarning("[StoryProgressListener] MainKarinding.Core.StoryState.Instance null, progres hanya di-log.");
            return;
        }

        switch (newState)
        {
            case StoryState.HariPertama_BelajarKarinding_Selesai:
                // Tutorial rhythm selesai -> tandai beat S3_TutorialRhythm.
                story.SetBeat(MainKarinding.Core.Ch1Beat.S3_TutorialRhythm);
                break;
            case StoryState.MasaKecil_MenyelesaikanKarinding:
                // Assembly crafting selesai (S6) -> tandai beat konfigurasi.
                story.SetBeat(assemblyCompleteBeat);
                break;
            default:
                break;
        }
    }
}
