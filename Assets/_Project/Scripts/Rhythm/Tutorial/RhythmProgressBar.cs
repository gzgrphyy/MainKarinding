using UnityEngine;
using UnityEngine.UI;

public class RhythmProgressBar : MonoBehaviour
{
    [SerializeField] private Slider progressBar;

    private RhythmMinigameController _controller;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null)
            _controller = FindFirstObjectByType<RhythmMinigameController>();

        if (_controller != null)
        {
            _controller.OnMinigameComplete += HandleMinigameComplete;
        }

        ResetProgress();
    }

    private void OnDestroy()
    {
        if (_controller != null)
            _controller.OnMinigameComplete -= HandleMinigameComplete;
    }

    private void Update()
    {
        if (_controller == null || !_controller.IsPlaying) return;

        float totalDuration = _controller.TotalDuration;
        if (totalDuration <= 0f) return;

        float progress = Mathf.Clamp01(_controller.SongTime / totalDuration);

        if (progressBar != null)
            progressBar.value = progress;
    }

    private void HandleMinigameComplete(RhythmMinigameResult result)
    {
        if (progressBar != null)
            progressBar.value = 1f;
    }

    private void ResetProgress()
    {
        if (progressBar != null)
            progressBar.value = 0f;
    }
}