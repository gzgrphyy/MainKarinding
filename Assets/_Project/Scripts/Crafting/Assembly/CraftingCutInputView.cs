using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Deteksi gesture "cut" lurus selama fase Cutting (0-50%).
/// Algoritma: straightness ratio = totalDisplacement / totalPathLength.
/// Hardware jari tidak pernah 100% lurus, jadi toleransi via cutMinStraightness (default 0.85).
/// </summary>
public class CraftingCutInputView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform areaRect;
    [Tooltip("Area preview 3D (drag putar model). Klik di dalamnya tidak dihitung sebagai gesture cut.")]
    [SerializeField] private RectTransform previewGuardRect;

    [Header("Feedback (optional)")]
    [SerializeField] private string sfxCutEvent;

    [Header("Visual Line (mouse/touch guide)")]
    [SerializeField] private LineRenderer guideLine;
    [SerializeField] private LineRenderer trailLine;
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private float fadeDuration = 0.25f;

    private readonly List<Vector2> pathPoints = new List<Vector2>();
    private Vector2 startPos;
    private bool isTracking;
    private int activePointerId = -1;
    private Camera uiCam;
    private bool isMouseTracking;

    private CraftingAssemblyController controller;

    private void Awake()
    {
        uiCam = Camera.main;
        if (areaRect == null) areaRect = GetComponent<RectTransform>();
        controller = GetComponentInParent<CraftingAssemblyController>();
        if (controller == null) controller = FindFirstObjectByType<CraftingAssemblyController>();

        if (guideLine == null) guideLine = GetComponentInChildren<LineRenderer>(true);
        if (previewGuardRect == null)
        {
            var guardGO = GameObject.Find("Canvas/AssemblyRoot/KarindingPreviewMini");
            if (guardGO == null) guardGO = GameObject.Find("KarindingPreviewMini");
            if (guardGO != null) previewGuardRect = guardGO.GetComponent<RectTransform>();
        }
        // trailLine optional second renderer
        SetupLines();
    }

    private void SetupLines()
    {
        if (guideLine != null)
        {
            guideLine.useWorldSpace = true;
            guideLine.positionCount = 0;
            guideLine.startWidth = lineWidth;
            guideLine.endWidth = lineWidth;
            guideLine.numCapVertices = 6;
            guideLine.numCornerVertices = 4;
            guideLine.sortingOrder = 10;
            guideLine.enabled = false;
            if (guideLine.material == null || guideLine.material.shader.name == "Hidden/InternalErrorShader")
            {
                guideLine.material = new Material(Shader.Find("Sprites/Default"));
            }
        }
        if (trailLine != null)
        {
            trailLine.useWorldSpace = true;
            trailLine.positionCount = 0;
            trailLine.startWidth = lineWidth * 0.6f;
            trailLine.endWidth = lineWidth * 0.6f;
            trailLine.numCapVertices = 4;
            trailLine.sortingOrder = 9;
            trailLine.enabled = false;
            if (trailLine.material == null || trailLine.material.shader.name == "Hidden/InternalErrorShader")
            {
                trailLine.material = new Material(Shader.Find("Sprites/Default"));
            }
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (!active)
        {
            isTracking = false;
            isMouseTracking = false;
            activePointerId = -1;
            pathPoints.Clear();
            ClearLines();
        }
    }

    private void Update()
    {
        // Hybrid mouse fallback: InputSystem mouse may not fire IPointer for some configs
        // Poll legacy Input for mouse left button when in Cutting phase
        if (!IsInCuttingPhase()) return;
        if (areaRect == null) return;
        if (uiCam == null) uiCam = Camera.main;

        Vector2 mousePos = Input.mousePosition;

        // Mouse Down
        if (Input.GetMouseButtonDown(0))
        {
            if (isTracking) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(areaRect, mousePos, null)) return;
            if (IsOverPreviewGuard(mousePos)) return;
            isTracking = true;
            isMouseTracking = true;
            activePointerId = -999;
            startPos = mousePos;
            pathPoints.Clear();
            pathPoints.Add(startPos);
            Debug.Log($"[CutInput] MouseDown pos={startPos} phase={controller.CurrentPhase}");
            ShowLines(startPos);
        }
        else if (Input.GetMouseButton(0) && isTracking && isMouseTracking)
        {
            // Add point if moved enough to avoid duplicate
            if (pathPoints.Count == 0 || Vector2.Distance(pathPoints[pathPoints.Count - 1], mousePos) > 2f)
            {
                pathPoints.Add(mousePos);
                UpdateLines(mousePos);
            }
        }
        else if (Input.GetMouseButtonUp(0) && isTracking && isMouseTracking)
        {
            Vector2 endPos = mousePos;
            if (pathPoints.Count == 0 || pathPoints[pathPoints.Count - 1] != endPos)
                pathPoints.Add(endPos);

            isTracking = false;
            isMouseTracking = false;
            activePointerId = -1;

            if (!IsInCuttingPhase())
            {
                ClearLines();
                pathPoints.Clear();
                return;
            }

            ValidateAndSubmitCut(startPos, endPos);
            // Fade lines after short delay
            DOVirtual.DelayedCall(fadeDuration, () => ClearLines());
            pathPoints.Clear();
        }
        else if (isTracking && isMouseTracking)
        {
            // Update guide color realtime while dragging
            UpdateLines(mousePos);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // If mouse tracking already via Update, ignore pointer duplicate
        if (isMouseTracking) return;

        if (isTracking)
        {
            Debug.Log($"[CutInput] Down ignored isTracking true id={eventData.pointerId}");
            return;
        }

        if (!IsInCuttingPhase())
        {
            string phaseStr = controller != null ? controller.CurrentPhase.ToString() : "null ctrl";
            Debug.Log($"[CutInput] Down ignored not Cutting phase={phaseStr}");
            return;
        }

        if (IsOverPreviewGuard(eventData.position)) return;

        isTracking = true;
        isMouseTracking = false;
        activePointerId = eventData.pointerId;
        startPos = eventData.position;
        pathPoints.Clear();
        pathPoints.Add(startPos);

        Debug.Log($"[CutInput] Down id={activePointerId} pos={startPos} phase={controller.CurrentPhase}");
        ShowLines(startPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isMouseTracking) return;
        if (!isTracking) return;
        if (eventData.pointerId != activePointerId) return;
        if (!IsInCuttingPhase()) return;

        pathPoints.Add(eventData.position);
        UpdateLines(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isMouseTracking) return;
        if (!isTracking) return;
        if (eventData.pointerId != activePointerId) return;

        isTracking = false;
        activePointerId = -1;

        if (!IsInCuttingPhase())
        {
            ClearLines();
            pathPoints.Clear();
            return;
        }

        Vector2 endPos = eventData.position;
        if (pathPoints.Count == 0 || pathPoints[pathPoints.Count - 1] != endPos)
            pathPoints.Add(endPos);

        ValidateAndSubmitCut(startPos, endPos);
        DOVirtual.DelayedCall(fadeDuration, () => ClearLines());
        pathPoints.Clear();
    }

    private void ValidateAndSubmitCut(Vector2 start, Vector2 end)
    {
        var cfg = GetConfig();
        if (controller == null)
        {
            Debug.LogWarning("[CutInput] Validate no controller");
            return;
        }

        float totalDisplacement = Vector2.Distance(start, end);

        float totalPathLength = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
            totalPathLength += Vector2.Distance(pathPoints[i - 1], pathPoints[i]);

        if (totalPathLength < 0.01f)
        {
            Debug.Log($"[CutInput] Ignored pathLength too small {totalPathLength}");
            return;
        }

        float straightnessRatio = totalDisplacement / totalPathLength;

        float minDistance = cfg != null ? cfg.cutMinDistance : 80f;
        if (cfg != null && cfg.useNormalizedCutDistance && areaRect != null)
        {
            float width = areaRect.rect.width;
            if (width < 1f) width = Screen.width;
            minDistance = width * cfg.cutMinDistanceNormalized;
        }
        float minStraightness = cfg != null ? cfg.cutMinStraightness : 0.8f;

        // Filter arah: hanya atas -> bawah (golok jatuh)
        bool isTopToBottom = true;
        Vector2 dir = (end - start);
        float dirMag = dir.magnitude;
        if (dirMag > 0.01f) dir /= dirMag;
        bool requireTopBottom = cfg == null || cfg.cutRequireTopToBottom;
        if (requireTopBottom)
        {
            float minDown = cfg != null ? cfg.cutMinDownDot : -0.35f;
            float minVertRatio = cfg != null ? cfg.cutMinVerticalRatio : 1.25f;
            // Harus kebawah (dir.y negatif di screen space)
            if (dir.y > minDown) isTopToBottom = false;
            // Harus dominan vertikal |dy| >= |dx| * ratio
            if (Mathf.Abs(dir.y) < Mathf.Abs(dir.x) * minVertRatio) isTopToBottom = false;
        }

        // Debug selalu log agar bisa tuning mouse berkali-kali
        Debug.Log($"[CutInput] Up start={start} end={end} dir={dir} disp={totalDisplacement:F1} path={totalPathLength:F1} ratio={straightnessRatio:F2} top2bottom={isTopToBottom} minDist={minDistance:F1} minStraight={minStraightness:F2} valid={(totalDisplacement>=minDistance && straightnessRatio>=minStraightness && isTopToBottom)} points={pathPoints.Count}");

        if (totalDisplacement < minDistance) return;
        if (straightnessRatio < minStraightness)
        {
            if (controller != null) controller.NotifyWrongInput();
            return;
        }
        if (!isTopToBottom)
        {
            if (controller != null) controller.NotifyWrongInput();
            return;
        }

        controller.AddCutProgress(straightnessRatio);
        PlayFeedback();
    }

    private bool IsOverPreviewGuard(Vector2 screenPos)
    {
        if (previewGuardRect == null) return false;
        if (!previewGuardRect.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(previewGuardRect, screenPos, null);
    }

    private bool IsInCuttingPhase()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<CraftingAssemblyController>();
            if (controller == null) controller = FindFirstObjectByType<CraftingAssemblyController>();
            if (controller == null) return false;
        }
        return controller.CurrentPhase == AssemblyPhase.Cutting && !controller.IsCompleted;
    }

    private CraftingAssemblyConfig GetConfig()
    {
        if (controller != null && controller.Config != null) return controller.Config;
        return null;
    }

    private void PlayFeedback()
    {
        if (areaRect != null)
        {
            try
            {
                DOTween.Kill(areaRect);
                areaRect.DOPunchScale(Vector3.one * 0.03f, 0.15f, 4, 0.5f);
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(sfxCutEvent))
        {
            // FMODUnity.RuntimeManager.PlayOneShot(sfxCutEvent, transform.position);
        }
    }

    private void ShowLines(Vector2 start)
    {
        if (uiCam == null) uiCam = Camera.main;
        if (guideLine != null)
        {
            guideLine.enabled = true;
            guideLine.positionCount = 2;
            guideLine.startColor = invalidColor;
            guideLine.endColor = invalidColor;
            SetLinePositions(guideLine, start, start);
        }
        if (trailLine != null)
        {
            trailLine.enabled = true;
            trailLine.positionCount = 1;
            trailLine.startColor = trailColor;
            trailLine.endColor = trailColor;
            SetLinePositions(trailLine, start, start);
        }
    }

    private void UpdateLines(Vector2 currentPos)
    {
        if (uiCam == null) uiCam = Camera.main;
        var cfg = GetConfig();

        // Guide line start->current
        if (guideLine != null && guideLine.enabled)
        {
            SetLinePositions(guideLine, startPos, currentPos);

            // Color by validity preview
            float disp = Vector2.Distance(startPos, currentPos);
            float pathLen = 0f;
            for (int i = 1; i < pathPoints.Count; i++) pathLen += Vector2.Distance(pathPoints[i - 1], pathPoints[i]);
            // Include segment to current if not yet added
            if (pathPoints.Count > 0) pathLen += Vector2.Distance(pathPoints[pathPoints.Count - 1], currentPos);

            float minDist = cfg != null ? cfg.cutMinDistance : 60f;
            if (cfg != null && cfg.useNormalizedCutDistance && areaRect != null)
            {
                float w = areaRect.rect.width;
                if (w < 1f) w = Screen.width;
                minDist = w * cfg.cutMinDistanceNormalized;
            }
            float minStraight = cfg != null ? cfg.cutMinStraightness : 0.75f;
            float ratio = pathLen > 0.01f ? disp / pathLen : 1f;
            bool willBeTop2Bottom = true;
            if (cfg == null || cfg.cutRequireTopToBottom)
            {
                Vector2 pDir = currentPos - startPos;
                float pm = pDir.magnitude;
                if (pm > 0.01f) pDir /= pm;
                float minDown = cfg != null ? cfg.cutMinDownDot : -0.35f;
                float minVert = cfg != null ? cfg.cutMinVerticalRatio : 1.25f;
                if (pDir.y > minDown) willBeTop2Bottom = false;
                if (Mathf.Abs(pDir.y) < Mathf.Abs(pDir.x) * minVert) willBeTop2Bottom = false;
            }
            bool willBeValid = disp >= minDist && ratio >= minStraight && willBeTop2Bottom;
            Color c = willBeValid ? validColor : invalidColor;
            guideLine.startColor = c;
            guideLine.endColor = c;
        }

        // Trail polyline
        if (trailLine != null && trailLine.enabled && pathPoints.Count > 1)
        {
            trailLine.positionCount = pathPoints.Count;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                Vector3 wp = ScreenToWorld(pathPoints[i]);
                trailLine.SetPosition(i, wp);
            }
            trailLine.startColor = trailColor;
            trailLine.endColor = trailColor;
        }
    }

    private void ClearLines()
    {
        if (guideLine != null)
        {
            guideLine.enabled = false;
            guideLine.positionCount = 0;
        }
        if (trailLine != null)
        {
            trailLine.enabled = false;
            trailLine.positionCount = 0;
        }
        // Delayed clear already handled via DOVirtual in caller; keep immediate hide for now
    }

    private void SetLinePositions(LineRenderer lr, Vector2 a, Vector2 b)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, ScreenToWorld(a));
        lr.SetPosition(1, ScreenToWorld(b));
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (uiCam == null) uiCam = Camera.main;
        // For Screen Space Overlay, camera may be null, use plane at z=0
        if (uiCam == null)
            return new Vector3(screenPos.x, screenPos.y, 0f);

        float z = Mathf.Abs(uiCam.transform.position.z) + 10f;
        // If ortho, z controls distance, but we want plane near 0
        Vector3 wp = uiCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        wp.z = 0f;
        return wp;
    }
}
