using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Overlay tutorial: dim background + highlight lane + hand pointer + arrow pulse.
/// Dibuat runtime seperti DialogueSystem, bisa di-drag sebagai prefab modular.
/// SortingOrder 150 di atas DialoguePanel (100).
/// </summary>
public class KarindingTutorialOverlay : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float dimAlpha = 0.62f;
    [SerializeField] private float highlightPulseScale = 1.08f;
    [SerializeField] private float highlightPulseDuration = 0.45f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.45f, 1f);

    private Canvas _canvas;
    private CanvasGroup _rootGroup;
    private Image _dimImage;
    private GameObject _rootObject;
    private readonly Dictionary<LaneSide, Image> _laneOverlays = new Dictionary<LaneSide, Image>();
    private readonly Dictionary<LaneSide, Tween> _pulseTweens = new Dictionary<LaneSide, Tween>();
    private RectTransform _handRect;
    private Image _handImage;
    private RectTransform _arrowRect;
    private TextMeshProUGUI _instructionText;
    private CanvasGroup _instructionGroup;
    private GameObject _instructionObj;
    private TextMeshProUGUI _toastText;
    private CanvasGroup _toastGroup;
    private GameObject _toastObj;
    private Tween _instructionFade;
    private Tween _toastTween;
    private Coroutine _instructionCoroutine;
    private GameObject _dotsContainer;
    private readonly List<Image> _dotImages = new List<Image>();
    private GameObject _epilogContainer;
    private Button _btnRetry;
    private Button _btnContinue;
    private bool _isBuilt;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
        BuildUI();
    }

    private void OnDestroy()
    {
        foreach (var kv in _pulseTweens) kv.Value?.Kill();
        _pulseTweens.Clear();
        _instructionFade?.Kill();
        _toastTween?.Kill();
    }

    private void BuildUI()
    {
        if (_isBuilt) return;
        if (_canvas == null)
        {
            Debug.LogError("[TutorialOverlay] Canvas not found");
            return;
        }

        _rootObject = new GameObject("TutorialOverlayRoot");
        _rootObject.transform.SetParent(_canvas.transform, false);

        // Make sure we are on top of DialoguePanel (Canvas sorting 100)
        var rootCanvas = _rootObject.AddComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = 150;
        _rootObject.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = _rootObject.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = _rootObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _rootGroup = _rootObject.AddComponent<CanvasGroup>();
        _rootGroup.alpha = 0f;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;

        // Dim background - covers all but raycastTarget false so clicks pass to lanes
        GameObject dimObj = new GameObject("DimBG");
        dimObj.transform.SetParent(_rootObject.transform, false);
        RectTransform dimRect = dimObj.GetComponent<RectTransform>();
        if (dimRect == null) dimRect = dimObj.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        _dimImage = dimObj.AddComponent<Image>();
        _dimImage.color = new Color(0f, 0f, 0f, 0f);
        _dimImage.raycastTarget = false;

        // Hand pointer - use TapHand sprite if available, fallback to white dot
        GameObject handObj = new GameObject("HandPointer");
        handObj.transform.SetParent(_rootObject.transform, false);
        _handRect = handObj.GetComponent<RectTransform>();
        if (_handRect == null) _handRect = handObj.AddComponent<RectTransform>();
        _handRect.sizeDelta = new Vector2(140f, 140f);
        _handRect.anchorMin = new Vector2(0.5f, 0.5f);
        _handRect.anchorMax = new Vector2(0.5f, 0.5f);
        _handRect.anchoredPosition = new Vector2(0f, -220f);
        _handImage = handObj.AddComponent<Image>();
        _handImage.raycastTarget = false;
        _handImage.enabled = false;
        // Try load TapHand sprite
        var tapHand = Resources.Load<Sprite>("TapHand");
        if (tapHand != null)
        {
            _handImage.sprite = tapHand;
            _handImage.preserveAspect = true;
            _handRect.sizeDelta = new Vector2(170f, 170f);
        }
        else
        {
            _handImage.color = new Color(1f, 1f, 1f, 0.9f);
        }

        // Arrow above hand
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(handObj.transform, false);
        _arrowRect = arrowObj.GetComponent<RectTransform>();
        if (_arrowRect == null) _arrowRect = arrowObj.AddComponent<RectTransform>();
        _arrowRect.anchorMin = new Vector2(0.5f, 1f);
        _arrowRect.anchorMax = new Vector2(0.5f, 1f);
        _arrowRect.pivot = new Vector2(0.5f, 0f);
        _arrowRect.anchoredPosition = new Vector2(0f, 18f);
        _arrowRect.sizeDelta = new Vector2(44f, 44f);
        var arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = new Color(1f, 0.95f, 0.4f, 1f);
        arrowImg.raycastTarget = false;
        // Simple triangle via rotation - use default sprite as placeholder
        arrowImg.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        arrowObj.SetActive(false);

        // Instruction Text - top center, non-blocking (Android friendly)
        GameObject instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(_rootObject.transform, false);
        RectTransform instrRect = instrObj.GetComponent<RectTransform>();
        if (instrRect == null) instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.05f, 0.78f);
        instrRect.anchorMax = new Vector2(0.95f, 0.92f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;
        // Background panel
        var bgObj = new GameObject("InstructionBG");
        bgObj.transform.SetParent(instrObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        if (bgRect == null) bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-12f, -10f);
        bgRect.offsetMax = new Vector2(12f, 10f);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.52f);
        bgImg.raycastTarget = false;
        // Text
        _instructionObj = instrObj;
        _instructionGroup = instrObj.AddComponent<CanvasGroup>();
        _instructionGroup.alpha = 0f;
        _instructionGroup.blocksRaycasts = false;
        _instructionGroup.interactable = false;
        _instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        _instructionText.alignment = TextAlignmentOptions.Center;
        _instructionText.fontSize = 30;
        _instructionText.fontStyle = FontStyles.Bold;
        _instructionText.color = Color.white;
        _instructionText.enableWordWrapping = true;
        _instructionText.overflowMode = TextOverflowModes.Truncate;
        _instructionText.raycastTarget = false;
        _instructionText.text = "";
        instrObj.SetActive(false);

        // Toast Text - center, for brief feedback (sumbang / bagus)
        GameObject toastObj = new GameObject("ToastText");
        toastObj.transform.SetParent(_rootObject.transform, false);
        RectTransform toastRect = toastObj.GetComponent<RectTransform>();
        if (toastRect == null) toastRect = toastObj.AddComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 0.5f);
        toastRect.anchorMax = new Vector2(0.5f, 0.5f);
        toastRect.pivot = new Vector2(0.5f, 0.5f);
        toastRect.anchoredPosition = new Vector2(0f, 120f);
        toastRect.sizeDelta = new Vector2(900f, 80f);
        _toastObj = toastObj;
        _toastGroup = toastObj.AddComponent<CanvasGroup>();
        _toastGroup.alpha = 0f;
        _toastText = toastObj.AddComponent<TextMeshProUGUI>();
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.fontSize = 36;
        _toastText.fontStyle = FontStyles.Bold;
        _toastText.color = Color.white;
        _toastText.enableWordWrapping = true;
        _toastText.raycastTarget = false;
        _toastText.text = "";
        toastObj.SetActive(false);

        // Progress Dots - top, 4 dots for 4 buttons (clear & fast)
        _dotsContainer = new GameObject("ProgressDots");
        _dotsContainer.transform.SetParent(_rootObject.transform, false);
        RectTransform dotsRect = _dotsContainer.GetComponent<RectTransform>();
        if (dotsRect == null) dotsRect = _dotsContainer.AddComponent<RectTransform>();
        dotsRect.anchorMin = new Vector2(0.5f, 0.92f);
        dotsRect.anchorMax = new Vector2(0.5f, 0.92f);
        dotsRect.pivot = new Vector2(0.5f, 0.5f);
        dotsRect.anchoredPosition = Vector2.zero;
        dotsRect.sizeDelta = new Vector2(260f, 36f);
        var dotsLayout = _dotsContainer.AddComponent<HorizontalLayoutGroup>();
        dotsLayout.spacing = 18f;
        dotsLayout.childAlignment = TextAnchor.MiddleCenter;
        dotsLayout.childForceExpandHeight = false;
        dotsLayout.childForceExpandWidth = false;
        dotsLayout.childControlHeight = true;
        dotsLayout.childControlWidth = true;
        _dotImages.Clear();
        Color[] dotColors = new Color[] { new Color(1f, 0.38f, 0.38f, 1f), new Color(1f, 0.85f, 0.25f, 1f), new Color(0.18f, 0.78f, 0.38f, 1f), new Color(0.22f, 0.65f, 0.92f, 1f) };
        for (int i = 0; i < 4; i++)
        {
            GameObject dot = new GameObject($"Dot{i}");
            dot.transform.SetParent(_dotsContainer.transform, false);
            RectTransform dRect = dot.AddComponent<RectTransform>();
            dRect.sizeDelta = new Vector2(28f, 28f);
            var img = dot.AddComponent<Image>();
            img.color = new Color(dotColors[i].r, dotColors[i].g, dotColors[i].b, 0.35f);
            img.raycastTarget = false;
            // use circle via rounded? simple fill, will punch on complete
            _dotImages.Add(img);
        }
        _dotsContainer.SetActive(false);

        // Epilog 2 buttons: ULANG (kiri) + LANJUT (kanan) - bersampingan, tidak ke-timpa, safe area Android
        _epilogContainer = new GameObject("EpilogButtons");
        _epilogContainer.transform.SetParent(_rootObject.transform, false);
        RectTransform epiRect = _epilogContainer.GetComponent<RectTransform>();
        if (epiRect == null) epiRect = _epilogContainer.AddComponent<RectTransform>();
        epiRect.anchorMin = new Vector2(0.04f, 0.02f);
        epiRect.anchorMax = new Vector2(0.96f, 0.16f);
        epiRect.offsetMin = Vector2.zero;
        epiRect.offsetMax = Vector2.zero;
        var epiLayout = _epilogContainer.AddComponent<HorizontalLayoutGroup>();
        epiLayout.spacing = 16f;
        epiLayout.padding = new RectOffset(8, 8, 8, 8);
        epiLayout.childAlignment = TextAnchor.MiddleCenter;
        epiLayout.childForceExpandHeight = true;
        epiLayout.childForceExpandWidth = true;
        epiLayout.childControlHeight = true;
        epiLayout.childControlWidth = true;
        // Btn ULANG kiri (merah) - bersampingan
        _btnRetry = CreateEpilogButton("Btn_Ulang", "ULANG", new Color(0.85f, 0.18f, 0.18f, 1f));
        // Btn LANJUT kanan (hijau) - bersampingan
        _btnContinue = CreateEpilogButton("Btn_Lanjut", "LANJUT", new Color(0.18f, 0.68f, 0.28f, 1f));
        _epilogContainer.SetActive(false);

        _rootObject.SetActive(false);
        _isBuilt = true;
        Debug.Log("[TutorialOverlay] BuildUI done, canvas=" + _canvas.name);
    }

    public void ShowDim(bool show, float duration = 0.25f)
    {
        if (!_isBuilt) BuildUI();
        if (_rootObject == null) return;
        _rootObject.SetActive(true);
        _rootGroup.blocksRaycasts = false; // never block lane clicks
        if (show)
        {
            _rootGroup.alpha = 1f;
            _dimImage.DOKill();
            _dimImage.color = new Color(0f, 0f, 0f, 0f);
            _dimImage.DOFade(dimAlpha, duration).SetEase(Ease.OutQuad);
        }
        else
        {
            _dimImage.DOKill();
            _dimImage.DOFade(0f, duration).OnComplete(() =>
            {
                // Jangan sembunyikan root jika masih ada instruction/toast/dots/epilog visible (aba-aba & 2 tombol)
                bool hasVisibleContent = (_instructionGroup != null && _instructionObj != null && _instructionObj.activeSelf && _instructionGroup.alpha > 0.01f)
                                      || (_toastGroup != null && _toastObj != null && _toastObj.activeSelf && _toastGroup.alpha > 0.01f)
                                      || (_dotsContainer != null && _dotsContainer.activeSelf)
                                      || (_epilogContainer != null && _epilogContainer.activeSelf);
                if (!hasVisibleContent && _dimImage.color.a <= 0.01f)
                {
                    _rootGroup.alpha = 0f;
                    _rootObject.SetActive(false);
                }
            });
        }
    }

    public void HideAll(float duration = 0.2f)
    {
        ClearAllHighlights();
        ShowHand(false);
        HideInstruction(0.15f);
        HideToast();
        SetProgressDotsVisible(false);
        ShowEpilogButtons(false);
        _dimImage.DOKill();
        _dimImage.DOFade(0f, duration).OnComplete(() =>
        {
            _rootGroup.alpha = 0f;
            _rootObject.SetActive(false);
        });
    }

    private Button CreateEpilogButton(string name, string label, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(_epilogContainer.transform, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = bgColor;
        btnImg.raycastTarget = true;
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        // Text - auto sizing biar muat di Android
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(8f, 4f);
        txtRect.offsetMax = new Vector2(-8f, -4f);
        var txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = 22;
        txt.fontSizeMax = 30;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.raycastTarget = false;
        // Layout - flexibleWidth 1 biar bersampingan rata
        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 88f;
        le.flexibleWidth = 1f;
        le.minWidth = 100f;
        return btn;
    }

    public void ShowEpilogButtons(bool show)
    {
        if (!_isBuilt) BuildUI();
        if (_epilogContainer == null) return;
        _epilogContainer.SetActive(show);
        if (show)
        {
            if (_rootObject != null && !_rootObject.activeSelf) { _rootObject.SetActive(true); _rootGroup.alpha = 1f; }
            // Ensure dim visible behind buttons
            if (_dimImage != null && _dimImage.color.a < 0.1f) _dimImage.color = new Color(0f, 0f, 0f, dimAlpha);
            // Hook controller actions if not yet
            var ctrl = FindFirstObjectByType<KarindingTutorialController>();
            if (ctrl != null)
            {
                _btnRetry.onClick.RemoveAllListeners();
                _btnContinue.onClick.RemoveAllListeners();
                _btnRetry.onClick.AddListener(() => ctrl.RestartTutorial());
                _btnContinue.onClick.AddListener(() => { PlayerPrefs.SetInt("tutorial_karinding_done", 1); PlayerPrefs.Save(); ctrl.LoadNextScene(); });
            }
            _epilogContainer.transform.localScale = Vector3.one * 0.9f;
            _epilogContainer.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    public void SetProgressDotsVisible(bool visible)
    {
        if (_dotsContainer == null) return;
        _dotsContainer.SetActive(visible);
    }

    public void UpdateProgressDots(int completed, int total = 4)
    {
        if (!_isBuilt) BuildUI();
        if (_dotsContainer == null) return;
        _dotsContainer.SetActive(true);
        for (int i = 0; i < _dotImages.Count; i++)
        {
            bool done = i < completed;
            var img = _dotImages[i];
            img.DOKill();
            if (done)
            {
                img.DOColor(new Color(img.color.r, img.color.g, img.color.b, 1f), 0.2f);
                img.transform.DOKill();
                img.transform.DOPunchScale(Vector3.one * 0.4f, 0.25f, 5, 0.8f);
            }
            else
            {
                img.DOColor(new Color(img.color.r, img.color.g, img.color.b, 0.35f), 0.2f);
                img.transform.localScale = Vector3.one;
            }
        }
    }

    public void ShowProgressDots(int completed) => UpdateProgressDots(completed);

    // Android-friendly instruction (non-blocking, no pause)
    public void ShowInstruction(string text, float duration = -1f)
    {
        if (!_isBuilt) BuildUI();
        if (_instructionObj == null || _instructionText == null) return;
        // Ensure root visible
        if (_rootObject != null && !_rootObject.activeSelf) { _rootObject.SetActive(true); _rootGroup.alpha = 1f; }
        _instructionObj.SetActive(true);
        _instructionText.text = text;
        _instructionFade?.Kill();
        _instructionGroup.DOKill();
        _instructionGroup.alpha = 0f;
        _instructionGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        // Scale pop
        _instructionText.transform.localScale = Vector3.one * 0.92f;
        _instructionText.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

        if (_instructionCoroutine != null) StopCoroutine(_instructionCoroutine);
        if (duration > 0f) _instructionCoroutine = StartCoroutine(HideInstructionAfter(duration));
    }

    public void HideInstruction(float duration = 0.15f)
    {
        if (_instructionGroup == null) return;
        if (_instructionCoroutine != null) { StopCoroutine(_instructionCoroutine); _instructionCoroutine = null; }
        _instructionFade?.Kill();
        _instructionGroup.DOKill();
        _instructionFade = _instructionGroup.DOFade(0f, duration).OnComplete(() =>
        {
            if (_instructionObj != null && _instructionGroup.alpha <= 0.01f) _instructionObj.SetActive(false);
        });
    }

    private System.Collections.IEnumerator HideInstructionAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInstruction(0.2f);
    }

    public void ShowToast(string text, Color? color = null, float duration = 1.1f)
    {
        if (!_isBuilt) BuildUI();
        if (_toastObj == null || _toastText == null) return;
        if (_rootObject != null && !_rootObject.activeSelf) { _rootObject.SetActive(true); _rootGroup.alpha = 1f; }
        _toastText.text = text;
        _toastText.color = color ?? Color.white;
        _toastObj.SetActive(true);
        _toastGroup.DOKill();
        _toastText.transform.DOKill();
        _toastGroup.alpha = 0f;
        _toastText.transform.localScale = Vector3.one * 0.7f;
        _toastText.transform.localPosition = Vector3.zero;
        Sequence seq = DOTween.Sequence();
        seq.Append(_toastGroup.DOFade(1f, 0.18f));
        seq.Join(_toastText.transform.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack));
        seq.AppendInterval(duration);
        seq.Append(_toastGroup.DOFade(0f, 0.25f));
        seq.Join(_toastText.transform.DOScale(Vector3.one * 0.9f, 0.25f));
        seq.OnComplete(() => { if (_toastObj != null) _toastObj.SetActive(false); });
        _toastTween = seq;
    }

    public void HideToast()
    {
        _toastTween?.Kill();
        if (_toastGroup != null) _toastGroup.alpha = 0f;
        if (_toastObj != null) _toastObj.SetActive(false);
    }

    public void HighlightLane(LaneSide lane, bool pulse = true)
    {
        var btn = FindLaneButton(lane);
        if (btn == null) return;
        var btnRect = btn.transform as RectTransform;
        var btnImg = btn.GetComponent<Image>();
        if (btnRect == null) return;

        // Use LaneButton.SetHighlight if available
        var laneBtn = btn.GetComponent<LaneButton>();
        if (laneBtn != null) laneBtn.SetHighlight(true);

        // Pulse tween on button scale
        if (_pulseTweens.TryGetValue(lane, out var existing)) existing.Kill();
        if (pulse)
        {
            var t = btnRect.DOScale(highlightPulseScale, highlightPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            _pulseTweens[lane] = t;
        }

        // Optional: flash color
        if (btnImg != null)
        {
            btnImg.DOKill();
            btnImg.DOColor(highlightColor, 0.2f).SetLoops(2, LoopType.Yoyo);
        }
    }

    public void ClearHighlight(LaneSide lane)
    {
        var btn = FindLaneButton(lane);
        if (btn != null)
        {
            var btnRect = btn.transform as RectTransform;
            var laneBtn = btn.GetComponent<LaneButton>();
            if (laneBtn != null) laneBtn.SetHighlight(false);
            if (btnRect != null) btnRect.DOKill();
            if (_pulseTweens.TryGetValue(lane, out var t)) { t.Kill(); _pulseTweens.Remove(lane); }
            if (btnRect != null) btnRect.localScale = Vector3.one;
            var img = btn.GetComponent<Image>();
            if (img != null) { img.DOKill(); img.color = Color.white; }
        }
    }

    public void ClearAllHighlights()
    {
        foreach (LaneSide lane in System.Enum.GetValues(typeof(LaneSide)))
            ClearHighlight(lane);
    }

    public void ShowHand(LaneSide lane, Vector2 offset = default)
    {
        var btn = FindLaneButton(lane);
        if (btn == null || _handRect == null) return;
        var btnRect = btn.transform as RectTransform;
        if (btnRect == null) return;

        // Position hand slightly above button
        Vector3 worldPos = btnRect.position;
        Vector3 handWorld = worldPos + new Vector3(offset.x, offset.y + 55f, 0f);

        // Convert world to canvas local
        var parentRect = _handRect.parent as RectTransform;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, handWorld);
        // If canvas is ScreenSpaceCamera, use canvas camera
        Camera cam = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, cam, out var local);
        _handRect.anchoredPosition = local;
        _handImage.enabled = true;
        _handRect.gameObject.SetActive(true);
        _arrowRect.gameObject.SetActive(true);
        _handRect.localScale = Vector3.one * 0.85f;
        _handRect.DOKill();
        _handRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        // Bobbing
        _handRect.DOAnchorPosY(local.y + 10f, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        _arrowRect.DOKill();
        _arrowRect.localScale = Vector3.one;
        _arrowRect.DOScale(Vector3.one * 1.15f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public void ShowHand(bool show)
    {
        if (_handRect == null) return;
        _handRect.DOKill();
        _arrowRect.DOKill();
        _handRect.gameObject.SetActive(show);
        if (show) _handImage.enabled = true;
    }

    public void MoveHandTo(LaneSide lane, float duration = 0.4f)
    {
        var btn = FindLaneButton(lane);
        if (btn == null || _handRect == null) return;
        var btnRect = btn.transform as RectTransform;
        Vector3 worldPos = btnRect.position;
        var parentRect = _handRect.parent as RectTransform;
        Camera cam = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = _canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos + new Vector3(0f, 55f, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, cam, out var local);
        _handRect.DOKill();
        _handRect.DOAnchorPos(local, duration).SetEase(Ease.InOutQuad);
    }

    private LaneButton FindLaneButton(LaneSide lane)
    {
        var all = FindObjectsByType<LaneButton>(FindObjectsSortMode.None);
        foreach (var b in all) if (b.Lane == lane) return b;
        return null;
    }
}
