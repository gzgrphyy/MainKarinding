## PLAN.MD — KARINDING GAME: CHAPTER 1 FULL (SCENE 1–9) — v2 RECONCILED

> **Sumber:** Direkonsiliasi langsung terhadap isi repo via Unity MCP scan pada 25 Sep 2026. Semua path di bawah ini adalah path **asli** yang ada di project, kecuali bagian yang ditandai `[BARU — PERLU DIBUAT]`.

---

### 0. RINGKASAN PERUBAHAN DARI DRAFT SEBELUMNYA

Draft PLAN.md lama menganggap beberapa sistem "sudah ada / sudah final, tinggal reuse". Setelah di-scan, itu **tidak akurat**. Koreksi:

| Yang disangka draft lama | Fakta di repo (hasil scan) |
|---|---|
| `Ch1_Hutan.unity`, `MinigameRhythm.unity`, `MinigameCrafting.unity`, `TutorialMinigameRhythm.unity` = scene existing | **Tidak ada satupun.** Cuma ada `Ch1_TerasRumahKebun.unity` + `SampleScene.unity` default. Ke-4 scene ini harus **dibuat baru**. |
| `CraftingPuzzleController`, `RhythmMinigameController`, `KarindingTutorialController` = sistem existing, "sudah final/dikonfirmasi" | Folder `Scripts/Rhythm/` dan `Scripts/Crafting/` **kosong total** (0 file). Begitu juga `Prefabs/Modules/Rhythm/` dan `Prefabs/Modules/Crafting/`. Sistem ini **belum ada sama sekali** — harus dibangun dari nol, bukan sekadar "reuse". |
| `StoryState`/`QuestManager` singleton = sudah ada, sumber kebenaran progres | Tidak ditemukan di manapun di project. **Harus dibuat baru.** |
| Konvensi nama `Rimba_S3_PegangKarindingCanggung.png` (PascalCase) | Repo pakai **snake_case + prefix `s{N}_`**, contoh nyata: `abah_idle_teras_talking.png` (Scene1, tanpa prefix karena posisi folder sudah menandakan scene), `s2_abah_duduk.png` (Scene2, pakai prefix). Registry di bawah mengikuti pola nyata ini. |
| `RimbaSpriteSet`/`AbahSpriteSet` sudah punya slot Scene 2 | Kedua `.cs` ini baru punya field header **"Scene 1"** saja. `s2_rimba_duduk.png` & `s2_abah_duduk.png` sudah ada filenya tapi **belum di-assign ke field manapun** — field Scene 2 harus ditambahkan ke script. |
| `s2_bg_teras_siang.png` = kandidat BG resmi Scene 3/4 | **Dikonfirmasi user: masih data dummy**, bukan final art. Ditandai placeholder di registry. |
| `karinding.fbx` = model 3D siap pakai | Ada & belum dipakai, **1 mesh merged** (dari tim design, tidak ada akses re-export). Highlight per-bagian (Pancepengan/Panabeuh/Cecet Ucing) **tidak bisa** pakai submesh-split → dipakai teknik **overlay child-object terpisah** (dikonfirmasi user), primitif dummy dulu. |
| `Data/Dialogue/Chapter1`, `Data/Codex`, `Data/Input` | Semua kosong. |

**Implikasi ke Claude Code:** Scene 3–9 bukan cuma "sambungin path aset ke scene yang udah ada" — porsi terbesar kerjaannya adalah **membangun sistem minigame Rhythm & Crafting, StoryState singleton, dan 4 scene file baru dari nol**, baru di atasnya dipasang aset (yang sebagian juga dummy).

---

### BAGIAN A — STATUS IMPLEMENTASI AKTUAL (per scan 25 Sep 2026)

#### A.1 Scene Unity

| Scene File | Status | Dipakai untuk |
|---|---|---|
| `Assets/_Project/Scenes/Game/Ch1_TerasRumahKebun.unity` | ✅ **Ada**, Scene 1 & 2 sudah dipasang | Scene 1,2,3,4,6,7,8 (base/home scene) |
| `Ch1_Hutan.unity` | ❌ **[BARU — PERLU DIBUAT]** | Scene 5 & 9 |
| `MinigameRhythm.unity` | ❌ **[BARU — PERLU DIBUAT]** | Scene 7 |
| `MinigameCrafting.unity` | ❌ **[BARU — PERLU DIBUAT]** | Scene 5 (search) & 6 (assembly) |
| `TutorialMinigameRhythm.unity` | ❌ **[BARU — PERLU DIBUAT]** | Scene 3 |

#### A.2 Sistem Inti (Script)

| Sistem | Status |
|---|---|
| `StoryState` / `QuestManager` singleton | ❌ **[BARU]** — tidak ditemukan di project manapun |
| `CraftingPuzzleController`, `CraftingHotspot` | ❌ **[BARU]** — `Scripts/Crafting/` kosong |
| `RhythmMinigameController`, `KarindingTutorialController` | ❌ **[BARU]** — `Scripts/Rhythm/` kosong |
| `Scene1CutsceneController.cs` + sprite/dialog support (`CutsceneSpriteSwapper`, `CutsceneDialogueDisplay`, `FloatingDialogueController`, `CutsceneActionButton`, `CutsceneAudioManager`) | ✅ Ada, dipakai Scene 1 — pola ini jadi referensi untuk bikin controller Scene 2–9 |
| `RimbaWalker`, `AbahAutoWalker`, `AnalogStickController`, `WalkOffTrigger` (Exploration) | ✅ Ada, dipakai Scene 1 kebun-path |

