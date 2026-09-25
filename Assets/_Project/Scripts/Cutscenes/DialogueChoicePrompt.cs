using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// UI branching 3-opsi Scene 2 Beat 3 (PLAN D.5).
    /// 3 tombol tampil bareng, player pilih 1 -> lanjut jawaban Abah yang sesuai.
    /// Default: sekali pilih langsung lanjut (opsi lain hilang).
    /// Styling reuse s1_ui_dialoguebox.png kalau bisa; background per tombol pakai ui_choice_button.png.
    /// </summary>
    public class DialogueChoicePrompt : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [Tooltip("Background opsional per tombol (sprite ui_choice_button.png)")]
        [SerializeField] private Sprite choiceButtonSprite;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip tapConfirmClip;

        [Header("Tap blockers (dimatikan selama choice tampil)")]
        [Tooltip("Misal TapDetector fullscreen. Mati pas Show, nyala lagi pas Pick/Hide.")]
        [SerializeField] private GameObject[] tapBlockers;

        private System.Action<int> onPicked;
        private bool showing;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (root != null && root != gameObject)
                root.SetActive(false);
            else if (root == gameObject)
            {
                // host sendiri = root; matikan children visual saja biar coroutine aman
                SetChildrenActive(false);
            }
        }

        /// <summary>options harus 3 string (Opsi A/B/C teks Rimba). cb dipanggil sekali dengan index 0..2.</summary>
        public void Show(string[] options, System.Action<int> cb)
        {
            if (options == null || options.Length < 3)
            {
                Debug.LogError("[DialogueChoice] butuh 3 opsi!");
                return;
            }
            onPicked = cb;
            showing = true;
            SetVisible(true);
            SetBlockers(false);

            for (int i = 0; i < 3; i++)
            {
                int idx = i; // capture
                var btn = (choiceButtons != null && i < choiceButtons.Length) ? choiceButtons[i] : null;
                if (btn == null)
                {
                    Debug.LogError($"[DialogueChoice] choiceButtons[{i}] belum di-assign!");
                    continue;
                }
                // Set label
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.text = options[i];
                // Set background sprite kalau ada
                var img = btn.GetComponent<Image>();
                if (img != null && choiceButtonSprite != null) img.sprite = choiceButtonSprite;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Pick(idx));
                btn.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            showing = false;
            onPicked = null;
            SetVisible(false);
            SetBlockers(true);
        }

        private void SetBlockers(bool active)
        {
            if (tapBlockers == null) return;
            foreach (var go in tapBlockers)
                if (go != null) go.SetActive(active);
        }

        private void Pick(int idx)
        {
            if (!showing) return;
            showing = false;
            if (sfxSource != null && tapConfirmClip != null)
                sfxSource.PlayOneShot(tapConfirmClip);
            Debug.Log($"[DialogueChoice] picked Opsi {(char)('A' + idx)} (index {idx})");
            SetVisible(false);
            SetBlockers(true);
            var cb = onPicked;
            onPicked = null;
            cb?.Invoke(idx);
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root != gameObject)
            {
                root.SetActive(visible);
                return;
            }
            SetChildrenActive(visible);
        }

        private void SetChildrenActive(bool active)
        {
            if (choiceButtons == null) return;
            foreach (var b in choiceButtons)
                if (b != null) b.gameObject.SetActive(active);
        }

        private void OnDestroy()
        {
            if (choiceButtons == null) return;
            foreach (var b in choiceButtons)
                if (b != null) b.onClick.RemoveAllListeners();
        }
    }
}
