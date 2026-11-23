using UnityEngine;
using TMPro;

namespace KarindingLegacy.UI
{
    /// <summary>
    /// Menempelkan teks UI ke kunci lokalisasi. Teks otomatis diperbarui saat bahasa diganti.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text label;

        public string Key => key;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= Apply;
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            if (label == null) label = GetComponent<TMP_Text>();
            Apply();
        }

        public void Apply()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null || string.IsNullOrEmpty(key)) return;

            label.text = LocalizationManager.Get(key);
        }
    }
}
