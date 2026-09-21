using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Karinding.Dialogue;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class DialogueUIBuilder
{
    private const string ChoiceButtonPrefabPath = "Assets/_Project/Prefabs/UI/DialogueChoiceButton.prefab";
    private const string CanvasPrefabPath = "Assets/_Project/Prefabs/UI/DialogueCanvas.prefab";

    [MenuItem("Karinding/Dialogue/Build Dialogue UI")]
    public static void BuildDialogueUI()
    {
        bool buildChoice = true;
        bool buildCanvas = true;

        bool choiceExists = AssetDatabase.LoadAssetAtPath<GameObject>(ChoiceButtonPrefabPath) != null;
        bool canvasExists = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath) != null;

        if (choiceExists || canvasExists)
        {
            string msg = "Prefab sudah ada:\n";
            if (choiceExists) msg += $" - {ChoiceButtonPrefabPath}\n";
            if (canvasExists) msg += $" - {CanvasPrefabPath}\n";
            msg += "\nPilih aksi: Overwrite = timpa semua, Skip = lewati yang sudah ada, Cancel = batal.";

            int opt = EditorUtility.DisplayDialogComplex("Build Dialogue UI",
                msg, "Overwrite", "Cancel", "Skip");
            if (opt == 1) // Cancel
            {
                Debug.Log("[DialogueUIBuilder] Dibatalkan user.");
                return;
            }
            if (opt == 2) // Skip
            {
                buildChoice = !choiceExists;
                buildCanvas = !canvasExists;
                if (!buildChoice && !buildCanvas)
                {
                    EditorUtility.DisplayDialog("Build Dialogue UI", "Semua prefab sudah ada. Tidak ada yang dibangun (Skip).", "OK");
                    return;
                }
            }
            // opt 0 = Overwrite -> build both
        }

        EnsureFolder("Assets/_Project/Prefabs/UI");

        GameObject choicePrefab = null;
        if (buildChoice)
        {
            choicePrefab = BuildChoiceButtonPrefab();
            Debug.Log($"[DialogueUIBuilder] ChoiceButton prefab dibuat: {ChoiceButtonPrefabPath}");
        }
        else
        {
            choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChoiceButtonPrefabPath);
        }

        // Load prefab for DialogueView reference (after ensuring choice exists)
        if (choicePrefab == null)
        {
            choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChoiceButtonPrefabPath);
            if (choicePrefab == null)
            {
                Debug.LogError("[DialogueUIBuilder] ChoiceButton prefab tidak ditemukan. Build dibatalkan.");
                return;
            }
        }

        if (buildCanvas)
        {
            BuildCanvasPrefab(choicePrefab);
            Debug.Log($"[DialogueUIBuilder] DialogueCanvas prefab dibuat: {CanvasPrefabPath}");
        }

        EnsureEventSystem();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DialogueUIBuilder] Selesai. ChoiceButton: {(buildChoice ? "dibuat/overwrite" : "skip")}, DialogueCanvas: {(buildCanvas ? "dibuat/overwrite" : "skip")} — seret DialogueCanvas prefab ke scene Chapter1 jika belum ada, cek tidak ada Missing reference di Inspector.");
        // EditorUtility.DisplayDialog dulunya di sini (dibuat non-blocking agar MCP tidak hang); manual check tetap via log di atas.
    }

    // ---------- ChoiceButton ----------
    private static GameObject BuildChoiceButtonPrefab()
    {
        // Root
        var root = new GameObject("DialogueChoiceButton",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(DialogueChoiceButton));
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 80);

        var img = root.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        img.raycastTarget = true;

        var btn = root.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = img.color;
        colors.highlightedColor = new Color(0.22f, 0.22f, 0.26f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        // Label TMP
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(root.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, 10);
        labelRect.sizeDelta = new Vector2(-20, 40);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "Pilihan (Sunda)";
        labelTMP.fontSize = 22;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color = Color.white;
        labelTMP.textWrappingMode = TextWrappingModes.Normal;
        ApplyDefaultFont(labelTMP);

        // Translation TMP
        var transGO = new GameObject("Translation", typeof(RectTransform));
        transGO.transform.SetParent(root.transform, false);
        var transRect = transGO.GetComponent<RectTransform>();
        transRect.anchorMin = new Vector2(0, 0.5f);
        transRect.anchorMax = new Vector2(1, 0.5f);
        transRect.pivot = new Vector2(0.5f, 0.5f);
        transRect.anchoredPosition = new Vector2(0, -18);
        transRect.sizeDelta = new Vector2(-20, 30);
        var transTMP = transGO.AddComponent<TextMeshProUGUI>();
        transTMP.text = "Terjemahan (Indonesia)";
        transTMP.fontSize = 16;
        transTMP.fontStyle = FontStyles.Italic;
        transTMP.alignment = TextAlignmentOptions.Center;
        transTMP.color = new Color(1f, 1f, 1f, 0.7f);
        transTMP.textWrappingMode = TextWrappingModes.Normal;
        ApplyDefaultFont(transTMP);

        // Wire DialogueChoiceButton via SerializedObject
        var comp = root.GetComponent<DialogueChoiceButton>();
        var so = new SerializedObject(comp);
        so.FindProperty("button").objectReferenceValue = btn;
        so.FindProperty("labelText").objectReferenceValue = labelTMP;
        so.FindProperty("translationText").objectReferenceValue = transTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Layout helper: ContentSizeFitter + LayoutElement for dynamic height
        var fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 60;
        le.preferredHeight = 80;

        // Save prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ChoiceButtonPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------- Canvas ----------
    private static void BuildCanvasPrefab(GameObject choiceButtonPrefab)
    {
        // Root Canvas
        var canvasGO = new GameObject("DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100;

        // FIX: pastikan RectTransform root tidak scale 0 (bug sebelumnya bikin dialog invisible)
        var canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.zero;
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = Vector2.zero;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.localScale = Vector3.one;
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;

        // DialogueRoot (full screen, CanvasGroup + Image raycast catcher + DialogueView)
        var rootGO = new GameObject("DialogueRoot",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(DialogueView));
        rootGO.transform.SetParent(canvasGO.transform, false);
        var rootRect = rootGO.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var cg = rootGO.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        var rootImg = rootGO.GetComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0);
        rootImg.raycastTarget = true; // penting untuk IPointerClickHandler full-screen

        // PortraitLeft
        var leftGO = new GameObject("PortraitLeft", typeof(RectTransform), typeof(Image));
        leftGO.transform.SetParent(rootGO.transform, false);
        var leftRect = leftGO.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0.5f);
        leftRect.anchorMax = new Vector2(0, 0.5f);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.anchoredPosition = new Vector2(40, 200);
        leftRect.sizeDelta = new Vector2(380, 520);
        var leftImg = leftGO.GetComponent<Image>();
        leftImg.color = new Color(1, 1, 1, 0.9f);
        leftImg.preserveAspect = true;
        leftImg.raycastTarget = false;
        leftGO.SetActive(false);

        // PortraitRight
        var rightGO = new GameObject("PortraitRight", typeof(RectTransform), typeof(Image));
        rightGO.transform.SetParent(rootGO.transform, false);
        var rightRect = rightGO.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0.5f);
        rightRect.anchorMax = new Vector2(1, 0.5f);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.anchoredPosition = new Vector2(-40, 200);
        rightRect.sizeDelta = new Vector2(380, 520);
        var rightImg = rightGO.GetComponent<Image>();
        rightImg.color = new Color(1, 1, 1, 0.9f);
        rightImg.preserveAspect = true;
        rightImg.raycastTarget = false;
        rightGO.SetActive(false);

        // Panel (bottom)
        var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(rootGO.transform, false);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 40);
        panelRect.sizeDelta = new Vector2(-160, 320);
        var panelImg = panelGO.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.10f, 0.88f);
        panelImg.raycastTarget = false;

        // NameTag
        var nameTagGO = new GameObject("NameTag", typeof(RectTransform), typeof(Image));
        nameTagGO.transform.SetParent(panelGO.transform, false);
        var nameTagRect = nameTagGO.GetComponent<RectTransform>();
        nameTagRect.anchorMin = new Vector2(0, 1);
        nameTagRect.anchorMax = new Vector2(0, 1);
        nameTagRect.pivot = new Vector2(0, 1);
        nameTagRect.anchoredPosition = new Vector2(20, -10);
        nameTagRect.sizeDelta = new Vector2(320, 44);
        var nameTagImg = nameTagGO.GetComponent<Image>();
        nameTagImg.color = new Color(0.18f, 0.14f, 0.08f, 1f);
        nameTagImg.raycastTarget = false;

        var speakerGO = new GameObject("SpeakerText", typeof(RectTransform));
        speakerGO.transform.SetParent(nameTagGO.transform, false);
        var speakerRect = speakerGO.GetComponent<RectTransform>();
        speakerRect.anchorMin = Vector2.zero;
        speakerRect.anchorMax = Vector2.one;
        speakerRect.offsetMin = new Vector2(10, 5);
        speakerRect.offsetMax = new Vector2(-10, -5);
        var speakerTMP = speakerGO.AddComponent<TextMeshProUGUI>();
        speakerTMP.text = "Abah";
        speakerTMP.fontSize = 20;
        speakerTMP.fontStyle = FontStyles.Bold;
        speakerTMP.alignment = TextAlignmentOptions.Left;
        speakerTMP.color = new Color(1f, 0.92f, 0.6f, 1f);
        ApplyDefaultFont(speakerTMP);

        // BodyText
        var bodyGO = new GameObject("BodyText", typeof(RectTransform));
        bodyGO.transform.SetParent(panelGO.transform, false);
        var bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 1);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 1);
        bodyRect.anchoredPosition = new Vector2(0, -70);
        bodyRect.sizeDelta = new Vector2(-40, 120);
        var bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = "Contoh teks dialog Sunda di sini. Tap untuk melanjutkan.";
        bodyTMP.fontSize = 26;
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        bodyTMP.color = Color.white;
        bodyTMP.textWrappingMode = TextWrappingModes.Normal;
        bodyTMP.overflowMode = TextOverflowModes.Truncate;
        ApplyDefaultFont(bodyTMP);

        // TranslationText
        var transGO = new GameObject("TranslationText", typeof(RectTransform));
        transGO.transform.SetParent(panelGO.transform, false);
        var transRect2 = transGO.GetComponent<RectTransform>();
        transRect2.anchorMin = new Vector2(0, 1);
        transRect2.anchorMax = new Vector2(1, 1);
        transRect2.pivot = new Vector2(0.5f, 1);
        transRect2.anchoredPosition = new Vector2(0, -200);
        transRect2.sizeDelta = new Vector2(-40, 70);
        var transTMP2 = transGO.AddComponent<TextMeshProUGUI>();
        transTMP2.text = "Terjemahan Indonesia (italic, pudar)";
        transTMP2.fontSize = 18;
        transTMP2.fontStyle = FontStyles.Italic;
        transTMP2.alignment = TextAlignmentOptions.TopLeft;
        transTMP2.color = new Color(1f, 1f, 1f, 0.65f);
        transTMP2.textWrappingMode = TextWrappingModes.Normal;
        ApplyDefaultFont(transTMP2);

        // ContinueIndicator
        var contGO = new GameObject("ContinueIndicator", typeof(RectTransform));
        contGO.transform.SetParent(panelGO.transform, false);
        var contRect = contGO.GetComponent<RectTransform>();
        contRect.anchorMin = new Vector2(1, 0);
        contRect.anchorMax = new Vector2(1, 0);
        contRect.pivot = new Vector2(1, 0);
        contRect.anchoredPosition = new Vector2(-20, 20);
        contRect.sizeDelta = new Vector2(40, 30);
        var contTMP = contGO.AddComponent<TextMeshProUGUI>();
        contTMP.text = "▼";
        contTMP.fontSize = 24;
        contTMP.alignment = TextAlignmentOptions.BottomRight;
        contTMP.color = new Color(1f, 1f, 1f, 0.9f);
        ApplyDefaultFont(contTMP);
        contGO.SetActive(false);

        // TranslationToggle (Toggle)
        var toggleGO = new GameObject("TranslationToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGO.transform.SetParent(panelGO.transform, false);
        var toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1, 1);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.pivot = new Vector2(1, 1);
        toggleRect.anchoredPosition = new Vector2(-20, -14);
        toggleRect.sizeDelta = new Vector2(170, 36);
        var toggleImg = toggleGO.GetComponent<Image>();
        toggleImg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
        toggleImg.raycastTarget = true;

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = true;
        toggle.targetGraphic = toggleImg;
        // Background for toggle graphic
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(toggleGO.transform, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(10, 0);
        bgRect.sizeDelta = new Vector2(24, 24);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = Color.white;

        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(bgGO.transform, false);
        var checkRect = checkGO.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(4, 4);
        checkRect.offsetMax = new Vector2(-4, -4);
        var checkImg = checkGO.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);
        toggle.graphic = checkImg;

        var labelGO2 = new GameObject("Label", typeof(RectTransform));
        labelGO2.transform.SetParent(toggleGO.transform, false);
        var labelRect2 = labelGO2.GetComponent<RectTransform>();
        labelRect2.anchorMin = new Vector2(0, 0);
        labelRect2.anchorMax = new Vector2(1, 1);
        labelRect2.offsetMin = new Vector2(44, 4);
        labelRect2.offsetMax = new Vector2(-8, -4);
        var labelTMP2 = labelGO2.AddComponent<TextMeshProUGUI>();
        labelTMP2.text = "Terjemahan";
        labelTMP2.fontSize = 16;
        labelTMP2.alignment = TextAlignmentOptions.Left;
        labelTMP2.color = Color.white;
        ApplyDefaultFont(labelTMP2);

        // ChoiceContainer (Vertical Layout Group + ContentSizeFitter) - MUST be last sibling in DialogueRoot
        var choiceGO = new GameObject("ChoiceContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        choiceGO.transform.SetParent(rootGO.transform, false);
        var choiceRect = choiceGO.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(0.5f, 0.5f);
        choiceRect.anchorMax = new Vector2(0.5f, 0.5f);
        choiceRect.pivot = new Vector2(0.5f, 0.5f);
        choiceRect.anchoredPosition = new Vector2(0, 80);
        choiceRect.sizeDelta = new Vector2(720, 0);

        var vlg = choiceGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 12;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var csf = choiceGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        choiceGO.SetActive(false);

        // Ensure ChoiceContainer is last sibling
        choiceGO.transform.SetAsLastSibling();

        // DialogueManager GO (child of canvas)
        var mgrGO = new GameObject("DialogueManager", typeof(DialogueManager));
        mgrGO.transform.SetParent(canvasGO.transform, false);
        var mgr = mgrGO.GetComponent<DialogueManager>();

        // Wire DialogueView via SerializedObject
        var view = rootGO.GetComponent<DialogueView>();
        var soView = new SerializedObject(view);
        soView.FindProperty("rootGroup").objectReferenceValue = cg;
        soView.FindProperty("nameTag").objectReferenceValue = nameTagGO;
        soView.FindProperty("speakerText").objectReferenceValue = speakerTMP;
        soView.FindProperty("bodyText").objectReferenceValue = bodyTMP;
        soView.FindProperty("translationText").objectReferenceValue = transTMP2;
        soView.FindProperty("continueIndicator").objectReferenceValue = contGO.GetComponent<RectTransform>();
        soView.FindProperty("portraitLeft").objectReferenceValue = leftImg;
        soView.FindProperty("portraitRight").objectReferenceValue = rightImg;
        soView.FindProperty("choiceContainer").objectReferenceValue = choiceRect;
        // choiceButtonPrefab is DialogueChoiceButton type, but we have GameObject prefab - need to get component from prefab
        var choiceBtnComp = choiceButtonPrefab.GetComponent<DialogueChoiceButton>();
        if (choiceBtnComp == null)
        {
            // fallback: if prefab load returns GameObject but component missing, try instance
            Debug.LogWarning("[DialogueUIBuilder] ChoiceButton prefab tidak punya DialogueChoiceButton component. Cek prefab.");
        }
        soView.FindProperty("choiceButtonPrefab").objectReferenceValue = choiceBtnComp;
        soView.ApplyModifiedPropertiesWithoutUndo();

        // Wire DialogueManager view ref
        var soMgr = new SerializedObject(mgr);
        var viewProp = soMgr.FindProperty("view");
        if (viewProp != null) viewProp.objectReferenceValue = view;
        soMgr.ApplyModifiedPropertiesWithoutUndo();

        // Wire TranslationToggle -> DialogueManager.SetTranslationAndRefresh(bool)
        // Use persistent UnityEvent (generic) — AddBoolPersistentListener butuh arg ke-3 fixed, jadi pakai AddPersistentListener<bool>
        UnityEventTools.AddPersistentListener<bool>(toggle.onValueChanged, mgr.SetTranslationAndRefresh);
        // Set initial toggle state from PlayerPrefs (default true)
        toggle.isOn = PlayerPrefs.GetInt("dialogue_show_translation", 1) == 1;

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(canvasGO, CanvasPrefabPath);
        Object.DestroyImmediate(canvasGO);
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            // Ensure InputSystemUIInputModule, remove StandaloneInputModule if present
            var inputModule = existing.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var standalone = existing.GetComponent<StandaloneInputModule>();
                if (standalone != null) Object.DestroyImmediate(standalone, true);
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[DialogueUIBuilder] EventSystem diupdate ke InputSystemUIInputModule");
            }
