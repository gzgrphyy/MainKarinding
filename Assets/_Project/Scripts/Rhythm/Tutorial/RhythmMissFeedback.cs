using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RhythmMissFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image merah full-screen, alpha awal 0.")]
    [SerializeField] private Image flashImage;

    [Tooltip("Track yang di-shake saat miss.")]
    [SerializeField] private RectTransform trackRect;

    [Tooltip("Template popup MISS (Text + CanvasGroup), harus inactive.")]
    [SerializeField] private RectTransform popupTemplate;

    [Tooltip("Overlay gagal total (CanvasGroup), harus inactive.")]
    [SerializeField] private CanvasGroup failOverlayRoot;

    [Tooltip("Teks GAGAL di dalam failOverlayRoot (opsional).")]
    [SerializeField] private Text failText;

    [Tooltip("Overlay berhasil (CanvasGroup), harus inactive.")]
    [SerializeField] private CanvasGroup successOverlayRoot;

    [Tooltip("Teks BERHASIL di dalam successOverlayRoot (opsional).")]
    [SerializeField] private Text successText;

    [Tooltip("Teks ringkasan skor di dalam successOverlayRoot (opsional).")]
    [SerializeField] private Text resultSummaryText;

    [Tooltip("Tombol COBA LAGI di dalam failOverlayRoot (opsional).")]
    [SerializeField] private Button retryButton;

    [Header("Tuning")]
    [SerializeField] private float flashAlpha = 0.35f;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float popupDuration = 0.6f;
    [SerializeField] private float popupDriftY = 40f;
    [SerializeField] private float failOverlayAlpha = 0.85f;
    [SerializeField] private float successOverlayAlpha = 0.75f;

    [Header("Suppress (dipakai alur scripted)")]
    [Tooltip("True = jangan tampilkan Fail/Success overlay generic saat minigame selesai. Dipakai KarindingTutorialController supaya UI akhir hanya diatur epilog.")]
    [SerializeField] private bool suppressResultOverlay = false;

    /// <summary>Set true saat tampilnya overlay hasil diatur oleh skrip (mis. rangkaian tutorial).</summary>
    public bool SuppressResultOverlay
    {
        get { return suppressResultOverlay; }
        set { suppressResultOverlay = value; }
    }

    private RhythmMinigameController _controller;
    private readonly Dictionary<LaneSide, RectTransform> _laneButtons = new Dictionary<LaneSide, RectTransform>();
    private RectTransform _hitLineRect;
    [Header("Perfect / Good Popups")]
    [SerializeField] private RectTransform perfectPopupTemplate;
    [SerializeField] private RectTransform goodPopupTemplate;
    [SerializeField] private float gradePopupDuration = 0.6f;

    private Camera _uiCamera;
    private Tween _flashTween;
    private Tween _shakeTween;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null) _controller = FindFirstObjectByType<RhythmMinigameController>();
        if (_controller == null)
        {
            Debug.LogError("[RhythmMissFeedback] RhythmMinigameController not found in scene.");
            return;
        }

        _controller.OnNoteJudged += HandleNoteJudged;
        _controller.OnNoteSpawned += HandleNoteSpawned;
        _controller.OnMinigameComplete += HandleMinigameComplete;

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        CacheLaneButtons();

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _uiCamera = canvas.worldCamera;

        // HitLine bisa jadi anak Canvas, bukan anak Track — cari dari root canvas.
        if (canvas != null)
            _hitLineRect = canvas.transform.Find("HitLine") as RectTransform;
        if (_hitLineRect == null)
        {
            var found = GameObject.Find("HitLine");
            if (found != null) _hitLineRect = found.transform as RectTransform;
        }
        if (_hitLineRect == null)
            Debug.LogWarning("[RhythmMissFeedback] HitLine tidak ditemukan — popup MISS pakai posisi tombol lane.");
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnNoteJudged -= HandleNoteJudged;
            _controller.OnNoteSpawned -= HandleNoteSpawned;
            _controller.OnMinigameComplete -= HandleMinigameComplete;
        }
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
        _flashTween?.Kill();
        _shakeTween?.Kill();
    }

    private void HandleNoteJudged(NoteData data, HitGrade grade)
    {
        switch (grade)
        {
            case HitGrade.Miss:
                PlayFlash();
                PlayShake();
                SpawnMissPopup(data.lane);
                break;
            case HitGrade.Perfect:
                SpawnGradePopup(data.lane, perfectPopupTemplate, "PERFECT!");
                break;
            case HitGrade.Good:
                SpawnGradePopup(data.lane, goodPopupTemplate, "GOOD!");
                break;
        }
    }

    private void HandleNoteSpawned(NoteData data)
    {
        // Minigame dimulai lagi — sembunyikan overlay hasil kalau masih tampil.
        if (failOverlayRoot != null && failOverlayRoot.gameObject.activeSelf)
            failOverlayRoot.gameObject.SetActive(false);
        if (successOverlayRoot != null && successOverlayRoot.gameObject.activeSelf)
            successOverlayRoot.gameObject.SetActive(false);
    }

    private void HandleMinigameComplete(RhythmMinigameResult result)
    {
        if (result == null) return;
        // Mulai lagi HANYA via klik Ulang Lagi (KarindingResultController.OnUlangClicked), bukan otomatis.
        // Kalau sedang alur scripted (tutorial), overlay hasil generic di-suppress total supaya tidak
        // muncul tombol ULANG/LANJUT bertumpuk di tengah tutorial.
        if (suppressResultOverlay)
        {
            Debug.Log("[RhythmMissFeedback] overlay hasil di-suppress (alur scripted, mis. tutorial)");
            return;
        }
        if (result.passed) ShowSuccessOverlay(result);
        else ShowFailOverlay();
    }

    private void PlayFlash()
    {
        if (flashImage == null) return;
        Color c = flashImage.color;
        c.a = flashAlpha;
        flashImage.color = c;
        _flashTween?.Kill();
        _flashTween = flashImage.DOFade(0f, flashDuration);
    }

    private void PlayShake()
    {
        if (trackRect == null) return;
        _shakeTween?.Kill();
        _shakeTween = trackRect.DOShakeAnchorPos(shakeDuration, shakeStrength, 40, 90f, true);
    }

    private void SpawnMissPopup(LaneSide lane)
    {
        if (popupTemplate == null) return;
        if (!_laneButtons.TryGetValue(lane, out var laneRect)) return;

        Vector3 laneWorld = laneRect.position;
        Vector3 targetWorld = new Vector3(laneWorld.x, _hitLineRect != null ? _hitLineRect.position.y : laneWorld.y, laneWorld.z);

        var parentRect = popupTemplate.parent as RectTransform;
        Vector2 localPos;
        if (parentRect != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCamera, targetWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, _uiCamera, out localPos);
        }
        else
        {
            localPos = targetWorld;
        }

        var popup = Instantiate(popupTemplate, popupTemplate.parent);
        popup.gameObject.SetActive(true);
        popup.anchoredPosition = localPos;
        popup.localScale = Vector3.one * 0.6f;

        var group = popup.GetComponent<CanvasGroup>();
        var text = popup.GetComponent<Text>();
        if (text != null) text.text = "MISS!";

        popup.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);
        popup.DOAnchorPosY(localPos.y + popupDriftY, popupDuration).SetEase(Ease.OutQuad);

        if (group != null)
        {
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(popupDuration * 0.4f);
            seq.Append(group.DOFade(0f, popupDuration * 0.6f));
            seq.OnComplete(() => Destroy(popup.gameObject));
        }
        else
        {
            Destroy(popup.gameObject, popupDuration);
        }
    }

    private void OnRetryClicked()
    {
        if (_controller == null) return;
        if (!_controller.RetryLast()) return;
        if (failOverlayRoot != null)
            failOverlayRoot.gameObject.SetActive(false);
    }

    private void ShowFailOverlay()
    {
        if (failOverlayRoot == null) return;
        failOverlayRoot.gameObject.SetActive(true);
        failOverlayRoot.alpha = 0f;
        failOverlayRoot.interactable = true;
        failOverlayRoot.blocksRaycasts = true;
        failOverlayRoot.DOFade(failOverlayAlpha, 0.4f).SetEase(Ease.OutQuad);

        if (failText != null)
        {
            failText.transform.localScale = Vector3.one * 0.5f;
            failText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
    }

    private void ShowSuccessOverlay(RhythmMinigameResult result)
    {
        if (successOverlayRoot == null) return;
        // Pastikan tidak auto-retry: hanya tampilkan overlay, mulai lagi hanya via KarindingResultController.OnUlangClicked (klik Ulang Lagi)
        successOverlayRoot.gameObject.SetActive(true);
        successOverlayRoot.alpha = 0f;
        successOverlayRoot.blocksRaycasts = true;
        successOverlayRoot.interactable = true;
        successOverlayRoot.DOFade(successOverlayAlpha, 0.4f).SetEase(Ease.OutQuad);

        if (resultSummaryText != null)
            resultSummaryText.text = "Skor: " + result.totalScore +
                "\nPerfect: " + result.perfectCount + "   Good: " + result.goodCount + "   Miss: " + result.missCount +
                "\nAccuracy: " + result.accuracy.ToString("F1") + "%   Rank: " + result.rank;

        if (successText != null)
        {
            successText.transform.localScale = Vector3.one * 0.5f;
            successText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// Tampilkan overlay berhasil + ringkasan skor dari luar (dipakai epilog scripted, mis. akhir tutorial).
    /// Tombol ULANG/LANJUT di overlay tetap hanya bisa diklik manual.
    /// </summary>
    public void ShowSuccessResult(RhythmMinigameResult result)
    {
        ShowSuccessOverlay(result);
    }

    private void SpawnGradePopup(LaneSide lane, RectTransform template, string label)
    {
        if (template == null) return;
        if (!_laneButtons.TryGetValue(lane, out var laneRect)) return;

        Vector3 laneWorld = laneRect.position;
        Vector3 targetWorld = new Vector3(laneWorld.x, _hitLineRect != null ? _hitLineRect.position.y : laneWorld.y, laneWorld.z);

        var parentRect = template.parent as RectTransform;
        Vector2 localPos;
        if (parentRect != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCamera, targetWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, _uiCamera, out localPos);
        }
        else
        {
            localPos = targetWorld;
        }

        var popup = Instantiate(template, template.parent);
        popup.gameObject.SetActive(true);
        popup.anchoredPosition = localPos;
        popup.localScale = Vector3.one * 0.6f;

        var group = popup.GetComponent<CanvasGroup>();
        var text = popup.GetComponent<Text>();
        if (text != null) text.text = label;

        popup.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);
        popup.DOAnchorPosY(localPos.y + popupDriftY, gradePopupDuration).SetEase(Ease.OutQuad);

        if (group != null)
        {
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(gradePopupDuration * 0.4f);
            seq.Append(group.DOFade(0f, gradePopupDuration * 0.6f));
            seq.OnComplete(() => Destroy(popup.gameObject));
        }
        else
        {
            Destroy(popup.gameObject, gradePopupDuration);
        }
        }

    private void CacheLaneButtons()
    {
        _laneButtons.Clear();
        var buttons = FindObjectsByType<LaneButton>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            var rect = btn.transform as RectTransform;
            if (rect != null && !_laneButtons.ContainsKey(btn.Lane))
                _laneButtons.Add(btn.Lane, rect);
        }
    }
}
