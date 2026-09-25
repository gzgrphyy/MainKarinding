using UnityEngine;

/// <summary>
/// Bridge story progress untuk TUTORIAL RHYTHM di project main.
/// DIVERGENSI dari project sumber: versi sumber subscribe ke CraftingAssemblyController
/// (sistem crafting S6), yang belum ada di main. Versi ini meneruskan progres tutorial
/// ke MainKarinding.Core.StoryState (beat S3_TutorialRhythm).
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

    [Header("Debug")]
    [SerializeField] private bool logOnAdvance = true;

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
                // Berasal dari sistem crafting (S6_Assembly) yang belum ada di main.
                // Biarkan log saja sampai crafting diimplementasikan.
                break;
            default:
                break;
        }
    }
}