#else
            // fallback if Input System not enabled (should not happen per project settings)
            if (existing.GetComponent<BaseInputModule>() == null)
                existing.gameObject.AddComponent<StandaloneInputModule>();
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
        // Mark as scene object, not prefab - will be created in scene when builder runs via menu?
        // For prefab build, we don't create EventSystem inside prefab; we ensure scene has one when needed.
        // However spec says "kalau scene aktif belum punya EventSystem, membuatnya" - so create in active scene
        // Since we are building prefab, we also ensure active scene has EventSystem now.
        // If we just created GO in memory, we need to keep it in scene.
        // Use EditorSceneManager to mark dirty? Simpler: create GO in active scene via new GameObject and keep it.
        // The GO we just created is already in scene (not prefab) because we didn't save as prefab.
        // So just log.
        Debug.Log("[DialogueUIBuilder] EventSystem dibuat dengan InputSystemUIInputModule");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
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

    private static void ApplyDefaultFont(TMP_Text tmp)
    {
        // Coba pakai font default TMP Settings; fallback biarkan Unity assign default
        try
        {
            var fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset != null)
            {
                tmp.font = fontAsset;
                return;
            }
        }
        catch { /* ignore */ }

        // Fallback: cari font SDF di project
        string[] guids = AssetDatabase.FindAssets("t:TMPro.TMP_FontAsset");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) tmp.font = font;
        }
    }
}
