using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controller untuk overlay berhasil (SuccessOverlay) — SATU-SATUNYA UI akhir.
/// 2 tombol bersampingan: Ulang (kiri, merah) dan Lanjut (kanan, hijau).
/// - Ulang = ulangi permainan/tutorial secara MANUAL (klik). Di scene tutorial: restart tutorial dari Fase 1.
///   Kalau tidak ada KarindingTutorialController: retry chart terakhir via RhythmMinigameController.RetryLast().
/// - Lanjut = load nextSceneName bila diisi. Kosong = placeholder (hanya log), dipakai untuk "lanjut scene".
/// Anti auto-ngulang: tombol baru bisa diklik setelah armDelay (tap yang sedang berjalan saat overlay muncul
/// tidak ikut menekan tombol) + tidak ada EventSystem auto-select sehingga Submit tidak memicu klik.
/// Mendukung struktur lama (Btn_Lanjut langsung di root) dan struktur baru (BtnContainer/Btn_UlangLagi + Btn_Lanjut).
/// </summary>
public class KarindingResultController : MonoBehaviour
{
    [Header("Placeholder tujuan")]
    [Tooltip("Nama scene tujuan untuk Lanjut. Kosong = placeholder, hanya log (biar bisa diisi scene lanjutan nanti).")]
    [SerializeField] private string nextSceneName = "";

    /// <summary>Di-set StandardRhythmLauncher saat runtime (scene file masih kosong).</summary>
    public void SetNextScene(string sceneName) { nextSceneName = sceneName; }

    [Header("Wiring - SuccessOverlay 2 Buttons")]
    [Tooltip("Tombol Ulang (kiri, merah). Kosong = cari BtnContainer/Btn_UlangLagi atau Btn_Ulang atau Btn_Retry.")]
    [SerializeField] private Button ulangButton;
    [Tooltip("Tombol Lanjut (kanan, hijau). Kosong = cari BtnContainer/Btn_Lanjut atau Btn_Lanjut.")]
    [SerializeField] private Button lanjutButton;

    [Tooltip("Overlay berhasil root (CanvasGroup). Kosong = ambil dari parent GetComponent<CanvasGroup>().")]
    [SerializeField] private CanvasGroup successOverlayRoot;

    [Header("Perilaku")]
    [Tooltip("True = ULANG mengulang tutorial dari awal (bila KarindingTutorialController ada di scene). False = ulangi chart permainan terakhir.")]
    [SerializeField] private bool ulangReplaysTutorial = true;
    [Tooltip("Jeda (detik realtime) sebelum tombol bisa diklik — mencegah tap yang sedang berjalan ikut menekan ULANG/LANJUT.")]
    [SerializeField] private float armDelay = 0.6f;
    [Tooltip("Jeda minimum antar-aksi (anti dobel klik).")]
    [SerializeField] private float actionCooldown = 0.6f;

    private RhythmMinigameController _rhythm;
    private KarindingTutorialController _tutorial;
    private bool _wired;
    private bool _armed;
    private float _lastActionTime = -999f;
    private Coroutine _armRoutine;

    /// <summary>True = tombol ULANG/LANJUT sudah boleh diklik (sudah lewat armDelay).</summary>
    public bool IsArmed { get { return _armed; } }

    private void Awake()
    {
        if (successOverlayRoot == null) successOverlayRoot = GetComponent<CanvasGroup>();
        _rhythm = RhythmMinigameController.Instance;
        if (_rhythm == null) _rhythm = FindFirstObjectByType<RhythmMinigameController>();
    }

