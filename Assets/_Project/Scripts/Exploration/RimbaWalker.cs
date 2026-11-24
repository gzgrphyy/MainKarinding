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
        [SerializeField] private float speed = 1.7f;
        [SerializeField] private float minX = -7.5f;
        [SerializeField] private float maxX = 7.5f;
        [SerializeField] private float minY = -3.4f;
        [SerializeField] private float maxY = -0.6f;
        [SerializeField] private float frameTime = 0.12f;

        [Header("Runtime (diisi otomatis Beat 8)")]
        [SerializeField] private AnalogStickController analog;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite idleSprite;

        [Header("SFX Langkah")]
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private float footstepVolume = 0.8f;
        [SerializeField] private float footstepFadeTime = 0.12f;
        [SerializeField] private bool randomizeStart = true;

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

            if (footstepSource == null) footstepSource = GetComponent<AudioSource>();
            if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = true;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 0f;
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
            bool moving = false;

            if (!Locked && analog != null && analog.isActiveAndEnabled)
            {
                Vector2 v = analog.Value;
                // Dikunci sumbu X saja: Y tetap di posisi spawn, analog atas/bawah diabaikan.
                if (Mathf.Abs(v.x) > 0.15f)
                {
                    Vector3 p = transform.position;
                    p.x = Mathf.Clamp(p.x + v.x * speed * Time.deltaTime, minX, maxX);
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

                    moving = true;
                }
                else
                {
                    frameTimer = 0f;
                    frameIndex = 0;
                    if (idleSprite != null && sr != null && sr.sprite != idleSprite)
                        sr.sprite = idleSprite;
                }
            }

            UpdateFootstep(moving);
        }

        private void UpdateFootstep(bool moving)
        {
            if (footstepSource == null) return;

            if (footstepClip == null)
            {
                if (footstepSource.isPlaying) footstepSource.Stop();
                return;
            }

            float fadeSpeed = footstepVolume / Mathf.Max(0.01f, footstepFadeTime);

            if (moving)
            {
                if (!footstepSource.isPlaying)
                {
                    footstepSource.clip = footstepClip;
                    footstepSource.volume = 0f;
                    footstepSource.Play();
                    if (randomizeStart)
                        footstepSource.time = Random.Range(0f, Mathf.Max(0f, footstepClip.length - 0.1f));
                }

                footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, footstepVolume, fadeSpeed * Time.unscaledDeltaTime);
            }
            else if (footstepSource.isPlaying)
            {
                footstepSource.volume = Mathf.MoveTowards(footstepSource.volume, 0f, fadeSpeed * Time.unscaledDeltaTime);
                if (footstepSource.volume <= 0.001f)
                {
                    footstepSource.volume = 0f;
                    footstepSource.Stop();
                }
            }
        }

        private void OnDisable()
        {
            if (footstepSource == null) return;
            footstepSource.Stop();
            footstepSource.volume = 0f;
        }

        private void SetFlip(bool faceLeft)
        {
            Vector3 s = transform.localScale;
            s.x = baseScaleX * (faceLeft ? -1f : 1f);
            transform.localScale = s;
        }
    }
}
