using UnityEditor;
using UnityEngine;
using Karinding.Codex;

public static class CodexGenerator
{
    [MenuItem("Karinding/Codex/Generate Chapter 1 Codex")]
    public static void Generate()
    {
        EnsureFolder("Assets/_Project/Data/Codex");
        Make("Codex_Pancepengan", "CODEX_PANCEPENGAN", "Pancepengan", "Pancepengan: bagian pegangan Karinding. Mengajarkan keyakinan (Yakin) dalam memegang dan menjalankan sesuatu. Cepeung sing yakin.");
        Make("Codex_Panabeuh", "CODEX_PANABEUH", "Panabeuh", "Panabeuh: bagian yang dipukul/digetarkan. Mengajarkan kesabaran (Sabar) dalam menghasilkan getaran yang indah.");
        Make("Codex_CecetUcing", "CODEX_CECET_UCING", "Cecet Ucing", "Cecet Ucing: bagian kecil penghasil resonansi. Mengajarkan kesadaran (Sadar) bahwa suara bukan dari kita, tapi dari resonansi.");
        Make("Codex_GetaranKehidupan", "CODEX_GETARAN_KEHIDUPAN", "Getaran dan Awal Kehidupan", "Getaran dan dengung dianggap awal kehidupan. Dari Alam Kosong yang awang-awang, tiba-tiba bergetar dan melahirkan suara dengung Ngung... hingga lahir kehidupan (Big Bang versi Sunda).");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CodexGen] 4 codex generated di Assets/_Project/Data/Codex");
    }

    // For MCP/execute_code without menu dialog
    public static void Gen() => Generate();
    static void Make(string file, string id, string title, string content)
    {
        string path = $"Assets/_Project/Data/Codex/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<CodexEntry>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<CodexEntry>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.entryId = id;
        so.title = title;
        so.content = content;
        if (!so.unlocked) so.unlocked = false;
        EditorUtility.SetDirty(so);
    }
    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
