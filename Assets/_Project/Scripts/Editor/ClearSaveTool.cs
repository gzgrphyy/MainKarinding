using UnityEditor;
using UnityEngine;
using Karinding.Core;

public static class ClearSaveTool
{
    [MenuItem("Karinding/Save/Clear Save (hapus progress)")]
    public static void ClearSave()
    {
        SaveSystem.Clear();
        PlayerPrefs.DeleteKey("dialogue_show_translation");
        PlayerPrefs.Save();
        Debug.Log("[ClearSaveTool] Save dihapus. Play lagi akan mulai dari S1. Jika dialog masih tidak muncul, cek DialogueCanvas scale (harus 1,1,1).");
        if (EditorUtility.DisplayDialog("Save Dihapus", "Save file & PlayerPrefs terjemahan sudah dihapus.\n\nPlay lagi dari awal akan muncul S1_Teras.\n\nJika masih tidak muncul, jalankan:\nKarinding -> Dialogue -> Build Dialogue UI (Overwrite)\nKarinding -> Setup -> Chapter1 Scene", "OK"))
        {
        }
    }

    [MenuItem("Karinding/Save/Buka Lokasi Save")]
    public static void OpenSaveLocation()
    {
        var path = SaveSystem.GetPath();
        Debug.Log($"[ClearSaveTool] Save path: {path} | Exists: {SaveSystem.HasSave()}");
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Karinding/Save/Force Start From S1 (debug)")]
    public static void ForceStartFromS1()
    {
        SaveSystem.Clear();
        PlayerPrefs.DeleteKey("dialogue_show_translation");
        PlayerPrefs.Save();
        Debug.Log("[ClearSaveTool] Force Start From S1: save cleared. Stop Play mode jika sedang Play, lalu Play lagi. Flow akan PlayFromStart()->S1. Jika masih di BelajarMainKarinding, tekan F1 untuk S3.");
        EditorUtility.DisplayDialog("Force Start From S1", "Save dihapus. Stop Play (jika Play) lalu Play lagi.\n\nAlternatif tanpa clear: saat Play di state BelajarMainKarinding, tekan F1 untuk lanjut S3->S4.", "OK");
    }

    [MenuItem("Karinding/Save/Always S1 In Editor (toggle)")]
    public static void ToggleAlwaysS1()
    {
        bool cur = Karinding.Story.Chapter1Flow.GetAlwaysS1Editor();
        bool next = !cur;
        Karinding.Story.Chapter1Flow.SetAlwaysS1Editor(next);
        Debug.Log($"[ClearSaveTool] Toggle AlwaysS1: {cur} -> {next}. {(next ? "Play akan selalu S1 (HARI PERTAMA SCENE 1)" : "Play akan resume save (BelajarMainKarinding -> tunggu F1)")}");
        EditorUtility.DisplayDialog("Always S1 In Editor", next
            ? "ON: Play di Editor SELALU mulai dari S1_Teras (Baba tos gugah...).\nSave tidak dihapus, hanya diabaikan.\n\nMatikan lagi via menu yang sama untuk test resume."
            : "OFF: Play akan resume save (mis. BelajarMainKarinding -> tunggu F1 untuk S3).\nGunakan Clear Save jika ingin S1 lagi.",
            "OK");
    }

    [MenuItem("Karinding/Save/Always S1 In Editor (toggle)", true)]
    public static bool ToggleAlwaysS1Validate()
    {
        bool cur = Karinding.Story.Chapter1Flow.GetAlwaysS1Editor();
        Menu.SetChecked("Karinding/Save/Always S1 In Editor (toggle)", cur);
        return true;
    }

    [MenuItem("Karinding/Save/Replay S1 Now (tanpa clear save)")]
    public static void ReplayS1Now()
    {
        var flow = Object.FindFirstObjectByType<Karinding.Story.Chapter1Flow>();
        if (flow == null)
        {
            Debug.LogWarning("[ClearSaveTool] Chapter1Flow tidak ditemukan di scene aktif. Buka Chapter1.unity dulu.");
            EditorUtility.DisplayDialog("Replay S1", "Chapter1Flow tidak ditemukan. Buka Scene Chapter1 dulu.", "OK");
            return;
        }
        // Hanya bisa saat Play mode
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ClearSaveTool] Hanya bisa Replay saat Play mode. Play dulu, lalu menu ini.");
            EditorUtility.DisplayDialog("Replay S1", "Play dulu (Enter Play Mode), lalu jalankan menu ini untuk paksa replay S1 tanpa hapus save.", "OK");
            return;
        }
        flow.DebugReplayS1();
        Debug.Log("[ClearSaveTool] Replay S1 dipicu (tanpa clear save)");
    }
}
