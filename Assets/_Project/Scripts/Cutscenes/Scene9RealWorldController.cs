using UnityEngine;
using System.Collections;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 9 — Rimba Terbangun (PLAN E9): state "RealWorld" di Ch1_Hutan.unity.
    /// Beat 1: mulai dari layar PUTIH -> fade out, hutan nyata terungkap.
    /// Beat 2: wide-shot dingin, Rimba sendirian sandar pohon.
    /// Beat 3: swap meraba pinggang (tangan meraba, kosong) + jeda.
    /// Beat 4: Rimba bergumam "...Moal kataekan." (baru sadar itu mimpi).
    /// Beat 5: swap berdiri -> freeze End of Chapter 1 (trigger Chapter 2 nanti).
    /// Framing WAJIB beda dari two-shot hangat mimpi: kamera di-reposition via kode.
    /// Audio SFX whiteout/transisi (sfx_whiteout.wav, sfx_transisi_hutan_nyata.wav) backlog —
    /// sementara visual saja + ambience reuse.
    /// </summary>
    public class Scene9RealWorldController : MonoBehaviour
    {
        [Header("Sprite Set")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;

        [Header("Shot - Hutan Nyata (milik Scene 9)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgHutanNyata;
        [Tooltip("Anchor di atas kepala Rimba untuk bubble dialog (kompromi 3 pose). Auto-find bila null.")]
        [SerializeField] private Transform headAnchor;

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
        [SerializeField] private float merabaHoldSeconds = 1.5f;
        [SerializeField] private float dialogueTimeoutSeconds = 15f;

        private readonly DialogueLine[] gumamLine = new[]
        {
            new DialogueLine("Rimba", "...Moal kataekan.", "...Tidak akan sampai."),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene9);
            SetBeat(Ch1Beat.S9_RimbaBangun);
            if (shotRoot != null) shotRoot.SetActive(true);
            ApplyColdCamera();
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.sandarPohon : null, "RimbaSpriteSet.sandarPohon");
            if (bgRenderer != null && bgHutanNyata != null) bgRenderer.sprite = bgHutanNyata;
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
            if (headAnchor == null && shotRimba != null)
            {
                var t = shotRimba.transform.Find("HeadAnchor");
                if (t != null) headAnchor = t;
            }
            if (headAnchor == null) Debug.LogWarning("[Scene9RealWorld] headAnchor belum di-assign/ketemu!");
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
            Debug.Log("[Scene9RealWorld] Beat 1 - whiteout fade, hutan nyata terungkap");
            yield return StartCoroutine(FadeWhite(false, whiteFadeSeconds));

            Debug.Log("[Scene9RealWorld] Beat 2 - wide-shot dingin, Rimba sandar pohon");
            yield return new WaitForSeconds(1f);

            Debug.Log("[Scene9RealWorld] Beat 3 - meraba pinggang, kosong");
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.merabaPinggang : null, "RimbaSpriteSet.merabaPinggang");
            yield return new WaitForSeconds(merabaHoldSeconds);

            Debug.Log("[Scene9RealWorld] Beat 4 - gumam Moal kataekan");
            yield return StartCoroutine(PlayDialogueSequence(gumamLine, null));

            Debug.Log("[Scene9RealWorld] Beat 5 - berdiri, End of Chapter 1");
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.berdiri : null, "RimbaSpriteSet.berdiri");
            yield return new WaitForSeconds(1f);
            Debug.Log("[Scene9RealWorld] End of Chapter 1 — freeze sampai Chapter 2 dibangun.");
        }

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shotRimba == null) return;
            if (sprite != null) shotRimba.sprite = sprite;
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
                Transform anchor = headAnchor != null ? headAnchor : shotRimba.transform;
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
