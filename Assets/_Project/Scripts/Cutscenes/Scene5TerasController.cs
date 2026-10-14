using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MainKarinding.Core;
using MainKarinding.Crafting;
using MainKarinding.Exploration;

namespace MainKarinding.Cutscenes
{
    /// <summary>
    /// Scene 5 — Teras side (PLAN E5 revisi + rombak asset berdiri S5).
    /// Beat 1 S5_AjakHutan: dialog ajak 3 baris (BG wide establishing) dengan swap
    ///   talk/idle/listen (Abah: stand_talk/stand_idle, Rimba: stand_talk/stand_listen),
    ///   Abah DIAM (tidak ikut jalan). Rimba player-controlled jalan ke KANAN di atas
    ///   BG wide yang sama -> ujung -> blackscreen -> LoadScene(Ch1_Hutan).
    /// Beat 3 S5_KembaliRumah: resume saat Teras di-load ulang (tas terbawa via BambooInventory)
    ///   — fade-in dari hitam, Rimba masuk lagi dari KANAN lalu player jalan KIRI ke Abah
    ///   (Abah menunggu pakai stand_idle). Saat Rimba sampai titik berhenti -> Abah
    ///   ganti ke receivebamboo, Rimba ganti ke bringbamboo, baru masuk handoff.
    /// Beat 4 S5_HandoffBambu: drag ikon dari tas ke Abah (urutan bebas) -> reaksi per-tag
    ///   (Tua->reaksi_tua, Tipis->reaksi_muda, Pas->reaksi_pas).
    /// </summary>
    public class Scene5TerasController : MonoBehaviour
    {
        [Header("Sprite Sets")]
        [SerializeField] private RimbaSpriteSet rimbaSpriteSet;
        [SerializeField] private AbahSpriteSet abahSpriteSet;

        [Header("Shot - Teras Siang (reuse Shot_TerasSiang)")]
        [SerializeField] private GameObject shotRoot;
        [SerializeField] private SpriteRenderer shotRimba;
        [SerializeField] private SpriteRenderer shotAbah;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private Sprite bgTerasSiang;
        [Header("BG Dialog + Walk (Beat 1)")]
        [Tooltip("BG dialog awal + jalan ke hutan. File: s1_bg_wide_establishing2.png")]
        [SerializeField] private Sprite bgDialogWide;

        [Header("Walk Phase BG (Beat 1 - jalan di teras siang)")]
        [Tooltip("Renderer BG di dalam explorationRoot (auto-find BG_teras-hutan_Walk kalau kosong). Sprite-nya ikut bgTerasSiang.")]
        [SerializeField] private SpriteRenderer exploreBgRenderer;

        [Header("UI Components (reuse)")]
        [SerializeField] private CutsceneDialogueDisplay dialogueDisplay;
        [SerializeField] private FloatingDialogueController floatingDialogue;
        [SerializeField] private GameObject blackscreenOverlay;

        [Header("Walk Phase (Beat 1 - Rimba sendiri, Abah diam)")]
        [SerializeField] private GameObject explorationRoot;
        [SerializeField] private RimbaWalker rimbaWalker;
        [SerializeField] private WalkOffTrigger walkTrigger;
        [SerializeField] private AnalogStickController analogStick;

        [Header("Beat 3 - Jalan Pulang ke Abah (setelah dari hutan)")]
        [Tooltip("X Rimba muncul lagi dari hutan (kanan). 7 = titik yang sama saat dia brangkat di Beat 1.")]
        [SerializeField] private float returnSpawnX = 7f;
        [Tooltip("Titik berhenti Rimba (drag di Scene view). Kalau kosong, pakai Abah Arrival X di bawah.")]
        [SerializeField] private Transform arrivalPoint;
        [Tooltip("Fallback X Rimba dianggap sudah sampai di Abah (trigger handoff). Dipakai kalau Arrival Point kosong.")]
        [SerializeField] private float abahArrivalX = -4f;
        [Tooltip("Jeda setelah Rimba sampai sebelum tas handoff muncul (baca: Rimba berhenti dulu).")]
        [SerializeField] private float arrivalPause = 0.6f;

