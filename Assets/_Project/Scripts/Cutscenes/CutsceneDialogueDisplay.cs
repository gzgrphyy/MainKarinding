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

        [Header("Voice (beat audio sinkron teks)")]
        [Tooltip("Clip voice default — dipakai kalau baris tidak punya clip sendiri.")]
        [SerializeField] private AudioClip defaultVoiceClip;
        [Tooltip("Batas bawah kecepatan typewriter saat ada voice (detik/karakter). 0 = bebas.")]
        [SerializeField] private float voiceMinCharDelay = 0f;
        [Tooltip("Batas atas kecepatan typewriter saat ada voice (detik/karakter). 0 = bebas.")]
        [SerializeField] private float voiceMaxCharDelay = 0f;
        [Tooltip("Volume voice. 0-1.")]
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;
        [Tooltip("Kalau true, kecepatan ngetik mengikuti durasi clip voice (audio-driven). Kalau false (default), pakai typingSpeed dari Inspector.")]
        [SerializeField] private bool syncTypingToVoice = false;
        private AudioSource voiceSource;

        [Header("Continue Indicator Animation")]
        [SerializeField] private float continueIndicatorBounceSpeed = 2f;

        private Coroutine typingCoroutine;
        private Coroutine continueAnimCoroutine;
        private bool isTyping = false;
        private bool waitingForInput = false;
        private bool skipRequested = false;
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
        EnsureVoiceSource();

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

    private void EnsureVoiceSource()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null)
        {
            GameObject src = new GameObject("VoiceSource");
            src.transform.SetParent(transform, false);
            voiceSource = src.AddComponent<AudioSource>();
        }
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.volume = voiceVolume;
        voiceSource.Stop();
    }

    private void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Stop();
        if (voiceSource != null)
            voiceSource.clip = null;
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

        /// <summary>Show dialogue dengan clip voice per-baris (null = pakai defaultVoiceClip).</summary>
        public void ShowDialogue(string text, System.Action onComplete, AudioClip voice)
        {
            ShowDialogue(text, "", onComplete, null, null, voice);
        }

        /// <summary>
        /// Show dialogue dengan info speaker ("Abah"/"Rimba").
        /// Layout BoxPointer tetap bisa diatur manual di Inspector — autoFlipPointer hanya set scale.x.
        /// </summary>
        public void ShowDialogue(string text, string speaker, System.Action onComplete = null)
        {
            ShowDialogue(text, speaker, onComplete, null, null);
        }

        /// <summary>Show dialogue per speaker + clip voice per-baris (null = pakai defaultVoiceClip).</summary>
        public void ShowDialogue(string text, string speaker, System.Action onComplete, AudioClip voice)
        {
            ShowDialogue(text, speaker, onComplete, null, null, voice);
        }

        /// <summary>
        /// Show dialogue dengan trigger kata opsional (dipakai Scene 4 "bledag!").
        /// Trigger fire sekali saat teks terketik berakhiran triggerWord (case-insensitive).
        /// voice: clip audio per-baris (null = pakai defaultVoiceClip).
        /// </summary>
        public void ShowDialogue(string text, string speaker, System.Action onComplete, string triggerWord, System.Action onTrigger, AudioClip voice = null)
        {
            currentSpeaker = speaker ?? "";

            // Nyalakan + isi label nama speaker dinamis per baris
            // (ShowStandby mematikan label, jadi WAJIB dihidupkan lagi di sini).
            if (speakerNameText != null)
            {
                bool hasName = !string.IsNullOrEmpty(speaker);
                speakerNameText.gameObject.SetActive(hasName);
                speakerNameText.text = hasName ? speaker : "";
            }

            currentTriggerWord = triggerWord;
            triggerCallback = onTrigger;
            triggerFired = false;

            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            EnsureVoiceSource();
            SetVisualActive(true);
            if (fullScreenTapButton != null)
                fullScreenTapButton.gameObject.SetActive(true);

            currentFullText = text;

            UpdateBoxPointer();
            
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            HideContinueIndicator();

            AudioClip resolvedVoice = voice != null ? voice : defaultVoiceClip;
            typingCoroutine = StartCoroutine(TypeText(text, onComplete, resolvedVoice));
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

            StopVoice();
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

            StopVoice();
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
            // Skip aman (tidak StopCoroutine — onComplete pemilik coroutine tetap hidup):
            // teks langsung penuh + audio langsung berhenti (dialog berhenti = sfx ikut berhenti).
            if (isTyping)
            {
                if (canSkipTyping) skipRequested = true;
                return;
            }
            if (!waitingForInput)
                return;
            waitingForInput = false;
        }

        private IEnumerator TypeText(string text, System.Action onComplete, AudioClip voice)
        {
            isTyping = true;
            waitingForInput = false;
            skipRequested = false;

            // Audio-driven sync (opsional): ngetik selesai (hampir) bareng audio selesai.
            // Default: kecepatan mengikuti typingSpeed dari Inspector.
            float charDelay = typingSpeed;
            if (syncTypingToVoice && voice != null && text.Length > 0)
            {
                charDelay = voice.length / text.Length;
                if (voiceMinCharDelay > 0f && charDelay < voiceMinCharDelay)
                    charDelay = voiceMinCharDelay;
                if (voiceMaxCharDelay > 0f && charDelay > voiceMaxCharDelay)
                    charDelay = voiceMaxCharDelay;
            }

            if (dialogueText != null)
                dialogueText.text = "";

            // Play voice sekali di awal; indikator "tap" muncul setelah teks + audio dua-duanya selesai.
            float voiceLength = 0f;
            float voiceStartTime = Time.time;
            if (voice != null)
            {
                EnsureVoiceSource();
                if (voiceSource != null)
                {
                    voiceSource.volume = voiceVolume;
                    voiceSource.clip = voice;
                    voiceSource.Play();
                    voiceLength = voice.length;
                }
            }

            // Type text with typewriter effect
            foreach (char c in text)
            {
                if (dialogueText != null)
                    dialogueText.text += c;

                CheckTrigger();
                if (skipRequested) break;
                yield return new WaitForSeconds(charDelay);
            }

            // Skip: isi sisa teks langsung + audio langsung berhenti (dialog berhenti = sfx berhenti).
            if (skipRequested)
            {
                if (dialogueText != null)
                    dialogueText.text = text;
                CheckTrigger();
                isTyping = false;
                StopVoice();
            }
            else
            {
                isTyping = false;

                // Dialog beres = sfx beres: voice langsung di-Stop tepat saat ngetik kelar.
                StopVoice();
            }

            waitingForInput = true;

            // Show continue indicator after typing + voice finish
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
