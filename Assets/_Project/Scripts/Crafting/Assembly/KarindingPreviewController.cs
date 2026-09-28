using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag untuk memutar preview 3D karinding (RenderTexture).
/// Pasang di RawImage preview. Sesuai arah: kiri-kanan = yaw, atas-bawah = pitch,
/// bebas 360° tanpa clamp (quaternion Euler akumulasi, tanpa roll).
/// Tengah diam: poros auto di-center ke bounds visual saat Awake.
/// Drag hanya mulai bila pointer pas di model 3D (ray dari previewCamera kena collider),
/// bukan area transparan RawImage.
/// </summary>
public class KarindingPreviewController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Target")]
    [Tooltip("Root model yang diputar. Kosong = cari KarindingPreviewRoot_Selesai.")]
    [SerializeField] private Transform modelPivot;
    [Tooltip("Kamera preview (RenderTexture). Kosong = cari KarindingPreviewCamera_Selesai.")]
    [SerializeField] private Camera previewCamera;
    [Tooltip("Layer yang boleh di-hit untuk drag model.")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("True = drag hanya mulai bila ray kena model. False = seluruh RawImage bisa diputar.")]
    [SerializeField] private bool requireModelHit = true;

    [Header("Tuning")]
    [SerializeField] private float dragSensitivity = 0.4f;
    [Tooltip("Kunci legacy: true = pitch di-clamp min/max. Biarkan FALSE untuk bebas 360°.")]
    [SerializeField] private bool clampPitch = false;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 35f;
    [SerializeField] private float inertiaDamping = 6f;
    [SerializeField] private float autoRotateSpeed = 20f;
    [SerializeField] private float idleDelay = 3f;
    [Tooltip("Fallback polling mouse bila event drag UI tidak sampai. Biarkan true.")]
    [SerializeField] private bool pollMouseFallback = true;

    private float _yaw;
    private float _pitch = 10f;
    private float _velYaw;
    private float _velPitch;
    private bool _eventDragging;
    private bool _pollDragging;
    private Vector2 _lastMousePos;
    private float _lastInteractTime = -10f;
    private RectTransform _ownRect;

    private bool IsDragging => _eventDragging || _pollDragging;

    private void Awake()
    {
        _ownRect = GetComponent<RectTransform>();
        if (modelPivot == null)
        {
            var found = GameObject.Find("KarindingPreviewRoot_Selesai");
            if (found == null) found = GameObject.Find("KarindingPreviewRoot");
            if (found != null) modelPivot = found.transform;
        }
        if (previewCamera == null)
        {
            var camGO = GameObject.Find("KarindingPreviewCamera_Selesai");
            if (camGO == null) camGO = GameObject.Find("KarindingPreviewCamera");
            if (camGO != null) previewCamera = camGO.GetComponent<Camera>();
        }
        EnsurePreviewCollider();
        EnsureCenteredPivot();
        FitCameraToBounds();
        if (modelPivot != null)
        {
            var e = modelPivot.rotation.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);
        }
    }

    private void Update()
    {
        if (modelPivot == null) return;

        PollMouseDrag();

        if (!IsDragging)
        {
            float dt = Time.unscaledDeltaTime;

            // Inertia: lanjutkan putaran terakhir lalu redam (sesuai arah geser)
            if (Mathf.Abs(_velYaw) > 0.01f || Mathf.Abs(_velPitch) > 0.01f)
            {
                _yaw += _velYaw * dt;
                _pitch += _velPitch * dt;
                SetRotation();
                float k = Mathf.Clamp01(inertiaDamping * dt);
                _velYaw = Mathf.Lerp(_velYaw, 0f, k);
                _velPitch = Mathf.Lerp(_velPitch, 0f, k);
            }

            // Auto-rotate saat idle: yaw saja, pitch diam (tengah tetap)
            if (Time.unscaledTime - _lastInteractTime > idleDelay && Mathf.Abs(autoRotateSpeed) > 0.01f)
            {
                _yaw += autoRotateSpeed * dt;
                SetRotation();
            }
        }
    }

    /// <summary>
    /// Fallback: pantau tombol mouse langsung. Aktif hanya bila press dimulai
    /// pas di model 3D dan event-system drag tidak sedang jalan.
    /// </summary>
    private void PollMouseDrag()
    {
        if (!pollMouseFallback || _ownRect == null) return;
        if (_eventDragging)
        {
            _pollDragging = false;
            return;
        }

        if (!_pollDragging)
        {
            if (Input.GetMouseButtonDown(0) && IsPointerOverModel(Input.mousePosition))
            {
                _pollDragging = true;
                _lastMousePos = Input.mousePosition;
                _velYaw = 0f;
                _velPitch = 0f;
                _lastInteractTime = Time.unscaledTime;
            }
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 mp = Input.mousePosition;
            Vector2 d = mp - _lastMousePos;
            _lastMousePos = mp;
            if (d.sqrMagnitude > 0f)
                ApplyDragDelta(d.x, d.y);
        }
        else
        {
            _pollDragging = false;
            _lastInteractTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// True bila screenPos ada di dalam rect DAN ray previewCamera mengenai modelPivot/child.
    /// Viewport dihitung dari posisi lokal di dalam RawImage sehingga akurat walau RawImage di-stretch.
    /// </summary>
    private bool IsPointerOverModel(Vector2 screenPos)
    {
        if (_ownRect == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(_ownRect, screenPos, null))
            return false;
        if (!requireModelHit) return true;
        if (previewCamera == null || modelPivot == null) return true; // fallback rect bila wiring belum lengkap

        Rect rect = _ownRect.rect;
        if (rect.width < 1f || rect.height < 1f) return false;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_ownRect, screenPos, null, out local))
            return false;

        float nx = (local.x - rect.xMin) / rect.width;
        float ny = (local.y - rect.yMin) / rect.height;
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f) return false;

        Ray ray = previewCamera.ViewportPointToRay(new Vector3(nx, ny, 0f));
        LayerMask mask = hitMask.value != 0 ? hitMask : ~0;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f, mask))
            return hit.transform == modelPivot || hit.transform.IsChildOf(modelPivot);
        return false;
    }

    private void ApplyDragDelta(float dx, float dy)
    {
        if (modelPivot == null) return;
        // Sesuai arah: dx -> yaw, dy -> pitch. Bebas 360°, tanpa clamp.
        _yaw += dx * dragSensitivity;
        _pitch += -dy * dragSensitivity;
        SetRotation();
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _velYaw = Mathf.Clamp(dx * dragSensitivity / dt * 0.05f, -360f, 360f);
        _velPitch = Mathf.Clamp(-dy * dragSensitivity / dt * 0.05f, -360f, 360f);
        _lastInteractTime = Time.unscaledTime;
    }

    /// <summary>
    /// Terapkan rotasi sesuai arah: yaw (Y) + pitch (X), roll selalu 0.
    /// _pitch unbounded = bebas 360°. clampPitch=true mengaktifkan clamp legacy bila dibutuhkan.
    /// </summary>
    private void SetRotation()
    {
        if (modelPivot == null) return;
        float p = clampPitch ? Mathf.Clamp(NormalizePitch(_pitch), minPitch, maxPitch) : _pitch;
        modelPivot.rotation = Quaternion.Euler(p, _yaw, 0f);
    }

    /// <summary>
    /// Tengah diam: geser anak mesh supaya center bounds visual tepat di posisi pivot.
    /// Light di bawah pivot tidak ikut digeser. Sekali saat Awake.
    /// </summary>
    private void EnsureCenteredPivot()
    {
        if (modelPivot == null) return;
        var renderers = modelPivot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        if (b.extents.sqrMagnitude < 0.000001f) return;
        Vector3 deltaWorld = b.center - modelPivot.position;
        if (deltaWorld.magnitude < 0.001f) return;
        Vector3 deltaLocal = modelPivot.InverseTransformVector(deltaWorld);
        foreach (Transform child in modelPivot)
        {
            if (child.GetComponent<Light>() != null) continue;
            if (child.GetComponentInChildren<MeshFilter>() == null &&
                child.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                continue;
            child.localPosition -= deltaLocal;
        }
    }

    /// <summary>
    /// Pastikan ada collider untuk hit-test ray. Cari MeshFilter pertama di bawah pivot,
    /// pasang MeshCollider bila belum ada; fallback BoxCollider bila mesh tidak ketemu.
    /// </summary>
    private void EnsurePreviewCollider()
    {
        if (modelPivot == null) return;
        if (modelPivot.GetComponentInChildren<Collider>() != null) return;
        var mf = modelPivot.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            return;
        }
        modelPivot.gameObject.AddComponent<BoxCollider>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Gestur yang dimulai di luar model diabaikan penuh
        if (requireModelHit && !IsPointerOverModel(eventData.position))
        {
            _eventDragging = false;
            return;
        }
        _eventDragging = true;
        _pollDragging = false;
        _velYaw = 0f;
        _velPitch = 0f;
        _lastInteractTime = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_eventDragging) return; // dimulai di luar model → abaikan
        _pollDragging = false;
        ApplyDragDelta(eventData.delta.x, eventData.delta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _eventDragging = false;
        _lastInteractTime = Time.unscaledTime;
    }

    public void ResetView()
    {
        _yaw = 30f;
        _pitch = 10f;
        _velYaw = 0f;
        _velPitch = 0f;
        _eventDragging = false;
        _pollDragging = false;
        _lastInteractTime = Time.unscaledTime;
        SetRotation();
        FitCameraToBounds();
    }

    /// <summary>
    /// Auto-fit kamera ke bounding sphere model supaya tidak kepotong di orientasi apapun.
    /// Jarak = r / sin(fov/2) * margin 1.5 — bola muat penuh + margin bener-bener clear.
    /// </summary>
    private void FitCameraToBounds()
    {
        if (previewCamera == null || modelPivot == null) return;
        if (previewCamera.fieldOfView < 44f) previewCamera.fieldOfView = 45f;
        Bounds b = GetCombinedBounds();
        if (b.extents.sqrMagnitude < 0.000001f) return;
        float r = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        if (r < 0.001f) return;
        float fovRad = previewCamera.fieldOfView * Mathf.Deg2Rad;
        if (fovRad < 0.001f) fovRad = 45f * Mathf.Deg2Rad;
        float dist = r / Mathf.Sin(fovRad * 0.5f) * 1.5f;
        Vector3 dir = previewCamera.transform.position - modelPivot.position;
        if (dir.sqrMagnitude < 0.000001f) dir = new Vector3(0f, 0.15f, -1f);
        dir.Normalize();
        previewCamera.transform.position = modelPivot.position + dir * dist;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = dist + r + 10f;
    }

    private Bounds GetCombinedBounds()
    {
        var renderers = modelPivot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(modelPivot.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static float NormalizePitch(float x)
    {
        if (x > 180f) x -= 360f;
        return x;
    }
}