`Ch1Beat` enum di draft lama (Prolog…S9_EndChapter) belum ada di kode manapun — harus dibuat bareng `StoryState`.

#### A.3 Keputusan Terkonfirmasi (update dari sesi sebelumnya)

1. **`karinding.fbx`** — 1 mesh merged, tidak bisa di-split. Highlight Scene 2 (dan referensi Scene 6) pakai **overlay child-object** (primitif sederhana di-parent ke model, posisi menutupi tiap bagian, toggle emissive/warna saat Abah menyebut nama bagian). Boleh dummy dulu (primitif polos), tinggal ganti shape/material kalau ada waktu.
2. **`s2_bg_teras_siang.png`** — dummy/placeholder, ditandai sebagai BG kerja Scene 2 sementara (bukan final).
3. Scene 3 pakai `TutorialMinigameRhythm.unity` (perlu dibangun) sebagai isinya — **default: ya**.
4. Scene 9 reuse framing/kamera Prolog kalau Prolog sudah jadi scene terpisah; kalau belum, fallback ke "RealWorld state" toggle di `Ch1_Hutan.unity` — **status Prolog sebagai scene terpisah: belum ada, jadi pakai fallback ini sebagai default.**

---

### BAGIAN B — KONVENSI PENAMAAN (diambil dari pola nyata di repo)

```
Karakter    : Art/Characters/{Rimba|Abah}/Scene{N}/s{N}_{nama}_{deskripsi}.png
              (khusus Scene1 tanpa prefix "s1_", tapi Scene2 dst pakai prefix — ikuti pola Scene2 utk seterusnya)
Environment : Art/Environment/Chapter1/Scene{N}/s{N}_bg_{deskripsi}.png
3D          : Art/Models/{Nama}/{nama}.fbx
Prop        : Art/Items/prop_{nama}.png
Audio SFX   : Audio/SFX/{Kategori}/sfx_{deskripsi}.wav
Audio Music : Audio/Music/Ambience/ambience_{preset}.wav
UI          : Art/UI/{Kategori}/ui_{deskripsi}.png
Script      : Scripts/{Modul}/{NamaKelas}.cs (PascalCase, sesuai konvensi C# yang sudah ada)
Scene       : Ch1_{Lokasi}.unity / {NamaMinigame}.unity (sudah sesuai draft lama, tidak berubah)
```

---

### BAGIAN C — MASTER ASSET REGISTRY: SCENE 1 & 2 (EXISTING, path asli)

#### C.1 Scene 1 — final, jangan diubah

**Karakter — Abah** (`Art/Characters/Abah/Scene1/`)
`abah_idle_teras_talking.png`, `abah_lookout_facing_mountain.png`, `abah_walk_right_1.png..4.png`

**Karakter — Rimba** (`Art/Characters/Rimba/Scene1/`)
`rimba_idle_pintu.png`, `rimba_lookout_facing_abah.png`, `rimba_lookout_facing_mountain.png`, `rimba_lookout_noleh1.png`, `rimba_lookout_noleh2.png`, `rimba_walk_right_1.png..4.png`

**Environment** (`Art/Environment/Chapter1/Scene1_TerasRumah/`)
`s1_bg_wide_establishing.png`, `s1_bg_lookout_window.png`, `s1_bg_lookout_mountain_sky.png`

**Environment — Exploration path** (`Art/Environment/Chapter1/Scene1_Exploration/`)
`explore_path_terasketebon_near.png`, `_mid.png`, `_far.png` (parallax 3 lapis)

**UI** — `Art/UI/ActionButtons/s1_ui_actionbutton_lihat.png`, `s1_ui_tapindicator.png`; `Art/UI/Analog/ui_analog_base.png`, `ui_analog_knob.png` (generic, reusable); `Art/UI/Dialogue/s1_ui_dialoguebox.png` (generic, reusable)

**Audio** — `Audio/Music/Ambience/s1_ambience_morning.wav`; `Audio/SFX/Transitions/s1_sfx_transition_blackscreen.wav`; `Audio/SFX/UI/s1_sfx_tap_confirm.wav`

**ScriptableObject** — `Data/Cutscenes/Chapter1/Scene1/RimbaSpriteSet.asset`, `AbahSpriteSet.asset`

#### C.2 Scene 2 — **BELUM SELESAI** (bukan "final, terpisah" seperti anggapan draft lama)

| File | Status |
|---|---|
| `Art/Characters/Abah/Scene2/s2_abah_duduk.png` | ✅ Ada, **belum di-wire** ke `AbahSpriteSet.cs` (field Scene 2 belum ada di script) |
| `Art/Characters/Rimba/Scene2/s2_rimba_duduk.png` | ✅ Ada, **belum di-wire** ke `RimbaSpriteSet.cs` |
| `Art/Environment/Chapter1/Scene2/s2_bg_teras_siang.png` | ✅ Ada, **status dummy** (dikonfirmasi user) |
| `Art/Models/Karinding/karinding.fbx` | ✅ Ada, belum dipakai, 1 mesh merged |
| `KarindingHighlightController.cs` (overlay highlight) | ❌ **[BARU]** belum ada |
| UI 3-opsi dialog choice (Opsi A/B/C) | ❌ **[BARU]** belum ada komponennya — lihat `D.5` |

**Aksi Claude Code untuk Scene 2:** tambahkan field baru `[Header("Scene 2")] public Sprite duduk;` ke `RimbaSpriteSet.cs` & `AbahSpriteSet.cs`, assign ke file existing di atas. Bangun `KarindingHighlightController.cs` (overlay 3 child object dummy) + wire ke `karinding.fbx`. Bangun UI branching 3-opsi (lihat `D.5`). Dialog lengkap: lihat **Bagian E2**.

