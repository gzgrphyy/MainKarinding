using UnityEngine;
using UnityEngine.EventSystems;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Memberikan efek animasi transisi skala halus ketika tombol di-hover atau ditekan.
    /// Menggunakan unscaledDeltaTime sehingga tetap responsif saat game di-pause.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Settings")]
        [Tooltip("Faktor perbesaran saat kursor di atas tombol")]
        [SerializeField] private float hoverScale = 1.05f;

        [Tooltip("Faktor pengecilan saat tombol ditekan")]
        [SerializeField] private float pressedScale = 0.95f;

        [Tooltip("Kecepatan lerp animasi")]
        [SerializeField] private float animationSpeed = 12f;

        [Header("Audio Feedback (Opsional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        private Vector3 originalScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;
        private bool isPointerOver = false;
        private bool isPressed = false;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one;
            }
            transform.localScale = originalScale;
            targetScale = originalScale;
            isPointerOver = false;
            isPressed = false;
        }

        private void Update()
        {
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (!isPressed)
            {
                targetScale = originalScale * hoverScale;
            }

            if (audioSource != null && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            if (!isPressed)
            {
                targetScale = originalScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            targetScale = originalScale * pressedScale;

            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            targetScale = isPointerOver ? (originalScale * hoverScale) : originalScale;
        }

        public void ResetScaleImmediate()
        {
            targetScale = originalScale;
            transform.localScale = originalScale;
        }
    }
}
