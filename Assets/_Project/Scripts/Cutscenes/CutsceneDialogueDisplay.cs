using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Display dialogue box with typewriter effect for cutscenes (ASFTU style).
    /// Single language (Sunda), compact bottom-right box, animated continue indicator.
    /// </summary>
    public class CutsceneDialogueDisplay : MonoBehaviour
    {
    [Header("UI References")]
    [SerializeField] private GameObject dialogueBoxRoot;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("Label nama pembicara (Shot 2). Buat TextMeshProUGUI sendiri di bawah Background, atur posisi/font manual, lalu drag ke sini.")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image dialogueBoxImage;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private Button fullScreenTapButton;

    [Header("BoxPointer - Layout (atur manual di Inspector/Scene View)")]
    [Tooltip("RectTransform ekor bubble. Geser manual di Scene View, semua tersimpan di scene.")]
    [SerializeField] private RectTransform boxPointerRect;
    [Tooltip("Image ekor bubble. Ganti sprite manual sesuai kebutuhan.")]
    [SerializeField] private Image boxPointerImage;
    [Tooltip("Kalau true, BoxPointer otomatis flip X sesuai speaker (Abah = kiri, Rimba = kanan). Matikan kalau mau atur manual penuh.")]
    [SerializeField] private bool autoFlipPointer = true;
    [Tooltip("Offset BoxPointer saat speaker Abah. User bisa ubah bebas.")]
    [SerializeField] private Vector2 pointerOffsetAbah = new Vector2(0f, 0f);
    [Tooltip("Offset BoxPointer saat speaker Rimba. User bisa ubah bebas.")]
    [SerializeField] private Vector2 pointerOffsetRimba = new Vector2(0f, 0f);

        [Header("Typewriter Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private bool canSkipTyping = true;

        [Header("Continue Indicator Animation")]
        [SerializeField] private float continueIndicatorBounceSpeed = 2f;

        private Coroutine typingCoroutine;
        private Coroutine continueAnimCoroutine;
        private bool isTyping = false;
        private bool waitingForInput = false;
        private string currentFullText = "";
        private string currentSpeaker = "";
        private string currentTriggerWord;
        private System.Action triggerCallback;
        private bool triggerFired;
        private Vector2 boxPointerBasePos;
        private bool boxPointerBaseCaptured = false;
        private Vector3 continueBaseScale;
        private bool continueBaseCaptured;

        public bool IsActive => dialogueBoxRoot != null && dialogueBoxRoot.activeSelf;
        public bool IsWaitingForInput => waitingForInput;

    private void Awake()
    {
        EnsureVisualRoot();

        if (dialogueBoxRoot != null && dialogueBoxRoot != gameObject)
            dialogueBoxRoot.SetActive(false);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        // Hook full-screen tap button
        if (fullScreenTapButton != null)
        {
            fullScreenTapButton.onClick.AddListener(OnTap);
            fullScreenTapButton.gameObject.SetActive(false);
        }
    }

    private void EnsureVisualRoot()
    {
        if (dialogueBoxRoot != null && dialogueBoxRoot != gameObject)
            return;
        Transform bg = transform.Find("Background");
        if (bg != null)
        {
            if (dialogueBoxRoot == gameObject)
                Debug.LogWarning("[DialogueDisplay] dialogueBoxRoot menunjuk host — auto-redirect ke 'Background'.");
            dialogueBoxRoot = bg.gameObject;
        }
    }

    private void SetVisualActive(bool active)
    {
        if (dialogueBoxRoot == null) return;
        if (dialogueBoxRoot == gameObject)
        {
            Debug.LogError("[DialogueDisplay] Tolak SetActive ke host (matikan coroutine). Assign ke Background.");
            return;
        }
        dialogueBoxRoot.SetActive(active);
    }

        /// <summary>
        /// Show dialogue box with Sunda text only (ASFTU style - no speaker name, single language).
        /// Speaker dipakai untuk atur BoxPointer (ekor bubble). Kosong = tidak flip.
        /// </summary>
        public void ShowDialogue(string text, System.Action onComplete = null)
        {
            ShowDialogue(text, "", onComplete);
        }

        /// <summary>
        /// Show dialogue dengan info speaker ("Abah"/"Rimba").
        /// Layout BoxPointer tetap bisa diatur manual di Inspector — autoFlipPointer hanya set scale.x.
        /// </summary>
        public void ShowDialogue(string text, string speaker, System.Action onComplete = null)
        {
            ShowDialogue(text, speaker, onComplete, null, null);
        }

        /// <summary>
        /// Show dialogue dengan trigger kata opsional (dipakai Scene 4 "bledag!").
        /// Trigger fire sekali saat teks terketik berakhiran triggerWord (case-insensitive).
        /// </summary>
        public void ShowDialogue(string text, string speaker, System.Action onComplete, string triggerWord, System.Action onTrigger)
        {
            currentSpeaker = speaker ?? "";
            currentTriggerWord = triggerWord;
            triggerCallback = onTrigger;
            triggerFired = false;

            if (speakerNameText != null)
            {
                speakerNameText.text = currentSpeaker;
                speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(currentSpeaker));
            }

            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            SetVisualActive(true);
            if (fullScreenTapButton != null)
                fullScreenTapButton.gameObject.SetActive(true);

            currentFullText = text;

            UpdateBoxPointer();
            
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            HideContinueIndicator();

            typingCoroutine = StartCoroutine(TypeText(text, onComplete));
        }

        /// <summary>
        /// Standby box: nyala visual + teks placeholder TANPA typing,
        /// tanpa continue indicator, tanpa tap fullscreen. Dipakai Shot 2
        /// sebelum tombol Lihat ditekan — dialog asli mulai via ShowDialogue().
        /// </summary>
        public void ShowStandby(string standbyText = "...")
        {
            currentSpeaker = "";
            currentFullText = standbyText ?? "";

            if (speakerNameText != null)
            {
                speakerNameText.text = "";
                speakerNameText.gameObject.SetActive(false);
            }

            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            SetVisualActive(true);
            // Sengaja TIDAK nyalakan fullScreenTapButton biar tap jatuh ke ActionButton.

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            HideContinueIndicator();

            if (dialogueText != null)
                dialogueText.text = currentFullText;

            isTyping = false;
            waitingForInput = false;
        }

        /// <summary>
        /// Update posisi/flip BoxPointer sesuai speaker.
        /// Base position = hasil tata manual kamu di Scene View (ditangkap otomatis saat pertama tampil).
        /// Offset per-speaker (pointerOffsetAbah/Rimba) ditambah di atas base — nol-kan kalau mau full manual.
        /// </summary>
        public void UpdateBoxPointer()
        {
            if (boxPointerRect == null) return;

            if (!boxPointerRect.gameObject.activeSelf)
                boxPointerRect.gameObject.SetActive(true);

            if (!boxPointerBaseCaptured)
            {
                boxPointerBasePos = boxPointerRect.anchoredPosition;
                boxPointerBaseCaptured = true;
            }

            Vector2 offset = Vector2.zero;
            if (currentSpeaker == "Abah") offset = pointerOffsetAbah;
            else if (currentSpeaker == "Rimba") offset = pointerOffsetRimba;

            boxPointerRect.anchoredPosition = boxPointerBasePos + offset;

            if (autoFlipPointer)
            {
                Vector3 s = boxPointerRect.localScale;
                float absX = Mathf.Abs(s.x);
                if (absX < 0.001f) absX = 1f;
                if (currentSpeaker == "Abah") s.x = -absX;
                else if (currentSpeaker == "Rimba") s.x = absX;
                boxPointerRect.localScale = s;
            }
        }

        /// <summary>
        /// Tampilkan BoxPointer manual (untuk setup layout di Edit Mode / testing).
        /// </summary>
        public void ShowBoxPointer()
        {
            if (boxPointerRect != null && !boxPointerRect.gameObject.activeSelf)
                boxPointerRect.gameObject.SetActive(true);
        }

        /// <summary>
        /// Sembunyikan BoxPointer manual.
        /// </summary>
        public void HideBoxPointer()
        {
            if (boxPointerRect != null && boxPointerRect.gameObject.activeSelf)
                boxPointerRect.gameObject.SetActive(false);
        }

        /// <summary>
        /// Hide dialogue box immediately.
        /// </summary>
        public void Hide()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            HideContinueIndicator();

            // Cegah trigger basi fire setelah Hide.
            triggerFired = true;
            triggerCallback = null;
            currentTriggerWord = null;

            isTyping = false;
            waitingForInput = false;
            currentSpeaker = "";
            if (speakerNameText != null)
                speakerNameText.text = "";

            SetVisualActive(false);
            if (fullScreenTapButton != null)
                fullScreenTapButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Handle user tap input during dialogue.
        /// </summary>
        public void OnTap()
        {
            // Tap selama teks masih mengetik diabaikan total.
            // Dulu ada skip-typing di sini, tapi itu membunuh coroutine
            // pemilik onComplete sehingga dialog macet dan tidak lanjut.
            if (!waitingForInput)
                return;
            waitingForInput = false;
        }

        private IEnumerator TypeText(string text, System.Action onComplete)
        {
            isTyping = true;
            waitingForInput = false;

            if (dialogueText != null)
                dialogueText.text = "";

            // Type text with typewriter effect
            foreach (char c in text)
            {
                if (dialogueText != null)
                    dialogueText.text += c;

                CheckTrigger();
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            waitingForInput = true;

            // Show continue indicator after typing finishes
            ShowContinueIndicator();

            // Wait for player tap to continue
            yield return new WaitUntil(() => !waitingForInput);

            HideContinueIndicator();

            onComplete?.Invoke();
            typingCoroutine = null;
        }

        /// <summary>
        /// Cek trigger keyword setiap karakter diketik. Fire sekali saja.
        /// </summary>
        private void CheckTrigger()
        {
            if (triggerFired || string.IsNullOrEmpty(currentTriggerWord)) return;
            string typed = dialogueText != null ? dialogueText.text : currentFullText;
            if (!string.IsNullOrEmpty(typed) && typed.EndsWith(currentTriggerWord, System.StringComparison.OrdinalIgnoreCase))
            {
                triggerFired = true;
                try { if (triggerCallback != null) triggerCallback(); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
        }

        private void ShowContinueIndicator()
        {
            if (continueIndicator == null) return;

            continueIndicator.SetActive(true);

            RectTransform rt = continueIndicator.GetComponent<RectTransform>();
            if (rt == null) return;

            if (!continueBaseCaptured)
            {
                continueBaseScale = rt.localScale;
                continueBaseCaptured = true;
            }
            rt.localScale = continueBaseScale;

            if (continueAnimCoroutine != null)
                StopCoroutine(continueAnimCoroutine);

            continueAnimCoroutine = StartCoroutine(AnimateContinueIndicator());
        }

        private void HideContinueIndicator()
        {
            if (continueAnimCoroutine != null)
            {
                StopCoroutine(continueAnimCoroutine);
                continueAnimCoroutine = null;
            }

            if (continueIndicator != null)
            {
                RectTransform rt = continueIndicator.GetComponent<RectTransform>();
                if (rt != null && continueBaseCaptured)
                    rt.localScale = continueBaseScale;
                continueIndicator.SetActive(false);
            }
        }

        private IEnumerator AnimateContinueIndicator()
        {
            if (continueIndicator == null) yield break;

            RectTransform rt = continueIndicator.GetComponent<RectTransform>();
            if (rt == null) yield break;

            if (!continueBaseCaptured)
            {
                continueBaseScale = rt.localScale;
                continueBaseCaptured = true;
            }

            while (true)
            {
                // Bounce scale 1.0 → 1.2 → 1.0
                float scale = 1f + Mathf.PingPong(Time.time * continueIndicatorBounceSpeed, 0.2f);
            rt.localScale = continueBaseScale * scale;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        // Cleanup button listener
        if (fullScreenTapButton != null)
            fullScreenTapButton.onClick.RemoveListener(OnTap);
    }
    }
}