        [Header("Handoff (Beat 4)")]
        [SerializeField] private GameObject tasPanel;
        [SerializeField] private List<BambooSlotUI> tasSlots = new List<BambooSlotUI>();
        [SerializeField] private NPCHandoffDropTarget dropTarget;
        [Tooltip("Ikon tas per-tag (prop bambu).")]
        [SerializeField] private Sprite iconTua;
        [SerializeField] private Sprite iconTipis;
        [SerializeField] private Sprite iconPas;

        [Header("Head Anchors")]
        [SerializeField] private Transform headAnchorRimba;
        [SerializeField] private Transform headAnchorAbah;

        [Header("Audio")]
        [SerializeField] private CutsceneAudioManager audioManager;
        [SerializeField] private AudioClip ambienceSiang;

        [Header("Settings")]
        [SerializeField] private float openingDelay = 1f;
        [SerializeField] private float blackscreenDuration = 0.7f;
        [SerializeField] private string hutanSceneName = "Ch1_Hutan";
        [SerializeField] private float dialogueTimeoutSeconds = 12f;

        private int deliveredCount = 0;
        private bool walkPhase = false;
        private bool returnWalkPhase = false;
        private bool scene5Complete = false;
        private bool reactionPlaying = false;
        private readonly Queue<BambooTag> reactionQueue = new Queue<BambooTag>();

        // ---- Dialog verbatim PLAN E5 ----

        private readonly DialogueLine[] beat1Dialogue = new[]
        {
            new DialogueLine("Abah", "Rimba, hayu urang milarian awi ka leuweung. Karinding Rimba kudu di jieun ku panangan Rimba sorangan.", "Rimba, ayo kita nyari bambu di hutan. Karinding Rimba harus dibuat sama tangan Rimba sendiri."),
            new DialogueLine("Rimba", "Kumaha cara milih awi nu sae, Bah?", "Gimana cara milih bambu yang bagus, Bah?"),
            new DialogueLine("Abah", "Awi nu sae teh nu tos cekap umur na. teu ngora teuing, teu kolot teuing. Batang na ge kudu seseug, ulah nu ipis.", "Bambu yang bagus itu yang udah cukup umurnya. Nggak terlalu muda, nggak terlalu tua. Batangnya juga harus padat, jangan kopong."),
        };

        private void Start()
        {
            StoryState.EnsureExists();
            BambooInventory.EnsureExists();
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            if (tasPanel != null) tasPanel.SetActive(false);
            if (explorationRoot != null) explorationRoot.SetActive(false);
            AutoFind();
            StoryState.Instance.SetEntryPoint(Ch1EntryPoint.Scene5);

            if (dropTarget != null) dropTarget.delivered += OnDelivered;

            if (StoryState.Instance.HasReached(Ch1Beat.S5_SearchBambu))
            {
                Debug.Log("[Scene5Teras] Resume: sudah dari hutan -> Beat 3/4");
                // Hutan sudah fade-to-black sebelum LoadScene, jadi scene ini dimuat gelap.
                // Tahan hitam sejak frame pertama lalu fade-in di Beat 3 (biar tidak "cut").
                SetBlackscreenOpaque();
                StartCoroutine(Beat3Routine());
            }
            else
            {
                ExecuteBeat1();
            }
        }

        private void OnDestroy()
        {
            if (dropTarget != null) dropTarget.delivered -= OnDelivered;
        }

