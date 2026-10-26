using UnityEngine;
using System.Collections;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 9 — Rimba Terbangun (PLAN E9, rombak 2 shot): state "RealWorld" di Ch1_Hutan.unity.
    /// Beat 1: mulai dari layar PUTIH -> fade out, hutan terungkap (bg authored: forest_night).
    /// Beat 2: Shot 1 — flipbook bangun: 5 frame (sleep -> check_waist), tanpa dialog.
    ///         Layout pos/scale = transform authored di scene (inspector).
    /// Beat 3: Cut ke Shot 2 — shot 1 mati, shot 2 hidup (bg forest_wide + karakter ter-crop
    ///         stand_back, layout authored di scene), jeda kecil, lalu Rimba bergumam
    ///         "...Moal kataekan." (baru sadar itu mimpi) -> freeze End of Chapter 1.
    /// Framing WAJIB beda dari two-shot hangat mimpi: kamera di-reposition via kode.
    /// Audio SFX whiteout/transisi (sfx_whiteout.wav, sfx_transisi_hutan_nyata.wav) backlog —
    /// sementara visual saja + ambience reuse.
    /// </summary>
    public class Scene9RealWorldController : MonoBehaviour
    {
        [Header("Sprite Set")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;

        [Header("Shot 1 - Flipbook bangun (layout dari inspector)")]
        [SerializeField] private GameObject shot1Root;
        [SerializeField] private SpriteRenderer shot1Rimba;

        [Header("Shot 2 - Fullscreen stand_back (layout dari inspector)")]
        [SerializeField] private GameObject shot2Root;
        [SerializeField] private SpriteRenderer shot2Rimba;
        [Tooltip("Anchor kepala Rimba stand_back untuk bubble gumam. Auto-find bila null.")]
        [SerializeField] private Transform shot2HeadAnchor;
        [Tooltip("Jeda setelah cut ke shot 2 sebelum bubble gumam muncul.")]
        [SerializeField] private float dialogueDelayAfterCut = 2f;

        [Header("Black Overlay (cut Shot1 -> Shot2)")]
        [Tooltip("Canvas hitam fullscreen untuk transisi fade-hitam antar shot.")]
        [SerializeField] private GameObject blackOverlay;
        [Tooltip("Durasi shot 1 memudar ke hitam.")]
        [SerializeField] private float blackFadeInSeconds = 0.3f;
        [Tooltip("Durasi hitam penuh sebelum shot 2 terungkap.")]
        [SerializeField] private float blackHoldSeconds = 0.5f;
        [Tooltip("Durasi fade keluar dari hitam ke shot 2.")]
        [SerializeField] private float blackFadeOutSeconds = 0.4f;

        [Header("UI Components (milik Scene 9)")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject whiteOverlay;

        [Header("Kamera Dingin (beda framing dari mimpi)")]
        [SerializeField] private Camera mainCamera;
        [Tooltip("Posisi kamera wide-shot dingin Scene 9.")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 0f, -10f);
        [Tooltip("Orthographic size wide-shot dingin Scene 9 (4.7 = pas BG 1672x940).")]
        [SerializeField] private float cameraOrthoSize = 4.7f;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [Tooltip("Placeholder: reuse ambience. Swap ke audio hutan nyata kalau sudah ada.")]
        [SerializeField] private AudioClip ambienceNyata;

        [Header("Timing")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float whiteFadeSeconds = 2f;
        [Tooltip("Detik per frame flipbook (0.2 x 5 frame = 1.0s).")]
        [SerializeField] private float flipbookFrameSeconds = 0.2f;
        [SerializeField] private float dialogueTimeoutSeconds = 15f;

        private readonly DialogueLine[] gumamLine = new[]
        {
            new DialogueLine("Rimba", "...Moal kataekan.", "...Tidak akan sampai."),
        };

        private void Start()
        {
            // Whitescreen WAJIB tampil dari frame pertama — nyala sebelum aksi apa pun.
            if (whiteOverlay != null)
            {
                whiteOverlay.SetActive(true);
                var whiteImage = whiteOverlay.GetComponent<UnityEngine.UI.Image>();
                if (whiteImage != null)
                {
                    var whiteC = whiteImage.color;
                    whiteC.a = 1f;
                    whiteImage.color = whiteC;
                }
                var whiteCg = whiteOverlay.GetComponent<CanvasGroup>();
                if (whiteCg != null) whiteCg.alpha = 1f;
            }
            StoryState.EnsureExists();
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene9);
            SetBeat(Ch1Beat.S9_RimbaBangun);
            // Mutual eksklusif: shot 1 hidup, shot 2 mati sampai cut Beat 3.
            if (shot1Root != null) shot1Root.SetActive(true);
            if (shot2Root != null) shot2Root.SetActive(false);
            ApplyColdCamera();
            // Layout shot 1 (pos/scale Rimba) = transform authored di scene (inspector).
            if (rimbaSpriteSet != null && rimbaSpriteSet.s9WakeSequence != null && rimbaSpriteSet.s9WakeSequence.Length > 0)
                ApplyRimbaSprite(rimbaSpriteSet.s9WakeSequence[0], "RimbaSpriteSet.s9WakeSequence[0]");
            else
                Debug.LogWarning("[Scene9RealWorld] s9WakeSequence kosong — frame bangun belum di-wire!");
            if (audioManager != null && ambienceNyata != null)
                audioManager.PlayAmbient(ambienceNyata, 0.5f);
            // Bubble tersimpan aktif di scene — sembunyikan sampai dialog pertama.
            if (floatingDialogue != null) floatingDialogue.Hide();
            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(ExecuteBeats())));
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene9RealWorld] rimbaSpriteSet belum di-assign!");
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
            if (shot2HeadAnchor == null && shot2Rimba != null)
            {
                var t = shot2Rimba.transform.Find("HeadAnchor");
                if (t != null) shot2HeadAnchor = t;
            }
            if (shot2HeadAnchor == null) Debug.LogWarning("[Scene9RealWorld] shot2HeadAnchor belum di-assign/ketemu!");
            if (shot1Rimba == null || shot2Rimba == null)
                Debug.LogWarning("[Scene9RealWorld] Renderer shot 1/2 belum di-wire!");
        }
        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        private void ApplyColdCamera()
        {
            if (mainCamera == null) return;
            savedCamPos = mainCamera.transform.position;
            savedOrthoSize = mainCamera.orthographicSize;
            mainCamera.transform.position = cameraPosition;
            if (mainCamera.orthographic)
                mainCamera.orthographicSize = cameraOrthoSize;
        }

        private Vector3 savedCamPos;
        private float savedOrthoSize = 5f;

        private void OnDestroy()
        {
            // Kembalikan framing semula supaya test Scene 5 sesudah Scene 9 dalam satu sesi tetap benar.
            if (mainCamera != null)
            {
                mainCamera.transform.position = savedCamPos;
                if (mainCamera.orthographic)
                    mainCamera.orthographicSize = savedOrthoSize;
            }
        }

        private IEnumerator ExecuteBeats()
        {
            Debug.Log("[Scene9RealWorld] Beat 1 - whiteout fade, hutan terungkap");
            yield return StartCoroutine(FadeWhite(false, whiteFadeSeconds));

            Debug.Log("[Scene9RealWorld] Beat 2 - Shot 1: flipbook bangun (5 frame, senyap, layout dari scene)");
            yield return StartCoroutine(PlayWakeFlipbook());

            Debug.Log("[Scene9RealWorld] Beat 3 - Cut fade-hitam, shot 2 wide terungkap, gumam Moal kataekan");
            // Fade ke hitam di atas shot 1, swap shot di balik hitam, lalu fade keluar mengungkap shot 2.
            yield return StartCoroutine(FadeBlack(true, blackFadeInSeconds));
            if (shot1Root != null) shot1Root.SetActive(false);
            if (shot2Root != null) shot2Root.SetActive(true);
            yield return new WaitForSeconds(blackHoldSeconds);
            yield return StartCoroutine(FadeBlack(false, blackFadeOutSeconds));
            yield return new WaitForSeconds(dialogueDelayAfterCut);
            yield return StartCoroutine(PlayDialogueSequence(gumamLine, null));

            yield return new WaitForSeconds(1f);
            Debug.Log("[Scene9RealWorld] End of Chapter 1 — freeze sampai Chapter 2 dibangun.");
        }

        private IEnumerator PlayWakeFlipbook()
        {
            if (rimbaSpriteSet == null || rimbaSpriteSet.s9WakeSequence == null || rimbaSpriteSet.s9WakeSequence.Length == 0)
            {
                Debug.LogWarning("[Scene9RealWorld] s9WakeSequence kosong — flipbook dilewati.");
                yield break;
            }
            foreach (var frame in rimbaSpriteSet.s9WakeSequence)
            {
                if (frame != null && shot1Rimba != null) shot1Rimba.sprite = frame;
                yield return new WaitForSeconds(flipbookFrameSeconds);
            }
        }

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shot1Rimba == null) return;
            if (sprite != null) shot1Rimba.sprite = sprite;
            else Debug.LogWarning($"[Scene9RealWorld] {fieldName} belum di-assign!");
        }

        private IEnumerator PlayDialogueSequence(DialogueLine[] lines, System.Action onComplete)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.textSunda))
                {
                    yield return new WaitForSeconds(0.8f);
                    continue;
                }
                bool finished = false;
                Transform anchor = shot2HeadAnchor != null ? shot2HeadAnchor : (shot2Rimba != null ? shot2Rimba.transform : null);
                if (floatingDialogue != null && anchor != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene9RealWorld] Tidak ada dialogue display! Skip: " + line.textSunda);
                    finished = true;
                }
                yield return StartCoroutine(WaitDialogueOrTimeout(() => finished, dialogueTimeoutSeconds));
                yield return new WaitForSeconds(0.2f);
            }
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            onComplete?.Invoke();
        }

        private IEnumerator WaitDialogueOrTimeout(System.Func<bool> isDone, float timeout)
        {
            float elapsed = 0f;
            while (!isDone() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!isDone())
                Debug.Log("[Scene9RealWorld] Auto-lanjut (timeout, player tidak tap).");
        }

        private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private IEnumerator FadeWhite(bool fadeIn, float duration)
        {
            if (whiteOverlay == null) yield break;
            var canvasGroup = whiteOverlay.GetComponent<CanvasGroup>();
            var image = whiteOverlay.GetComponent<UnityEngine.UI.Image>();
            if (canvasGroup == null && image == null)
            {
                Debug.LogWarning("[Scene9RealWorld] whiteOverlay tidak punya CanvasGroup/Image — fade dilewati.");
                yield break;
            }
            whiteOverlay.SetActive(true);
            float start = fadeIn ? 0f : 1f;
            float end = fadeIn ? 1f : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, end, Mathf.Clamp01(t / Mathf.Max(0.01f, duration)));
                if (canvasGroup != null) canvasGroup.alpha = a;
                if (image != null)
                {
                    var c = image.color;
                    c.a = a;
                    image.color = c;
                }
                yield return null;
            }
        }

        private IEnumerator FadeBlack(bool fadeIn, float duration)
        {
            if (blackOverlay == null)
            {
                Debug.LogWarning("[Scene9RealWorld] blackOverlay belum di-wire — transisi hitam dilewati (hard cut).");
                yield break;
            }
            var canvasGroup = blackOverlay.GetComponent<CanvasGroup>();
            var image = blackOverlay.GetComponent<UnityEngine.UI.Image>();
            if (canvasGroup == null && image == null)
            {
                Debug.LogWarning("[Scene9RealWorld] blackOverlay tidak punya CanvasGroup/Image — fade dilewati.");
                yield break;
            }
            blackOverlay.SetActive(true);
            float start = fadeIn ? 0f : 1f;
            float end = fadeIn ? 1f : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, end, Mathf.Clamp01(t / Mathf.Max(0.01f, duration)));
                if (canvasGroup != null) canvasGroup.alpha = a;
                if (image != null)
                {
                    var c = image.color;
                    c.a = a;
                    image.color = c;
                }
                yield return null;
            }
        }

        [System.Serializable]
        private struct DialogueLine
        {
            public string speaker;
            public string textSunda;
            public string textIndo;
            public DialogueLine(string speaker, string sunda, string indo)
            {
                this.speaker = speaker;
                this.textSunda = sunda;
                this.textIndo = indo;
            }
        }
    }
}
