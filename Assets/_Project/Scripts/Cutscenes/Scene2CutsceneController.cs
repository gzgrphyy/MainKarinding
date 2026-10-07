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

        #region Sprite Swap (talk/idle/play/hold)

        private static bool IsRimbaLine(string speaker)
        {
            return speaker != null && speaker.ToLower().Contains("rimba");
        }

        private bool IsKarindingLoopPlaying()
        {
            return karindingLoopSource != null && karindingLoopSource.isPlaying;
        }

        private void StopKarindingLoop()
        {
            if (karindingLoopSource != null && karindingLoopSource.isPlaying)
                karindingLoopSource.Stop();
        }

        private void SetRimbaSpeaking(bool speaking)
        {
            if (shotRimba == null || rimbaSpriteSet == null)
                return;
            Sprite s = speaking ? rimbaSpriteSet.sitTalk : rimbaSpriteSet.sitIdle;
            if (s == null) s = rimbaSpriteSet.duduk;
            if (s != null) shotRimba.sprite = s;
            else Debug.LogWarning("[Scene2Cutscene] RimbaSpriteSet.sitTalk/sitIdle/duduk kosong! Assign s2_rimba_sit_talk.png & s2_rimba_sit_idle.png");
        }

        /// <summary>
        /// Prioritas Abah: backsound bunyi -> play; else penjelasan bagian -> hold; else -> talk
        /// (talk dipakai juga saat Abah diam, tidak ada s2_abah_sit_idle).
        /// </summary>
        private void SetAbahForLine(bool isAbahLine, bool isPartExplanation)
        {
            if (shotAbah == null || abahSpriteSet == null)
                return;
            Sprite s;
            if (IsKarindingLoopPlaying())
                s = abahSpriteSet.sitPlayKarinding;
            else if (isPartExplanation)
                s = abahSpriteSet.sitHoldKarinding;
            else
                s = abahSpriteSet.sitTalk;
            if (s == null) s = abahSpriteSet.duduk;
            if (s != null) shotAbah.sprite = s;
            else Debug.LogWarning("[Scene2Cutscene] AbahSpriteSet.sitTalk/sitPlayKarinding/sitHoldKarinding kosong! Assign asset s2_abah_*.");
        }

        private void ApplyLineSprites(string speaker, bool isPartExplanation)
        {
            bool rimbaSpeaks = IsRimbaLine(speaker) && !isPartExplanation;
            bool abahSpeaks = !IsRimbaLine(speaker) || isPartExplanation;
            SetRimbaSpeaking(rimbaSpeaks);
            SetAbahForLine(abahSpeaks, isPartExplanation);
        }

        #endregion

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
            // Beat 1: belum ada dialog — Rimba idle. Sprite Abah (play) dipasang
            // setelah loop dengung jalan (play menang selama backsound bunyi).
            SetRimbaSpeaking(false);
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

            // Loop sudah jalan di titik ini -> Abah = play_karinding.
            SetAbahForLine(false, false);

            StartCoroutine(DelayedExecute(openingDelay, ExecuteBeat2));
        }

        // Beat 2 — intro dialog (backsound dimatikan biar sprite talk tampil — play menang selama loop bunyi)
        private void ExecuteBeat2()
        {
            Debug.Log("[Scene2Cutscene] Beat 2 - IntroDialog");
            SetBeat(Ch1Beat.S2_IntroDialog);
            StopKarindingLoop();
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
            // Selama menunggu pilihan: keduanya diam (Rimba idle, Abah talk).
            SetRimbaSpeaking(false);
            SetAbahForLine(false, false);
            choicePrompt.Show(opsiRimba, (idx) =>
            {
                int safe = Mathf.Clamp(idx, 0, 2);
                StartCoroutine(PlayDialogueSequence(new[] { jawabanAbah[safe] }, ExecuteBeat4));
            });
        }

        [Header("Beat 4 presentation")]
        [Tooltip("Karakter digelapkan (RGB) selama karinding tampil biar tidak adu tampil. Alpha tetap 1.")]
        [SerializeField] private float beat4DarkenFactor = 0.35f;
        [Tooltip("Auto-lanjut ke bagian berikutnya kalau player tidak tap (anti-stuck).")]
        [SerializeField] private float partAutoAdvanceSeconds = 8f;

        private Color savedRimbaColor = Color.white;
        private Color savedAbahColor = Color.white;
        private Color savedBgColor = Color.white;
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

            // Baris Abah per bagian — Abah hold karinding, Rimba idle.
            // Box statis bawah ala Shot 2 Scene 1: Sunda saja + label speaker.
            ApplyLineSprites("Abah", true);
            bool done = false;
            if (dialogueDisplay != null)
                dialogueDisplay.ShowDialogue(part.barissunda, "Abah", () => done = true);
            else
            {
                Debug.LogWarning("[Scene2Cutscene] dialogueDisplay belum di-assign! Skip: " + part.barissunda);
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
                if (!charColorsSaved)
                {
                    savedRimbaColor = shotRimba != null ? shotRimba.color : Color.white;
                    savedAbahColor = shotAbah != null ? shotAbah.color : Color.white;
                    savedBgColor = bgRenderer != null ? bgRenderer.color : Color.white;
                    charColorsSaved = true;
                }
                if (shotRimba != null)
                    shotRimba.color = Darken(savedRimbaColor);
                if (shotAbah != null)
                    shotAbah.color = Darken(savedAbahColor);
                if (bgRenderer != null)
                    bgRenderer.color = Darken(savedBgColor);
            }
            else if (charColorsSaved)
            {
                if (shotRimba != null) shotRimba.color = savedRimbaColor;
                if (shotAbah != null) shotAbah.color = savedAbahColor;
                if (bgRenderer != null) bgRenderer.color = savedBgColor;
                charColorsSaved = false;
            }
        }

        /// <summary>
        /// Gelapkan warna (RGB) tapi pertahankan alpha asal — karakter solid gelap, bukan transparan.
        /// </summary>
        private Color Darken(Color c)
        {
            float f = Mathf.Clamp01(beat4DarkenFactor);
            return new Color(c.r * f, c.g * f, c.b * f, c.a);
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
            StopKarindingLoop();
            // Posisi diam akhir: Rimba idle, Abah talk.
            SetRimbaSpeaking(false);
            SetAbahForLine(false, false);
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
                // Sprite per baris: Rimba ngomong -> talk, else idle; Abah -> talk (play kalau loop bunyi).
                // Box statis bawah ala Shot 2 Scene 1: Sunda saja + label speaker.
                ApplyLineSprites(line.speaker, false);
                bool finished = false;
                if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene2Cutscene] dialogueDisplay belum di-assign! Skip: " + line.textSunda);
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