---

### BAGIAN D — MASTER ASSET REGISTRY: SCENE 3–9 `[BARU — PERLU DIBUAT, nama di bawah bebas kamu ganti]`

#### D.1 Karakter

| File yang perlu dibuat | Scene | Catatan |
|---|---|---|
| `Art/Characters/Rimba/Scene3/s3_rimba_pegang_canggung.png` | S3 | sebelum minigame |
| `Art/Characters/Rimba/Scene3/s3_rimba_bangga.png` | S3 | setelah minigame sukses |
| `Art/Characters/Abah/Scene3/s3_abah_serahkan.png` | S3 | menyerahkan karinding |
| `Art/Characters/Abah/Scene3/s3_abah_senyum_tipis.png` | S3 | closing line |
| `Art/Characters/Abah/Scene4/s4_abah_bercerita.png` | S4 | **cek dulu** — coba reuse `abah_idle_teras_talking.png` (S1) atau `s2_abah_duduk.png` sebelum bikin baru |
| `Art/Characters/Abah/Scene5/s5_abah_ajak_teras.png` | S5 | dialog ajak ke hutan |
| `Art/Characters/Rimba/Scene5/s5_rimba_mencari_hutan.png` | S5 | idle/searching pose di hutan |
| `Art/Characters/Abah/Scene5/s5_abah_reaksi_muda.png` / `_tipis.png` / `_pas.png` | S5 | 3 reaksi terima bambu |
| `Art/Characters/Rimba/Scene5/s5_rimba_menyerahkan.png` | S5 | drag-to-Abah handoff |
| `Art/Characters/Abah/Scene6/s6_abah_kerja_bareng.png` | S6 | fokus di meja kerja |
| `Art/Characters/Abah/Scene6/s6_abah_encourage.png` / `s6_abah_koreksi.png` | S6 | bark sukses / bark salah taruh |
| `Art/Characters/Rimba/Scene7/s7_rimba_main_sendiri.png` | S7 | selama minigame kedua |
| `Art/Characters/Rimba/Scene7/s7_rimba_freeplay_senang.png` | S7 | free-play moment |
| `Art/Characters/Abah/Scene8/s8_abah_duduk_sore.png` | S8 | awal scene, nada tenang |
| `Art/Characters/Abah/Scene8/s8_abah_bersiap.png` | S8 | berdiri, ikat bedog |
| `Art/Characters/Rimba/Scene8/s8_rimba_cemas.png` | S8 | "Abah, bade kamana?" |
| `Art/Characters/Rimba/Scene8/s8_rimba_berlari.png` | S8 | chase (bisa 1 pose statis dulu; kalau mau animasi jalan, ikuti pola `walkRight[4]` jadi `s8_rimba_berlari_1..N.png`) |
| `Art/Characters/Rimba/Scene8/s8_rimba_meraih.png` | S8 | tap/hold reach prompt |
| `Art/Characters/Abah/Scene8/s8_abah_menoleh_terakhir.png` | S8 | "Moal Kataekan" |
| `Art/Characters/Rimba/Scene9/s9_rimba_sandar_pohon.png` | S9 | cold wide-shot, mirror Prolog |
| `Art/Characters/Rimba/Scene9/s9_rimba_meraba_pinggang.png` | S9 | tangan meraba, kosong |
| `Art/Characters/Rimba/Scene9/s9_rimba_berdiri.png` | S9 | penutup chapter |

#### D.2 Environment

| File | Scene | Catatan |
|---|---|---|
| `Art/Environment/Chapter1/Scene2/s2_bg_teras_siang.png` | S2, S3, S4 | **sudah ada tapi dummy** — reuse untuk 3 scene ini (tint sama, framing tidak berubah per instruksi naskah) |
| `Art/Environment/Chapter1/Scene5/s5_bg_hutan_mimpi_wide.png` | S5 | area search, 3 hotspot bambu ditanam di sini |
| — (reuse `s1_bg_wide_establishing.png` / `s1_bg_lookout_window.png`, tint "Siang") | S5 (bagian rumah), S6, S7 | tidak perlu BG baru |
| `Art/Environment/Chapter1/Scene8/s8_bg_teras_sore.png` **atau** shader tint "Sore" di atas BG lookout existing | S8 | **rekomendasi:** tint preset baru, bukan file terpisah (konsisten sama sistem lighting preset) |
| `Art/Environment/Chapter1/Scene9/s9_bg_hutan_nyata_wide.png` | S9 | kondisional — lihat Keputusan #4 (fallback: state baru di `Ch1_Hutan.unity`), cold/desaturated, mirror Prolog |

#### D.3 3D & Prop

| Aset | Scene | Catatan |
|---|---|---|
| `Art/Models/Karinding/karinding.fbx` | S2, S6 | sudah ada — dipakai dengan overlay highlight, bukan submesh |
| Overlay highlight (3x child GameObject, primitif dummy — **tidak perlu file art**, dibuat langsung di Unity) | S2 | Pancepengan / Panabeuh / Cecet Ucing |
| `Art/Items/prop_bedog.png` | S8 | terikat di pinggang Abah saat "bersiap-siap" |

#### D.4 Audio

