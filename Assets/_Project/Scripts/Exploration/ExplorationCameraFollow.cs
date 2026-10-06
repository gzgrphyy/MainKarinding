using UnityEngine;

namespace MainKarinding.Exploration
{
    /// <summary>
    /// Kamera ngikutin Rimba di area exploration BG panjang.
    /// Follow halus (Lerp) sumbu X saja, dijepit (clamp) supaya tidak keluar BG.
    /// Auto-bind ke Scene1_Cutscene/ExplorationRoot kalau field kosong di Inspector.
    /// Saat exploration nonaktif, kamera balik ke posisi home (cutscene).
    /// </summary>
    public class ExplorationCameraFollow : MonoBehaviour
    {
        [Header("Target - boleh kosong (auto-bind Scene 1)")]
        [SerializeField] private Transform target;
        [SerializeField] private SpriteRenderer bgRenderer;

        [Header("Gerak - USER ATUR")]
        [Tooltip("Makin besar makin nempel (rekomendasi 3-5).")]
        [SerializeField] private float smoothSpeed = 4f;
        [Tooltip("Margin dalam unit dari tepi BG. 0 = kamera boleh mentok sampai ujung BG.")]
        [SerializeField] private float edgeMargin = 0f;

        private Camera cam;
        private Vector3 homePos;
        private bool hasHome;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = Camera.main;
            homePos = transform.position;
            hasHome = true;
            AutoBind();
        }

        private void AutoBind()
        {
            if (target == null || bgRenderer == null)
            {
                GameObject root = GameObject.Find("Scene1_Cutscene/ExplorationRoot");
                if (root != null)
                {
                    if (target == null)
                    {
                        Transform t = root.transform.Find("WalkRimba");
                        if (t != null)
                            target = t;
                    }
                    if (bgRenderer == null)
                    {
                        Transform b = root.transform.Find("BG_Exploration");
                        if (b != null)
                            bgRenderer = b.GetComponent<SpriteRenderer>();
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (cam == null || !hasHome)
                return;
            if (target == null || bgRenderer == null)
            {
                AutoBind();
                return;
            }

            // Exploration sedang tidak aktif -> kembalikan kamera ke home (posisi cutscene).
            if (!target.gameObject.activeInHierarchy)
            {
                transform.position = Vector3.Lerp(transform.position, homePos, smoothSpeed * Time.deltaTime);
                return;
            }

            float camHalfH = cam.orthographicSize;
            float camHalfW = camHalfH * cam.aspect;

            Bounds b = bgRenderer.bounds;
            float minCamX;
            float maxCamX;
            if (b.size.x <= camHalfW * 2f)
            {
                // BG lebih sempit dari layar: kunci di tengah BG.
                minCamX = b.center.x;
                maxCamX = b.center.x;
            }
            else
            {
                minCamX = b.min.x + camHalfW - edgeMargin;
                maxCamX = b.max.x - camHalfW + edgeMargin;
            }

            float desiredX = Mathf.Clamp(target.position.x, minCamX, maxCamX);
            Vector3 desired = new Vector3(desiredX, homePos.y, homePos.z);
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }
    }
}