    private void OnEnable()
    {
        ResolveButtons();
        Wire(true);
        // Anti auto-select: jangan biarkan EventSystem auto-select Lanjut sehingga Submit terpicu otomatis
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
        Debug.Log("[KarindingResult] SuccessOverlay show - menunggu klik Ulang/Lanjut manual (tidak auto)");
        if (successOverlayRoot != null)
        {
            successOverlayRoot.blocksRaycasts = true;
            successOverlayRoot.interactable = true;
        }
        // Tombol di-arm setelah armDelay: tap yang sedang berjalan saat ronde baru selesai
        // tidak akan ikut menekan ULANG -> tidak ada "otomatis ngulang".
        ArmSoon();
    }

    private void OnDisable()
    {
        if (_armRoutine != null) { StopCoroutine(_armRoutine); _armRoutine = null; }
        _armed = false;
        SetButtonsInteractable(false);
        Wire(false);
    }

    private void ArmSoon()
    {
        _armed = false;
        SetButtonsInteractable(false);
        if (_armRoutine != null) { StopCoroutine(_armRoutine); _armRoutine = null; }
        if (!isActiveAndEnabled)
        {
            _armed = true;
            SetButtonsInteractable(true);
            return;
        }
        _armRoutine = StartCoroutine(ArmRoutine());
    }