| File | Scene | Catatan |
|---|---|---|
| `Audio/Music/Ambience/ambience_siang.wav`, `ambience_sore.wav` | S2-S8 | pelengkap `s1_ambience_morning.wav` yang sudah ada (preset Pagi) |
| `Audio/SFX/Karinding/sfx_karinding_dengung_low.wav` / `_mid.wav` / `_high.wav` | S2, S3, S6, S7 | belum ada di repo — cek dulu apakah tim audio sudah punya sebelum bikin baru |
| `Audio/SFX/Karinding/sfx_bigbang_boom.wav` | S4 | trigger tepat di kata "...bledag!" |
| AudioMixer snapshot `Dropout_S4` (bukan file, dibuat di dalam AudioMixer asset) | S4 | duck ambient sepersekian detik, auto-return |
| `Audio/SFX/Bambu/sfx_gesek_bambu.wav`, `sfx_potong_bambu.wav` | S5, S6 | search + cut-phase |
| `Audio/SFX/Rimba/sfx_lari_rimba.wav` | S8 | footstep cepat selama chase |
| `Audio/SFX/UI/sfx_whoosh_raih.wav` | S8 | konsisten folder dengan `s1_sfx_tap_confirm.wav` yang sudah ada |
| `Audio/SFX/Transitions/sfx_whiteout.wav`, `sfx_transisi_hutan_nyata.wav` | S9 | konsisten folder dengan `s1_sfx_transition_blackscreen.wav`; crossfade bukan cut keras |

#### D.5 UI Baru

| Komponen | Scene | Catatan |
|---|---|---|
| `Art/UI/DialogueChoice/ui_choice_button.png` + `Scripts/Cutscenes/DialogueChoicePrompt.cs` | S2 | 3 tombol pilihan (Opsi A/B/C), tampil bareng, player pilih salah satu → lanjut ke jawaban Abah yang sesuai. Reuse styling `s1_ui_dialoguebox.png` kalau bisa. |
| `Art/UI/HoldButtonPrompt/ui_holdbutton_radial.png` + `Scripts/UI/HoldButtonPrompt.cs` | S8 | radial-fill, tap-and-hold, generic reusable (beda dari tap-button Scene 1) |
| `Scripts/Crafting/NPCHandoffDropTarget.cs` | S5 | drop target di atas sprite Abah, drag-source reuse dari crafting drag component baru |

#### D.6 Script & Sistem Baru `[semua BARU, lihat Bagian A.2]`

| Script | Modul | Dipakai |
|---|---|---|
| `Scripts/Core/StoryState.cs` (singleton `DontDestroyOnLoad`, termasuk enum `Ch1Beat`) | Core | Semua scene 1-9 — sumber kebenaran progres |
| `Scripts/Rhythm/RhythmMinigameController.cs` | Rhythm | S3 (via tutorial wrapper), S7 (standard + free-play flag) |
| `Scripts/Rhythm/KarindingTutorialController.cs` | Rhythm | S3 — Prolog→HandPlacement→RhythmSabar(3 note)→Resonansi(7 note forced-fail-then-succeed)→Epilog |
| `Scripts/Crafting/CraftingPuzzleController.cs`, `CraftingHotspot.cs` | Crafting | S5 (search, no-decoy 3 hotspot), S6 (assembly Cut 0-50% / Tap 50-100%) |
| `Scripts/Karinding/KarindingHighlightController.cs` | Karinding | S2 — toggle overlay per tag saat Abah sebut nama bagian |
| `Scripts/Cutscenes/Scene{2..9}CutsceneController.cs` | Cutscenes | ikuti pola `Scene1CutsceneController.cs` yang sudah ada |

---

### BAGIAN E — BEAT BREAKDOWN PER SCENE (1–9)

> Behavior rules & dialog Sunda mengikuti naskah asli (`Storyline2.docx`) — tidak diparafrase, tidak dipotong. Scene 1 & 2 dimasukkan penuh di sini juga karena Scene 2 statusnya belum selesai (lihat `C.2`), bukan dokumen terpisah seperti anggapan draft lama.

#### E1. Scene 1 — Teras Rumah, Pagi
**Scene file:** `Ch1_TerasRumahKebun.unity` — ✅ sudah final/terpasang, breakdown di bawah untuk referensi/cross-check saja

| Beat | Isi | StoryState | Aset |
|---|---|---|---|
| 1 | Rimba berdiri di depan pintu, melihat Abah menyiapkan sarapan di teras | `S1_SpawnEstablishing` | `rimba_idle_pintu.png`, `abah_idle_teras_talking.png`, `s1_bg_wide_establishing.png` |
| 2 | Transisi layar hitam → Rimba & Abah duduk berdua di teras setelah cuci muka | `S1_CuciMuka` | `s1_sfx_transition_blackscreen.wav` |
| 3 | Abah menatap sejenak ke gunung yang terlihat dari teras, bicara pelan hampir bergumam | `S1_ShotDuaMenghadapGunung` | `abah_lookout_facing_mountain.png`, `rimba_lookout_facing_abah.png`, `s1_bg_lookout_mountain_sky.png` / `s1_bg_lookout_window.png` |
| 4 | Interaksi: player tap supaya Rimba ikut menoleh ke arah yang sama | `S1_TapMenoleh` | `rimba_lookout_noleh1.png`, `rimba_lookout_noleh2.png`, `rimba_lookout_facing_mountain.png`, `s1_ui_tapindicator.png` |
| 5 | Dialog inti (lihat tabel di bawah) | `S1_DialogGunung` | — |
| 6 | Transisi ke kebun: layar hitam → area setapak, player-controlled walk mengikuti Abah sampai kebun → cut ke Scene 2 (tanpa menunjukkan aktivitas kebun) | `S1_HandoffKebun` | `rimba_walk_right_1..4.png`, `abah_walk_right_1..4.png`, `explore_path_terasketebon_near/mid/far.png` |

