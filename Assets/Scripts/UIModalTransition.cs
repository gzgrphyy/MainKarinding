using System.Collections;
using UnityEngine;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Mengelola animasi transisi pop-up dan fade yang halus untuk panel modal (Settings & Credits).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIModalTransition : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float duration = 0.22f;
        [SerializeField] private RectTransform contentTransform;

        private CanvasGroup canvasGroup;
        private Coroutine activeRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (contentTransform == null)
            {
                if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRect)
                {
                    contentTransform = childRect;
                }
                else
                {
                    contentTransform = GetComponent<RectTransform>();
                }
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateModal(true));
        }

        public void Hide()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (gameObject.activeInHierarchy)
            {
                if (activeRoutine != null) StopCoroutine(activeRoutine);
                activeRoutine = StartCoroutine(AnimateModal(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator AnimateModal(bool opening)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : (opening ? 0f : 1f);
            float targetAlpha = opening ? 1f : 0f;

            Vector3 startScale = contentTransform != null ? contentTransform.localScale : Vector3.one;
            Vector3 targetScale = opening ? Vector3.one : new Vector3(0.92f, 0.92f, 1f);

            if (opening)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                if (contentTransform != null) contentTransform.localScale = new Vector3(0.92f, 0.92f, 1f);
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease-out quad curve
                float ease = opening ? 1f - (1f - t) * (1f - t) : t * t;

                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);
                if (contentTransform != null)
                {
                    contentTransform.localScale = Vector3.Lerp(startScale, targetScale, ease);
                }

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
            if (contentTransform != null) contentTransform.localScale = targetScale;

            if (!opening)
            {
                gameObject.SetActive(false);
            }

            activeRoutine = null;
        }
    }
}
