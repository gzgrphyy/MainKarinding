using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Tap cepat selama fase Tapping (50-100%). Grace window + decay ditangani di controller.
/// Silent ignore jika bukan fase Tapping. Rate-limit untuk anti-macro.
/// </summary>
public class CraftingTapInputView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    [Header("Feedback (optional)")]
    [SerializeField] private string sfxTapEvent;

    private CraftingAssemblyController controller;
    private float lastTapTime = -999f;
    private RectTransform areaRect;

    private void Awake()
    {
        controller = GetComponentInParent<CraftingAssemblyController>();
        if (controller == null) controller = FindFirstObjectByType<CraftingAssemblyController>();
        areaRect = GetComponent<RectTransform>();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private bool IsInTapping()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<CraftingAssemblyController>();
            if (controller == null) controller = FindFirstObjectByType<CraftingAssemblyController>();
            if (controller == null) return false;
        }
        return controller.CurrentPhase == AssemblyPhase.Tapping && !controller.IsCompleted;
    }

    private void Update()
    {
        // Hybrid mouse fallback: InputSystem IPointer kadang tidak fire untuk mouse di Editor
        if (!IsInTapping()) return;
        if (areaRect == null) areaRect = GetComponent<RectTransform>();
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (!RectTransformUtility.RectangleContainsScreenPoint(areaRect, mousePos, null)) return;
            HandleTap(mousePos, "Mouse");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleTap(eventData.position, $"Pointer id={eventData.pointerId}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
    }

    private void HandleTap(Vector2 screenPos = default, string source = "Pointer")
    {
        if (controller == null)
        {
            controller = GetComponentInParent<CraftingAssemblyController>();
            if (controller == null) controller = FindFirstObjectByType<CraftingAssemblyController>();
            if (controller == null)
            {
                Debug.LogWarning("[TapInput] No controller");
                return;
            }
        }

        if (controller.CurrentPhase != AssemblyPhase.Tapping)
        {
            Debug.Log($"[TapInput] Ignored not Tapping phase={controller.CurrentPhase} source={source}");
            return;
        }
        if (controller.IsCompleted)
        {
            Debug.Log($"[TapInput] Ignored completed source={source}");
            return;
        }

        float minInterval = controller.Config != null ? controller.Config.tapMinInterval : 0.05f;
        float delta = Time.time - lastTapTime;
        if (delta < minInterval)
        {
            Debug.Log($"[TapInput] Ignored interval {delta:F2} < {minInterval} source={source}");
            return;
        }

        lastTapTime = Time.time;
        float before = controller.Progress;
        controller.AddTapProgress();
        Debug.Log($"[TapInput] {source} valid screen={screenPos} prog {before:F1} -> {controller.Progress:F1}");
        PlayFeedback();
    }

    // Backward compat for previous calls without pos
    private void HandleTap()
    {
        HandleTap(Input.mousePosition, "Pointer");
    }

    private void PlayFeedback()
    {
        try
        {
            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.DOKill();
                rt.DOShakeAnchorPos(0.12f, new Vector2(3f, 3.5f), 10, 90f, false, true);
            }
        }
        catch { }

        if (!string.IsNullOrEmpty(sfxTapEvent))
        {
            // FMODUnity.RuntimeManager.PlayOneShot(sfxTapEvent, transform.position);
        }
    }
}
