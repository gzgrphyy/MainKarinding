using UnityEngine;
using TMPro;

public class RhythmLiveScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private RhythmMinigameController _controller;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null)
            _controller = FindFirstObjectByType<RhythmMinigameController>();

        if (_controller != null)
            _controller.OnNoteJudged += HandleNoteJudged;

        UpdateScore();
    }

    private void OnDestroy()
    {
        if (_controller != null)
            _controller.OnNoteJudged -= HandleNoteJudged;
    }

    private void HandleNoteJudged(NoteData data, HitGrade grade)
    {
        UpdateScore();
    }

    private void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = _controller.CurrentScore.ToString();
    }
}
