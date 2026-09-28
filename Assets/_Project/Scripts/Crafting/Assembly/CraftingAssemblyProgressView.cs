using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Visual progress bar untuk assembly (0-100%). Warna berubah + warning pulse saat decay imminent.
/// ExecuteAlways: di editor, base pose TapHand disinkron kontinu dari hasil drag (tanpa Play).
/// </summary>
[ExecuteAlways]
public class CraftingAssemblyProgressView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI phaseLabel;
    [SerializeField] private CanvasGroup warningGroup;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private TextMeshProUGUI toastText;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private CanvasGroup successOverlay;
    [SerializeField] private RectTransform successPanel;
    [SerializeField] private TextMeshProUGUI successTitle;
    [SerializeField] private Image successIcon;

    [Header("Selesai Preview 3D")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private KarindingPreviewController previewController;

    [Header("Colors")]
    [SerializeField] private Color cuttingColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color tappingNormalColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color tappingWarningColor = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color tappingDecayColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color completedColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Warning")]
    [SerializeField] private float warningThresholdFactor = 0.7f; // 0.7 * graceTime = mulai warning

    [Header("Tap Visual (tangan naik ke background)")]
    [Tooltip("Opsional: isi manual, kosong = auto-load Resources/TapHand, bg_tapKarinding & TapStageBG")]
    [SerializeField] private Sprite tapHandSprite;
    [SerializeField] private Sprite tapBgSprite;
    [SerializeField] private Sprite tapStageSprite;
    [Tooltip("Rentang gerak + pose awal default (dipakai saat objek dibuat). Pose final = hasil drag editor (auto-sync).")]
    [SerializeField] private float tapHandStartYNorm = -0.28f;
    [SerializeField] private float tapHandEndYNorm = 0.24f;
    [Tooltip("Otomatis: base pose tangan dari drag editor. Jangan ubah manual.")]
    [SerializeField, HideInInspector] private float tapHandBaseYNorm = -0.28f;
    [Tooltip("Tinggi gambar tangan sebagai fraksi tinggi TapArea.")]
    [SerializeField] private float tapHandHeightNorm = 1.0f;
    [Tooltip("Tinggi gambar kayu target sebagai fraksi tinggi TapArea (dipakai saat objek dibuat).")]
    [SerializeField] private float tapStageHeightNorm = 0.85f;
    [Tooltip("Posisi Y kayu target (fraksi tinggi TapArea, dipakai saat objek dibuat).")]
    [SerializeField] private float tapStageYNorm = 0.20f;
    [Tooltip("true = posisi/ukuran TapStage murni milikmu (drag bebas di editor, kode tidak utak-atik).")]
    [SerializeField] private bool tapStageFree = true;
    [Tooltip("Kecepatan halus gerak tangan (lebih besar = lebih responsif).")]
    [SerializeField] private float tapHandSmooth = 10f;
    [Tooltip("EDIT SAJA (tidak dipakai saat main): geser untuk intip pose tangan di progress 0-1.")]
    [Range(0f, 1f)]
    [SerializeField] private float tapPreviewT = 0f;

    [Header("Cut Visual (karinding2d main + golok sec) Atas->Bawah")]
    [Tooltip("Sprite bambu3_benar sebagai target utama di CutArea (0-50%). Kosong = auto-load Resources/bambu3_benar")]
    [SerializeField] private Sprite cutStageSprite;
    [Tooltip("Sprite golok tangan untuk CutArea (0-50%). Kosong = auto-load Resources/hand-golok3")]
    [SerializeField] private Sprite cutHandSprite;
    [Tooltip("Posisi Y awal golok (atas) sebagai fraksi tinggi CutArea")]
    [SerializeField] private float cutHandStartYNorm = 0.32f;
    [Tooltip("Posisi Y akhir golok (bawah mendekati karinding) sebagai fraksi tinggi CutArea")]
    [SerializeField] private float cutHandEndYNorm = -0.18f;
    [Tooltip("Otomatis: base pose golok dari drag editor. Jangan ubah manual.")]
    [SerializeField, HideInInspector] private float cutHandBaseYNorm = 0.32f;
    [Tooltip("Tinggi gambar golok sebagai fraksi tinggi CutArea")]
    [SerializeField] private float cutHandHeightNorm = 0.55f;
    [Tooltip("Tinggi bambu target sebagai fraksi tinggi CutArea")]
    [SerializeField] private float cutStageHeightNorm = 0.48f;
    [Tooltip("Posisi Y bambu target (fraksi tinggi CutArea)")]
    [SerializeField] private float cutStageYNorm = -0.08f;
    [Tooltip("true = posisi/ukuran CutStage murni milikmu (drag bebas di editor, kode tidak utak-atik).")]
    [SerializeField] private bool cutStageFree = true;
    [Tooltip("true = posisi/ukuran CutGolokHand murni milikmu (drag bebas di editor, kode tidak utak-atik).")]
    [SerializeField] private bool cutHandFree = true;
    [Tooltip("Kecepatan halus gerak golok (lebih besar = lebih responsif)")]
    [SerializeField] private float cutHandSmooth = 12f;
    [Tooltip("EDIT SAJA (tidak dipakai saat main): geser untuk intip pose golok di progress 0-50%.")]
    [Range(0f, 1f)]
    [SerializeField] private float cutPreviewT = 0f;

    private bool isWarningActive;
    private Tween warningTween;
    private Tween barPulseTween;
    private Tween toastTween;
    private Tween successTween;

    private RectTransform tapAreaRect;
    private RectTransform tapHandRect;
    private RectTransform tapStageRect;
    private RectTransform tapFullBgRect;
    private float tapHandTargetY;
    private float tapPreviewApplied;
    private bool tapArrivedPing;

    private RectTransform cutAreaRect;
    private RectTransform cutHandRect;
    private RectTransform cutStageRect;
    private float cutHandTargetY;
    private float cutPreviewApplied;
    private bool cutArrivedPing;

    private void Awake()
    {
        // ExecuteAlways: di editor jangan wiring apa pun (mencegah listener bocor ke scene)
        if (!Application.isPlaying) return;
        if (progressSlider == null) progressSlider = GetComponentInChildren<Slider>(true);
        if (fillImage == null && progressSlider != null && progressSlider.fillRect != null)
            fillImage = progressSlider.fillRect.GetComponent<Image>();
        if (barRect == null && progressSlider != null)
            barRect = progressSlider.GetComponent<RectTransform>();

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 100f;
            progressSlider.value = 0f;
        }

        if (warningGroup != null)
        {
            warningGroup.alpha = 0f;
            warningGroup.blocksRaycasts = false;
        }
        if (toastGroup != null)
        {
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
        }
        if (toastText != null) toastText.text = "";
        if (successOverlay == null)
        {
            var allCG = Resources.FindObjectsOfTypeAll<CanvasGroup>();
            foreach (var cg in allCG) if (cg.gameObject.name == "SuccessOverlay") { successOverlay = cg; break; }
            if (successPanel == null && successOverlay != null)
            {
                var p = successOverlay.transform.Find("Panel");
                if (p != null) successPanel = p as RectTransform;
            }
            if (successTitle == null && successPanel != null)
            {
                var t = successPanel.Find("Title");
                if (t != null) successTitle = t.GetComponent<TextMeshProUGUI>();
            }
            if (successIcon == null && successPanel != null)
            {
                var ic = successPanel.Find("Icon");
                if (ic != null) successIcon = ic.GetComponent<Image>();
            }
        }
        if (successOverlay != null)
        {
            successOverlay.alpha = 0f;
            successOverlay.blocksRaycasts = false;
            successOverlay.interactable = false;
            successOverlay.gameObject.SetActive(false);
        }
        // Auto-wire BtnLanjut (harus pakai FindObjectsOfTypeAll karena parent inactive)
        {
            Button btn = null;
            if (successOverlay != null)
            {
                var btnTr = successOverlay.transform.Find("Panel/BtnLanjut");
                if (btnTr != null) btn = btnTr.GetComponent<Button>();
            }
            if (btn == null)
            {
                var allBtn = Resources.FindObjectsOfTypeAll<Button>();
                foreach (var b in allBtn) if (b.gameObject.name == "BtnLanjut" && b.transform.IsChildOf(successOverlay != null ? successOverlay.transform : null)) { btn = b; break; }
                if (btn == null) foreach (var b in allBtn) if (b.gameObject.name == "BtnLanjut") { btn = b; break; }
            }
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnLanjutClicked);
                btn.onClick.AddListener(OnLanjutClicked);
            }
        }

        SetProgress(0f, AssemblyPhase.Cutting, 0f);
    }

    public void SetPhaseVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible)
        {
            ClearWarning();
        }
    }

    public void SetProgress(float progress, AssemblyPhase phase, float timeSinceLastTap)
    {
        if (progressSlider != null) progressSlider.value = progress;
        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(progress)}%";

        string label = phase == AssemblyPhase.Cutting ? "CUT" :
                       phase == AssemblyPhase.Tapping ? "TAP!" :
                       "SELESAI";
        if (phaseLabel != null) phaseLabel.text = label;

        // Color logic
        Color targetColor = cuttingColor;
        if (phase == AssemblyPhase.Cutting)
        {
            targetColor = cuttingColor;
            ClearWarning();
        }
        else if (phase == AssemblyPhase.Tapping)
        {
            var cfg = GetConfig();
            float grace = cfg != null ? cfg.tapDecayGraceTime : 0.25f;
            float warnAt = grace * warningThresholdFactor;

            if (timeSinceLastTap > grace)
            {
                targetColor = tappingDecayColor;
                SetWarning(true, true); // decay active
            }
            else if (timeSinceLastTap > warnAt)
            {
                targetColor = tappingWarningColor;
                SetWarning(true, false); // imminent
            }
            else
            {
                targetColor = tappingNormalColor;
                SetWarning(false, false);
            }
        }
        else if (phase == AssemblyPhase.Completed)
        {
            targetColor = completedColor;
            ClearWarning();
        }

        if (fillImage != null) fillImage.color = targetColor;

        UpdateCutVisualTarget(progress, phase);
        UpdateTapVisualTarget(progress, phase);
    }

    private void Update()
    {
#if UNITY_EDITOR
        // ExecuteAlways: di editor, sinkron kontinu base pose dari hasil drag (tanpa perlu Play).
        // Tanpa ini, drag transform tidak memicu OnValidate sehingga base tidak pernah tersinkron.
        if (!Application.isPlaying)
        {
            SyncTapHandBaseFromPose();
            SyncCutHandBaseFromPose();
            return;
        }
#endif
        // Gerak halus TapHand menuju target (termasuk mundur saat decay)
        if (tapHandRect != null && tapAreaRect != null && tapAreaRect.gameObject.activeInHierarchy)
        {
            var p = tapHandRect.anchoredPosition;
            float k = 1f - Mathf.Exp(-tapHandSmooth * Time.deltaTime);
            p.y = Mathf.Lerp(p.y, tapHandTargetY, k);
            tapHandRect.anchoredPosition = p;
        }
        // Gerak halus CutGolokHand menuju target (atas -> bawah, 0-50%)
        if (cutHandRect != null && cutAreaRect != null && cutAreaRect.gameObject.activeInHierarchy)
        {
            var cp = cutHandRect.anchoredPosition;
            float ck = 1f - Mathf.Exp(-cutHandSmooth * Time.deltaTime);
            cp.y = Mathf.Lerp(cp.y, cutHandTargetY, ck);
            cutHandRect.anchoredPosition = cp;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Baca pose drag murni (tanpa offset preview) sebagai base. Dipakai tiap frame editor
    /// dan oleh OnValidate — base selalu segar walau user hanya drag tanpa menyentuh inspector.
    /// </summary>
    private void SyncTapHandBaseFromPose()
    {
        if (tapAreaRect == null || tapHandRect == null) return;
        float areaH = tapAreaRect.rect.height;
        if (areaH < 1f) return;
        var p = tapHandRect.anchoredPosition;
        float nb = (p.y - tapPreviewApplied) / areaH;
        // Guard epsilon: jangan tandai scene dirty tiap frame bila tidak ada perubahan
        if (Mathf.Abs(nb - tapHandBaseYNorm) > 0.0001f) tapHandBaseYNorm = nb;
    }

    private void SyncCutHandBaseFromPose()
    {
        if (cutAreaRect == null || cutHandRect == null) return;
        float areaH = cutAreaRect.rect.height;
        if (areaH < 1f) return;
        var p = cutHandRect.anchoredPosition;
        float nb = (p.y - cutPreviewApplied) / areaH;
        if (Mathf.Abs(nb - cutHandBaseYNorm) > 0.0001f) cutHandBaseYNorm = nb;
    }
#endif

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        tapPreviewT = Mathf.Clamp01(tapPreviewT);
        cutPreviewT = Mathf.Clamp01(cutPreviewT);
        // Preview edit-mode: bangun objek bila belum ada, lalu pose-kan tangan sesuai slider.
        EnsureTapVisual();
        EnsureCutVisual();
        // Tap preview
        if (tapAreaRect != null && tapHandRect != null)
        {
            LayoutCover(tapFullBgRect, tapBgSprite);
            if (!tapStageFree) LayoutTapStage();
            LayoutTapHand();
            float areaH = tapAreaRect.rect.height;
            if (areaH >= 1f)
            {
                SyncTapHandBaseFromPose();
                float range = (tapHandEndYNorm - tapHandStartYNorm) * areaH;
                var p = tapHandRect.anchoredPosition;
                p.y += range * tapPreviewT;
                tapHandRect.anchoredPosition = p;
                tapPreviewApplied = range * tapPreviewT;
            }
        }
        // Cut preview (atas -> bawah)
        if (cutAreaRect != null && cutHandRect != null)
        {
            if (!cutStageFree) LayoutCutStage();
            if (!cutHandFree) LayoutCutHand();
            float cAreaH = cutAreaRect.rect.height;
            if (cAreaH >= 1f)
            {
                SyncCutHandBaseFromPose();
                float cRange = (cutHandEndYNorm - cutHandStartYNorm) * cAreaH;
                var cp = cutHandRect.anchoredPosition;
                cp.y += cRange * cutPreviewT;
                cutHandRect.anchoredPosition = cp;
                cutPreviewApplied = cRange * cutPreviewT;
            }
        }
#endif
    }

    /// <summary>
    /// Auto-build FullBG (fullscreen) + TapStage (kayu) + TapHand di dalam TapArea (tanpa edit scene),
    /// lalu petakan progress 50->100% menjadi gerak tangan bawah->atas mendekati kayu.
    /// Dipakai bersama oleh OnValidate (preview editor) dan runtime saat main.
    /// </summary>
    private void EnsureTapVisual()
    {
        if (tapAreaRect == null)
        {
#if UNITY_2023_1_OR_NEWER
            var tapInput = FindFirstObjectByType<CraftingTapInputView>(FindObjectsInactive.Include);
#else
            var tapInput = FindObjectOfType<CraftingTapInputView>(true);
#endif
            if (tapInput != null) tapAreaRect = tapInput.GetComponent<RectTransform>();
        }
        if (tapAreaRect == null) return;
        // Saat main wajib aktif; di editor boleh nonaktif (AssemblyRoot memang inactive sebelum Play)
        if (!tapAreaRect.gameObject.activeInHierarchy && Application.isPlaying) return;

        if (tapHandSprite == null) tapHandSprite = Resources.Load<Sprite>("TapHand");
        if (tapBgSprite == null) tapBgSprite = Resources.Load<Sprite>("bg_tapKarinding");
        if (tapStageSprite == null) tapStageSprite = Resources.Load<Sprite>("TapStageBG");

        // TapArea: transparan + diperlebar biar muat gambar besar (raycast tetap jalan)
        tapAreaRect.anchorMin = new Vector2(0.05f, 0.14f);
        tapAreaRect.anchorMax = new Vector2(0.95f, 0.72f);
        tapAreaRect.offsetMin = Vector2.zero;
        tapAreaRect.offsetMax = Vector2.zero;
        var areaImg = tapAreaRect.GetComponent<Image>();
        if (areaImg != null)
        {
            var ac = areaImg.color;
            ac.a = 0.03f;
            areaImg.color = ac;
        }

        // Background FULLSCREEN (anak AssemblyRoot, paling belakang, cover-fit ke layar)
        var rootRect = tapAreaRect.parent as RectTransform;
        if (rootRect != null)
        {
            var fullT = rootRect.Find("FullBG");
            if (fullT == null)
            {
                var fullGO = new GameObject("FullBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fullGO.transform.SetParent(rootRect, false);
                fullGO.transform.SetAsFirstSibling();
                fullT = fullGO.transform;
            }
            tapFullBgRect = (RectTransform)fullT;
            var fullImg = fullT.GetComponent<Image>();
            if (fullImg != null)
            {
                // Hanya isi bila kosong: hormati sprite yang sudah kamu pasang manual di scene
                if (fullImg.sprite == null && tapBgSprite != null) fullImg.sprite = tapBgSprite;
                fullImg.preserveAspect = true;
                fullImg.raycastTarget = false;
                fullImg.color = Color.white;
            }
            LayoutCover(tapFullBgRect, tapBgSprite);
        }

        // Outline hitam pada label biar tetap kebaca di atas background ramai
        var labelT = tapAreaRect.Find("Label");
        if (labelT != null && labelT.GetComponent<Outline>() == null)
        {
            var outline = labelT.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // Kayu target (statis di atas, garis finish tangan).
        // Bila tapStageFree: posisi/ukuran 100% milikmu (drag bebas, kode tidak utak-atik).
        var stageT = tapAreaRect.Find("TapStage");
        bool stageNew = stageT == null;
        if (stageNew)
        {
            var stageGO = new GameObject("TapStage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stageGO.transform.SetParent(tapAreaRect, false);
            stageGO.transform.SetAsFirstSibling();
            stageT = stageGO.transform;
        }
        tapStageRect = (RectTransform)stageT;
        var stageImg = stageT.GetComponent<Image>();
        if (stageImg != null)
        {
            if (stageImg.sprite == null && tapStageSprite != null) stageImg.sprite = tapStageSprite;
            stageImg.preserveAspect = true;
            stageImg.raycastTarget = false;
        }
        if (stageNew || !tapStageFree) LayoutTapStage();

        // Tangan (bergerak)
        var handT = tapAreaRect.Find("TapHand");
        bool handNew = handT == null;
        if (handNew)
        {
            var handGO = new GameObject("TapHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handGO.transform.SetParent(tapAreaRect, false);
            tapHandRect = (RectTransform)handGO.transform;
            var label = tapAreaRect.Find("Label");
            if (label != null) tapHandRect.SetSiblingIndex(label.GetSiblingIndex());
            else tapHandRect.SetAsLastSibling();
        }
        else tapHandRect = (RectTransform)handT;

        var handImg = tapHandRect.GetComponent<Image>();
        if (handImg != null)
        {
            if (handImg.sprite == null && tapHandSprite != null) handImg.sprite = tapHandSprite;
            handImg.preserveAspect = true;
            handImg.raycastTarget = false;
        }

        LayoutTapHand();

        // Objek baru: pose-kan di start + jadikan base (biar konsisten walau belum pernah di-preview)
        float areaH0 = tapAreaRect.rect.height;
        if (handNew && areaH0 >= 1f)
        {
            var cp = tapHandRect.anchoredPosition;
            cp.y = tapHandStartYNorm * areaH0;
            tapHandRect.anchoredPosition = cp;
            tapHandBaseYNorm = tapHandStartYNorm;
            tapPreviewApplied = 0f;
        }
    }

    /// <summary>
    /// Auto-build karinding2d (main) + golok (sec) di dalam CutArea (0-50%) - Atas -> Bawah.
    /// Mirip EnsureTapVisual tapi untuk fase Cutting.
    /// </summary>
    private void EnsureCutVisual()
    {
        if (cutAreaRect == null)
        {
#if UNITY_2023_1_OR_NEWER
            var cutInput = FindFirstObjectByType<CraftingCutInputView>(FindObjectsInactive.Include);
#else
            var cutInput = FindObjectOfType<CraftingCutInputView>(true);
#endif
            if (cutInput != null) cutAreaRect = cutInput.GetComponent<RectTransform>();
        }
        if (cutAreaRect == null) return;
        if (!cutAreaRect.gameObject.activeInHierarchy && Application.isPlaying) return;

        if (cutStageSprite == null)
        {
            cutStageSprite = Resources.Load<Sprite>("bambu3_benar");
#if UNITY_EDITOR
            if (cutStageSprite == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("bambu3_benar t:Sprite");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    cutStageSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
#endif
            if (cutStageSprite == null) Debug.LogWarning("[CutVisual] bambu3_benar not found - assign cutStageSprite manually to bambu3_benar");
        }
        if (cutHandSprite == null)
        {
            cutHandSprite = Resources.Load<Sprite>("hand-golok3");
            if (cutHandSprite == null) cutHandSprite = Resources.Load<Sprite>("CutGolokHand");
#if UNITY_EDITOR
            if (cutHandSprite == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("hand-golok3 t:Sprite");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    cutHandSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
#endif
            if (cutHandSprite == null) Debug.LogWarning("[CutVisual] hand-golok3 not found - assign cutHandSprite manually to hand-golok3");
        }

        // CutArea: transparan tipis agar FullBG (bg_tapKarinding) terlihat
        var areaImg = cutAreaRect.GetComponent<Image>();
        if (areaImg != null)
        {
            var ac = areaImg.color;
            ac.a = 0.03f;
            areaImg.color = ac;
        }

        // Outline hitam pada label CutArea
        var labelT = cutAreaRect.Find("Label");
        if (labelT != null && labelT.GetComponent<Outline>() == null)
        {
            var outline = labelT.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // Karinding target (MAIN) - statis di tengah-bawah
        var stageT = cutAreaRect.Find("CutStage");
        bool stageNew = stageT == null;
        if (stageNew)
        {
            var stageGO = new GameObject("CutStage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stageGO.transform.SetParent(cutAreaRect, false);
            stageGO.transform.SetAsFirstSibling();
            stageT = stageGO.transform;
        }
        cutStageRect = (RectTransform)stageT;
        var stageImg = stageT.GetComponent<Image>();
        if (stageImg != null)
        {
            // Force ke karinding2d (main) - overwrite jika masih pisau Tap
            if (cutStageSprite != null && stageImg.sprite != cutStageSprite) stageImg.sprite = cutStageSprite;
            stageImg.preserveAspect = true;
            stageImg.raycastTarget = false;
        }
        if (stageNew || !cutStageFree) LayoutCutStage();

        // Golok tangan (SEC) - bergerak atas -> bawah
        var handT = cutAreaRect.Find("CutGolokHand");
        bool handNew = handT == null;
        if (handNew)
        {
            var handGO = new GameObject("CutGolokHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handGO.transform.SetParent(cutAreaRect, false);
            cutHandRect = (RectTransform)handGO.transform;
            var label = cutAreaRect.Find("Label");
            if (label != null) cutHandRect.SetSiblingIndex(label.GetSiblingIndex());
            else cutHandRect.SetAsLastSibling();
        }
        else cutHandRect = (RectTransform)handT;

        var handImg = cutHandRect.GetComponent<Image>();
        if (handImg != null)
        {
            // Force ke hand-golok3 (sec) - overwrite pisau Tap jika masih nempel
            if (cutHandSprite != null && handImg.sprite != cutHandSprite) handImg.sprite = cutHandSprite;
            if (handImg.sprite == null && cutHandSprite != null) handImg.sprite = cutHandSprite;
            handImg.preserveAspect = true;
            handImg.raycastTarget = false;
        }

        if (handNew || !cutHandFree) LayoutCutHand();

        float areaH0 = cutAreaRect.rect.height;
        if (handNew && areaH0 >= 1f)
        {
            var cp = cutHandRect.anchoredPosition;
            cp.y = cutHandStartYNorm * areaH0;
            cutHandRect.anchoredPosition = cp;
            cutHandBaseYNorm = cutHandStartYNorm;
            cutPreviewApplied = 0f;
            cutHandTargetY = cp.y;
        }
        else if (areaH0 >= 1f)
        {
            // Sync target awal jika sudah ada
            cutHandTargetY = cutHandBaseYNorm * areaH0;
        }
    }

    private void LayoutCover(RectTransform r, Sprite s)
    {
        if (r == null) return;
        var parent = r.parent as RectTransform;
        if (parent == null) return;
        float pw = parent.rect.width;
        float ph = parent.rect.height;
        if (pw < 1f || ph < 1f) return;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        float imgAspect = 16f / 9f;
        if (s != null && s.rect.height > 0f)
            imgAspect = s.rect.width / s.rect.height;
        float parentAspect = pw / ph;
        // Cover: penuhi parent, crop sisi yang lebih — tanpa distorsi (layar yang memotong)
        if (imgAspect > parentAspect)
            r.sizeDelta = new Vector2(ph * imgAspect, ph);
        else
            r.sizeDelta = new Vector2(pw, pw / imgAspect);
    }

    private void LayoutTapStage()
    {
        if (tapAreaRect == null || tapStageRect == null) return;
        float areaH = tapAreaRect.rect.height;
        if (areaH < 1f) return;
        float stageH = areaH * tapStageHeightNorm;
        float aspect = 1f;
        if (tapStageSprite != null && tapStageSprite.rect.height > 0f)
            aspect = tapStageSprite.rect.width / tapStageSprite.rect.height;
        tapStageRect.anchorMin = tapStageRect.anchorMax = new Vector2(0.5f, 0.5f);
        tapStageRect.pivot = new Vector2(0.5f, 0.5f);
        tapStageRect.sizeDelta = new Vector2(stageH * aspect, stageH);
        var p = tapStageRect.anchoredPosition;
        p.x = 0f;
        p.y = tapStageYNorm * areaH;
        tapStageRect.anchoredPosition = p;
    }

    private void LayoutTapHand()
    {
        if (tapAreaRect == null || tapHandRect == null) return;
        float areaH = tapAreaRect.rect.height;
        float areaW = tapAreaRect.rect.width;
        if (areaH < 1f || areaW < 1f) return;
        float handH = areaH * tapHandHeightNorm;
        float aspect = 1f;
        if (tapHandSprite != null && tapHandSprite.rect.height > 0f)
            aspect = tapHandSprite.rect.width / tapHandSprite.rect.height;
        tapHandRect.anchorMin = tapHandRect.anchorMax = new Vector2(0.5f, 0.5f);
        tapHandRect.pivot = new Vector2(0.5f, 0.5f);
        tapHandRect.sizeDelta = new Vector2(handH * aspect, handH);
    }

    private void LayoutCutStage()
    {
        if (cutAreaRect == null || cutStageRect == null) return;
        float areaH = cutAreaRect.rect.height;
        if (areaH < 1f) return;
        float stageH = areaH * cutStageHeightNorm;
        float aspect = 1f;
        if (cutStageSprite != null && cutStageSprite.rect.height > 0f)
            aspect = cutStageSprite.rect.width / cutStageSprite.rect.height;
        cutStageRect.anchorMin = cutStageRect.anchorMax = new Vector2(0.5f, 0.5f);
        cutStageRect.pivot = new Vector2(0.5f, 0.5f);
        cutStageRect.sizeDelta = new Vector2(stageH * aspect, stageH);
        var p = cutStageRect.anchoredPosition;
        p.x = 0f;
        p.y = cutStageYNorm * areaH;
        cutStageRect.anchoredPosition = p;
    }

    private void LayoutCutHand()
    {
        if (cutAreaRect == null || cutHandRect == null) return;
        float areaH = cutAreaRect.rect.height;
        float areaW = cutAreaRect.rect.width;
        if (areaH < 1f || areaW < 1f) return;
        float handH = areaH * cutHandHeightNorm;
        float aspect = 1f;
        if (cutHandSprite != null && cutHandSprite.rect.height > 0f)
            aspect = cutHandSprite.rect.width / cutHandSprite.rect.height;
        cutHandRect.anchorMin = cutHandRect.anchorMax = new Vector2(0.5f, 0.5f);
        cutHandRect.pivot = new Vector2(0.5f, 0.5f);
        cutHandRect.sizeDelta = new Vector2(handH * aspect, handH);
    }

    private void UpdateCutVisualTarget(float progress, AssemblyPhase phase)
    {
        if (phase != AssemblyPhase.Cutting)
        {
            cutArrivedPing = false;
            return;
        }
        EnsureCutVisual();
        if (cutAreaRect == null || cutHandRect == null) return;
        if (!cutStageFree) LayoutCutStage();
        if (!cutHandFree) LayoutCutHand();

        var ctrl = CraftingAssemblyController.Instance;
        float threshold = ctrl != null && ctrl.Config != null ? ctrl.Config.cuttingToTappingThreshold : 50f;
        // 0 -> threshold mapping, atas -> bawah (end < start, jadi turun)
        float t = Mathf.InverseLerp(0f, threshold, progress);

        float areaH = cutAreaRect.rect.height;
        if (areaH < 1f) return;
        float newTarget = cutHandBaseYNorm * areaH + (cutHandEndYNorm - cutHandStartYNorm) * areaH * t;

        // Saat progress naik (golok turun), newTarget < cutHandTargetY, beda negatif
        if (Mathf.Abs(newTarget - cutHandTargetY) > 0.5f)
        {
            // Turun (cut valid) -> getar
            cutHandRect.DOKill();
            cutHandRect.DOShakeAnchorPos(0.13f, new Vector2(3f, 4f), 10, 90f, false, true);
            if (cutStageRect != null)
            {
                cutStageRect.DOKill();
                cutStageRect.DOShakeAnchorPos(0.11f, new Vector2(2.5f, 2.5f), 10, 90f, false, true);
            }
        }
        cutHandTargetY = newTarget;

        if (t >= 0.999f && !cutArrivedPing)
        {
            cutArrivedPing = true;
            cutHandRect.DOKill();
            cutHandRect.DOShakeAnchorPos(0.16f, new Vector2(4f, 5f), 11, 90f, false, true);
        }
        else if (t < 0.99f) cutArrivedPing = false;
    }

    private void UpdateTapVisualTarget(float progress, AssemblyPhase phase)
    {
        if (phase != AssemblyPhase.Tapping)
        {
            tapArrivedPing = false;
            return;
        }
        EnsureTapVisual();
        if (tapAreaRect == null || tapHandRect == null) return;
        LayoutCover(tapFullBgRect, tapBgSprite);
        if (!tapStageFree) LayoutTapStage();
        LayoutTapHand();

        var ctrl = CraftingAssemblyController.Instance;
        float threshold = ctrl != null && ctrl.Config != null ? ctrl.Config.cuttingToTappingThreshold : 50f;
        float complete = ctrl != null && ctrl.Config != null ? ctrl.Config.completionThreshold : 100f;
        float t = Mathf.InverseLerp(threshold, complete, progress);

        float areaH = tapAreaRect.rect.height;
        if (areaH < 1f) return;
        // Base = pose drag editor (auto-sync via OnValidate), travel = (end - start) * tinggiArea
        float newTarget = tapHandBaseYNorm * areaH + (tapHandEndYNorm - tapHandStartYNorm) * areaH * t;

        if (newTarget > tapHandTargetY + 0.5f)
        {
            // Naik (tap valid) → getar halus saja, tanpa membesar
            tapHandRect.DOKill();
            tapHandRect.DOShakeAnchorPos(0.13f, new Vector2(3f, 4f), 10, 90f, false, true);
            if (tapStageRect != null)
            {
                tapStageRect.DOKill();
                tapStageRect.DOShakeAnchorPos(0.11f, new Vector2(2.5f, 2.5f), 10, 90f, false, true);
            }
        }
        tapHandTargetY = newTarget;

        if (t >= 0.999f && !tapArrivedPing)
        {
            // Sampai 100% → getar halus sedikit lebih terasa, tanpa membesar
            tapArrivedPing = true;
            tapHandRect.DOKill();
            tapHandRect.DOShakeAnchorPos(0.16f, new Vector2(4f, 5f), 11, 90f, false, true);
        }
        else if (t < 0.99f) tapArrivedPing = false;
    }

    public void PlayCompleteFeedback()
    {
        // Upgraded: bar punch + fill flash + success overlay
        if (barRect != null)
        {
            barRect.DOKill();
            barRect.localScale = Vector3.one;
            Sequence seq = DOTween.Sequence();
            seq.Append(barRect.DOPunchScale(Vector3.one * 0.18f, 0.45f, 7, 0.8f));
            seq.Join(barRect.DOScale(1.05f, 0.18f).SetLoops(2, LoopType.Yoyo));
            if (fillImage != null)
            {
                fillImage.DOKill();
                seq.Join(fillImage.DOColor(Color.white, 0.12f).SetLoops(2, LoopType.Yoyo).OnComplete(() => fillImage.color = completedColor));
            }
            if (progressText != null)
            {
                progressText.transform.DOKill();
                progressText.transform.localScale = Vector3.one;
                seq.Join(progressText.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 5, 0.6f));
            }
        }
        else
        {
            // Fallback show overlay cepat jika bar null
            ShowSuccessOverlay();
            return;
        }

        // Delay overlay biar punch keliatan dulu
        DOVirtual.DelayedCall(0.55f, () => ShowSuccessOverlay());
    }

    public void ShowSuccessOverlay()
    {
        if (successOverlay == null)
        {
            var found = GameObject.Find("SuccessOverlay");
            if (found != null) successOverlay = found.GetComponent<CanvasGroup>();
        }
        if (successPanel == null && successOverlay != null)
        {
            var panel = successOverlay.transform.Find("Panel");
            if (panel != null) successPanel = panel as RectTransform;
        }
        if (successTitle == null && successPanel != null)
        {
            var t = successPanel.Find("Title");
            if (t != null) successTitle = t.GetComponent<TextMeshProUGUI>();
        }
        if (successIcon == null && successPanel != null)
        {
            var ic = successPanel.Find("Icon");
            if (ic != null) successIcon = ic.GetComponent<Image>();
        }

        if (successOverlay == null) { Debug.LogWarning("[ProgressView] successOverlay null"); return; }

        successOverlay.gameObject.SetActive(true);
        // Halaman selesai: sembunyikan preview mini assembly agar tidak double-drive pivot
        var miniPrev = GameObject.Find("Canvas/AssemblyRoot/KarindingPreviewMini");
        if (miniPrev != null) miniPrev.SetActive(false);
        if (previewCamera == null)
        {
            var camGO = GameObject.Find("KarindingPreviewCamera_Selesai");
            if (camGO != null) previewCamera = camGO.GetComponent<Camera>();
        }
        if (previewController == null && successOverlay != null)
            previewController = successOverlay.GetComponentInChildren<KarindingPreviewController>(true);
        if (previewCamera != null) previewCamera.enabled = true;
        if (previewController != null) previewController.ResetView();
        successOverlay.DOKill();
        successOverlay.blocksRaycasts = true;
        successOverlay.interactable = true;
        successOverlay.alpha = 0f;
        successOverlay.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);

        if (successPanel != null)
        {
            successPanel.DOKill();
            successPanel.localScale = Vector3.one * 0.5f;
            successPanel.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            successPanel.anchoredPosition = new Vector2(0, 20);
            successPanel.DOAnchorPos(new Vector2(0, 20), 0.5f).SetEase(Ease.OutBack);
        }
        if (successTitle != null)
        {
            successTitle.DOKill();
            successTitle.transform.localScale = Vector3.one;
            successTitle.transform.DOPunchScale(Vector3.one * 0.12f, 0.4f, 4, 0.6f).SetDelay(0.35f);
        }
        if (successIcon != null)
        {
            successIcon.transform.DOKill();
            successIcon.transform.localScale = Vector3.one;
            successIcon.transform.DOScale(1.08f, 0.25f).SetLoops(2, LoopType.Yoyo).SetDelay(0.4f);
            // Flash color
            successIcon.DOColor(Color.white, 0.15f).SetLoops(2, LoopType.Yoyo).SetDelay(0.3f);
        }
        // Camera shake subtle
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.DOKill();
            cam.transform.DOShakePosition(0.35f, strength: new Vector3(0.25f, 0.25f, 0), vibrato: 12, randomness: 90f);
        }
    }

    public void HideSuccessOverlay(float duration = 0.3f)
    {
        if (successOverlay == null) return;
        successOverlay.DOKill();
        successOverlay.DOFade(0f, duration).OnComplete(() =>
        {
            if (successOverlay != null)
            {
                successOverlay.blocksRaycasts = false;
                successOverlay.interactable = false;
                successOverlay.gameObject.SetActive(false);
                if (previewCamera != null) previewCamera.enabled = false;
            }
        });
    }

    private void SetWarning(bool active, bool isDecaying)
    {
        if (active == isWarningActive && !isDecaying) return; // sudah active dan bukan transisi ke decaying

        if (active)
        {
            isWarningActive = true;
            if (warningGroup != null)
            {
                warningGroup.DOKill();
                warningGroup.alpha = isDecaying ? 1f : 0.6f;
                if (isDecaying)
                {
                    warningTween?.Kill();
                    warningTween = warningGroup.DOFade(0.3f, 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }
                else
                {
                    warningTween?.Kill();
                    warningTween = warningGroup.DOFade(1f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }
            }

            if (barRect != null && isDecaying)
            {
                // Pulse bar saat decay aktif
                barPulseTween?.Kill();
                barPulseTween = barRect.DOScale(1.03f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            else if (barRect != null && !isDecaying)
            {
                barPulseTween?.Kill();
                barRect.localScale = Vector3.one;
            }
        }
        else
        {
            ClearWarning();
        }
    }

    private void ClearWarning()
    {
        isWarningActive = false;
        warningTween?.Kill();
        warningTween = null;
        barPulseTween?.Kill();
        barPulseTween = null;
        if (warningGroup != null)
        {
            warningGroup.DOKill();
            warningGroup.alpha = 0f;
        }
        if (barRect != null)
        {
            barRect.DOKill();
            barRect.localScale = Vector3.one;
        }
    }

    private CraftingAssemblyConfig GetConfig()
    {
        var ctrl = CraftingAssemblyController.Instance;
        if (ctrl != null && ctrl.Config != null) return ctrl.Config;
        // Fallback find
        var found = FindFirstObjectByType<CraftingAssemblyController>();
        if (found != null) return found.Config;
        return null;
    }

    public void ShowToast(string msg, float duration = 1.2f)
    {
        if (toastText == null || toastGroup == null)
        {
            Debug.Log($"[ProgressView Toast] {msg}");
            return;
        }
        toastText.text = msg;
        toastTween?.Kill();
        toastGroup.DOKill();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastText.DOKill();
        toastText.transform.DOKill();
        toastText.transform.localScale = Vector3.one;
        toastGroup.alpha = 1f;
        toastText.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 4, 0.6f);
        toastTween = DOVirtual.DelayedCall(duration, () =>
        {
            if (toastGroup != null) toastGroup.DOFade(0f, 0.3f);
        });
        Debug.Log($"[Toast] {msg}");
    }

    public void PlayFallFeedback()
    {
        if (barRect != null)
        {
            barRect.DOKill();
            barRect.DOShakePosition(0.35f, strength: new Vector3(8f, 0f, 0f), vibrato: 20, randomness: 90f);
        }
        if (fillImage != null)
        {
            fillImage.DOKill();
            Color orig = fillImage.color;
            fillImage.DOColor(new Color(1f, 0.3f, 0.3f, 1f), 0.08f).SetLoops(2, LoopType.Yoyo).OnComplete(() => fillImage.color = orig);
        }
    }

    public void PlayFailFeedback()
    {
        if (barRect != null)
        {
            barRect.DOKill();
            barRect.localScale = Vector3.one;
            barRect.DOShakePosition(0.5f, strength: new Vector3(14f, 6f, 0f), vibrato: 30, randomness: 90f);
        }
        if (fillImage != null)
        {
            fillImage.DOKill();
            fillImage.DOColor(Color.red, 0.15f).SetLoops(3, LoopType.Yoyo);
        }
        if (warningGroup != null)
        {
            warningGroup.DOKill();
            warningGroup.alpha = 1f;
            warningGroup.DOFade(0f, 0.6f).SetDelay(0.6f);
        }
    }

    private void OnLanjutClicked()
    {
        HideSuccessOverlay(0.3f);
        var ds = FindFirstObjectByType<DialogueSystem>();
        if (ds != null) ds.ShowDialogue("Karinding berhasil dibuat! Luar biasa!");
        else Debug.Log("[SuccessOverlay] Lanjut clicked");
    }

    private void OnDestroy()
    {
        warningTween?.Kill();
        barPulseTween?.Kill();
        toastTween?.Kill();
        successTween?.Kill();
        if (tapHandRect != null) tapHandRect.DOKill();
        if (tapStageRect != null) tapStageRect.DOKill();
        if (cutHandRect != null) cutHandRect.DOKill();
        if (cutStageRect != null) cutStageRect.DOKill();
        if (successOverlay != null)
        {
            var btnTr = successOverlay.transform.Find("Panel/BtnLanjut");
            if (btnTr != null)
            {
                var btn = btnTr.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveListener(OnLanjutClicked);
            }
        }
    }
}