    private IEnumerator ArmRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, armDelay));
        _armRoutine = null;
        if (!isActiveAndEnabled) yield break;
        _armed = true;
        SetButtonsInteractable(true);
        // Pastikan tidak ada seleksi otomatis supaya Submit (Enter/Space/gamepad) tidak memicu klik.
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
        Debug.Log("[KarindingResult] Tombol ULANG/LANJUT aktif (armed) - hanya klik manual");
    }

    private void SetButtonsInteractable(bool value)
    {
        if (ulangButton != null) ulangButton.interactable = value;
        if (lanjutButton != null) lanjutButton.interactable = value;
    }

    private void ResolveButtons()
    {
        // Resolve Ulang
        if (ulangButton == null)
        {
            Transform t = null;
            // Preferred new structure
            t = transform.Find("BtnContainer/Btn_UlangLagi");
            if (t == null) t = transform.Find("BtnContainer/Btn_Ulang");
            if (t == null) t = transform.Find("Btn_UlangLagi");
            if (t == null) t = transform.Find("Btn_Ulang");
            if (t == null) t = transform.Find("Btn_Retry");
            if (t != null) ulangButton = t.GetComponent<Button>();
            // Fallback: search any Button with name containing Ulang/Rety
            if (ulangButton == null)
            {
                var all = GetComponentsInChildren<Button>(true);
                foreach (var b in all)
                {
                    string n = b.gameObject.name.ToLowerInvariant();
                    if (n.Contains("ulang") || n.Contains("retry") || n.Contains("coba"))
                    {
                        // jangan ambil Lanjut
                        if (!n.Contains("lanjut")) { ulangButton = b; break; }
                    }
                }
            }
        }
        // Resolve Lanjut
        if (lanjutButton == null)
        {
            Transform t = null;
            t = transform.Find("BtnContainer/Btn_Lanjut");
            if (t == null) t = transform.Find("Btn_Lanjut");
            if (t == null) t = transform.Find("BtnLanjut");
            if (t != null) lanjutButton = t.GetComponent<Button>();
            if (lanjutButton == null)
            {
                var all = GetComponentsInChildren<Button>(true);
                foreach (var b in all)
                {
                    string n = b.gameObject.name.ToLowerInvariant();
                    if (n.Contains("lanjut"))
                    {
                        lanjutButton = b; break;
                    }
                }
            }
        }
        if (successOverlayRoot == null) successOverlayRoot = GetComponent<CanvasGroup>();
        if (_rhythm == null)
        {
            _rhythm = RhythmMinigameController.Instance;
            if (_rhythm == null) _rhythm = FindFirstObjectByType<RhythmMinigameController>();
        }
    }

    private void Wire(bool add)
    {
        if (_wired == add) return;
        if (add)
        {
            if (ulangButton != null) ulangButton.onClick.AddListener(OnUlangClicked);
            if (lanjutButton != null) lanjutButton.onClick.AddListener(OnLanjutClicked);
            _wired = true;
        }
        else
        {
            if (ulangButton != null) ulangButton.onClick.RemoveListener(OnUlangClicked);
            if (lanjutButton != null) lanjutButton.onClick.RemoveListener(OnLanjutClicked);
            _wired = false;
        }
    }

    // Dipanggil via Inspector OnClick juga bisa — HANYA via klik manual (setelah tombol armed)
    public void OnUlangClicked()
    {
        if (!PassGuard("ULANG")) return;
        Debug.Log("[KarindingResult] ULANG clicked - restart manual stack=" + System.Environment.StackTrace.Split('\n')[1].Trim());
        // Hide overlay dulu
        HideOverlay();

        // Di scene tutorial: ULANG = ulangi TUTORIAL dari Fase 1 (bukan auto, harus klik dulu).
        if (ulangReplaysTutorial)
        {
            var tutorial = ResolveTutorial();
            if (tutorial != null)
            {
                tutorial.RestartTutorial();
                return;
            }
            Debug.Log("[KarindingResult] KarindingTutorialController tidak ada - fallback ulang chart terakhir");
        }

        if (_rhythm == null)
        {
            _rhythm = RhythmMinigameController.Instance;
            if (_rhythm == null) _rhythm = FindFirstObjectByType<RhythmMinigameController>();
        }
        if (_rhythm != null)
        {
            _rhythm.PrepareManualRestart();
            bool ok = _rhythm.RetryLast();
            if (!ok) Debug.LogWarning("[KarindingResult] RetryLast gagal - _activeConfig null, coba StartMinigame manual");
        }
        else
        {
            Debug.LogWarning("[KarindingResult] RhythmMinigameController not found for retry");
        }
    }

    /// <summary>
    /// Gate anti tap-tak-sengaja & anti dobel klik. True = aksi boleh lanjut.
    /// </summary>
    private bool PassGuard(string action)
    {
        if (!_armed)
        {
            Debug.Log("[KarindingResult] " + action + " diabaikan - tombol belum armed (anti tap saat overlay baru muncul)");
            return false;
        }
        if (Time.unscaledTime - _lastActionTime < actionCooldown)
        {
            Debug.Log("[KarindingResult] " + action + " diabaikan - masih cooldown (anti dobel klik)");
            return false;
        }
        _lastActionTime = Time.unscaledTime;
        return true;
    }

    private void HideOverlay()
    {
        if (successOverlayRoot != null) successOverlayRoot.gameObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    private KarindingTutorialController ResolveTutorial()
    {
        if (_tutorial == null) _tutorial = FindFirstObjectByType<KarindingTutorialController>();
        return _tutorial;
    }

    public void OnLanjutClicked()
    {
        if (!PassGuard("LANJUT")) return;
        Debug.Log("[KarindingResult] Lanjut clicked, nextScene=" +
            (string.IsNullOrEmpty(nextSceneName) ? "(belum diisi)" : nextSceneName));

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Validasi scene ada di BuildSettings
            bool exists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains(nextSceneName)) { exists = true; break; }
            }
            if (exists)
            {
                SceneManager.LoadScene(nextSceneName);
                return;
            }
            else
            {
                Debug.LogWarning("[KarindingResult] Scene " + nextSceneName + " not in BuildSettings, fallback hide overlay");
            }
        }
        // Fallback: hide overlay, optionally advance story via listener
        HideOverlay();
        var storyListener = FindFirstObjectByType<StoryProgressListener>();
        if (storyListener != null)
        {
            // Coba advance jika ada state lanjutan, tidak paksa
            Debug.Log("[KarindingResult] Lanjut fallback - storyListener found, overlay hidden");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-resolve di editor untuk preview wiring
        if (ulangButton == null || lanjutButton == null)
        {
            // delay resolve agar tidak error saat prefab edit
        }
    }
#endif
}
