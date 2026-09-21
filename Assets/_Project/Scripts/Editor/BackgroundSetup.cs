using UnityEditor;
using UnityEngine;

public static class BackgroundSetup
{
    private const string BgPath = "Assets/_Project/Art/Environment/bgscene1-4_fix.png";
    private const string BgPathOld = "Assets/_Project/Art/Environment/bgScene1-4.png";

    [MenuItem("Karinding/Setup/Background")]
    public static void Setup()
    {
        FixImport();
        EnsureBackground();
        AssetDatabase.SaveAssets();
        Debug.Log("[BackgroundSetup] BG setup selesai untuk Scene 1-4 (same bg)");
    }

    public static void FixImport()
    {
        // Fix kedua file (baru fix + lama) biar sama-sama Sprite
        foreach (var path in new string[]{ BgPath, BgPathOld })
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.maxTextureSize != 2048) { importer.maxTextureSize = 2048; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.isReadable) { importer.isReadable = false; dirty = true; }
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.Automatic;
            android.compressionQuality = 50;
            importer.SetPlatformTextureSettings(android);
            if (dirty) { importer.SaveAndReimport(); Debug.Log($"[BackgroundSetup] Import fixed: {path} -> Sprite"); }
            else Debug.Log($"[BackgroundSetup] Import sudah benar: {path}");
        }
    }

    public static void EnsureBackground()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        if (sprite == null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BgPath);
            if (tex != null) Debug.LogWarning($"[BackgroundSetup] Bg masih Texture2D bukan Sprite, cek import: {BgPath}");
            Debug.LogError($"[BackgroundSetup] Sprite tidak ditemukan: {BgPath}. Pastikan FixImport sudah dijalankan.");
            return;
        }

        // Bersihkan BG lama yang corrupt (CanvasRenderer+SpriteRenderer tanpa Transform)
        var bgGO = GameObject.Find("Background");
        if (bgGO == null) bgGO = GameObject.Find("Background_Scene1-4");
        // Jika ada BG lama yang punya CanvasRenderer atau RectTransform atau Image, hapus total dan bikin baru
        if (bgGO != null)
        {
            bool hasCanvasRenderer = bgGO.GetComponent<CanvasRenderer>() != null;
            bool hasRect = bgGO.GetComponent<RectTransform>() != null;
            bool hasImage = bgGO.GetComponent<UnityEngine.UI.Image>() != null;
            bool hasCanvas = bgGO.GetComponent<Canvas>() != null;
            // Scene file tadi tunjukkan Background punya CanvasRenderer+SpriteRenderer tanpa Transform -> corrupt
            if (hasCanvasRenderer || hasRect || hasImage || hasCanvas)
            {
                Debug.Log($"[BackgroundSetup] Hapus Background corrupt (CanvasRenderer:{hasCanvasRenderer} Rect:{hasRect} Image:{hasImage})");
                Object.DestroyImmediate(bgGO, true);
                bgGO = null;
            }
            else if (bgGO.transform.parent != null && bgGO.transform.parent.GetComponent<Canvas>() != null)
            {
                bgGO.transform.SetParent(null);
                Debug.Log("[BackgroundSetup] Pindah Background keluar dari Canvas");
            }
        }

        if (bgGO == null)
        {
            bgGO = new GameObject("Background");
            var sr = bgGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.maskInteraction = SpriteMaskInteraction.None;
            bgGO.transform.position = new Vector3(0, 0, 10);
            Debug.Log("[BackgroundSetup] Background GO dibuat (SpriteRenderer -100, z 10)");
        }
        else
        {
            var sr = bgGO.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                // Jika sebelumnya Image, sudah dihapus di atas, tambah SpriteRenderer
                sr = bgGO.AddComponent<SpriteRenderer>();
            }
            // Bersihkan Image leftover
            var oldImg = bgGO.GetComponent<UnityEngine.UI.Image>();
            if (oldImg != null) Object.DestroyImmediate(oldImg, true);
            sr.sprite = sprite;
            sr.sortingOrder = -100;
            sr.drawMode = SpriteDrawMode.Simple;
            Debug.Log($"[BackgroundSetup] Background sudah ada: {bgGO.name}, sprite di-update ke fix, sorting -100");
        }

        // Pastikan di belakang, scale cover camera
        var bgTransform = bgGO.transform;
        bgTransform.position = new Vector3(0, 0, 10);
        bgTransform.localScale = Vector3.one;

        var sr2 = bgGO.GetComponent<SpriteRenderer>();
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null && sr2 != null && sr2.sprite != null)
        {
            // Hitung scale agar cover orthographic camera 1920x1080
            // Camera orthographicSize 5 => height 10 units, width 10 * aspect
            // Sprite bounds size in units
            float spriteH = sr2.sprite.bounds.size.y;
            float spriteW = sr2.sprite.bounds.size.x;
            if (spriteH > 0 && spriteW > 0)
            {
                float camH = cam.orthographic ? cam.orthographicSize * 2f : 10f;
                float camW = camH * cam.aspect;
                float scaleY = camH / spriteH;
                float scaleX = camW / spriteW;
                float scale = Mathf.Max(scaleX, scaleY); // cover
                bgTransform.localScale = new Vector3(scale, scale, 1);
                Debug.Log($"[BackgroundSetup] Scale cover: {scale:F2} (cam {camW:F1}x{camH:F1}, sprite {spriteW:F1}x{spriteH:F1})");
            }
            // Pastikan camera orthographic untuk 2D BG
            if (!cam.orthographic)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                Debug.Log("[BackgroundSetup] Camera di-set orthographic size 5");
            }
        }

        // Pastikan BG tidak terhalang UI
        bgGO.SetActive(true);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(bgGO.scene);
    }
}