        private void AutoFind()
        {
            if (rimbaSpriteSet == null) Debug.LogWarning("[Scene5Teras] rimbaSpriteSet belum di-assign!");
            if (abahSpriteSet == null) Debug.LogWarning("[Scene5Teras] abahSpriteSet belum di-assign!");
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
            if (tasSlots.Count == 0 && tasPanel != null)
                tasSlots.AddRange(tasPanel.GetComponentsInChildren<BambooSlotUI>(true));
            if (dropTarget == null) dropTarget = Object.FindFirstObjectByType<NPCHandoffDropTarget>();
            if (arrivalPoint == null && explorationRoot != null)
            {
                var ap = explorationRoot.transform.Find("RimbaArrivalPoint");
                if (ap != null) arrivalPoint = ap;
            }
            if (exploreBgRenderer == null && explorationRoot != null)
            {
                var bgT = explorationRoot.transform.Find("BG_teras-hutan_Walk");
                if (bgT != null) exploreBgRenderer = bgT.GetComponent<SpriteRenderer>();
                if (exploreBgRenderer == null)
                {
                    foreach (var sr in explorationRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sr.GetComponent<RimbaWalker>() == null) { exploreBgRenderer = sr; break; }
                    }
                }
            }
        }

        private Transform ResolveAnchor(string speaker)
        {
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            Transform anchor = isRimba ? headAnchorRimba : headAnchorAbah;
            if (anchor == null)
                Debug.LogError($"[Scene5Teras] Anchor NULL untuk '{speaker}'! Assign di Inspector.");
            return anchor;
        }

        private void SetBeat(Ch1Beat beat)
        {
            if (StoryState.Instance != null) StoryState.Instance.SetBeat(beat);
        }

        // ---- Beat 1: dialog ajak -> jalan kanan (Rimba sendiri) ----

