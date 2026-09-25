using UnityEngine;

namespace MainKarinding.Karinding
{
    /// <summary>
    /// Highlight 2D Scene 2 Beat 4 — 3 sprite ditumpuk jadi 1 karinding utuh.
    /// HighlightPart(nama) nyalakan 1 sprite saja (tint kuning + pulse scale), matikan sisanya.
    /// Sway ringan ±swayAngle sebagai pengganti putar 3D. Alias: pacepeungan=pancepengan, paneunggeul=panabeuh.
    /// </summary>
    public class Karinding2DHighlightController : MonoBehaviour
    {
        [Header("Sprite per bagian (tumpuk, posisi sama = 1 utuh)")]
        [SerializeField] private SpriteRenderer pacepeunganRenderer;
        [SerializeField] private SpriteRenderer paneunggeulRenderer;
        [SerializeField] private SpriteRenderer cecetUcingRenderer;

        [Header("Highlight")]
        [SerializeField] private Color highlightTint = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color dimTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseScaleAmp = 0.04f;

        [Header("Sway (pengganti putar)")]
        [SerializeField] private bool swayEnabled = true;
        [SerializeField] private float swayAngle = 5f;
        [SerializeField] private float swaySpeed = 0.8f;

        private SpriteRenderer activeRenderer;
        private Vector3[] baseScales = new Vector3[3];
        private float pulseT;
        private float swayT;
        private float baseZRot;

        private void Awake()
        {
            CacheBaseScales();
            baseZRot = transform.eulerAngles.z;
            ClearAll();
        }

        private void CacheBaseScales()
        {
            var arr = All();
            for (int i = 0; i < arr.Length; i++)
            {
                Vector3 s = arr[i] != null ? arr[i].transform.localScale : Vector3.one;
                // Anti-nol permanen: jangan pernah cache/menulis scale ~0 (bug: sprite tak terlihat).
                baseScales[i] = s.sqrMagnitude < 1e-6f ? Vector3.one : s;
            }
        }

        private SpriteRenderer[] All() => new[] { pacepeunganRenderer, paneunggeulRenderer, cecetUcingRenderer };

        private void Update()
        {
            if (swayEnabled)
            {
                swayT += Time.deltaTime * swaySpeed;
                float z = baseZRot + Mathf.Sin(swayT * Mathf.PI * 2f) * swayAngle;
                var e = transform.eulerAngles;
                e.z = z;
                transform.eulerAngles = e;
            }
            if (activeRenderer != null)
            {
                pulseT += Time.deltaTime * pulseSpeed;
                float k = 1f + Mathf.Sin(pulseT) * pulseScaleAmp;
                int idx = IndexOf(activeRenderer);
                Vector3 b = idx >= 0 ? baseScales[idx] : Vector3.one;
                if (b.sqrMagnitude < 1e-6f) b = Vector3.one;
                activeRenderer.transform.localScale = b * k;
            }
        }

        public void HighlightPart(string partName)
        {
            ClearAll();
            var target = Resolve(partName);
            if (target == null)
            {
                Debug.LogWarning($"[Karinding2D] part tidak dikenal: '{partName}' (Pancepengan/Panabeuh/CecetUcing)");
                return;
            }
            activeRenderer = target;
            pulseT = 0f;
            target.color = highlightTint;
            Debug.Log($"[Karinding2D] ON: {partName}");
        }

        public void ClearAll()
        {
            activeRenderer = null;
            var arr = All();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) continue;
                arr[i].color = dimTint;
                // Jangan tulis scale nol ke scene (pertahankan 1 kalau cache rusak).
                arr[i].transform.localScale = baseScales[i].sqrMagnitude < 1e-6f ? Vector3.one : baseScales[i];
            }
        }

        public void SetFullBright()
        {
            activeRenderer = null;
            foreach (var r in All())
                if (r != null) { r.color = Color.white; }
        }

        private int IndexOf(SpriteRenderer r)
        {
            var arr = All();
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == r) return i;
            return -1;
        }

        private SpriteRenderer Resolve(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return null;
            string key = partName.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (key.Contains("pancepeng") || key.Contains("pacepeung")) return pacepeunganRenderer;
            if (key.Contains("panabeuh") || key.Contains("paneunggeul") || key.Contains("tabeuh") || key.Contains("pukul")) return paneunggeulRenderer;
            if (key.Contains("cecet") || key.Contains("ucing") || key.Contains("resonansi")) return cecetUcingRenderer;
            return null;
        }

        // Dipakai saat setup awal dari code: assign renderer + sprite sekaligus.
        public void Bind(SpriteRenderer pacepeungan, SpriteRenderer paneunggeul, SpriteRenderer cecetUcing)
        {
            pacepeunganRenderer = pacepeungan;
            paneunggeulRenderer = paneunggeul;
            cecetUcingRenderer = cecetUcing;
            CacheBaseScales();
            ClearAll();
        }
    }
}
