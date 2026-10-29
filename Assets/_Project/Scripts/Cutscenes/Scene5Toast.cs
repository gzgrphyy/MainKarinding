using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Toast kecil non-blocking (pengganti bubble besar yang menghalangi layar).
    /// Dipakai Scene 5 Hutan untuk pesan hint/pulang: muncul di atas layar,
    /// hilang sendiri setelah beberapa detik, tanpa perlu tap, tidak mengikuti pemain.
    /// Generik — bisa dipakai ulang scene lain (misal Scene 6).
    /// </summary>
    [AddComponentMenu("Main Karinding/Cutscenes/Scene Toast")]
    [RequireComponent(typeof(CanvasGroup))]
    public class Scene5Toast : MonoBehaviour
    {
        [Header("References (auto-find kalau kosong)")]
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private Image toastBackground;
        [Tooltip("Kotak luar toast (wrapper). Kalau diisi, ukuran native PNG diterapkan ke kotak ini — sama seperti dialog box diatur di scene. Kalau kosong, pakai RectTransform toast itu sendiri.")]
        [SerializeField] private RectTransform outerBox;

        [Header("Settings")]
        [SerializeField] private float fadeSeconds = 0.3f;
        [Tooltip("Matikan ukuran toast sesuai ukuran asli PNG (preserve aspect + native size). Di-off = ukuran diatur manual di scene/Inspector, sama seperti dialog box.")]
        [SerializeField] private bool matchNativeSize = false;
        [Tooltip("Padding tambahan di sekitar PNG (dalam piksel canvas). Diabaikan kalau matchNativeSize mati.")]
        [SerializeField] private Vector2 nativeSizePadding = Vector2.zero;
        [Header("Editor Preview")]
        [Tooltip("Nyalakan agar gambar toast tetap terlihat di Scene view saat Edit Mode, biar mudah atur tata letak posisi dan ukuran. Matikan kembali sebelum build.")]
        [SerializeField] private bool previewInEditMode = false;

        private CanvasGroup group;
        private Coroutine running;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (toastText == null) toastText = GetComponentInChildren<TextMeshProUGUI>();
            if (toastBackground == null) toastBackground = GetComponent<Image>();
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            if (toastBackground != null) toastBackground.raycastTarget = false;

            MatchNativeSize();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            group ??= GetComponent<CanvasGroup>();
            if (group != null && previewInEditMode)
                group.alpha = 1f;
            // Jika previewInEditMode false: hormati nilai alpha di Inspector/YAML, jangan timpa
        }

        /// <summary>Tampilkan toast. Tidak blocking — jalan terus, auto-hilang.</summary>
        public void ShowToast(string text, float seconds = 2.5f)
        {
            MatchNativeSize();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ToastRoutine(text, Mathf.Max(0.5f, seconds)));
        }

        /// <summary>Atur ukuran toast agar sesuai aspek rasio PNG asli (mirip dialog box diatur di scene).</summary>
        private void MatchNativeSize()
        {
            if (!matchNativeSize) return;
            var img = toastBackground;
            if (img == null || img.sprite == null) return;

            var rt = outerBox != null ? outerBox : GetComponent<RectTransform>();
            if (rt == null) return;

            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var sprite = img.sprite;
            float w = sprite.rect.width + nativeSizePadding.x;
            float h = sprite.rect.height + nativeSizePadding.y;
            if (w <= 0f || h <= 0f) return;

            // Hanya timpa sizeDelta — anchor/pivot/anchoredPosition dipertahankan sesuai Inspector (polanya sama seperti dialog box diatur di scene).
            rt.sizeDelta = new Vector2(w, h);
        }

        public void HideNow()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            if (group != null) group.alpha = 0f;
        }

        private IEnumerator ToastRoutine(string text, float seconds)
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            if (toastText != null) toastText.text = text;
            yield return StartCoroutine(FadeTo(1f));
            yield return new WaitForSeconds(seconds);
            yield return StartCoroutine(FadeTo(0f));
            running = null;
        }

        private IEnumerator FadeTo(float target)
        {
            if (group == null) yield break;
            float start = group.alpha;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds)));
                yield return null;
            }
            group.alpha = target;
        }
    }
}
