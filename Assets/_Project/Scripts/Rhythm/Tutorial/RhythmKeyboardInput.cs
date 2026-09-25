using UnityEngine;
using DG.Tweening;

public class RhythmKeyboardInput : MonoBehaviour
{
    private RhythmMinigameController _controller;
    private RectTransform _trackRect;

    private void Start()
    {
        _controller = RhythmMinigameController.Instance;
        if (_controller == null)
            _controller = FindFirstObjectByType<RhythmMinigameController>();

        var track = GameObject.Find("Track");
        if (track != null)
            _trackRect = track.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_controller == null) return;

        LaneSide? lane = null;
        if (Input.GetKeyDown(KeyCode.D)) lane = LaneSide.LeftOuter;
        else if (Input.GetKeyDown(KeyCode.F)) lane = LaneSide.LeftInner;
        else if (Input.GetKeyDown(KeyCode.J)) lane = LaneSide.RightInner;
        else if (Input.GetKeyDown(KeyCode.K)) lane = LaneSide.RightOuter;

        if (lane.HasValue)
        {
            // Tutorial hand placement intercept
            if (KarindingTutorialController.Instance != null && KarindingTutorialController.Instance.IsInHandPlacementPhase)
            {
                KarindingTutorialController.Instance.NotifyHandTap(lane.Value, TapResult.EmptyNoNote);
                return;
            }

            var result = _controller.HandleLaneButtonPressed(lane.Value);

            // Notify tutorial for rush detection
            if (KarindingTutorialController.Instance != null && KarindingTutorialController.Instance.IsTutorialActive)
            {
                KarindingTutorialController.Instance.NotifyAnyTap(lane.Value, result);
            }

            if (result == TapResult.HitPerfect || result == TapResult.HitGood) return;

            // Differentiated empty feedback: WrongLane subtle vs EmptyNoNote minimal
            if (result == TapResult.WrongLane)
            {
                if (_trackRect != null)
                    _trackRect.DOShakeAnchorPos(0.15f, 6f, 40, 90f, false);
            }
            else // EmptyNoNote
            {
                if (_trackRect != null)
                    _trackRect.DOShakeAnchorPos(0.1f, 4f, 30, 90f, false);
            }

            // Shake button with reason
            var buttons = FindObjectsByType<LaneButton>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn.Lane == lane.Value)
                {
                    btn.PlayFailedTapFeedback(result);
                    break;
                }
            }
        }
    }
}
