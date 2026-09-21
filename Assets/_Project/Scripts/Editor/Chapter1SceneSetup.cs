using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Karinding.Story;
using Karinding.Dialogue;
using Karinding.Codex;
using Karinding.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class Chapter1SceneSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Game/Chapter1.unity";
    private const string CanvasPrefabPath = "Assets/_Project/Prefabs/UI/DialogueCanvas.prefab";

    private const string S1Path = "Assets/_Project/Data/Dialogue/Chapter1/Ch1_S1_Teras.asset";
    private const string S2Path = "Assets/_Project/Data/Dialogue/Chapter1/Ch1_S2_Pengenalan.asset";
    private const string S3Path = "Assets/_Project/Data/Dialogue/Chapter1/Ch1_S3_Selesai.asset";
    private const string S4Path = "Assets/_Project/Data/Dialogue/Chapter1/Ch1_S4_Filosofi.asset";

    [MenuItem("Karinding/Setup/Chapter1 Scene")]
    public static void SetupChapter1Scene()
    {
        EnsureFolder("Assets/_Project/Scenes/Game");

        // Load or create scene
        var scene = GetOrCreateScene();
        bool isNewScene = string.IsNullOrEmpty(scene.path) || scene.path != ScenePath;

        // Main Camera + Physics2DRaycaster
        EnsureMainCamera();

        // EventSystem with InputSystemUIInputModule
        EnsureEventSystem();

        // DialogueCanvas prefab instance
        EnsureDialogueCanvas();

        // Chapter1Flow GO
        EnsureChapter1Flow();

        // Managers (Story, Codex, Affinity) — Fase 4 backbone
        EnsureManagers();

        // Background untuk Scene 1-4 (same bg)
        BackgroundSetup.FixImport();
        BackgroundSetup.EnsureBackground();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Chapter1SceneSetup] Scene ready: {ScenePath} (new={isNewScene})");
        Debug.Log($"[Chapter1SceneSetup] Scene setup selesai: {ScenePath} — Main Camera + Physics2DRaycaster ✔, DialogueCanvas prefab ✔, Chapter1Flow (S1,S2,S3_Selesai,S4 auto-assigned) ✔, EventSystem (InputSystemUIInputModule) ✔ — buka scene, cek Inspector Chapter1Flow tidak ada None, Play → S1→S2 berjalan.");
        // EditorUtility.DisplayDialog dulunya di sini (non-blocking agar MCP tidak hang)
    }

    private static UnityEngine.SceneManagement.Scene GetOrCreateScene()
    {
        // Jika file scene sudah ada, buka. Jika belum, buat baru.
        var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (existing != null)
        {
            var opened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[Chapter1SceneSetup] Membuka scene existing: {ScenePath}");
            return opened;
        }
        else
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Tambah lighting default minimal
            // Save langsung supaya path terisi
            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[Chapter1SceneSetup] Membuat scene baru: {ScenePath}");
            // Re-open untuk pastikan handle benar
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void EnsureMainCamera()
    {
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam = go.GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0, 0, -10);
            Debug.Log("[Chapter1SceneSetup] Main Camera dibuat");
        }
        else
        {
            Debug.Log($"[Chapter1SceneSetup] Main Camera sudah ada: {cam.name}");
            if (cam.tag != "MainCamera") cam.tag = "MainCamera";
        }

        // Physics2DRaycaster untuk klik objek dunia 2D (konvensi Bagian 2)
        var raycaster = cam.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
        {
            cam.gameObject.AddComponent<Physics2DRaycaster>();
            Debug.Log("[Chapter1SceneSetup] Physics2DRaycaster ditambahkan ke Main Camera");
        }

        // Camera harus punya GraphicRaycaster? Tidak, GraphicRaycaster di Canvas. Physics2DRaycaster cukup.
        Undo.RegisterCreatedObjectUndo(cam.gameObject, "Ensure Main Camera");
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            var inputModule = existing.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var standalone = existing.GetComponent<StandaloneInputModule>();
                if (standalone != null) Object.DestroyImmediate(standalone, true);
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[Chapter1SceneSetup] EventSystem diupdate ke InputSystemUIInputModule");
            }
