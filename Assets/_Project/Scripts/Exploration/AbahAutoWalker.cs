using UnityEngine;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Abah auto-jalan ke kanan dengan kecepatan konstan, berhenti di stopX.
    /// Jalan otomatis saat object aktif (OnEnable) — dipakai Beat 8.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AbahAutoWalker : MonoBehaviour
    {
        [Header("Gerak - USER ATUR")]
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float stopX = 5f;
        [SerializeField] private float frameTime = 0.14f;

        [Header("Runtime (diisi otomatis Beat 8)")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite idleSprite;

        public bool Walking { get; private set; }

        private SpriteRenderer sr;
        private float frameTimer;
        private int frameIndex;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Walking = true;
            frameTimer = 0f;
            frameIndex = 0;
        }

        /// <summary>Dipanggil controller saat Beat 8 mulai.</summary>
        public void Configure(Sprite[] walk, Sprite idle)
        {
            walkFrames = walk;
            idleSprite = idle;
            if (idleSprite == null && walkFrames != null && walkFrames.Length > 0)
                idleSprite = walkFrames[0];
            Walking = true;
        }

        /// <summary>Lanjut jalan ke titik baru (dipakai Scene 8: keluar layar setelah "Moal Kataekan").</summary>
        public void ResumeWalking(float newStopX)
        {
            stopX = newStopX;
            Walking = true;
        }

        private void Update()
        {
            if (!Walking) return;

            if (transform.position.x < stopX)
            {
                Vector3 p = transform.position;
                p.x += speed * Time.deltaTime;
                transform.position = p;

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
            else if (idleSprite != null && sr != null && sr.sprite != idleSprite)
            {
                sr.sprite = idleSprite;
            }
        }
    }
}
