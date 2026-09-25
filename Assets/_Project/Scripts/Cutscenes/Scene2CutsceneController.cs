using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MainKarinding.Core;
using MainKarinding.Karinding;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 2 — Pengenalan Karinding (PLAN E2).
    /// Reuse Shot 2 Scene 1, tint Siang, BG s2_bg_teras_siang.png (dummy).
    /// 5 beat: S2_AbahMainKarinding > S2_IntroDialog > S2_PilihanOpsi > S2_ModelHighlight > S2_ClosingFilosofi.
    /// Pola ikuti Scene1CutsceneController (dialogue/floating/audio/blackscreen).
    /// </summary>
    public class Scene2CutsceneController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Siang (reuse Shot 2)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSiang;

        [Header("Karinding 3D (legacy, dipensiunkan — disimpan buat Scene 6)")]
        [SerializeField] private GameObject karindingRoot;
        [SerializeField] private KarindingHighlightController highlightController;

        [Header("Karinding 2D (aktif Beat 4 — 3 sprite tumpuk jadi 1 utuh)")]
        [SerializeField] private GameObject karinding2DRoot;
        [SerializeField] private Karinding2DHighlightController highlight2D;

        [Header("UI Components")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private DialogueChoicePrompt choicePrompt;
        [SerializeField] private GameObject blackscreenOverlay;

        [Header("Head Anchors")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceSiang;
        [SerializeField] private AudioClip dengungLow;
        [Tooltip("Loop dengung pelan Beat 1+4. Auto-find/buat kalau kosong.")]
        [SerializeField] private AudioSource karindingLoopSource;
        [SerializeField] private AudioClip sfxTapConfirm;

        [Header("Settings")]
        [SerializeField] private float openingDelay = 1f;

        private int currentBeat = 0;
        private bool cutsceneActive = false;

        // ---- Dialog verbatim PLAN E2 ----
        private readonly DialogueLine[] introDialogue = new[]
        {
            new DialogueLine("Rimba", "Eta naon, bah? naha kitu sora na?", "Itu apa, bah? Kenapa suaranya begitu?"),
            new DialogueLine("Abah", "Ieu teh nami na Karinding, lain ngan saukur alat bebunyian.", "Ini namanya Karinding, bukan cuma alat bebunyian biasa."),
        };

        private readonly string[] opsiRimba = new[]
        {
            "Rimba nembe ningal, naha jarang aya nu maenkeun bah?",
            "Kumaha cara maenna bah?",
            "Naha abah tiasa gaduh?",
        };

        private readonly DialogueLine[] jawabanAbah = new[]
        {
            new DialogueLine("Abah", "Tos lami teu diwariskeun, Ba. Kapungkur mah unggal jalmi teh aya. ayeuna mah ngan sakedik nu terang.", "Udah lama nggak diwariskan, Ba. Dulu tiap orang punya. Sekarang cuma sedikit yang tau."),
            new DialogueLine("Abah", "Teu hese, tapi teu gampang oge. Kudu cepeung sing yakin, tabeuh sing sabat.", "Nggak susah, tapi nggak gampang juga. Harus pegang dengan yakin, pukul dengan sabar."),
            new DialogueLine("Abah", "Ieu teh Uyut didinya kapungkur, di wariskeun ka Abah. Ayeuna abah nu nyepeung.", "Ini dari Uyut dulu, diwariskan ke Abah. Sekarang Abah yang pegang."),
        };

        // Urutan atas->bawah bilah tegak: Panabeuh, Cecet Ucing, Pancepeungan
        private readonly PartLine[] partLines = new[]
        {
            new PartLine("Panabeuh", "Panabeuh, tabeuh sing sabar.", "Panabeuh — bagian yang dipukul/digetarkan, mengajarkan kesabaran dalam menghasilkan getaran."),
            new PartLine("Cecet Ucing", "Cecet Ucing, kudu sadar lamun eta lain sora urang.", "Cecet Ucing — bagian kecil pembantu resonansi, hingga suara benar-benar terdengar."),
            new PartLine("Pancepengan", "Pancepengan, cepeung sing yakin.", "Pancepengan — bagian pegangan, mengajarkan keyakinan dalam memegang dan menjalankan sesuatu."),
        };

        private readonly DialogueLine[] closingDialogue = new[]
        {
            new DialogueLine("Abah", "Unggal bagian aya hartina, Baba. Sami oge sareng kahirupan, kudu yakin, kudu sabar, jeung kudu sadar lamun arurang tos menang hasil tina naon nu dilakonan.", "Setiap bagian ada artinya, Baba. Sama juga dengan kehidupan, harus percaya, harus sabar, dan sadar kalau kita udah dapat hasil dari yang dijalani."),
        };

        private void Start()
        {
            if (blackscreenOverlay != null)
                blackscreenOverlay.SetActive(false);
            AutoFind();
            if (highlightController != null)
                highlightController.ClearAll();
            if (highlight2D != null)
                highlight2D.ClearAll();
            // 3D dipensiunkan untuk Beat 4 (disimpan buat Scene 6), 2D yang tampil.
            if (karindingRoot != null) karindingRoot.SetActive(false);
            if (karinding2DRoot != null) karinding2DRoot.SetActive(false);
            StartCutscene();
        }

        private void AutoFind()
        {
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
            if (karindingLoopSource == null)
            {
                karindingLoopSource = GetComponent<AudioSource>();
                if (karindingLoopSource == null)
                    karindingLoopSource = gameObject.AddComponent<AudioSource>();
                karindingLoopSource.loop = true;
                karindingLoopSource.playOnAwake = false;
            }
            StoryState.EnsureExists();
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLower().Contains("rimba");
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene2Cutscene] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        public void StartCutscene()
        {
            cutsceneActive = true;
            ExecuteBeat1();
        }

        private void SetBeat(Ch1Beat beat)
        {
            currentBeat = (int)beat;
            if (StoryState.Instance != null)
                StoryState.Instance.SetBeat(beat);
        }

        // Beat 1 — Abah main karinding, dengung pelan
        private void ExecuteBeat1()
        {
            Debug.Log("[Scene2Cutscene] Beat 1 - AbahMainKarinding");
            SetBeat(Ch1Beat.S2_AbahMainKarinding);

            if (shotRoot != null) shotRoot.SetActive(true);
            if (shotRimba != null && rimbaSpriteSet != null)
            {
                if (rimbaSpriteSet.duduk != null) shotRimba.sprite = rimbaSpriteSet.duduk;
                else Debug.LogWarning("[Scene2Cutscene] RimbaSpriteSet.duduk belum di-assign! Assign s2_rimba_duduk.png");
            }
            if (shotAbah != null && abahSpriteSet != null)
            {
                if (abahSpriteSet.duduk != null) shotAbah.sprite = abahSpriteSet.duduk;
                else Debug.LogWarning("[Scene2Cutscene] AbahSpriteSet.duduk belum di-assign! Assign s2_abah_duduk.png");
            }
            if (bgRenderer != null && bgTerasSiang != null)
                bgRenderer.sprite = bgTerasSiang;

            if (karindingRoot != null) karindingRoot.SetActive(false);
            if (karinding2DRoot != null) karinding2DRoot.SetActive(false);

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);

            // Dengung khas mengalun pelan (loop soft di bawah dialog)
            if (karindingLoopSource != null && dengungLow != null)
            {
                karindingLoopSource.clip = dengungLow;
                karindingLoopSource.volume = 0.35f;
                karindingLoopSource.loop = true;
                karindingLoopSource.Play();
            }
            else if (dengungLow == null)
            {
                Debug.LogWarning("[Scene2Cutscene] dengungLow belum di-assign! Assign sfx_karinding_dengung_low.wav");
            }

            StartCoroutine(DelayedExecute(openingDelay, ExecuteBeat2));
        }

        // Beat 2 — intro dialog
        private void ExecuteBeat2()
        {
            Debug.Log("[Scene2Cutscene] Beat 2 - IntroDialog");
            SetBeat(Ch1Beat.S2_IntroDialog);
            StartCoroutine(PlayDialogueSequence(introDialogue, ExecuteBeat3));
        }

        // Beat 3 — pilihan 3 opsi, sekali pilih langsung lanjut
        private void ExecuteBeat3()
        {
            Debug.Log("[Scene2Cutscene] Beat 3 - PilihanOpsi");
            SetBeat(Ch1Beat.S2_PilihanOpsi);

            if (choicePrompt == null)
            {
                Debug.LogError("[Scene2Cutscene] choicePrompt belum di-assign! Assign DialogueChoicePrompt di Inspector.");
                // Fallback: pakai jawaban A biar tidak stuck
                StartCoroutine(PlayDialogueSequence(new[] { jawabanAbah[0] }, ExecuteBeat4));
                return;
            }
            choicePrompt.Show(opsiRimba, (idx) =>
            {
                int safe = Mathf.Clamp(idx, 0, 2);
                StartCoroutine(PlayDialogueSequence(new[] { jawabanAbah[safe] }, ExecuteBeat4));
            });
        }

        [Header("Beat 4 presentation")]
        [Tooltip("Karakter di-fade selama karinding tampil biar tidak adu tampil.")]
        [SerializeField] private float beat4CharDimAlpha = 0.35f;
        [Tooltip("Auto-lanjut ke bagian berikutnya kalau player tidak tap (anti-stuck).")]
        [SerializeField] private float partAutoAdvanceSeconds = 8f;

        private Color savedRimbaColor = Color.white;
        private Color savedAbahColor = Color.white;
        private bool charColorsSaved;

        // Beat 4 — karinding 2D tampil tengah, highlight per bagian satu-persatu + sway
        private void ExecuteBeat4()
        {
            Debug.Log("[Scene2Cutscene] Beat 4 - ModelHighlight (2D)");
            SetBeat(Ch1Beat.S2_ModelHighlight);
            if (karindingRoot != null) karindingRoot.SetActive(false);
            if (karinding2DRoot != null) karinding2DRoot.SetActive(true);
            DimCharacters(true);
            StartCoroutine(PlayPartSequence(0));
        }

        private IEnumerator PlayPartSequence(int partIdx)
        {
            if (partIdx >= partLines.Length)
            {
                ExecuteBeat5();
                yield break;
            }

            var part = partLines[partIdx];
            if (highlight2D != null)
                highlight2D.HighlightPart(part.partName);
            else if (highlightController != null)
                highlightController.HighlightPart(part.partName);
            else
                Debug.LogWarning("[Scene2Cutscene] highlight2D/highlightController belum di-assign!");

            // Baris Abah per bagian (full text + makna tiap bagian)
            var line = new DialogueLine("Abah", part.barissunda, part.makna);
            bool done = false;
            Transform anchor = ResolveAnchor("Abah");
            if (floatingDialogue != null && anchor != null)
                floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => done = true);
            else if (dialogueDisplay != null)
                dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => done = true);
            else
            {
                Debug.LogWarning("[Scene2Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
                done = true;
            }
            yield return StartCoroutine(WaitDialogueOrTimeout(() => done, partAutoAdvanceSeconds));
            yield return new WaitForSeconds(0.2f);

            // Lanjut bagian berikutnya (highlight sebelumnya otomatis mati via HighlightPart)
            StartCoroutine(PlayPartSequence(partIdx + 1));
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
                Debug.Log("[Scene2Cutscene] Auto-lanjut (timeout, player tidak tap).");
        }

        private void DimCharacters(bool dim)
        {
            if (dim)
            {
                if (shotRimba != null)
                {
                    if (!charColorsSaved)
                    {
                        savedRimbaColor = shotRimba.color;
                        savedAbahColor = shotAbah != null ? shotAbah.color : Color.white;
                        charColorsSaved = true;
                    }
                    Color c = shotRimba.color; c.a = beat4CharDimAlpha; shotRimba.color = c;
                }
                if (shotAbah != null)
                {
                    Color c = shotAbah.color; c.a = beat4CharDimAlpha; shotAbah.color = c;
                }
            }
            else if (charColorsSaved)
            {
                if (shotRimba != null) shotRimba.color = savedRimbaColor;
                if (shotAbah != null) shotAbah.color = savedAbahColor;
                charColorsSaved = false;
            }
        }

        // Beat 5 — closing filosofi
        private void ExecuteBeat5()
        {
            Debug.Log("[Scene2Cutscene] Beat 5 - ClosingFilosofi");
            SetBeat(Ch1Beat.S2_ClosingFilosofi);
            if (highlightController != null)
                highlightController.ClearAll();
            if (highlight2D != null)
                highlight2D.ClearAll();
            if (karinding2DRoot != null) karinding2DRoot.SetActive(false);
            DimCharacters(false);
            StartCoroutine(PlayDialogueSequence(closingDialogue, OnScene2Complete));
        }

        private void OnScene2Complete()
        {
            cutsceneActive = false;
            if (karindingLoopSource != null && karindingLoopSource.isPlaying)
                karindingLoopSource.Stop();
            Debug.Log("[Scene2Cutscene] Scene 2 selesai — lanjut Scene 3 (S3_Handoff). Freeze di sini sampai Scene 3 dibangun.");
            // Tidak auto-load scene lain; StoryState.CurrentBeat = S2_ClosingFilosofi jadi acuan Scene 3.
        }

        #region Helpers (mirip Scene 1)

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
                Transform anchor = ResolveAnchor(line.speaker);
                if (floatingDialogue != null && anchor != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene2Cutscene] Tidak ada dialogue display! Skip: " + line.textSunda);
                    finished = true;
                }
                yield return new WaitUntil(() => finished);
                yield return new WaitForSeconds(0.2f);
            }
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            onComplete?.Invoke();
        }

        private IEnumerator DelayedExecute(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
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

        [System.Serializable]
        private struct PartLine
        {
            public string partName;
            public string barissunda;
            public string makna;
            public PartLine(string part, string sunda, string makna)
            {
                this.partName = part;
                this.barissunda = sunda;
                this.makna = makna;
            }
        }

        #endregion
    }
}
