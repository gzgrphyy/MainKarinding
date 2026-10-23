using UnityEngine;
using TMPro;
using DG.Tweening;

public class RhythmCountdownDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private CanvasGroup countdownGroup;
    [SerializeField] private float punchDuration = 0.35f;

    private RhythmMinigameController _controller;
    private Tween _tween;

    private void Awake()
    {
        if (countdownText == null) countdownText = GetComponent<TextMeshProUGUI>();
        if (countdownGroup == null) countdownGroup = GetComponent<CanvasGroup>();
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (countdownGroup != null) countdownGroup.alpha = 0f;
    }

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null) _controller = FindFirstObjectByType<RhythmMinigameController>();
        if (_controller != null) _controller.OnCountdownTick += HandleCountdownTick;
        // also hide on complete / spawned
        if (_controller != null) _controller.OnMinigameComplete += HandleComplete;
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnCountdownTick -= HandleCountdownTick;
            _controller.OnMinigameComplete -= HandleComplete;
        }
        _tween?.Kill();
    }

    private void HandleComplete(RhythmMinigameResult _)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (countdownGroup != null) countdownGroup.alpha = 0f;
    }

    private void HandleCountdownTick(int value)
    {
        if (countdownText == null) return;
        _tween?.Kill();
        countdownText.gameObject.SetActive(true);
        if (countdownGroup != null) countdownGroup.alpha = 1f;

        string label = value > 0 ? value.ToString() : "GO!";
        Color col = value > 0 ? new Color(1f, 0.95f, 0.4f, 1f) : new Color(0.4f, 1f, 0.5f, 1f);
        countdownText.text = label;
        countdownText.color = col;
        countdownText.transform.localScale = Vector3.one * 0.5f;
        countdownText.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        if (value == 0)
        {
            _tween = countdownText.transform.DOScale(Vector3.one * 1.2f, punchDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (countdownGroup != null) countdownGroup.DOFade(0f, 0.25f).OnComplete(() => countdownText.gameObject.SetActive(false));
                else countdownText.gameObject.SetActive(false);
            });
        }
        else
        {
            countdownText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.8f);
            _tween = DOVirtual.DelayedCall(0.7f, () => { /* keep visible until next tick */ });
        }
    }
}
