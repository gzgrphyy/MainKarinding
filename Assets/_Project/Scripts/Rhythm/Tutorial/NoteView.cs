using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class NoteView : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image image;
    [SerializeField] private Image iconImage;
    [Header("RhythmUI Icons")]
    [SerializeField] private Sprite iconBulatSprite;   // spiral
    [SerializeField] private Sprite iconDiamondSprite;
    [SerializeField] private Sprite iconWaveSprite;

    public NoteData Data { get; private set; }
    public bool Judged { get; private set; }
    public float TravelDuration { get; private set; }

    private RhythmMinigameController _owner;
    private Vector2 _spawnPos;
    private Vector2 _hitZonePos;
    private bool _travelling;
    private float _elapsed;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<UnityEngine.UI.Image>();
    }

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<UnityEngine.UI.Image>();
        if (iconImage == null)
        {
            var iconTr = transform.Find("Icon");
            if (iconTr != null) iconImage = iconTr.GetComponent<Image>();
        }
    }

    public void Setup(NoteData data, float travelDuration, RhythmMinigameController owner)
    {
        Data = data;
        TravelDuration = travelDuration;
        _owner = owner;
        Judged = false;
        _travelling = false;

        ApplyLaneVisual(data.lane);
    }

    public void MarkJudged()
    {
        Judged = true;
    }

    public void BeginTravel(Vector2 spawnPos, Vector2 hitZonePos)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        _spawnPos = spawnPos;
        _hitZonePos = hitZonePos;
        rectTransform.anchoredPosition = _spawnPos;
        _elapsed = 0f;
        _travelling = true;
    }

    private void Update()
    {
        if (!_travelling || Judged) return;

        _elapsed += Time.deltaTime;
        float t = (TravelDuration > 0f) ? Mathf.Clamp01(_elapsed / TravelDuration) : 1f;
        rectTransform.anchoredPosition = Vector2.Lerp(_spawnPos, _hitZonePos, t);

        if (t >= 1f)
        {
            _travelling = false;
            if (_owner != null) _owner.NotifyNoteReachedHitZone(this);
            else Destroy(gameObject);
        }
    }

    private void ApplyLaneVisual(LaneSide lane)
    {
        // Pill color per lane (match btn_* baked colors but as tint if no pill sprite)
        if (image != null)
        {
            switch (lane)
            {
                case LaneSide.LeftOuter: image.color = new Color(1f, 0.38f, 0.38f, 1f); break; // red pill
                case LaneSide.LeftInner: image.color = new Color(1f, 0.85f, 0.25f, 1f); break; // yellow pill
                case LaneSide.RightInner: image.color = new Color(0.18f, 0.78f, 0.38f, 1f); break; // green pill
                case LaneSide.RightOuter: image.color = new Color(0.22f, 0.65f, 0.92f, 1f); break; // blue pill
            }
        }
        // Icon overlay per lane: diamond for 2 left lanes, spiral for green, wave for blue
        if (iconImage != null)
        {
            Sprite icon = null;
            switch (lane)
            {
                case LaneSide.LeftOuter:
                case LaneSide.LeftInner:
                    icon = iconDiamondSprite; break;
                case LaneSide.RightInner:
                    icon = iconBulatSprite; break;
                case LaneSide.RightOuter:
                    icon = iconWaveSprite; break;
            }
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
                iconImage.color = new Color(1f, 1f, 1f, 0.95f);
            }
            else
            {
                iconImage.enabled = false;
            }
        }
    }

    // Legacy wrapper for prefab upgrade
    private void ApplyLaneColor(LaneSide lane) => ApplyLaneVisual(lane);

    public void PlayMissFeedback()
    {
        if (image == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.one;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(Vector3.one * 0.4f, 0.25f, 6, 0.8f));
        seq.Join(image.DOColor(Color.red, 0.1f));
        seq.OnComplete(() => Destroy(gameObject));
    }

    public void PlayPerfectFeedback()
    {
        if (image == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.one;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 8, 1f));
        seq.Join(image.DOColor(Color.white, 0.08f));
        seq.AppendInterval(0.1f);
        seq.Append(image.DOColor(new Color(1f, 0.85f, 0.2f, 1f), 0.15f));
        seq.OnComplete(() => Destroy(gameObject));
    }

    public void PlayGoodFeedback()
    {
        if (image == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.one;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 6, 0.8f));
        seq.Join(image.DOColor(new Color(0.2f, 1f, 0.4f, 1f), 0.1f));
        seq.AppendInterval(0.1f);
        seq.Append(image.DOFade(0f, 0.2f));
        seq.OnComplete(() => Destroy(gameObject));
    }
}