**Dialog Tabel:**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Baba tos gugah gening? Sok gera wawasuh heula." | Baba udah bangun ternyata? Sana cuci muka dulu. |
| Abah *(menatap ke gunung, bicara pelan hampir bergumam)* | "Aya jalan nu kudu dilakonan ku nyalira, Ba. Abah keur ngora ge kitu." | Ada jalan yang harus ditempuh sendiri, Ba. Ketika Abah muda juga begitu. |
| Rimba | "Maksudna kumaha bah?" | Maksudnya gimana bah? |
| Abah | "Moal karasa ayeuna. Engke ge ngarti sorangan." | Nggak bakal kerasa sekarang. Nanti juga ngerti sendiri. |
| *(narasi: Rimba kebingungan, berusaha memahami maksud Abah)* | | |
| Abah | "Tos tong di pikiran teuing. Ayeuna mah urang ka kebon we hayu, bisi kaburu panas." | Udah, jangan terlalu dipikirin. Sekarang kita ke kebun aja, takutnya keburu panas. |
| Rimba | "Muhun bah, Hayu." | Iya bah, ayo. |

Behavior: tap-to-noleh (Beat 4) murni interaksi kecil, tidak ada fail-state. Setelah sampai kebun, langsung cut ke Scene 2 — tidak ada aktivitas kebun yang ditampilkan.

---

#### E2. Scene 2 — Pengenalan Karinding `[BELUM SELESAI — lihat C.2]`
**Scene file:** `Ch1_TerasRumahKebun.unity` (reuse Shot 2 Scene 1, tint Siang — pakai `s2_bg_teras_siang.png` dummy)

| Beat | Isi | StoryState | Aset |
|---|---|---|---|
| 1 | Siang hari, mereka istirahat di teras. Abah keluarkan Karinding, mainkan perlahan — dengung khas mengalun pelan. Rimba penasaran memperhatikan | `S2_AbahMainKarinding` | `s2_bg_teras_siang.png` (dummy), `s2_abah_duduk.png`, `s2_rimba_duduk.png`, `karinding.fbx`, SFX dengung ambient *(cek reuse `sfx_karinding_dengung_low.wav` atau butuh clip terpisah yang lebih soft/ambient)* |
| 2 | Dialog intro (lihat tabel) | `S2_IntroDialog` | — |
| 3 | Titik interaksi: player pilih 1 dari 3 opsi pertanyaan (lihat tabel Opsi A/B/C) | `S2_PilihanOpsi` | `DialogueChoicePrompt` (lihat `D.5`) |
| 4 | Karinding ditampilkan di tengah layar, player bisa putar/geser lihat tiap bagian dari sudut berbeda. Abah jelasin 3 bagian satu-satu, tiap kali disebut bagian itu menyala/highlight di model | `S2_ModelHighlight` | `karinding.fbx` + overlay highlight (`KarindingHighlightController`, lihat `D.3`) |
| 5 | Closing filosofi (lihat tabel) | `S2_ClosingFilosofi` | — |

**Dialog Tabel Beat 2 (intro):**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Rimba | "Eta naon, bah? naha kitu sora na?" | Itu apa, bah? Kenapa suaranya begitu? |
| Abah | "Ieu teh nami na Karinding, lain ngan saukur alat bebunyian." | Ini namanya Karinding, bukan cuma alat bebunyian biasa. |

**Dialog Tabel Beat 3 (Opsi A/B/C — player pilih salah satu):**

| Opsi | Rimba tanya (Sunda) | Terjemahan | Jawaban Abah (Sunda) | Terjemahan |
|---|---|---|---|---|
| A | "Rimba nembe ningal, naha jarang aya nu maenkeun bah?" | Rimba baru lihat, kenapa jarang ada yang mainin, bah? | "Tos lami teu diwariskeun, Ba. Kapungkur mah unggal jalmi teh aya. ayeuna mah ngan sakedik nu terang." | Udah lama nggak diwariskan, Ba. Dulu tiap orang punya. Sekarang cuma sedikit yang tau. |
| B | "Kumaha cara maenna bah?" | Gimana cara mainnya, bah? | "Teu hese, tapi teu gampang oge. Kudu cepeung sing yakin, tabeuh sing sabat." | Nggak susah, tapi nggak gampang juga. Harus pegang dengan yakin, pukul dengan sabar. |
| C | "Naha abah tiasa gaduh?" | Kenapa abah bisa punya? | "Ieu teh Uyut didinya kapungkur, di wariskeun ka Abah. Ayeuna abah nu nyepeung." | Ini dari Uyut dulu, diwariskan ke Abah. Sekarang Abah yang pegang. |

**Flagged Design Note:** naskah asli nulis "player pilih salah satu" tapi nggak jelas apakah player boleh buka menu lagi buat lihat 2 opsi lain sebelum lanjut ke Beat 4, atau begitu pilih 1 opsi langsung lanjut (dan 2 opsi lain hilang selamanya). **Default direkomendasikan:** sekali pilih, lanjut (opsi lain hilang) — supaya konsisten dengan filosofi "kudu yakin" di narasi. Koreksi kalau maksudnya beda.

**Dialog Tabel Beat 4 (penjelasan bagian, trigger highlight per baris):**

| Bagian | Baris Abah (Sunda, ringkas) | Makna |
|---|---|---|
| Pancepengan | "cepeung sing yakin" | Bagian pegangan — mengajarkan keyakinan dalam memegang & menjalankan sesuatu |
| Panabeuh | "tabeuh sing sabar" | Bagian yang dipukul/digetarkan — mengajarkan kesabaran dalam menghasilkan getaran |
| Cecet Ucing | "kudu sadar lamun eta lain sora urang" | Bagian kecil pembantu resonansi, hingga suara benar-benar terdengar |