#endif
            return;
        }

        var esGO = new GameObject("EventSystem",
            typeof(EventSystem)
#if ENABLE_INPUT_SYSTEM
            , typeof(InputSystemUIInputModule)
#else
            , typeof(StandaloneInputModule)
#endif
        );
        Debug.Log("[Chapter1SceneSetup] EventSystem dibuat");
    }

    private static void EnsureDialogueCanvas()
    {
        // FIX: repair jika scale 0 atau Toggle rusak (bug lama sebelum BG)
        var existingMgr = Object.FindFirstObjectByType<DialogueManager>();
        var existingCanvasGO = GameObject.Find("DialogueCanvas");
        bool needsRebuild = false;
        string rebuildReason = "";
        if (existingMgr != null || existingCanvasGO != null)
        {
            var targetGO = existingMgr != null ? existingMgr.gameObject.transform.root.gameObject : existingCanvasGO;
            // cek DialogueCanvas rect scale
            if (targetGO != null)
            {
                var canvasTr = targetGO.GetComponent<RectTransform>();
                if (canvasTr == null) canvasTr = targetGO.GetComponentInChildren<RectTransform>(true);
                // cari DialogueCanvas secara spesifik
                var canvasObj = GameObject.Find("DialogueCanvas");
                if (canvasObj != null)
                {
                    var r = canvasObj.GetComponent<RectTransform>();
                    if (r != null && (r.localScale == Vector3.zero || r.localScale.x == 0))
                    {
                        needsRebuild = true; rebuildReason = $"scale {r.localScale} (bug lama)";
                    }
                    // cek Toggle graphic hilang
                    if (!needsRebuild)
                    {
                        var toggle = canvasObj.GetComponentInChildren<Toggle>(true);
                        if (toggle != null && toggle.graphic == null)
                        {
                            needsRebuild = true; rebuildReason = "Toggle.graphic null (prefab stale)";
                        }
                    }
                    // cek Background/Checkmark hilang
                    if (!needsRebuild && GameObject.Find("Background") != null)
                    {
                        // Background world vs UI - abaikan, cek khusus Toggle Background
                        var t = canvasObj.transform.Find("DialogueRoot/Panel/TranslationToggle/Background");
                        if (t == null)
                        {
                            needsRebuild = true; rebuildReason = "TranslationToggle/Background missing (prefab stale)";
                        }
                    }
                }
            }
            if (needsRebuild)
            {
                Debug.LogWarning($"[Chapter1SceneSetup] DialogueCanvas rusak ({rebuildReason}) -> hapus & rebuild dari prefab");
                if (existingCanvasGO != null) Object.DestroyImmediate(existingCanvasGO, true);
                else if (existingMgr != null) Object.DestroyImmediate(existingMgr.transform.root.gameObject, true);
                // lanjut ke instantiate di bawah (jangan return)
            }
            else
            {
                Debug.Log($"[Chapter1SceneSetup] DialogueCanvas sudah ada di scene (found {(existingMgr != null ? existingMgr.name : existingCanvasGO.name)}) - OK, skip");
                // repair scale jika masih 0 (tanpa rebuild penuh)
                var canvasObj2 = GameObject.Find("DialogueCanvas");
                if (canvasObj2 != null)
                {
                    var r2 = canvasObj2.GetComponent<RectTransform>();
                    if (r2 != null && r2.localScale == Vector3.zero) { r2.localScale = Vector3.one; Debug.Log("[Chapter1SceneSetup] Repair scale 0->1 (tanpa rebuild)"); }
                }
                return;
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Chapter1SceneSetup] Prefab tidak ditemukan: {CanvasPrefabPath}. Jalankan Karinding → Dialogue → Build Dialogue UI dulu.");
            // DisplayDialog non-blocking diganti log agar MCP tidak hang
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "DialogueCanvas";
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Debug.Log("[Chapter1SceneSetup] DialogueCanvas di-instantiate dari prefab");
    }

    private static void EnsureChapter1Flow()
    {
        var existing = Object.FindFirstObjectByType<Chapter1Flow>();
        GameObject go;
        Chapter1Flow flow;

        if (existing != null)
        {
            flow = existing;
            go = existing.gameObject;
            Debug.Log($"[Chapter1SceneSetup] Chapter1Flow sudah ada: {go.name}");
        }
        else
        {
            go = new GameObject("Chapter1Flow");
            flow = go.AddComponent<Chapter1Flow>();
            Debug.Log("[Chapter1SceneSetup] Chapter1Flow GO dibuat");
        }

        // Auto-assign 4 sequence dari Data/Dialogue/Chapter1
        var s1 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(S1Path);
        var s2 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(S2Path);
        var s3 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(S3Path);
        var s4 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(S4Path);

        if (s1 == null || s2 == null || s3 == null || s4 == null)
        {
            Debug.LogWarning($"[Chapter1SceneSetup] Beberapa sequence tidak ditemukan. Cek:\n{S1Path} -> {s1}\n{S2Path} -> {s2}\n{S3Path} -> {s3}\n{S4Path} -> {s4}\nJalankan Generate Chapter 1 dulu.");
        }

        var so = new SerializedObject(flow);
        // Field names sesuai Chapter1Flow.cs (private [SerializeField])
        var p1 = so.FindProperty("s1_Teras");
        var p2 = so.FindProperty("s2_Pengenalan");
        var p3 = so.FindProperty("s3_Selesai");
        var p4 = so.FindProperty("s4_Filosofi");

        bool changed = false;
        if (p1 != null && p1.objectReferenceValue != s1) { p1.objectReferenceValue = s1; changed = true; }
        if (p2 != null && p2.objectReferenceValue != s2) { p2.objectReferenceValue = s2; changed = true; }
        if (p3 != null && p3.objectReferenceValue != s3) { p3.objectReferenceValue = s3; changed = true; }
        if (p4 != null && p4.objectReferenceValue != s4) { p4.objectReferenceValue = s4; changed = true; }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Chapter1SceneSetup] Chapter1Flow sequences di-assign otomatis (S1,S2,S3_Selesai,S4)");
        }
        else
        {
            Debug.Log("[Chapter1SceneSetup] Chapter1Flow sequences sudah terisi, tidak diubah");
        }

        // Pastikan GO aktif
        go.SetActive(true);
    }

    private static void EnsureManagers()
    {
        // StoryManager
        var storyMgr = Object.FindFirstObjectByType<StoryManager>();
        if (storyMgr == null)
        {
            var go = new GameObject("StoryManager");
            storyMgr = go.AddComponent<StoryManager>();
            Debug.Log("[Chapter1SceneSetup] StoryManager GO dibuat");
        }
        else Debug.Log($"[Chapter1SceneSetup] StoryManager sudah ada: {storyMgr.name}");

        // CodexManager
        var codexMgr = Object.FindFirstObjectByType<CodexManager>();
        if (codexMgr == null)
        {
            var go = new GameObject("CodexManager");
            codexMgr = go.AddComponent<CodexManager>();
            Debug.Log("[Chapter1SceneSetup] CodexManager GO dibuat");
        }
        else Debug.Log($"[Chapter1SceneSetup] CodexManager sudah ada: {codexMgr.name}");

        // Assign 4 CodexEntry ke CodexManager jika belum
        var entries = new System.Collections.Generic.List<CodexEntry>();
        string[] codexPaths = new string[]{
            "Assets/_Project/Data/Codex/Codex_Pancepengan.asset",
            "Assets/_Project/Data/Codex/Codex_Panabeuh.asset",
            "Assets/_Project/Data/Codex/Codex_CecetUcing.asset",
            "Assets/_Project/Data/Codex/Codex_GetaranKehidupan.asset"
        };
        foreach (var p in codexPaths)
        {
            var e = AssetDatabase.LoadAssetAtPath<CodexEntry>(p);
            if (e != null) entries.Add(e);
            else Debug.LogWarning($"[Chapter1SceneSetup] CodexEntry tidak ditemukan: {p}. Jalankan Karinding/Codex/Generate Chapter 1 Codex dulu.");
        }
        if (entries.Count == 4)
        {
            var so = new SerializedObject(codexMgr);
            var prop = so.FindProperty("entries");
            // Clear and re-add to avoid duplicate
            prop.ClearArray();
            for (int i = 0; i < entries.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(codexMgr);
            Debug.Log("[Chapter1SceneSetup] CodexManager entries di-assign (4 entries)");
        }

        // AffinityManager
        var affMgr = Object.FindFirstObjectByType<AffinityManager>();
        if (affMgr == null)
        {
            var go = new GameObject("AffinityManager");
            affMgr = go.AddComponent<AffinityManager>();
            Debug.Log("[Chapter1SceneSetup] AffinityManager GO dibuat");
        }
        else Debug.Log($"[Chapter1SceneSetup] AffinityManager sudah ada: {affMgr.name}");
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
}
