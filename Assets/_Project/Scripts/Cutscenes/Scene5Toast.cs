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

        [Header("Settings")]
        [SerializeField] private float fadeSeconds = 0.3f;

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
        }

        /// <summary>Tampilkan toast. Tidak blocking — jalan terus, auto-hilang.</summary>
        public void ShowToast(string text, float seconds = 2.5f)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ToastRoutine(text, Mathf.Max(0.5f, seconds)));
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
