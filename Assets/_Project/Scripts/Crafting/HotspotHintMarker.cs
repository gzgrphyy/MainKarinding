using UnityEngine;
using DG.Tweening;

namespace MainKarinding.Crafting
{
    /// <summary>
    /// Penanda hint kuning berkedip di titik claim bambu hutan Scene 5 (Beat 2).
    /// Dipasang sebagai child CraftingHotspot (posisi = titik tata letak),
    /// otomatis ikut hilang saat hotspot di-claim (induk SetActive(false)).
    /// Sprite dot putih + tint kuning dari kode (jamin kuning walau sprite diganti).
    /// </summary>
    [AddComponentMenu("Main Karinding/Crafting/Hotspot Hint Marker")]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HotspotHintMarker : MonoBehaviour
    {
        [Header("Hint")]
        [Tooltip("Warna hint. Default kuning.")]
        [SerializeField] private Color hintColor = new Color(1f, 0.85f, 0.2f, 1f);
        [Tooltip("Alpha terendah saat kedip.")]
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.35f;
        [Tooltip("Durasi setengah denyut (detik).")]
        [SerializeField] private float pulseDuration = 0.65f;
        [Tooltip("Skala terkecil (pengali skala awal).")]
        [SerializeField] private float minScaleMul = 0.9f;
        [Tooltip("Skala terbesar (pengali skala awal).")]
        [SerializeField] private float maxScaleMul = 1.15f;

        private SpriteRenderer sr;
        private Vector3 baseScale;
        private Tween scaleTween;
        private Tween fadeTween;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            ApplyColor(1f);
            StopTweens();
            scaleTween = transform
                .DOScale(baseScale * maxScaleMul, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            // Mulai dari skala kecil supaya denyut langsung terlihat.
            transform.localScale = baseScale * minScaleMul;
            fadeTween = sr
                .DOFade(minAlpha, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            StopTweens();
        }

        private void OnDestroy()
        {
            StopTweens();
        }

        private void ApplyColor(float alpha)
        {
            if (sr == null) return;
            var c = hintColor;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }

        private void StopTweens()
        {
            if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
            if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();
            scaleTween = null;
            fadeTween = null;
        }
    }
}
