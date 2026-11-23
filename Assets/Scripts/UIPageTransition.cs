using System.Collections;
using UnityEngine;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Transisi halaman penuh dengan fade dan geser halus, dipakai saat berpindah
    /// dari main menu ke halaman pilihan mode dan sebaliknya.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPageTransition : MonoBehaviour
    {
        [Header("Page Transition")]
        [SerializeField] private float duration = 0.26f;
        [SerializeField] private Vector2 hiddenOffset = new Vector2(-90f, 0f);

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector2 visiblePosition;
        private Coroutine activeRoutine;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;

            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                visiblePosition = rectTransform.anchoredPosition;
            }

            initialized = true;
        }

        public void Show(bool animated = true)
        {
            Initialize();
            gameObject.SetActive(true);

            if (activeRoutine != null) StopCoroutine(activeRoutine);

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (!animated)
            {
                SetImmediate(true);
                return;
            }

            activeRoutine = StartCoroutine(Animate(true));
        }

        public void Hide(bool animated = true)
        {
            Initialize();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (!animated || !gameObject.activeInHierarchy)
            {
                SetImmediate(false);
                return;
            }

            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(Animate(false));
        }

        public void SetVisibleImmediate(bool visible)
        {
            Initialize();
            gameObject.SetActive(visible);
            SetImmediate(visible);
        }

        private void SetImmediate(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = visible ? visiblePosition : visiblePosition + hiddenOffset;
            }
        }

        private IEnumerator Animate(bool opening)
        {
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : (opening ? 0f : 1f);
            float targetAlpha = opening ? 1f : 0f;
            Vector2 startPosition = rectTransform != null ? rectTransform.anchoredPosition : visiblePosition;
            Vector2 targetPosition = opening ? visiblePosition : visiblePosition + hiddenOffset;
            float elapsed = 0f;

            if (opening && rectTransform != null && canvasGroup != null && canvasGroup.alpha <= 0.001f)
            {
                startPosition = visiblePosition + hiddenOffset;
                rectTransform.anchoredPosition = startPosition;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = opening ? 1f - (1f - t) * (1f - t) : t * t;

                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);
                if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, ease);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
            if (rectTransform != null) rectTransform.anchoredPosition = targetPosition;

            activeRoutine = null;

            if (!opening)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
