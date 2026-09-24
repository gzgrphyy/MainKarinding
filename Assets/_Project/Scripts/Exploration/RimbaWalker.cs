using UnityEngine;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Rimba jalan 4 arah via analog. Flip otomatis ikut arah X terakhir.
    /// Frame jalan dari RimbaSpriteSet.walkRight (di-inject controller),
    /// diam pakai idleRight (fallback walkRight[0]).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RimbaWalker : MonoBehaviour
    {
        [Header("Gerak - USER ATUR")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private float minX = -7.5f;
        [SerializeField] private float maxX = 7.5f;
        [SerializeField] private float minY = -3.4f;
        [SerializeField] private float maxY = -0.6f;
        [SerializeField] private float frameTime = 0.12f;

        [Header("Runtime (diisi otomatis Beat 8)")]
        [SerializeField] private AnalogStickController analog;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite idleSprite;

        public bool Locked { get; set; } = true;

        private SpriteRenderer sr;
        private float frameTimer;
        private int frameIndex;
        private float baseScaleX;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScaleX = Mathf.Abs(transform.localScale.x);
            if (baseScaleX < 0.001f) baseScaleX = 1f;
        }

        /// <summary>Dipanggil controller saat Beat 8 mulai.</summary>
        public void Configure(AnalogStickController stick, Sprite[] walk, Sprite idle)
        {
            analog = stick;
            walkFrames = walk;
            idleSprite = idle;
            if (idleSprite == null && walkFrames != null && walkFrames.Length > 0)
                idleSprite = walkFrames[0];
            if (idleSprite != null && sr != null)
                sr.sprite = idleSprite;
            frameTimer = 0f;
            frameIndex = 0;
        }

        private void Update()
        {
            if (Locked || analog == null || !analog.isActiveAndEnabled) return;

            Vector2 v = analog.Value;
            if (v.magnitude > 0.15f)
            {
                Vector3 p = transform.position;
                p.x = Mathf.Clamp(p.x + v.x * speed * Time.deltaTime, minX, maxX);
                p.y = Mathf.Clamp(p.y + v.y * speed * Time.deltaTime, minY, maxY);
                transform.position = p;

                if (v.x > 0.1f) SetFlip(false);
                else if (v.x < -0.1f) SetFlip(true);

                frameTimer += Time.deltaTime;
                if (frameTimer >= frameTime)
                {
                    frameTimer = 0f;
                    if (walkFrames != null && walkFrames.Length > 0)
                    {
                        frameIndex = (frameIndex + 1) % walkFrames.Length;
                        if (walkFrames[frameIndex] != null && sr != null)
                            sr.sprite = walkFrames[frameIndex];
                    }
                }
            }
            else
            {
                frameTimer = 0f;
                frameIndex = 0;
                if (idleSprite != null && sr != null && sr.sprite != idleSprite)
                    sr.sprite = idleSprite;
            }
        }

        private void SetFlip(bool faceLeft)
        {
            Vector3 s = transform.localScale;
            s.x = baseScaleX * (faceLeft ? -1f : 1f);
            transform.localScale = s;
        }
    }
}
