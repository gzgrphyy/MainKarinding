using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RhythmPauseController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private CanvasGroup pauseOverlay;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButtonPause;
    [SerializeField] private Text pauseTitle;

    private RhythmMinigameController _controller;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null) _controller = FindFirstObjectByType<RhythmMinigameController>();

        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (retryButtonPause != null) retryButtonPause.onClick.AddListener(OnRetryClicked);

        if (pauseOverlay != null)
        {
            pauseOverlay.alpha = 0f;
            pauseOverlay.gameObject.SetActive(false);
            pauseOverlay.blocksRaycasts = false;
            pauseOverlay.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (retryButtonPause != null) retryButtonPause.onClick.RemoveListener(OnRetryClicked);
    }

    private void Update()
    {
        if (_controller == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (_controller.IsPaused) OnResumeClicked();
            else if (_controller.IsPlaying && !_controller.IsCountingDown) OnPauseClicked();
        }
        // hide pause button when not playing
        if (pauseButton != null)
        {
            bool show = _controller.IsPlaying && !_controller.IsCountingDown && !_controller.IsPaused;
            // use canvasGroup or active; keep simple
            pauseButton.gameObject.SetActive(show);
        }
    }

    private void OnPauseClicked()
    {
        if (_controller == null) return;
        if (!_controller.IsPlaying || _controller.IsCountingDown || _controller.IsPaused) return;
        _controller.PauseMinigame();
        if (pauseOverlay != null)
        {
            pauseOverlay.gameObject.SetActive(true);
            pauseOverlay.alpha = 0f;
            pauseOverlay.blocksRaycasts = true;
            pauseOverlay.interactable = true;
            pauseOverlay.DOFade(1f, 0.25f).SetUpdate(true);
            if (pauseTitle != null)
            {
                pauseTitle.transform.localScale = Vector3.one * 0.8f;
                pauseTitle.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    private void OnResumeClicked()
    {
        if (_controller == null) return;
        if (!_controller.IsPaused) return;
        if (pauseOverlay != null)
        {
            pauseOverlay.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
            {
                pauseOverlay.gameObject.SetActive(false);
                pauseOverlay.blocksRaycasts = false;
                pauseOverlay.interactable = false;
            });
        }
        _controller.ResumeMinigame();
    }

    private void OnRetryClicked()
    {
        if (_controller == null) return;
        if (pauseOverlay != null)
        {
            pauseOverlay.gameObject.SetActive(false);
            pauseOverlay.alpha = 0f;
        }
        _controller.ResumeMinigame();
        _controller.RetryLast();
        // hide overlay after retry will be handled by HandleNoteSpawned hiding overlays
        if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
    }
}