**Dialog Tabel Beat 5 (closing):**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Unggal bagian aya hartina, Baba. Sami oge sareng kahirupan, kudu yakin, kudu sabar, jeung kudu sadar lamun arurang tos menang hasil tina naon nu dilakonan." | Setiap bagian ada artinya, Baba. Sama juga dengan kehidupan, harus percaya, harus sabar, dan sadar kalau kita udah dapat hasil dari yang dijalani. |

Behavior: highlight per-bagian di Beat 4 pakai overlay child-object (bukan submesh, lihat Keputusan #1) — nyala tepat saat Abah ucapkan nama bagian, mati lagi setelah lanjut ke bagian berikutnya (jangan overlap 2 highlight nyala bersamaan).

---

#### E3. Scene 3 — Belajar Memainkan Karinding
**Scene file:** `Ch1_TerasRumahKebun.unity` + additive `TutorialMinigameRhythm.unity` `[keduanya perlu dibangun]`
**Lokasi:** Teras (reuse `s2_bg_teras_siang.png`, dummy)

| Beat | Isi | StoryState | Aset |
|---|---|---|---|
| 1 | Abah serahkan karinding, handoff singkat | `S3_Handoff` | `s3_abah_serahkan.png` |
| 2 | Load additive tutorial rhythm, jalankan `KarindingTutorialController` | `S3_TutorialRhythm` | `s3_rimba_pegang_canggung.png`, `sfx_karinding_dengung_low/mid/high.wav` |
| 3 | Unload minigame, dialog closing | `S3_ClosingDialog` | `s3_rimba_bangga.png`, `s3_abah_senyum_tipis.png` |

**Dialog Tabel (Beat 3, closing):**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Ulah maksa, Rimba. Lain kuat na leungeun nu di penta, tapi tenang jeung sabar na haté." | Jangan maksa, Rimba. Bukan kekuatan tangan yang diminta, tapi sabar dan ketenangan hati. |
| *(narasi: Rimba tersenyum lebar, bangga dengan pencapaiannya sendiri)* | | |
| Abah *(tersenyum tipis)* | "Omat eta lain sora Rimba, tapi hasil tina getaran nu di barengan ku resonansi." | Ingat, itu bukan suara Rimba, tapi hasil dari getaran yang dibarengi dengan resonansi. |

Behavior: baris "Ulah maksa" tampil SEBELUM minigame mulai (sebagai encouragement, bukan interupsi mid-gameplay — sistem minigame tidak mendukung pause-for-dialogue di tengah main); forced-fail-then-succeed tetap no-game-over; handoff balik ke teras tidak mengubah posisi karakter.

#### E4. Scene 4 — Filosofi Getaran dan Kehidupan
**Scene file:** `Ch1_TerasRumahKebun.unity` — dialogue-only, tidak ada scene tambahan
**Lokasi:** sama persis Scene 3, tidak ada perpindahan kamera

| Beat | Isi | Aset |
|---|---|---|
| 1 | Dialog panjang Abah soal Big Bang & Alam Kosong | `s4_abah_bercerita.png` (cek reuse dulu) |
| 2 | Tepat di "...bledag!": VFX flash + SFX + audio dropout | `sfx_bigbang_boom.wav`, AudioMixer snapshot `Dropout_S4` |

**Dialog Tabel:**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Jadi orang Sunda teh percanten lamun alam semesta ieu teh ka bentuk tina dentuman tarik, nyaeta Big Bang. Getaran jeung dengung teh dianggap awal tina kahirupan, Ba. Jadi sateuacan aya na kahirupan teh aya nu nami na Alam Kosong, awang uwung tong enong margana enong, kosong suwung eweuh nanaon didinya. Alam ieu ngadadak endeur jeung ngalahirkeun sora dengung, laksaketi mangrewu sora tawon Ngung… **bledag!** Tah tina getaran jeung dengung eta lah lahir kahirupan." | Getaran dan dengung dianggap awal dari kehidupan, Ba. Jadi sebelum adanya kehidupan itu ada yang namanya Alam Kosong, awang uwung tong enong margana enong, kosong melompong nggak ada apa-apa di sana. Tiba-tiba alam bergetar dan melahirkan suara dengung, laksaketi mangrewu sora tawon Ngung… **meledak!** Nah dari getaran dan dengung itulah lahir kehidupan. |

**Trigger point:** VFX/SFX/audio-dropout dipicu TEPAT di kata **"bledag!"** di tengah paragraf (bold di atas) — bukan di akhir baris/paragraf. Text-render dialog harus bisa expose event/callback di karakter tertentu dalam string, bukan cuma "OnLineComplete".

Behavior: framing & dialog box TIDAK terganggu (VFX cuma di background); clue halus ini JANGAN diberi indikator/highlight tambahan. Setelah dialog: fade to black + title card "HARI KEDUA" → Scene 5.

#### E5. Scene 5 — Membuat Karinding Sendiri (Hutan)
**Scene file:** `Ch1_TerasRumahKebun.unity` → `LoadScene(Ch1_Hutan)` `[baru]` → additive `MinigameCrafting.unity` `[baru]` search-phase → `LoadScene(Ch1_TerasRumahKebun)` lagi

| Beat | Isi | StoryState | Aset |
|---|---|---|---|
| 1 | Dialog ajak ke hutan | `S5_AjakHutan` | `s5_abah_ajak_teras.png` |
| 2 | Search-phase 3 hotspot bambu, no-decoy | `S5_SearchBambu` | `s5_bg_hutan_mimpi_wide.png`, `s5_rimba_mencari_hutan.png`, `sfx_gesek_bambu.wav` |
| 3 | Load balik ke teras | `S5_KembaliRumah` | — |
| 4 | Drag & drop bambu ke `NPCHandoffDropTarget` di atas Abah, reaksi per-tag | `S5_HandoffBambu` | `s5_rimba_menyerahkan.png`, `s5_abah_reaksi_muda/tipis/pas.png` |

**Dialog Tabel Beat 1 (ajak ke hutan):**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Rimba, hayu urang milarian awi ka leuweung. Karinding Rimba kudu di jieun ku panangan Rimba sorangan." | Rimba, ayo kita nyari bambu di hutan. Karinding Rimba harus dibuat sama tangan Rimba sendiri. |
| Rimba | "Kumaha cara milih awi nu sae, Bah?" | Gimana cara milih bambu yang bagus, Bah? |
| Abah | "Awi nu sae teh nu tos cekap umur na. teu ngora teuing, teu kolot teuing. Batang na ge kudu seseug, ulah nu ipis." | Bambu yang bagus itu yang udah cukup umur nya. Nggak terlalu muda, nggak terlalu tua. Batangnya juga harus padat, jangan kopong. |

**Dialog Tabel Beat 4 (reaksi per-tag bambu, urutan drag bebas — bukan hardcode slot):**

| Tag Bambu | Baris Abah (Sunda) | Terjemahan |
|---|---|---|
| `Muda` | "Ieu mah... acan cekap umurna, Ba. Masih keneh hejo." | Ini mah... belum cukup umurnya, Ba. Masih hijau. |
| `Tipis` | "Ieu ge ipis teuing, gampang belah." | Ini juga terlalu tipis, gampang patah. |
| `Pas` | "Tah... ieu pas. Teu ngora teuing, teu kolot teuing, seseug deuih. Ieu nu rék dipake nyieun Karinding Rimba." | Nah... ini pas. Nggak terlalu muda, nggak terlalu tua, padat juga. Ini yang bakal dipake bikin Karinding Rimba. |

Behavior: search-phase sendiri TIDAK berubah dari desain no-decoy `CraftingPuzzleController` — hanya reaksi naratif setelah handoff yang baru. Reaksi berbasis tag metadata bambu, bukan urutan drag. Setelah 3 bambu selesai di-drop: langsung cut ke Scene 6, tanpa transisi scene tambahan.

#### E6. Scene 6 — Membuat Karinding Sendiri (Assembly)
**Scene file:** `Ch1_TerasRumahKebun.unity` + additive `MinigameCrafting.unity` assembly-phase (Cut 0-50%, Tap 50-100%)

| Karakter | Teks (Sunda) | Terjemahan | Trigger | Aset |
|---|---|---|---|---|
| Abah | "Bagian-bagian na tos aya, Ba. Ayeuna kari nyusun jeng ngantelkeun na. nepi ka ngabentuk Karinding." | Bagian-bagiannya udah ada, Ba. Sekarang tinggal susun dan tempelin, sampai jadi bentuk Karinding. | Awal Scene 6, sebelum Cut phase mulai | `s6_abah_kerja_bareng.png` |
| Abah | "Nya kitu, Ba. Titen jeung teu gagabah, eta nu disebat cermat teh." | Iya kaya gitu, Ba. Teliti dan nggak gegabah, itu yang disebut cermat. | Sekali, saat crossing Cut→Tap (50%) atau input benar pertama | `s6_abah_encourage.png` |
| Abah | "Sanes didinya, Ba. Titenan deui bentuk na, meh pas." | Bukan di situ, Ba. Perhatiin lagi bentuknya, biar pas. | **Sekali saja**, di wrong-input PERTAMA — lihat catatan konflik di behavior rules | `s6_abah_koreksi.png` |

Behavior: TETAP no visual penalty/shake, tambahkan voice-bark one-time saja supaya tidak spam (selaras no-fail philosophy). Tuning: `cutMinStraightness 0.85`, `tapDecayGraceTime 0.5s`, `tapDecayRate 7%/s`.

#### E7. Scene 7 — Main Karinding Buatan Sendiri
**Scene file:** `Ch1_TerasRumahKebun.unity` + additive `MinigameRhythm.unity` (standard, bukan tutorial)

| Beat | Isi | StoryState | Aset |
|---|---|---|---|
| 1 | Dialog pembuka, rhythm minigame standar | `S7_RhythmKedua` | `s7_rimba_main_sendiri.png` |
| 2 | Sukses → closing → free-play tap bebas | `S7_FreePlay` | `s7_rimba_freeplay_senang.png` |
| 3 | Pilihan "Main Lagi" (chart Hard) atau lanjut | `S7_PilihanMainLagi` | — |

**Dialog Tabel:**

| Karakter | Teks (Sunda) | Terjemahan |
|---|---|---|
| Abah | "Sok cobaan, Ba. Ieu mah benteun, jieunan Rimba sorangan." | Coba, Rimba. Ini mah beda, buatan Rimba sendiri. |
| Abah | "Ayeuna Rimba tos gaduh jeung tiasa maenkeun Karinding. Tapi Rimba ge kudu emut eta lain ngan saukur sora ti Rimba." | Sekarang Rimba udah punya Karinding sendiri dan bisa memainkannya. Tapi Rimba juga harus sadar kalo itu bukan cuma suara dari Rimba. |

Setelah beberapa detik free-play: dikasih choice mau main lagi atau nggak — kalau main lagi, saran naskah: kasih kesulitan Hard.

Behavior: free-play pakai flag `freePlayMode = true` (matikan scoring/miss-detection), jangan reuse full controller state. Kalau chart Hard belum ada: skip opsi, langsung lanjut Scene 8 (tidak blocking).

#### E8. Scene 8 — Sore Hari Sebelum Abah Pergi
**Scene file:** `Ch1_TerasRumahKebun.unity`, lighting preset **Sore** (baru)

| Karakter | Teks (Sunda) | Terjemahan | Catatan | Aset |
|---|---|---|---|---|
| Abah | "Rimba... Karinding ieu ulah ngan saukur jang Rimba nyalira. Abah hoyong nu sanes ge terang kana alat bebunyian ieu." | Rimba... Karinding ini jangan cuma buat kamu sendiri. Abah ingin orang lain juga tau sama alat bebunyian ini. | Nada pelan, menatap gunung | `s8_abah_duduk_sore.png` |
| Rimba | "Maksudna kumaha, Bah?" | Maksudnya gimana, Bah? | | — |
| *(narasi: Abah tidak langsung menjawab, berdiri, bersiap-siap seperti hendak pergi mengambil bekal seadanya, mengikat `prop_bedog.png` di pinggangnya)* | | | **Petunjuk halus terkuat chapter ini** — "mengikat sesuatu di pinggang" (bedog), tergantung desainer | `s8_abah_bersiap.png`, `prop_bedog.png` |
| Rimba | "Abah, bade kamana?" | Abah mau kemana? | Mendekat, cemas | `s8_rimba_cemas.png` |
| Abah | "Néangan nu eweuh." | Mencari sesuatu yang tidak ada. | | — |
| Rimba | "Dimana bah nu eweuh teh?" | Dimana bah yang gaada itu? | Abah hanya tersenyum kecil, tidak menjawab, mulai melangkah menjauh dari rumah | — |
| *(player-controlled)* | Rimba mengejar Abah beberapa langkah | | Input gerak singkat, BUKAN cutscene | `s8_rimba_berlari.png`, `sfx_lari_rimba.wav` |
| *(interaksi)* | Prompt `HoldButtonPrompt`: "coba raih tangan Abah" | | Apapun hasilnya, Abah tetap menghilang — **no fail-state** | `s8_rimba_meraih.png`, `sfx_whoosh_raih.wav` |
| Abah *(menoleh sekali, sebelum hilang di balik pepohonan)* | "Moal Kataekan" | *(sengaja tidak diterjemahkan di naskah — biarkan makna ambigu tetap terjaga, jangan tambahkan terjemahan di UI)* | Baris penutup chapter | `s8_abah_menoleh_terakhir.png` |

Behavior: chase & reach cosmetic, tidak ada branching hasil sama sekali.

#### E9. Scene 9 — Rimba Terbangun
**Scene file:** kondisional (lihat Keputusan #4) — default: state baru "RealWorld" di `Ch1_Hutan.unity`

| Beat | Isi | Aset |
|---|---|---|
| 1 | Whiteout + crossfade audio | `sfx_whiteout.wav` → `sfx_transisi_hutan_nyata.wav` |
| 2 | Cut ke wide-shot dingin, Rimba sendirian | `s9_bg_hutan_nyata_wide.png`, `s9_rimba_sandar_pohon.png` |
| 3 | Tangan meraba pinggang, kosong | `s9_rimba_meraba_pinggang.png` |
| 4 | Rimba *(bergumam pelan)*: "...Moal kataekan." — Rimba baru sadar semua yang dialaminya cuma mimpi | — |
| 5 | Rimba berdiri, End of Chapter 1 | `s9_rimba_berdiri.png` |

Behavior: kamera WAJIB beda dari framing hangat two-shot sepanjang mimpi. Tidak ada interaksi player selain lanjut ke Chapter 2.

---

### BAGIAN F — DEFINITION OF DONE (CHAPTER 1 PENUH, update)

- [ ] `StoryState` singleton + `Ch1Beat` enum dibangun & dipakai konsisten di semua scene
- [ ] Scene 1 & 2 terintegrasi tanpa regresi (Scene 2: field Scene2 ditambahkan ke SpriteSet, highlight overlay karinding jalan)
- [ ] `TutorialMinigameRhythm.unity` + `KarindingTutorialController` **dibangun dari nol**, terhubung sebagai isi Scene 3
- [ ] Scene 4: VFX flash + SFX dropout trigger tepat di "bledag!", tidak mengganggu dialog box
- [ ] `Ch1_Hutan.unity` **dibangun dari nol**; `MinigameCrafting.unity` **dibangun dari nol** (search-phase no-decoy + assembly Cut/Tap)
- [ ] Scene 5: handoff drag-to-Abah 3 reaksi tag berjalan; transisi Hutan↔Teras tidak merusak StoryState
- [ ] Scene 6: assembly phase + one-time wrong-input bark tanpa penalty apa pun
- [ ] `MinigameRhythm.unity` **dibangun dari nol**; Scene 7: rhythm standar + free-play mode terpisah dari scoring
- [ ] Scene 8: chase + hold-prompt cosmetic, tidak ada fail-state, `prop_bedog.png` muncul tepat waktu
- [ ] Scene 9: environment hutan nyata terpasang, whiteout+crossfade audio, trigger ke Chapter 2
- [ ] Semua placeholder (overlay highlight karinding, `s2_bg_teras_siang.png`, SFX yang belum dicek ada/nggaknya) tertandai jelas di kode/komentar untuk gampang di-swap
