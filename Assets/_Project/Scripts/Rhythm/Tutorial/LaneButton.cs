using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class LaneButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private LaneSide lane;
    [SerializeField] private Image buttonImage;

    public LaneSide Lane => lane;

    private void Awake()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
    }

    private bool _isHighlighted;
    private Tween _highlightTween;
    private Color _origColor = Color.white;

    public void SetHighlight(bool on)
    {
        _isHighlighted = on;
        _highlightTween?.Kill();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (on)
        {
            if (buttonImage != null) _origColor = buttonImage.color;
            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                _highlightTween = rect.DOScale(1.08f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
            if (buttonImage != null) buttonImage.DOColor(new Color(1f, 0.95f, 0.45f, 1f), 0.2f);
        }
        else
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null) rect.localScale = Vector3.one;
            if (buttonImage != null)
            {
                buttonImage.DOKill();
                buttonImage.color = Color.white;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[LaneButton] tapped lane=" + lane);
        // Tutorial hand placement intercept - bypass rhythm logic
        if (KarindingTutorialController.Instance != null && KarindingTutorialController.Instance.IsInHandPlacementPhase)
        {
            KarindingTutorialController.Instance.NotifyHandTap(lane, TapResult.EmptyNoNote);
            return;
        }

        if (RhythmMinigameController.Instance == null) return;

        var result = RhythmMinigameController.Instance.HandleLaneButtonPressed(lane);

        // Notify tutorial for rush detection in Fase2
        if (KarindingTutorialController.Instance != null && KarindingTutorialController.Instance.IsTutorialActive)
        {
            KarindingTutorialController.Instance.NotifyAnyTap(lane, result);
        }

        if (result == TapResult.HitPerfect || result == TapResult.HitGood) return;

        if (result == TapResult.WrongLane) PlayWrongLaneFeedback();
        else PlayFailedTapFeedback(result);
    }

    public void PlayFailedTapFeedback(TapResult reason = TapResult.EmptyNoNote)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;
        if (reason == TapResult.WrongLane)
        {
            rect.DOShakeAnchorPos(0.18f, 7f, 40, 90f, false);
            if (buttonImage != null) buttonImage.DOColor(new Color(1f, 0.7f, 0.2f, 1f), 0.08f).OnComplete(() => buttonImage.DOColor(Color.white, 0.15f));
        }
        else
        {
            rect.DOShakeAnchorPos(0.12f, 6f, 30, 90f, false);
            if (buttonImage != null) buttonImage.DOColor(new Color(0.85f, 0.85f, 0.85f, 1f), 0.06f).OnComplete(() => buttonImage.DOColor(Color.white, 0.12f));
        }
    }

    public void PlayWrongLaneFeedback()
    {
        PlayFailedTapFeedback(TapResult.WrongLane);
    }
}
