using UnityEngine;

namespace MainKarinding.Karinding
{
    /// <summary>
    /// Highlight overlay untuk karinding.fbx 1-mesh-merged (PLAN Keputusan #1).
    /// 3 child overlay (primitif dummy) diposisikan menutupi tiap bagian,
    /// toggle emissive/warna tepat saat Abah menyebut nama bagian (Beat 4).
    /// Jangan nyalakan 2 highlight bersamaan — selalu eksklusif.
    /// Swap ke mesh 3-part nanti: cukup ganti isi HighlightPart(), logic Scene 2 tidak berubah.
    /// </summary>
    public class KarindingHighlightController : MonoBehaviour
    {
        [Header("Overlay per bagian (child GameObject, primitif dummy boleh)")]
        [Tooltip("Pancepengan — bagian pegangan (cepeung sing yakin)")]
        [SerializeField] private GameObject pancepenganOverlay;
        [Tooltip("Panabeuh — bagian pukul/getar (tabeuh sing sabar)")]
        [SerializeField] private GameObject panabeuhOverlay;
        [Tooltip("Cecet Ucing — resonansi kecil")]
        [SerializeField] private GameObject cecetUcingOverlay;

        [Header("Warna highlight")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.3f, 0.55f);
        [SerializeField] private bool autoBuildDummies = true;

        private void Awake()
        {
            if (autoBuildDummies)
                EnsureDummyOverlays();
            ClearAll();
        }

        /// <summary>Bikin primitif dummy kalau slot overlay kosong (placeholder, gampang di-swap).</summary>
        [ContextMenu("Build Dummy Overlays")]
        public void EnsureDummyOverlays()
        {
            pancepenganOverlay = EnsureDummy(pancepenganOverlay, "HL_Pancepengan", new Vector3(-0.35f, 0f, 0f), new Vector3(0.12f, 0.5f, 0.12f));
            panabeuhOverlay = EnsureDummy(panabeuhOverlay, "HL_Panabeuh", new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.08f, 0.08f));
            cecetUcingOverlay = EnsureDummy(cecetUcingOverlay, "HL_CecetUcing", new Vector3(0.35f, 0f, 0f), new Vector3(0.1f, 0.1f, 0.1f));
        }

        private GameObject EnsureDummy(GameObject existing, string childName, Vector3 localPos, Vector3 localScale)
        {
            if (existing != null) return existing;
            Transform found = transform.Find(childName);
            if (found != null) return found.gameObject;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = childName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            ApplyHighlightMaterial(go, false);
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Nyalakan 1 bagian saja, matikan sisanya. partName: Pancepengan / Panabeuh / CecetUcing (case-insensitive).
        /// </summary>
        public void HighlightPart(string partName)
        {
            ClearAll();
            GameObject target = Resolve(partName);
            if (target == null)
            {
                Debug.LogWarning($"[KarindingHighlight] part tidak dikenal: '{partName}' (pakai Pancepengan/Panabeuh/CecetUcing)");
                return;
            }
            target.SetActive(true);
            ApplyHighlightMaterial(target, true);
            Debug.Log($"[KarindingHighlight] ON: {partName}");
        }

        public void ClearAll()
        {
            SetOff(pancepenganOverlay);
            SetOff(panabeuhOverlay);
            SetOff(cecetUcingOverlay);
        }

        private void SetOff(GameObject go)
        {
            if (go == null) return;
            ApplyHighlightMaterial(go, false);
            go.SetActive(false);
        }

        private GameObject Resolve(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return null;
            string key = partName.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (key.Contains("pancepeng")) return pancepenganOverlay;
            if (key.Contains("panabeuh") || key.Contains("tabeuh")) return panabeuhOverlay;
            if (key.Contains("cecet") || key.Contains("ucing") || key.Contains("resonansi")) return cecetUcingOverlay;
            return null;
        }

        private void ApplyHighlightMaterial(GameObject go, bool on)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            // Material instance per overlay biar tidak bocor ke primitif lain.
            var mat = on ? GetOrCreateHighlightMat(rend) : GetOrCreateDimMat(rend);
            rend.sharedMaterial = mat;
        }

        private Material highlightMat;
        private Material dimMat;

        private Material GetOrCreateHighlightMat(Renderer rend)
        {
            if (highlightMat == null)
            {
                highlightMat = new Material(Shader.Find("Standard"));
                highlightMat.SetColor("_Color", highlightColor);
                highlightMat.SetColor("_EmissionColor", highlightColor * 1.5f);
                highlightMat.EnableKeyword("_EMISSION");
                highlightMat.SetFloat("_Mode", 3); // transparent
            }
            return highlightMat;
        }

        private Material GetOrCreateDimMat(Renderer rend)
        {
            if (dimMat == null)
            {
                dimMat = new Material(Shader.Find("Standard"));
                dimMat.SetColor("_Color", new Color(1f, 1f, 1f, 0.15f));
                dimMat.DisableKeyword("_EMISSION");
                dimMat.SetFloat("_Mode", 3);
            }
            return dimMat;
        }
    }
}