        private void ExecuteBeat1()
        {
            Debug.Log("[Scene5Teras] Beat 1 - S5_AjakHutan");
            SetBeat(Ch1Beat.S5_AjakHutan);

            if (shotRoot != null) shotRoot.SetActive(true);
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.standIdle : null, "AbahSpriteSet.standIdle");
            ApplyRimbaSprite(rimbaSpriteSet != null ? rimbaSpriteSet.standListen : null, "RimbaSpriteSet.standListen");
            if (bgRenderer != null && bgDialogWide != null) bgRenderer.sprite = bgDialogWide;

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);

            StartCoroutine(DelayedExecute(openingDelay, () => StartCoroutine(PlayDialogueSequence(beat1Dialogue, StartWalkPhase))));
        }

        private void StartWalkPhase()
        {
            Debug.Log("[Scene5Teras] Walk phase: Rimba ke kanan (Abah diam).");
            if (shotRoot != null) shotRoot.SetActive(false);
            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();
            if (explorationRoot == null || rimbaWalker == null || walkTrigger == null)
            {
                Debug.LogError("[Scene5Teras] explorationRoot/rimbaWalker/walkTrigger belum di-setup! Assign di Inspector (pola Scene 1 Beat 8).");
                return;
            }
            explorationRoot.SetActive(true);
            // Jalan ke hutan pakai BG wide establishing (sama kayak dialog awal).
            if (exploreBgRenderer != null && bgDialogWide != null)
                exploreBgRenderer.sprite = bgDialogWide;
            if (rimbaSpriteSet != null)
                rimbaWalker.Configure(analogStick, rimbaSpriteSet.walkRight, rimbaSpriteSet.idleRight);
            walkTrigger.Configure(rimbaWalker.transform, OnWalkRightArrived);
            rimbaWalker.Locked = false;
            walkPhase = true;
        }

        private void OnWalkRightArrived()
        {
            if (!walkPhase) return;
            walkPhase = false;
            if (rimbaWalker != null) rimbaWalker.Locked = true;
            Debug.Log("[Scene5Teras] Rimba sampai ujung kanan -> ke hutan.");
            StartCoroutine(LoadHutanRoutine());
        }

        private IEnumerator LoadHutanRoutine()
        {
            if (audioManager != null) audioManager.FadeOutAmbient(0.35f);
            yield return StartCoroutine(FadeBlackscreen(true, blackscreenDuration));
            if (!SceneExists(hutanSceneName))
            {
                Debug.LogError($"[Scene5Teras] Scene '{hutanSceneName}' tidak ada di Build Settings! Tambah Ch1_Hutan.");
                yield break;
            }
            Debug.Log($"[Scene5Teras] LoadScene({hutanSceneName}).");
            UnityEngine.SceneManagement.SceneManager.LoadScene(hutanSceneName);
        }

        // ---- Beat 3: kembali dari hutan -> jalan ke Abah -> baru handoff ----

        /// <summary>
        /// Fade-in dari hitam (sisa fade-out hutan), baru masuk Beat 3.
        /// </summary>
        private IEnumerator Beat3Routine()
        {
            yield return StartCoroutine(FadeBlackscreen(false, blackscreenDuration));
            // Overlay wajib dimatikan lagi: Image alpha 0 masih menahan raycast
            // ke tombol analog di bawahnya.
            if (blackscreenOverlay != null) blackscreenOverlay.SetActive(false);
            ExecuteBeat3();
        }

        private void ExecuteBeat3()
        {
            Debug.Log("[Scene5Teras] Beat 3 - S5_KembaliRumah");
            SetBeat(Ch1Beat.S5_KembaliRumah);
            returnWalkPhase = false;

            if (audioManager != null && ambienceSiang != null)
                audioManager.PlayAmbient(ambienceSiang, 0.6f);

            StartReturnWalkPhase();
        }

        /// <summary>
        /// Rimba masuk lagi dari KANAN (baru pulang dari hutan), lalu player menggerakkannya
        /// KIRI menuju Abah. Abah sudah terlihat menunggu di depan tangga: Char_Abah diaktifkan,
        /// sementara Char_Rimba (pose duduk diam) & BG shot dimatikan supaya tidak ada Rimba
        /// ganda dan tidak dobel gambar (BG fase jalan sprite-nya sama, sortingOrder -10).
        /// </summary>
        private void StartReturnWalkPhase()
        {
            if (explorationRoot == null || rimbaWalker == null || walkTrigger == null || shotAbah == null)
            {
                Debug.LogWarning("[Scene5Teras] Setup walk pulang belum lengkap — langsung ke handoff.");
                ShowHandoffShot();
                ExecuteBeat4();
                return;
            }

            Debug.Log($"[Scene5Teras] Walk pulang: Rimba masuk dari kanan (x={returnSpawnX}) -> jalan ke KIRI sampai Abah (x={abahArrivalX}).");

            if (shotRoot != null) shotRoot.SetActive(true);
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.standIdle : null, "AbahSpriteSet.standIdle");
            if (shotRimba != null) shotRimba.gameObject.SetActive(false);
            if (bgRenderer != null) bgRenderer.gameObject.SetActive(false);

            if (dialogueDisplay != null) dialogueDisplay.Hide();
            if (floatingDialogue != null) floatingDialogue.Hide();

            explorationRoot.SetActive(true);

            // Spawn di ketinggian yang sama dengan pose duduk shot -> begitu sampai,
            // Rimba tidak "lompat" vertikal saat sprite-nya ditukar ke duduk.
            float walkY = shotRimba != null ? shotRimba.transform.position.y : rimbaWalker.transform.position.y;
            Vector3 pos = rimbaWalker.transform.position;
            rimbaWalker.transform.position = new Vector3(returnSpawnX, walkY, pos.z);

            // Baru datang dari hutan (kanan) -> hadap KIRI sejak frame pertama.
            // Awake sudah jalan saat explorationRoot diaktifkan di atas, jadi baseScaleX aman.
            Vector3 scale = rimbaWalker.transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            rimbaWalker.transform.localScale = scale;

            if (rimbaSpriteSet != null)
                rimbaWalker.Configure(analogStick, rimbaSpriteSet.walkRight, rimbaSpriteSet.idleRight);
            float arrivalX = arrivalPoint != null ? arrivalPoint.position.x : abahArrivalX;
            Debug.Log($"[Scene5Teras] Titik berhenti Rimba x={arrivalX} (point={(arrivalPoint != null ? "scene" : "fallback")}).");
            walkTrigger.Configure(rimbaWalker.transform, OnWalkToAbahArrived, WalkOffTrigger.ExitDirection.Left, arrivalX);
            rimbaWalker.Locked = false;
            returnWalkPhase = true;
        }

        /// <summary>Trigger Left: Rimba sudah sampai di posisi Abah -> stop & masuk handoff.</summary>
        private void OnWalkToAbahArrived()
        {
            if (!returnWalkPhase) return;
            returnWalkPhase = false;
            if (rimbaWalker != null) rimbaWalker.Locked = true;
            Debug.Log("[Scene5Teras] Rimba sampai di Abah -> mulai handoff bambu.");
            ApplyAbahSprite(abahSpriteSet != null ? abahSpriteSet.receiveBamboo : null, "AbahSpriteSet.receiveBamboo");
            StartCoroutine(HandoffAfterReturnRoutine());
        }

        private IEnumerator HandoffAfterReturnRoutine()
        {
            // Rimba berhenti (idle) sejenak di titik arrival. shotRimba digeser ke
            // posisi berhenti walker supaya handoff tetap di posisi awal (tanpa teleport).
            yield return new WaitForSeconds(arrivalPause);

            if (rimbaWalker != null && shotRimba != null)
            {
                Vector3 rp = shotRimba.transform.position;
                rp.x = rimbaWalker.transform.position.x;
                shotRimba.transform.position = rp;
            }
            if (explorationRoot != null) explorationRoot.SetActive(false);
            if (shotRimba != null) shotRimba.gameObject.SetActive(true);

            ShowHandoffShot();
            ExecuteBeat4();
        }

        /// <summary>
        /// Framing handoff: BG awal (wide dialog) + Abah (receiveBamboo) + Rimba (bringBamboo).
        /// Posisi tidak diubah di sini (shotRimba sudah digeser ke titik arrival).
        /// Dipakai saat walk pulang gagal disiapkan maupun saat Rimba sudah sampai di Abah.
        /// </summary>
        private void ShowHandoffShot()
        {
            if (shotRoot != null) shotRoot.SetActive(true);
            if (bgRenderer != null)
            {
                bgRenderer.gameObject.SetActive(true);
                if (bgDialogWide != null) bgRenderer.sprite = bgDialogWide;
                else if (bgTerasSiang != null) bgRenderer.sprite = bgTerasSiang;
            }
            if (shotRimba != null) shotRimba.gameObject.SetActive(true);
            Sprite rimbaHandoff = rimbaSpriteSet != null
                ? (rimbaSpriteSet.bringBamboo != null ? rimbaSpriteSet.bringBamboo : rimbaSpriteSet.standListen)
                : null;
            ApplyRimbaSprite(rimbaHandoff, "RimbaSpriteSet.bringBamboo");
            Sprite abahHandoff = abahSpriteSet != null
                ? (abahSpriteSet.receiveBamboo != null ? abahSpriteSet.receiveBamboo : abahSpriteSet.standIdle)
                : null;
            ApplyAbahSprite(abahHandoff, "AbahSpriteSet.receiveBamboo");
        }

        // ---- Beat 4: drag dari tas ke Abah ----

        private void ExecuteBeat4()
        {
            Debug.Log("[Scene5Teras] Beat 4 - S5_HandoffBambu (drag tas -> Abah, urutan bebas)");
            SetBeat(Ch1Beat.S5_HandoffBambu);
            deliveredCount = 0;
            scene5Complete = false;
            reactionQueue.Clear();
            if (tasPanel != null) tasPanel.SetActive(true);
            FillTasFromInventory();
        }

        private void FillTasFromInventory()
        {
            if (BambooInventory.Instance == null) return;
            var tags = BambooInventory.Instance.Collected;
            for (int i = 0; i < tasSlots.Count; i++)
            {
                if (tasSlots[i] == null) continue;
                tasSlots[i].SetIcons(iconTua, iconTipis, iconPas, null);
                if (i < tags.Count) tasSlots[i].SetItem(tags[i]);
                else tasSlots[i].Clear();
            }
            if (tags.Count == 0)
                Debug.LogWarning("[Scene5Teras] Tas kosong saat Beat 4! Harusnya 3/3 dari hutan.");
        }

        private void OnDelivered(BambooTag tag)
        {
            deliveredCount++;
            // Reaksi DIANTREKAN, bukan dijalankan paralel. Kalau player menyerahkan 3 bambu
            // berturut-turut cepat, 3 coroutine paralel akan berebut satu anchor dialog
            // (headAnchorAbah) -> box bertumpuk, dan callback-nya sama-sama melihat
            // deliveredCount >= 3 -> OnScene5Complete() terpanggil 3x.
            // Urutan penyerahan tetap BEBAS; yang diserialkan hanya pemutaran reaksinya.
            reactionQueue.Enqueue(tag);
            if (!reactionPlaying)
                StartCoroutine(PumpReactions());
        }

        /// <summary>
        /// Putar reaksi per-tag satu per satu sesuai urutan penyerahan, lalu selesaikan
        /// Scene 5 tepat SEKALI setelah semua reaksi habis.
        /// </summary>
        private IEnumerator PumpReactions()
        {
            reactionPlaying = true;
            while (reactionQueue.Count > 0)
            {
                BambooTag tag = reactionQueue.Dequeue();
                // Sprite reaksi di-set saat giliran tag ini, bukan saat drop, supaya
                // ekspresi Abah selalu cocok dengan dialog yang sedang tampil.
                // Rombak S5: Tua->reaksi_tua, Tipis->reaksi_muda, Pas->reaksi_pas.
                Sprite reaksi = tag switch
                {
                    BambooTag.Tua => abahSpriteSet != null
                        ? (abahSpriteSet.reaksiTua != null ? abahSpriteSet.reaksiTua : abahSpriteSet.reaksiTipis) : null,
                    BambooTag.Tipis => abahSpriteSet != null
                        ? (abahSpriteSet.reaksiMuda != null ? abahSpriteSet.reaksiMuda : abahSpriteSet.reaksiTipis) : null,
                    _ => abahSpriteSet != null ? abahSpriteSet.reaksiPas : null,
                };
                ApplyAbahSprite(reaksi, $"AbahSpriteSet.reaksi{tag}");
                // swapTalkSprites=false: Abah tetap pakai sprite reaksi, Rimba tetap bringBamboo.
                yield return StartCoroutine(PlayDialogueSequence(new[] { ReactionLine(tag) }, null, false));
            }
            reactionPlaying = false;

            if (deliveredCount >= 3)
                OnScene5Complete();
        }

        private DialogueLine ReactionLine(BambooTag tag)
        {
            return tag switch
            {
                BambooTag.Tua => new DialogueLine("Abah", "Ieu ge ipis teuing, gampang belah.", "Ini juga terlalu tipis, gampang patah."),
                BambooTag.Tipis => new DialogueLine("Abah", "Ieu mah... acan cekap umurna, Ba. Masih keneh hejo.", "Ini mah... belum cukup umurnya, Ba. Masih hijau."),
                _ => new DialogueLine("Abah", "Tah... ieu pas. Teu ngora teuing, teu kolot teuing, seseug deuih. Ieu nu rék dipake nyieun Karinding Rimba.", "Nah... ini pas. Nggak terlalu muda, nggak terlalu tua, padat juga. Ini yang bakal dipake bikin Karinding Rimba."),
            };
        }

        private void OnScene5Complete()
        {
            if (scene5Complete) return;
            scene5Complete = true;

            // Tas sudah kosong (3/3) — sembunyikan supaya bingkai akhir bersih,
            // bukan 3 kotak putih (iconEmpty belum di-assign).
            if (tasPanel != null) tasPanel.SetActive(false);
            Debug.Log("[Scene5Teras] Scene 5 selesai — lanjut Scene 6 (S6_Assembly). Freeze di sini sampai Scene 6 dibangun.");
        }

        #region Helpers (pola Scene 1-4)

        private void ApplyRimbaSprite(Sprite sprite, string fieldName)
        {
            if (shotRimba == null) return;
            if (sprite != null) shotRimba.sprite = sprite;
            else Debug.LogWarning($"[Scene5Teras] {fieldName} belum di-assign!");
        }

        private void ApplyAbahSprite(Sprite sprite, string fieldName)
        {
            if (shotAbah == null) return;
            if (sprite != null) shotAbah.sprite = sprite;
            else Debug.LogWarning($"[Scene5Teras] {fieldName} belum di-assign!");
        }

        private static bool SceneExists(string name)
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains(name)) return true;
            }
            return false;
        }

        /// <summary>
        /// Abah ngomong -> Abah stand_talk + Rimba stand_listen.
        /// Rimba ngomong -> Rimba stand_talk + Abah stand_idle.
        /// Dilewati saat reaksi handoff (swapTalkSprites=false) supaya sprite
        /// reaksi Abah dan bringBamboo Rimba tidak tertimpa.
        /// </summary>
        private void ApplyTalkSprites(string speaker)
        {
            if (rimbaSpriteSet == null || abahSpriteSet == null) return;
            bool isRimba = speaker != null && speaker.ToLowerInvariant().Contains("rimba");
            if (isRimba)
            {
                if (rimbaSpriteSet.standTalk != null) ApplyRimbaSprite(rimbaSpriteSet.standTalk, "RimbaSpriteSet.standTalk");
                if (abahSpriteSet.standIdle != null) ApplyAbahSprite(abahSpriteSet.standIdle, "AbahSpriteSet.standIdle");
            }
            else
            {
                if (abahSpriteSet.standTalk != null) ApplyAbahSprite(abahSpriteSet.standTalk, "AbahSpriteSet.standTalk");
                if (rimbaSpriteSet.standListen != null) ApplyRimbaSprite(rimbaSpriteSet.standListen, "RimbaSpriteSet.standListen");
            }
        }

        private IEnumerator PlayDialogueSequence(DialogueLine[] lines, System.Action onComplete, bool swapTalkSprites = true)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.textSunda))
                {
                    yield return new WaitForSeconds(0.8f);
                    continue;
                }
                if (swapTalkSprites) ApplyTalkSprites(line.speaker);
                bool finished = false;
                Transform anchor = ResolveAnchor(line.speaker);
                if (floatingDialogue != null && anchor != null)
                    floatingDialogue.Show(line.speaker, line.textSunda, line.textIndo, anchor, () => finished = true);
                else if (dialogueDisplay != null)
                    dialogueDisplay.ShowDialogue(line.textSunda, line.speaker, () => finished = true);
                else
                {
                    Debug.LogWarning("[Scene5Teras] Tidak ada dialogue display! Skip: " + line.textSunda);
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
                Debug.Log("[Scene5Teras] Auto-lanjut (timeout, player tidak tap).");
        }

        /// <summary>
        /// Tahan layar hitam penuh. Dipakai di awal Beat 3: scene ini dimuat tepat setelah
        /// hutan fade-to-black, jadi frame pertamanya harus gelap dulu sebelum fade-in.
        /// </summary>
        private void SetBlackscreenOpaque()
        {
            if (blackscreenOverlay == null) return;
            blackscreenOverlay.SetActive(true);
            var canvasGroup = blackscreenOverlay.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            var image = blackscreenOverlay.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                var c = image.color;
                c.a = 1f;
                image.color = c;
            }
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
                Debug.LogWarning("[Scene5Teras] blackscreenOverlay tidak punya CanvasGroup/Image — fade dilewati.");
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
