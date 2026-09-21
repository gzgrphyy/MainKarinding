using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Karinding.Dialogue;

public static class Chapter1DialogueGenerator
{
    private const string Folder = "Assets/_Project/Data/Dialogue/Chapter1";
    private static bool _overwrite;
    private static int _created, _skipped;

    [MenuItem("Karinding/Dialogue/Generate Chapter 1 (skip existing)")]
    private static void GenerateSafe() => Generate(false);

    [MenuItem("Karinding/Dialogue/Generate Chapter 1 (OVERWRITE)")]
    private static void GenerateOverwrite()
    {
        if (EditorUtility.DisplayDialog("Overwrite?",
            "Semua asset dialog Chapter 1 akan ditimpa (portrait yang sudah di-assign hilang).",
            "Overwrite", "Batal"))
            Generate(true);
    }

    // ---------- helpers ----------
    private static DialogueLine L(string who, string text, string tr, string evt = null) => new DialogueLine
    {
        speakerName = who,
        portraitSide = who == "Rimba" ? DialogueSide.Left : DialogueSide.Right,
        text = text,
        translation = tr,
        eventId = evt
    };

    private static LineData R(string who, string text, string tr) => new LineData
    {
        speakerName = who,
        portraitSide = who == "Rimba" ? DialogueSide.Left : DialogueSide.Right,
        text = text,
        translation = tr
    };

    private static DialogueChoice C(string label, string labelTr, params LineData[] responses) => new DialogueChoice
    {
        label = label,
        labelTranslation = labelTr,
        npcId = "abah",
        affinityDelta = 0,
        responseLines = new List<LineData>(responses)
    };

    private static DialogueLine WithChoices(DialogueLine line, params DialogueChoice[] choices)
    {
        line.choices = new List<DialogueChoice>(choices);
        return line;
    }

    private static void Make(string file, string id, params DialogueLine[] lines)
    {
        string path = $"{Folder}/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (so != null && !_overwrite) { _skipped++; return; }
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<DialogueSequence>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.sequenceId = id;
        so.lines = new List<DialogueLine>(lines);
        EditorUtility.SetDirty(so);
        _created++;
    }

    private static void EnsureFolder(string path)
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

