using UnityEngine;
using System.Collections;
using MainKarinding.Core;
using MainKarinding.Exploration;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 8 — Sore Hari Sebelum Abah Pergi (PLAN E8, revisi: duduk dialog -> langsung jalan).
    /// Fase 1 DudukDialog: shot teras siang (BG reuse s1_bg_wide_establishing2), dialog 2 baris.
    /// Fase 2 Jalan: tanpa blackscreen — langsung area exploration (BG sama, mulus); Abah spawn
    /// di bawah BG dulu, Rimba menyusul (rimbaJoinDelay); Abah auto-jalan (Rimba terkunci) ->
    /// tanya-jawab singkat -> "Moal Kataekan" TANPA terjemahan -> hening -> Abah keluar frame ->
    /// Rimba dibuka (analog muncul) -> WalkOffTrigger ujung -> blackscreen -> freeze ke Scene 9.
    /// Art: reuse duduk/idle/walk + s1_bg_wide_establishing2.
    /// </summary>
    public class Scene8CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Sore (milik Scene 8)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSore;
        [SerializeField] private GameObject uiRoot;

        [Header("UI Components (milik Scene 8)")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;

        [Header("Head Anchors (shot Scene 8)")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Head Anchors (area jalan)")]
        [SerializeField] private Transform walkHeadAnchorRimba;
        [SerializeField] private Transform walkHeadAnchorAbah;

        [Header("Toast hint jalan")]
        [SerializeField] private Scene5Toast followToast;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [Tooltip("Placeholder: ambience siang. Swap ke ambience_sore.wav kalau sudah ada.")]
        [SerializeField] private AudioClip ambienceSore;

        [Header("Area Jalan (milik Scene 8, duplikat ExplorationRoot Scene 1)")]
        [SerializeField] private GameObject explorationRoot;
        [SerializeField] private Transform walkRimba;
        [SerializeField] private Transform walkAbah;
        [SerializeField] private GameObject uiAnalogRoot;
        [Tooltip("Tint hangat untuk 3 lapis path siang supaya kebaca sore.")]
        [SerializeField] private SpriteRenderer[] pathBGs;
        [SerializeField] private Color pathSoreTint = new Color(1f, 0.82f, 0.66f, 1f);

        [Header("Timing")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float blackscreenDuration = 0.7f;
        [Tooltip("Jeda Abah muncul di area jalan sebelum Rimba muncul menyusul.")]
        [SerializeField] private float rimbaJoinDelay = 0.5f;
        [SerializeField] private float moalKataekanHoldSeconds = 1f;
        [Tooltip("X saat Abah dianggap keluar frame kanan (pojok layar ≈ ±8.3).")]
        [SerializeField] private float abahExitX = 10.5f;
        [SerializeField] private float dialogueTimeoutSeconds = 15f;

        private RimbaWalker rimbaWalker;
        private AbahAutoWalker abahWalker;
        private WalkOffTrigger walkOffTrigger;
        private AnalogStickController analogStick;
        private bool walkPhaseActive;

        // ---- Dialog verbatim (PLAN E8) ----
        private readonly DialogueLine[] openingDialogue = new[]
        {
            new DialogueLine("Abah", "Rimba... Karinding ieu ulah ngan saukur jang Rimba nyalira. Abah hoyong nu sanes ge terang kana alat bebunyian ieu.", "Rimba... Karinding ini jangan cuma buat kamu sendiri. Abah ingin orang lain juga tau sama alat bebunyian ini."),
            new DialogueLine("Rimba", "Maksudna kumaha, Bah?", "Maksudnya gimana, Bah?"),
        };

        private readonly DialogueLine[] walkDialogue = new[]
        {
            new DialogueLine("Rimba", "Abah, bade kamana?", "Abah mau kemana?"),
            new DialogueLine("Abah", "Néangan nu eweuh.", "Mencari sesuatu yang tidak ada."),
            new DialogueLine("Rimba", "Dimana bah nu eweuh teh?", "Dimana bah yang gaada itu?"),
        };

        private readonly DialogueLine[] closingLine = new[]
        {
            // INDO SENGAJA KOSONG — "Moal Kataekan" jangan diterjemahkan (ambigu dijaga).
            new DialogueLine("Abah", "Moal Kataekan", ""),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene8);
            SetBeat(Ch1Beat.S8_SoreAbahPergi);
            if (explorationRoot != null) explorationRoot.SetActive(false);
            if (uiAnalogRoot != null) uiAnalogRoot.SetActive(false);
            StartCoroutine(DelayedExecute(openingDelay, ExecuteDudukDialog));
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene8Cutscene] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene8Cutscene] abahSpriteSet belum di-assign!");
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
            if (explorationRoot != null)
            {
                if (walkRimba == null)
                {
                    var t = explorationRoot.transform.Find("WalkRimba");
                    if (t != null) walkRimba = t;
                }
                if (walkAbah == null)
                {
                    var t = explorationRoot.transform.Find("WalkAbah");
                    if (t != null) walkAbah = t;
                }
                if (uiAnalogRoot == null)
                {
                    var t = explorationRoot.transform.Find("UI_Analog");
                    if (t != null) uiAnalogRoot = t.gameObject;
                }
                rimbaWalker = walkRimba != null ? walkRimba.GetComponent<RimbaWalker>() : null;
                abahWalker = walkAbah != null ? walkAbah.GetComponent<AbahAutoWalker>() : null;
                walkOffTrigger = explorationRoot.GetComponent<WalkOffTrigger>();
                analogStick = uiAnalogRoot != null ? uiAnalogRoot.GetComponentInChildren<AnalogStickController>(true) : null;
                followToast = uiAnalogRoot != null ? uiAnalogRoot.GetComponentInChildren<Scene5Toast>(true) : null;
                if (walkHeadAnchorRimba == null)
                {
                    var t = explorationRoot.transform.Find("WalkRimba/HeadAnchor");
                    if (t != null) walkHeadAnchorRimba = t;
                }
                if (walkHeadAnchorAbah == null)
                {
                    var t = explorationRoot.transform.Find("WalkAbah/HeadAnchor");
                    if (t != null) walkHeadAnchorAbah = t;
                }
            }
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            // Fase jalan: anchor = kepala walker; fase duduk: head anchor shot.
            if (walkPhaseActive)
            {
                var walkHead = isRimba ? walkHeadAnchorRimba : walkHeadAnchorAbah;
                if (walkHead != null) return walkHead;
                return isRimba ? walkRimba : walkAbah;
            }
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene8Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        private static bool IsRimbaLine(string speaker)
        {
            return speaker != null && speaker.ToLower().Contains("rimba");
        }

        private void SetRimbaSpeaking(bool speaking)
        {
            if (shotRimba == null || rimbaSpriteSet == null)
                return;
            Sprite s = speaking ? rimbaSpriteSet.sitTalk : rimbaSpriteSet.sitIdle;
            if (s == null) s = rimbaSpriteSet.duduk;
            if (s != null) shotRimba.sprite = s;
            else Debug.LogWarning("[Scene8Cutscene] RimbaSpriteSet.sitTalk/sitIdle/duduk kosong!");
        }

        // ---- Fase 1: duduk berdua, dialog 2 baris ----
        private void ExecuteDudukDialog()
        {
            Debug.Log("[Scene8Cutscene] Fase 1 - DudukDialog");
            if (shotRoot != null) shotRoot.SetActive(true);
            if (uiRoot != null) uiRoot.SetActive(true);
            SetRimbaSpeaking(false);
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.duduk : null, "AbahSpriteSet.duduk");
            if (bgRenderer != null && bgTerasSore != null) bgRenderer.sprite = bgTerasSore;
            if (audioManager != null && ambienceSore != null)
                audioManager.PlayAmbient(ambienceSore, 0.6f);
            StartCoroutine(PlayDialogueSequence(openingDialogue, () => StartCoroutine(BerangkatJalanRoutine())));
        }

        // ---- Fase 2: langsung ke area jalan (tanpa blackscreen) ----
        private IEnumerator BerangkatJalanRoutine()
        {
            if (shotRoot != null) shotRoot.SetActive(false);
            if (explorationRoot != null) explorationRoot.SetActive(true);
            ApplyPathSoreTint();
            if (walkRimba != null) walkRimba.gameObject.SetActive(false); // Rimba ikut menyusul
            ConfigureWalkers();

            yield return new WaitForSeconds(rimbaJoinDelay);
            if (walkRimba != null) walkRimba.gameObject.SetActive(true);

            walkPhaseActive = true;
            // Tanya-jawab jalan (Abah tetap jalan selama dialog; trigger menoleh menunggu dialog + posisi).
            StartCoroutine(PlayDialogueSequence(walkDialogue, () => walkDialogueDone = true));
            StartCoroutine(MenolehRoutine());
        }

        private bool walkDialogueDone;

        private void ConfigureWalkers()
        {
            walkDialogueDone = false;
            if (rimbaWalker != null && walkRimba != null)
            {
                var stick = analogStick;
                rimbaWalker.Configure(stick,
                    rimbaSpriteSet != null ? rimbaSpriteSet.walkRight : null,
                    rimbaSpriteSet != null ? rimbaSpriteSet.idleRight : null);
                rimbaWalker.Locked = true; // terkunci sampai Abah menghilang
            }
            if (abahWalker != null)
            {
                abahWalker.Configure(
                    abahSpriteSet != null ? abahSpriteSet.walkRight : null,
                    abahSpriteSet != null ? abahSpriteSet.idleRight : null);
            }
            if (uiAnalogRoot != null) uiAnalogRoot.SetActive(false); // muncul = sinyal kontrol kembali (saran 3)
            if (walkOffTrigger != null && walkRimba != null)
                walkOffTrigger.Configure(walkRimba, OnRimbaArrived);
        }

        private void ApplyPathSoreTint()
        {
            if (pathBGs == null) return;
            foreach (var bg in pathBGs)
                if (bg != null) bg.color = pathSoreTint;
        }

        // Menunggu Abah sampai stopX DAN dialog selesai -> menoleh -> line -> hening ->
        // lanjut jalan keluar frame kanan (biar player tahu Abah ke mana, bisa diikuti) -> unlock Rimba.
        private IEnumerator MenolehRoutine()
        {
            if (abahWalker == null || walkAbah == null)
            {
                Debug.LogError("[Scene8Cutscene] Abah walker belum di-wire! Skip ke unlock Rimba.");
                UnlockRimba();
                yield break;
            }
            float stopX = walkAbah.position.x + 6f; // fallback bila stopX scene tidak ketemu
            // stopX dibaca dari serialized AbahAutoWalker via posisi: tunggu sampai Abah berhenti maju.
            float lastX = walkAbah.position.x;
            float stillTime = 0f;
            while (true)
            {
                yield return null;
                float x = walkAbah.position.x;
                if (Mathf.Approximately(x, lastX)) stillTime += Time.deltaTime;
                else { stillTime = 0f; lastX = x; }
                if ((stillTime > 0.4f || x >= stopX) && walkDialogueDone) break;
            }

            // Menoleh: hadap Rimba (flip), line tanpa terjemahan, tahan hening sejenak.
            var sr = walkAbah.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = true;
            yield return StartCoroutine(PlayDialogueSequence(closingLine, null));
            yield return new WaitForSeconds(moalKataekanHoldSeconds);

            // Pergi beneran: balik hadap kanan, jalan sampai keluar frame, baru nonaktif.
            if (sr != null) sr.flipX = false;
            abahWalker.ResumeWalking(abahExitX);
            while (walkAbah != null && walkAbah.position.x < abahExitX)
                yield return null;
            if (walkAbah != null) walkAbah.gameObject.SetActive(false);
            UnlockRimba();
        }

        private void UnlockRimba()
        {
            Debug.Log("[Scene8Cutscene] Rimba dibuka — analog muncul sebagai sinyal kontrol kembali.");
            if (rimbaWalker != null) rimbaWalker.Locked = false;
            if (uiAnalogRoot != null) uiAnalogRoot.SetActive(true);
            if (followToast != null) followToast.ShowToast("Ikuti Abah pakai analog!", 3f);
        }

        private void OnRimbaArrived()
        {
            Debug.Log("[Scene8Cutscene] Rimba sampai ujung — blackscreen ke Scene 9.");
            StartCoroutine(EndSceneRoutine());
        }

        private IEnumerator EndSceneRoutine()
        {
            if (audioManager != null) audioManager.FadeOutAmbient(0.5f);
            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration + 0.5f));
            Debug.Log("[Scene8Cutscene] Scene 8 selesai — whiteout ke Scene 9 (S9_RimbaBangun).");
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene9);
            // Ch1_Hutan terdaftar di Build Settings. Whiteout dibuka di Scene 9 (mulai putih).
            UnityEngine.SceneManagement.SceneManager.LoadScene("Ch1_Hutan");
        }

        private void ApplyAbahSprite(Sprite sprite, string fieldName)
        {
            if (shotAbah == null) return;
            if (sprite != null) shotAbah.sprite = sprite;
            else Debug.LogWarning($"[Scene8Cutscene] {fieldName} belum di-assign!");
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
                SetRimbaSpeaking(IsRimbaLine(line.speaker));
                Transform anchor = ResolveAnchor(line.speaker);
                if (floatingDialogue != null && anchor != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene8Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
                    finished = true;
                }
                yield return StartCoroutine(WaitDialogueOrTimeout(() => finished, dialogueTimeoutSeconds));
                yield return new WaitForSeconds(0.2f);
            }
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            SetRimbaSpeaking(false);
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
                Debug.Log("[Scene8Cutscene] Auto-lanjut (timeout, player tidak tap).");
        }

        private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private IEnumerator FadeBlackscreen(bool fadeIn, float duration)
        {
            if (blackscreenOverlay == null) yield break;
            var canvasGroup = blackscreenOverlay.GetComponent<CanvasGroup>();
            var image = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
            if (canvasGroup == null && image == null)
            {
                Debug.LogWarning("[Scene8Cutscene] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
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
    }
}
