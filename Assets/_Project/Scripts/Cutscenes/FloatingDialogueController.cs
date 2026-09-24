using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Floating dialogue bubble ala A Space for the Unbound.
    /// Bubble mengikuti HeadAnchor karakter (di atas kepala), dipakai Scene 1-8.
    ///
    /// LAYOUT 100% USER-ATUR DI INSPECTOR:
    /// - HeadAnchor posisi (drag di Scene View)
    /// - Offset bubble per speaker, offset BoxPointer per speaker
    /// - Font size / padding / ukuran Background diatur manual di RectTransform & TMP
    /// Script ini TIDAK hardcode layout, hanya follow + flip + typewriter.
    /// </summary>
    public class FloatingDialogueController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject dialogueBoxRoot;
        [SerializeField] private RectTransform bubbleRect;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera followCamera;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI translationText;
        [SerializeField] private Image dialogueBoxImage;
        [SerializeField] private RectTransform boxPointerRect;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button fullScreenTapButton;

        [Header("Layout - USER ATUR (pixel offset dari kepala)")]
        [Tooltip("Offset bubble saat speaker Rimba. X: kanan+, Y: atas+. Atur manual sampai enak.")]
        [SerializeField] private Vector2 offsetRimba = new Vector2(120f, 180f);
        [Tooltip("Offset bubble saat speaker Abah.")]
        [SerializeField] private Vector2 offsetAbah = new Vector2(-120f, 180f);
        [Tooltip("Offset BoxPointer (ekor) saat Rimba bicara, relatif ke bubble.")]
        [SerializeField] private Vector2 pointerOffsetRimba = new Vector2(-40f, -10f);
        [Tooltip("Offset BoxPointer saat Abah bicara.")]
        [SerializeField] private Vector2 pointerOffsetAbah = new Vector2(40f, -10f);
        [Tooltip("Kalau true, BoxPointer flip X otomatis mengikuti sisi bubble vs kepala.")]
        [SerializeField] private bool autoFlipPointer = true;

        [Header("Typewriter")]
        [SerializeField] private float typingSpeed = 0.03f;
        [SerializeField] private bool canSkipTyping = true;
        [SerializeField] private float continueBounceSpeed = 2f;

        private Transform currentAnchor;
        private string currentSpeaker = "";
        private Vector2 currentOffset;
        private Vector2 currentPointerOffset;
        private Coroutine typingCoroutine;
        private Coroutine continueAnimCoroutine;
        private Coroutine followCoroutine;
        private bool isTyping;
        private bool waitingForInput;
        private string currentFullSunda = "";
        private string currentFullIndo = "";

        public bool IsActive => dialogueBoxRoot != null && dialogueBoxRoot.activeSelf;
        public bool IsWaitingForInput => waitingForInput;
        public string CurrentSpeaker => currentSpeaker;

        private void Awake()
        {
            EnsureVisualRoot();

            if (bubbleRect == null && dialogueBoxRoot != null)
                bubbleRect = dialogueBoxRoot.GetComponent<RectTransform>();

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();

            if (followCamera == null)
                followCamera = Camera.main;

            SetVisualActive(false);

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (fullScreenTapButton != null)
            {
                fullScreenTapButton.onClick.AddListener(OnTap);
                // Mati di luar dialog biar tidak menelan tap tombol lain (misal Beat 5).
                fullScreenTapButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Guard: jangan pernah nonaktifkan host script (coroutine mati).
        /// Kalau dialogueBoxRoot == gameObject sendiri, redirect ke child "Background".
        /// </summary>
        private void EnsureVisualRoot()
        {
            if (dialogueBoxRoot != null && dialogueBoxRoot != gameObject)
                return;

            Transform bg = transform.Find("Background");
            if (bg != null)
            {
                if (dialogueBoxRoot == gameObject)
                    Debug.LogWarning("[FloatingDialogue] dialogueBoxRoot menunjuk host sendiri — auto-redirect ke child 'Background' biar coroutine tidak mati. Update Inspector ke Background.");
                dialogueBoxRoot = bg.gameObject;
            }
            else if (dialogueBoxRoot == gameObject)
            {
                Debug.LogError("[FloatingDialogue] dialogueBoxRoot == host! Pindahkan ke child visual (Background) agar Show() tidak error coroutine.");
            }
        }

        private void SetVisualActive(bool active)
        {
            if (dialogueBoxRoot == null)
                return;
            if (dialogueBoxRoot == gameObject)
            {
                Debug.LogError("[FloatingDialogue] Tolak SetActive ke host sendiri (bikin coroutine mati). Assign dialogueBoxRoot ke child Background.");
                return;
            }
            dialogueBoxRoot.SetActive(active);
        }

        private void OnDestroy()
        {
            if (fullScreenTapButton != null)
                fullScreenTapButton.onClick.RemoveListener(OnTap);
        }

        /// <summary>
        /// Show bubble untuk speaker tertentu. Anchor = HeadAnchor_* di atas kepala.
        /// Indo boleh kosong (misal reaksi visual).
        /// </summary>
        public void Show(string speaker, string sunda, string indo, Transform anchor, Action onComplete = null)
        {
            currentSpeaker = speaker ?? "";
            currentAnchor = anchor;
            currentFullSunda = sunda ?? "";
            currentFullIndo = indo ?? "";

            bool isRimba = currentSpeaker.ToLower().Contains("rimba");
            currentOffset = isRimba ? offsetRimba : offsetAbah;
            currentPointerOffset = isRimba ? pointerOffsetRimba : pointerOffsetAbah;

            EnsureVisualRoot();
            // Host harus active sebelum StartCoroutine, visual boleh baru nyala
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError("[FloatingDialogue] Host DialogueBox inactive! Aktifkan host, hanya Background yang boleh di-toggle.");
                gameObject.SetActive(true);
            }
            SetVisualActive(true);

            // Nyalakan tap fullscreen hanya selama dialog tampil
            if (fullScreenTapButton != null)
                fullScreenTapButton.gameObject.SetActive(true);

            if (followCamera == null)
                followCamera = Camera.main;

            // Posisikan langsung sekali, lalu follow tiap frame
            UpdateBubblePosition();

            if (followCoroutine != null)
                StopCoroutine(followCoroutine);
            followCoroutine = StartCoroutine(FollowAnchor());

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            HideContinueIndicator();

            typingCoroutine = StartCoroutine(TypeText(currentFullSunda, currentFullIndo, onComplete));
        }

        /// <summary>Backward-compat: tanpa speaker/anchor (bubble diam di tempat terakhir).</summary>
        public void ShowDialogue(string text, Action onComplete = null)
        {
            Show("", text, "", null, onComplete);
        }

        public void Hide()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
                followCoroutine = null;
            }
            HideContinueIndicator();
            isTyping = false;
            waitingForInput = false;
            currentAnchor = null;
            SetVisualActive(false);
            if (fullScreenTapButton != null)
                fullScreenTapButton.gameObject.SetActive(false);
        }

        public void OnTap()
        {
            if (isTyping && canSkipTyping)
            {
                if (typingCoroutine != null)
                    StopCoroutine(typingCoroutine);
                if (dialogueText != null)
                    dialogueText.text = currentFullSunda;
                if (translationText != null)
                    translationText.text = currentFullIndo;
                isTyping = false;
                waitingForInput = true;
                ShowContinueIndicator();
            }
            else if (waitingForInput)
            {
                waitingForInput = false;
            }
        }

        private IEnumerator FollowAnchor()
        {
            while (true)
            {
                UpdateBubblePosition();
                yield return null;
            }
        }

        private void UpdateBubblePosition()
        {
            if (bubbleRect == null || parentCanvas == null)
                return;
            if (currentAnchor == null || followCamera == null)
                return;

            Vector3 screen = followCamera.WorldToScreenPoint(currentAnchor.position);
            if (screen.z < 0f)
                return;

            screen.x += currentOffset.x;
            screen.y += currentOffset.y;

            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 local))
                bubbleRect.anchoredPosition = local;

            UpdatePointerFlip(screen);
        }

        private void UpdatePointerFlip(Vector3 bubbleScreenPos)
        {
            if (boxPointerRect == null || currentAnchor == null || followCamera == null)
                return;

            boxPointerRect.anchoredPosition = currentPointerOffset;

            if (!autoFlipPointer)
                return;

            Vector3 anchorScreen = followCamera.WorldToScreenPoint(currentAnchor.position);
            float side = Mathf.Sign(bubbleScreenPos.x - anchorScreen.x);
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            Vector3 s = boxPointerRect.localScale;
            s.x = Mathf.Abs(s.x) * (side >= 0f ? -1f : 1f);
            boxPointerRect.localScale = s;
        }

        private IEnumerator TypeText(string sunda, string indo, Action onComplete)
        {
            isTyping = true;
            waitingForInput = false;

            if (dialogueText != null)
                dialogueText.text = "";
            if (translationText != null)
                translationText.text = "";

            foreach (char c in sunda)
            {
                if (dialogueText != null)
                    dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            if (translationText != null)
                translationText.text = indo;

            isTyping = false;
            waitingForInput = true;
            ShowContinueIndicator();

            yield return new WaitUntil(() => !waitingForInput);

            HideContinueIndicator();
            onComplete?.Invoke();
            typingCoroutine = null;
        }

        private void ShowContinueIndicator()
        {
            if (continueIndicator == null) return;
            continueIndicator.SetActive(true);
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
                continueIndicator.SetActive(false);
        }

        private IEnumerator AnimateContinueIndicator()
        {
            if (continueIndicator == null) yield break;
            RectTransform rt = continueIndicator.GetComponent<RectTransform>();
            if (rt == null) yield break;
            Vector3 baseScale = rt.localScale;
            while (true)
            {
                float k = 1f + Mathf.PingPong(Time.time * continueBounceSpeed, 0.2f);
                rt.localScale = baseScale * k;
                yield return null;
            }
        }
    }
}
