using UnityEngine;
using System.Collections;
using TMPro;
using MainKarinding.Core;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 4 — Filosofi Getaran dan Kehidupan (PLAN E4).
    /// Scene file: Ch1_TerasRumahKebun.unity, framing SAMA PERSIS Scene 3 (tidak ada
    /// perpindahan kamera). Dialogue-only: 1 baris panjang Abah + trigger tepat di
    /// kata "bledag!" (VFX flash + SFX boom + dropout ambient), lalu fade to black +
    /// title card "HARI KEDUA" menuju Scene 5.
    ///
    /// Semua aset reuse Scene 2/3 (BG/sprite/ambience); satu-satunya file baru yang
    /// dipakai: Audio/SFX/Transitions/sfx_bigbang_boom.wav.
    /// </summary>
    public class Scene4CutsceneController : MonoBehaviour
    {
        private const string TriggerWord = "bledag!";

        [Header("Sprite Sets (reuse Scene 2/3)")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Scene 4 Overrides (hanya Scene 4, tidak ganggu Scene 2/3)")]
        [Tooltip("Kalau diisi, dipakai sebagai pengganti rimbaSpriteSet.duduk. Kosong = fallback ke duduk.")]
        [SerializeField] private Sprite overrideRimba;
        [Tooltip("Kalau diisi, dipakai sebagai pengganti abahSpriteSet.duduk. Kosong = fallback ke duduk.")]
        [SerializeField] private Sprite overrideAbah;

        [Header("Shot - Teras Siang (salinan milik Scene 4)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSiang;

        [Header("UI Components")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;
        [Tooltip("Image putih fullscreen di lapisan BAWAH dialog (VFX flash tidak menutup bubble).")]
        [SerializeField] private UnityEngine.UI.Image flashImage;
        [Tooltip("Teks 'HARI KEDUA' di bawah Blackscreen, hidden default.")]
        [SerializeField] private TextMeshProUGUI titleCardText;

        [Header("Head Anchors")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceSiang;
        [SerializeField] private AudioClip sfxBigBang;

        [Header("Scene 4 Settings")]
        [SerializeField] private float openingDelay = 1f;
        [Tooltip("Paragraf panjang (~450 char); 12 dtk tidak cukup sampai 'bledag!' (~85% teks).")]
        [SerializeField] private float dialogueTimeoutSeconds = 30f;
        [Tooltip("Durasi fade ke hitam di akhir scene.")]
        [SerializeField] private float endFadeSeconds = 1f;
        [Tooltip("Lama title card 'HARI KEDUA' tampil.")]
                [SerializeField] private float titleCardSeconds = 2.2f;
        [Tooltip("Durasi reveal dari hitam di awal scene (pola Scene 3).")]
                [SerializeField] private float revealSeconds = 0.6f;

        [Header("Handoff Scene 5")]
        [Tooltip("Drag Scene5_Cutscene. Diaktifkan otomatis setelah fade + title card (EntryPoint Scene5 diset lebih dulu).")]
        [SerializeField] private GameObject scene5Root;
        [Tooltip("UI milik scene ini yang disembunyikan saat handoff ke Scene 5 (pola S2->S3).")]
        [SerializeField] private GameObject[] uiRootsToHideOnHandoff = new GameObject[0];

        private bool cutsceneActive = false;
        private bool bledagFired = false;

        // ---- Dialog PLAN E4, dipecah 3 bubble (~150 karakter/bubble, muat di box
        // 600x150 font 24). Sunda saja, tanpa terjemahan (keputusan 26 Sep 2026).
        // Teks verbatim, trigger "bledag!" di segmen 3. ----

        private readonly DialogueLine[] beatDialogue = new[]
        {
            new DialogueLine(
                "Abah",
                "Jadi orang Sunda teh percanten lamun alam semesta ieu teh ka bentuk tina dentuman tarik, nyaeta Big Bang. Getaran jeung dengung teh dianggap awal tina kahirupan, Ba.",
                ""),
            new DialogueLine(
                "Abah",
                "Jadi sateuacan aya na kahirupan teh aya nu nami na Alam Kosong, awang uwung tong enong margana enong, kosong suwung eweuh nanaon didinya.",
                ""),
            new DialogueLine(
                "Abah",
                "Alam ieu ngadadak endeur jeung ngalahirkeun sora dengung, laksaketi mangrewu sora tawon Ngung… bledag! Tah tina getaran jeung dengung eta lah lahir kahirupan.",
                ""),
        };

        private void Start()
        {
            StoryState.EnsureExists();
                        if (blackscreenOverlay != null)
            {
                // Mulai OPAQUE: handoff dari Scene 3 terjadi di balik hitam, lalu reveal.
                blackscreenOverlay.SetActive(true);
                var bi = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
                if (bi != null)
                {
                    var bc = bi.color;
                    bc.a = 1f;
                    bi.color = bc;
                }
            }
            if (titleCardText != null) titleCardText.gameObject.SetActive(false);
            if (flashImage != null)
            {
                flashImage.raycastTarget = false;
                var fc = flashImage.color;
                fc.a = 0f;
                flashImage.color = fc;
            }
            AutoFind();

            // Tandai entry point supaya gate Scene 1/2/3 tetap menutup.
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene4);

            cutsceneActive = true;
            StartCutscene();
        }

        public void StartCutscene()
        {
            ExecuteBeat();
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene4Cutscene] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene4Cutscene] abahSpriteSet belum di-assign!");

            if (headAnchorRimba == null)
            {
                var t = transform.Find("Shot_TerasSiang/Char_Rimba/HeadAnchor");
                if (t != null) headAnchorRimba = t;
            }
            if (headAnchorAbah == null)
            {
                var t = transform.Find("Shot_TerasSiang/Char_Abah/HeadAnchor");
                if (t != null) headAnchorAbah = t;
            }
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene4Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        // Beat tunggal — dialog filosofi + trigger bledag di tengah
        private void ExecuteBeat()
        {
            Debug.Log("[Scene4Cutscene] Beat - S4_FilosofiBigBang");
            SetBeat(Ch1Beat.S4_FilosofiBigBang);

            if (shotRoot != null) shotRoot.SetActive(true);
            ApplyRimbaSprite(overrideRimba != null ? overrideRimba : (rimbaSpriteSet != null ? rimbaSpriteSet.duduk : null), "Rimba(overrideScene4/duduk)");
            ApplyAbahSprite(overrideAbah != null ? overrideAbah : (abahSpriteSet != null ? abahSpriteSet.duduk : null), "Abah(overrideScene4/duduk)");
            if (bgRenderer != null && bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);

                        StartCoroutine(RevealThen(openingDelay, () => StartCoroutine(PlayDialogueSequence(beatDialogue, () => StartCoroutine(EndSceneRoutine())))));
        }

        // Trigger tepat di kata "bledag!" — VFX/SFX/dropout, dialog box tidak terganggu
        private void OnBledag()
        {
            if (bledagFired) return;
            bledagFired = true;
            Debug.Log("[Scene4Cutscene] BLEEDAG! — flash + boom + dropout");

            if (audioManager != null)
            {
                if (sfxBigBang != null) audioManager.PlaySFX(sfxBigBang, 1f);
                else Debug.LogWarning("[Scene4Cutscene] sfxBigBang belum di-assign! Assign sfx_bigbang_boom.wav");
                audioManager.DuckAmbient(0.08f, 0.08f, 0.4f, 1.2f);
            }
            if (flashImage != null) StartCoroutine(FlashRoutine());
            else Debug.LogWarning("[Scene4Cutscene] flashImage belum di-assign! VFX flash dilewati.");
        }

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shotRimba == null) return;
            if (sprite != null) shotRimba.sprite = sprite;
            else Debug.LogWarning($"[Scene4Cutscene] {fieldName} belum di-assign!");
        }

        private void ApplyAbahSprite(Sprite sprite, string fieldName)
        {
            if (shotAbah == null) return;
            if (sprite != null) shotAbah.sprite = sprite;
            else Debug.LogWarning($"[Scene4Cutscene] {fieldName} belum di-assign!");
        }

        // Fade to black + title card "HARI KEDUA" menuju Scene 5
        private IEnumerator EndSceneRoutine()
        {
            cutsceneActive = false;
            Debug.Log("[Scene4Cutscene] Dialog selesai - fade + HARI KEDUA");

            yield return StartCoroutine(FadeBlackscreen(true, endFadeSeconds));

            if (titleCardText != null)
            {
                titleCardText.gameObject.SetActive(true);
                titleCardText.text = "HARI KEDUA";
            }
            yield return new WaitForSeconds(titleCardSeconds);
            if (titleCardText != null) titleCardText.gameObject.SetActive(false);

            // Lanjut ke Scene 5 hanya kalau StoryState masih berada di Scene 4.
            if (MainKarinding.Core.StoryState.Instance != null
                && MainKarinding.Core.StoryState.Instance.EntryPoint == MainKarinding.Core.Ch1EntryPoint.Scene4)
            {
                // EntryPoint HARUS diset SEBELUM Scene 5 aktif - SceneEntryGate Scene 5
                // baru jalan saat aktivasi dan akan menutup lagi kalau belum Scene5.
                MainKarinding.Core.StoryState.Instance.SetEntryPoint(MainKarinding.Core.Ch1EntryPoint.Scene5);

                if (scene5Root != null)
                    scene5Root.SetActive(true);
                else
                    Debug.LogError("[Scene4Cutscene] scene5Root belum di-assign! Drag Scene5_Cutscene di Inspector.");

                // Dunia Scene 4 dimatikan di balik hitam (pola S2->S3).
                if (shotRoot != null) shotRoot.SetActive(false);
                if (uiRootsToHideOnHandoff != null)
                {
                    for (int i = 0; i < uiRootsToHideOnHandoff.Length; i++)
                        if (uiRootsToHideOnHandoff[i] != null)
                            uiRootsToHideOnHandoff[i].SetActive(false);
                }
                Debug.Log("[Scene4Cutscene] Scene 4 selesai + blackscreen - lanjut Scene 5 (S5_AjakHutan).");
            }
            else
            {
                Debug.Log("[Scene4Cutscene] StoryState bukan Scene4 - handoff ke Scene 5 dilewati.");
            }
        }

        #region Helpers (pola Scene 1–3)

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
                if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true, TriggerWord, OnBledag);
                else if (floatingDialogue != null)
                {
                    Transform anchor = ResolveAnchor(line.speaker);
                    if (anchor != null)
                        floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true, TriggerWord, OnBledag);
                    else
                        finished = true;
                }
                else
                {
                    Debug.LogWarning("[Scene4Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
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
                Debug.Log("[Scene4Cutscene] Auto-lanjut (timeout, player tidak tap).");
        }

                private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        // Reveal dari hitam (pola Scene 3): blackscreen opaque saat setup, lalu fade out.
        private IEnumerator RevealThen(float delayAfter, System.Action after)
        {
            yield return StartCoroutine(RevealFromBlack());
            yield return new WaitForSeconds(Mathf.Max(0f, delayAfter));
            after?.Invoke();
        }

        private IEnumerator RevealFromBlack()
        {
            if (blackscreenOverlay == null) yield break;
            blackscreenOverlay.SetActive(true);
            yield return StartCoroutine(FadeBlackscreen(false, revealSeconds));
            blackscreenOverlay.SetActive(false);
        }

        private IEnumerator FlashRoutine()
        {
            if (flashImage == null) yield break;
            float t = 0f;
            const float inTime = 0.08f;
            const float holdTime = 0.05f;
            const float outTime = 0.25f;
            const float peak = 0.85f;
            while (t < inTime)
            {
                t += Time.unscaledDeltaTime;
                SetFlashAlpha(Mathf.Lerp(0f, peak, Mathf.Clamp01(t / inTime)));
                yield return null;
            }
            SetFlashAlpha(peak);
            yield return new WaitForSecondsRealtime(holdTime);
            t = 0f;
            while (t < outTime)
            {
                t += Time.unscaledDeltaTime;
                SetFlashAlpha(Mathf.Lerp(peak, 0f, Mathf.Clamp01(t / outTime)));
                yield return null;
            }
            SetFlashAlpha(0f);
        }

        private void SetFlashAlpha(float a)
        {
            if (flashImage == null) return;
            var c = flashImage.color;
            c.a = a;
            flashImage.color = c;
        }

        private IEnumerator FadeBlackscreen(bool fadeIn, float duration)
        {
            if (blackscreenOverlay == null) yield break;

            var canvasGroup = blackscreenOverlay.GetComponent<CanvasGroup>();
            var image = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
            if (canvasGroup == null && image == null)
            {
                Debug.LogWarning("[Scene4Cutscene] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
                yield break;
            }

            blackscreenOverlay.SetActive(true);
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

        #endregion
    }
}
