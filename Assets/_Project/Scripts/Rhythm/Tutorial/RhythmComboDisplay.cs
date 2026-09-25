using UnityEngine;
using UnityEngine.UI;

public class RhythmComboDisplay : MonoBehaviour
{
    [SerializeField] private Text comboText;

    private RhythmMinigameController _controller;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null)
            _controller = FindFirstObjectByType<RhythmMinigameController>();

        if (_controller != null)
            _controller.OnNoteJudged += HandleNoteJudged;

        UpdateCombo();
    }

    private void OnDestroy()
    {
        if (_controller != null)
            _controller.OnNoteJudged -= HandleNoteJudged;
    }

    private void HandleNoteJudged(NoteData data, HitGrade grade)
    {
        UpdateCombo();
    }

    private void UpdateCombo()
    {
        if (comboText == null) return;

        int combo = _controller.CurrentCombo;
        if (combo > 0)
        {
            comboText.text = "x" + combo;
            comboText.gameObject.SetActive(true);
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }
}