    // ---------- data ----------
    private static void Generate(bool overwrite)
    {
        _overwrite = overwrite; _created = 0; _skipped = 0;
        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
        {
            EditorUtility.DisplayDialog("Folder tidak ditemukan",
                "Folder Assets/_Project tidak ditemukan.\nPastikan struktur Assets/_Project ada dan bukan Assets/Assets/_Project.\n\nGenerator dibatalkan untuk mencegah path ganda seperti Assets/Assets/_Project.",
                "OK");
            Debug.LogError("[DialogueGen] Assets/_Project tidak ditemukan. Generate dibatalkan.");
            return;
        }
        if (Folder.Contains("Assets/Assets"))
        {
            EditorUtility.DisplayDialog("Path ganda terdeteksi",
                $"Folder konstanta mengandung path ganda: {Folder}\nSeharusnya Assets/_Project/Data/Dialogue/Chapter1",
                "OK");
            Debug.LogError($"[DialogueGen] Path ganda terdeteksi: {Folder}");
            return;
        }
        EnsureFolder(Folder);

        // HARI 1 - SCENE 1: Teras rumah (pagi)
        Make("Ch1_S1_Teras", "ch1_s1_teras",
            L("Abah", "Baba tos gugah gening ? Sok gera wawasuh heula", "Baba udah bangun ternyata? Sana cuci muka dulu"),
            L("Abah", "Aya jalan nu kudu dilakonan ku nyalira, Ba. Abah keur ngora ge kitu", "Ada jalan yang harus ditempuh sendiri, Ba. Ketika Abah muda juga begitu"),
            L("Rimba", "Maksudna kumaha bah ?", "Maksudnya gimana bah?"),
            L("Abah", "Moal karasa ayeuna. Engke ge ngarti sorangan", "Ga bakal kerasa sekarang. Nanti juga ngerti sendiri."),
            L("Abah", "Tos tong di pikiran teuing. Ayeuna mah urang ka kebon we hayu, bisi kaburu panas", "Udah jangan terlalu dipikirin. Sekarang kita ke kebun aja, takutnya keburu panas"),
            L("Rimba", "Muhun bah, Hayu", "Iya bah, ayo"));

        // HARI 1 - SCENE 2: Pengenalan Karinding (ada titik interaksi A/B/C)
        Make("Ch1_S2_Pengenalan", "ch1_s2_pengenalan",
            L("Rimba", "Eta naon, bah? naha kitu sora na?", "Abah itu apa? kenapa suaranya begitu?"),
            WithChoices(
                L("Abah", "Ieu teh nami na Karinding, lain ngan saukur alat bebunyian", "Ini namanya Karinding, bukan cuma alat bebunyian biasa"),
                C("Rimba nembe ningal, naha jarang aya nu maenkeun bah?", "Rimba baru liat, kenapa jarang ada yang mainin bah?",
                    R("Abah", "Tos lami teu diwariskeun, Ba. Kapungkur mah unggal jalmi teh aya. ayeuna mah ngan sakedik nu terang", "Sudah lama tidak diwariskan, Ba. Dulu setiap orang punya. Sekarang hanya sedikit yang tahu")),
                C("Kumaha cara maenna bah?", "Gimana cara mainin nya bah?",
                    R("Abah", "Teu hese, tapi teu gampang oge. Kudu cepeung sing yakin, tabeuh sing sabar", "Ga susah, tapi ga gampang juga. Harus pegang dengan yakin, pukul dengan sabar")),
                C("Naha abah tiasa gaduh?", "Kenapa abah bisa punya?",
                    R("Abah", "Ieu teh Uyut didinya kapungkur, di wariskeun ka Abah. Ayeuna abah nu nyepeung.", "Ini punya Uyut kamu dulu, diwariskan ke Abah. Sekarang Abah yang memegang."))),
            L("Abah", "Pancepengan. Cepeung sing yakin.", "Pancepengan: pegang dengan yakin. Bagian pegangan, mengajarkan keyakinan dalam memegang dan menjalankan sesuatu.", "CODEX_PANCEPENGAN"),
            L("Abah", "Panabeuh. Tabeuh sing sabar.", "Panabeuh: pukul dengan sabar. Bagian yang dipukul/digetarkan, mengajarkan kesabaran dalam menghasilkan getaran.", "CODEX_PANABEUH"),
            L("Abah", "Cecet Ucing. Kudu sadar lamun eta lain sora urang.", "Cecet Ucing: harus sadar bahwa itu bukan suara kita. Bagian kecil yang membantu resonansi hingga suara benar-benar terdengar.", "CODEX_CECET_UCING"),
            L("Abah", "Unggal bagian aya harti na, Baba. Sami oge sareng kahirupan, kudu percaya, kudu sabar, jeung kudu sadar lamun arurang tos menang hasil tina naon nu ku arurang lakonan", "Setiap bagian ada artinya, Baba. Sama juga dengan kehidupan, harus percaya, harus sabar dan sadar jika kita sudah dapat hasil dari yang dijalani."));

        // HARI 1 - SCENE 3: Belajar memainkan (hint saat gagal + setelah berhasil)
        Make("Ch1_S3_HintMaksa", "ch1_s3_hint_maksa",
            L("Abah", "Ulah maksa, Rimba. Lain kuat na leungeun nu di penta, tapi tenang jeung sabar na haté.", "Jangan maksa, Rimba. Bukan kekuatan tangan yang diminta, tapi sabar dan ketenangan hati."));

        Make("Ch1_S3_Selesai", "ch1_s3_selesai",
            L("Abah", "Omat eta lain sora Rimba, tapi hasil tina getaran nu di barengan ku resonansi.", "Ingat, itu bukan suara Rimba, tapi hasil dari getaran yang dibarengi dengan resonansi"));

        // HARI 1 - SCENE 4: Filosofi getaran dan kehidupan
        Make("Ch1_S4_Filosofi", "ch1_s4_filosofi",
            L("Abah", "Jadi orang Sunda teh percanten lamun alam semesta ieu teh ka bentuk tina dentuman tarik, nyaeta Big Bang. Getaran jeung dengung teh dianggap awal tina kahirupan, Ba.",
                "Orang Sunda percaya alam semesta ini terbentuk dari dentuman besar, yaitu Big Bang. Getaran dan dengung dianggap awal dari kehidupan, Ba.", "CODEX_GETARAN_KEHIDUPAN"),
            L("Abah", "Jadi sateuacan aya na kahirupan teh aya nu nami na Alam Kosong, awang uwung tong enong margana enong, kosong suwung eweuh nanaon didinya.",
                "Sebelum adanya kehidupan itu ada yang namanya Alam Kosong, awang uwung tong enong margana enong, kosong melompong tidak ada apa-apa di sana."),
            L("Abah", "Alam ieu ngadadak endeur jeung ngalahirkeun sora dengung, laksaketi mangrewu sora tawon... <i>Ngung…</i> bledag!",
                "Tiba-tiba alam bergetar dan melahirkan suara dengung, laksaketi mangrewu suara tawon... Ngung… meledak!"),
            L("Abah", "Tah tina getaran jeung dengung eta lah lahir kahirupan.", "Nah, dari getaran dan dengung itu lah lahir kehidupan."));

        // HARI 2 - SCENE 5: Cari bahan
        Make("Ch1_S5_Intro", "ch1_s5_intro",
            L("Abah", "Rimba, hayu urang milarian awi ka leuweung. Karinding Rimba kudu di jieun ku panangan Rimba sorangan.", "Rimba, ayo kita nyari bambu di hutan. Karinding Rimba harus dibuat sama tangan Rimba sendiri"),
            L("Rimba", "Kumaha cara milih awi nu sae, Bah?", "Gimana cara milih bambu yang bagus, Bah?"),
            L("Abah", "Awi nu sae teh nu tos cekap umur na. teu ngora teuing, teu kolot teuing. Batang na ge kudu seseug, ulah nu ipis", "Bambu yang bagus itu yang sudah cukup umur. Ngga terlalu muda, ngga terlalu tua. Batangnya juga harus padat, jangan kopong"));

        // (opsional - hanya terpakai kalau ada item salah pilih; desain sekarang tanpa decoy)
        Make("Ch1_S5_BambuSalah", "ch1_s5_bambu_salah",
            L("Abah", "Sanes nu ieu, Ba. Sok pariksa deui. emut emut kumaha ciri-ciri na tadi.", "Bukan yang ini, Ba. Periksa lagi dan inget-inget gimana ciri-cirinya"));
        Make("Ch1_S5_BambuBenar", "ch1_s5_bambu_benar",
            L("Abah", "Tah nu kieu. Rimba tos tiasa ngabedakeun mana awi nu cocok jang di jadi keun Karinding.", "Nah yang kaya gini. Rimba udah bisa membedakan mana bambu yang cocok untuk dijadikan Karinding"));

        // HARI 2 - SCENE 6: Awal proses membuat
        Make("Ch1_S6_Intro", "ch1_s6_intro",
            L("Abah", "Bagian-bagian na tos aya, Ba. Ayeuna kari nyusun jeng ngantelkeun na. nepi ka ngabentuk Karinding", "Bagian-bagiannya udah ada, Ba. Sekarang tinggal susun dan tempelin, sampai jadi bentuk Karinding."));

        // HARI 2 - SCENE 7: Main Karinding buatan sendiri
        Make("Ch1_S7_Coba", "ch1_s7_coba",
            L("Abah", "Sok cobaan, Ba. Ieu mah benteun, jeunan Rimba sorangan", "Coba, Rimba. Ini mah beda, buatan Rimba sendiri."));
        Make("Ch1_S7_Selesai", "ch1_s7_selesai",
            L("Abah", "Ayeuna Rimba tos gaduh jeung tiasa maenkeun Karinding. Tapi Rimba ge kudu emut eta lain ngan saukur sora ti Rimba", "Sekarang Rimba udah punya Karinding sendiri dan bisa memainkannya. Tapi Rimba juga harus sadar itu bukan cuma suara dari Rimba."));

        // HARI 2 - SCENE 8: Sore sebelum Abah pergi
        Make("Ch1_S8_Perpisahan", "ch1_s8_perpisahan",
            L("Abah", "Rimba... Karinding ieu ulah ngan saukur jang Rimba nyalira. Abah hoyong nu sanes ge terang kana alat bebunyian ieu.", "Rimba... Karinding ini jangan cuma buat kamu sendiri. Abah ingin orang lain juga tau sama alat bebunyian ini."),
            L("Rimba", "Maksudna kumaha, Bah?", "Maksudnya gimana, Bah?"),
            L("Rimba", "Abah, bade kamana?", "Abah mau kemana?", "ABAH_IKAT_BEDOG"),
            L("Abah", "Néangan nu eweuh.", "Mencari sesuatu yang tidak ada."),
            L("Rimba", "Dimana bah nu eweuh teh ?", "Dimana bah yang gaada itu?"));

        // dipanggil setelah interaksi "coba raih tangan Abah"
        Make("Ch1_S8_Akhir", "ch1_s8_akhir",
            L("Abah", "Moal Kataekan", "Tidak akan sampai / tidak akan tercapai", "CHAPTER1_END"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DialogueGen] Chapter 1 selesai. Dibuat/ditimpa: {_created}, dilewati (sudah ada): {_skipped}");
    }
}