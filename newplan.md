# PLAN.md — Karinding Chapter 1 (Master Reference)

> **CATATAN BUAT CLAUDE CODE:** Dokumen ini punya 2 bagian besar.
> - **BAGIAN A** = konteks cerita penuh Chapter 1, HANYA UNTUK REFERENSI. Jangan diimplementasikan sekarang.
> - **BAGIAN B** = task yang HARUS dikerjakan SEKARANG: Scene 1 sampai bisa dimainkan penuh.
> Kalau ada instruksi yang ambigu, prioritaskan BAGIAN B. Jangan bikin sistem buat Scene 2-8 kecuali diminta eksplisit.

---

# BAGIAN A — KONTEKS CERITA (REFERENSI, BELUM DIKERJAKAN)

## A.1 Sinopsis
Rimba, anak kecil di sebuah desa, dibesarkan oleh kakeknya, Abah Harja, yang mengajarkannya alat musik tradisional Karinding beserta filosofinya. Suatu hari Abah Harja pergi "mencari sesuatu yang tidak ada" dan tidak pernah kembali. Rimba mencarinya, kelelahan, lalu tertidur — dan bermimpi mengenang masa-masa belajar Karinding bersama Abah. Chapter 1 ini adalah rangkaian mimpi tersebut (flashback).

## A.2 Struktur Chapter 1 (8 Scene, 2 Hari)

**HARI PERTAMA:**
- **Scene 1** — Teras Rumah (Pagi): Rimba bangun, Abah ngajak cuci muka, ngobrol soal "jalan yang harus ditempuh sendiri", lalu berangkat ke kebun. **[INI YANG SEDANG DIKERJAKAN — lihat Bagian B]**
- **Scene 2** — Teras Rumah (Siang): Abah memperkenalkan Karinding (nama, bagian-bagian: Pancepengan, Panabeuh, Cecet Ucing), ada titik interaksi 3D inspect + dialog pilihan (3 opsi cabang)
- **Scene 3** — Belajar main Karinding: minigame ritme tap pertama, gagal-berhasil, Abah kasih nasihat soal sabar
- **Scene 4** — Filosofi Getaran & Kehidupan: Abah cerita soal asal kehidupan (Big Bang versi Sunda), ada efek visual kilatan langit pas kata "bledag!"

**HARI KEDUA:**
- **Scene 5** — Hutan: cari bambu (minigame search, ambil 2-3 bambu sesuai kriteria)
- **Scene 6** — Teras Rumah: minigame rakit Karinding (assembly puzzle)
- **Scene 7** — Teras Rumah: minigame ritme kedua, pakai Karinding buatan sendiri
- **Scene 8** — Teras Rumah (Sore): Abah pamit pergi "mencari sesuatu yang tidak ada", menghilang di balik pepohonan, ucap "Moal Kataekan" — END OF CHAPTER 1, Rimba bangun dari mimpi

## A.3 Arsitektur Scene Unity (Sudah Diputuskan)

| File `.unity` | Isi | Cara Load |
|---|---|---|
| `Ch1_TerasRumahKebun.unity` | Scene 1, 2, 4, 6, 8 penuh (dialog/cutscene) + jalur kebun (walkable continuous), pakai beberapa `MovementBoundary2D`/region berbeda. Lighting preset (pagi/siang/sore) & Canvas dialog di-toggle via Story/Game Manager | Scene utama, jarang di-reload |
| `Ch1_Hutan.unity` | Scene 5 (cari bambu) | `SceneManager.LoadScene()` — lokasi terpisah fisik, wajar ada loading screen |
| `MinigameRhythm.unity` | Scene 3 & 7 (minigame ritme, dipakai 2x) | `SceneManager.LoadScene(mode: Additive)` dari dalam TerasRumahKebun, biar modular & aman dari merge conflict tim |
| `MinigameCrafting.unity` | Scene 6 (assembly Karinding) | Additive, sama seperti Rhythm |

**Alasan:** minimalkan loading screen di momen dialog/jalan kaki (paling butuh terasa mulus), tapi minigame tetap file terpisah biar modular dan aman kalau dikerjakan tim berbeda.

---

# BAGIAN B — TASK SEKARANG: SCENE 1 (Teras Rumah, Pagi)

## B.1 Context Project
- Unity 6000.3.4f1, target mobile Android (API 28+)
- Project: "Main Karinding"
- File aktif: `Assets/_Project/Scenes/Game/Ch1_TerasRumahKebun.unity` (rename dari `Chapter1.unity`, GUID sama)
- Sistem yang BELUM ADA (jangan diklaim sudah ada):
  - PlayerMovementController + MovementBoundary2D (belum ada file, folder Dialogue/Rhythm/Crafting/Codex masih kosong)
  - DialogueSystem lama / StoryState / QuestManager
  - Sistem dialog yang dipakai sekarang: `FloatingDialogueController` (reusable Scene 1-8) + `CutsceneDialogueDisplay` (fallback)
- Overhaul saat ini: VISUAL ONLY (pixel art + side-scrolling, referensi "A Space for the Unbound"). Gameplay/minigame lama TIDAK diubah.
- Beat 8 untuk sekarang: loop balik ke Beat 1 buat testing (`loopForTesting=true`). Handoff exploration nanti setelah movement siap.

## B.2 Scope
Bikin **Scene1CutsceneController** yang menjalankan Beat 1-8 di bawah pakai `FloatingDialogueController` (reusable Scene 1-8), lalu loop untuk testing.

**Target selesai:** Scene 1 bisa dimainkan penuh dari spawn sampai Beat 8 loop balik (playable buat testing layout bubble).

## B.3 Referensi Visual
- **Shot 1** (Beat 1-2): komposisi wide, full-body, karakter berdiri di ground level dengan environment luas di belakang (rumah, jalan) — skala mirip shot exploration side-scroll, BUKAN close-up sempit
- **Shot 2** (Beat 4-7): Rimba & Abah **duduk berdua** di meja teras (ada vas bunga + buku di meja), tampak dari samping/belakang, menghadap jendela ke arah pemandangan gunung. Dialogue floating bubble di atas kepala karakter yang bicara (ada `BoxPointer` ekor kecil, auto-flip per speaker), BUKAN box statis di bawah layar
- **Layout rule:** semua offset bubble + `BoxPointer` 100% USER-atur di Inspector/Scene View (`FloatingDialogueController`: `offsetRimba/Abah`, `pointerOffsetRimba/Abah`, `autoFlipPointer`; `HeadAnchor_*` digeser di Scene View). Code hanya follow + flip + typewriter, TIDAK hardcode posisi.

## B.4 Daftar Aset — Final

### Background Cutscene (3 file)
| Nama File | Dipakai selama |
|---|---|
| `s1_bg_wide_establishing.png` | Beat 1 → 2 |
| `s1_bg_lookout_window.png` | Beat 4 → 7 |
| `s1_bg_lookout_mountain_sky.png` | Beat 4 → 7 |

### Background Exploration (3 file, ASET BARU — bukan reuse dari cutscene)
| Nama File | Fungsi |
|---|---|
| `explore_path_terasketebon_near.png` | Parallax layer dekat (tanah, setapak, rumput) |
| `explore_path_terasketebon_mid.png` | Parallax layer tengah (pohon, semak) |
| `explore_path_terasketebon_far.png` | Parallax layer jauh (gunung, langit), palet konsisten sama `s1_bg_lookout_mountain_sky.png` |

### Karakter — Rimba (5 file)
| Nama File | Dipakai selama |
|---|---|
| `rimba_idle_pintu.png` | Beat 1 → 2 |
| `rimba_lookout_facing_abah.png` | Beat 4 → 5 |
| `rimba_lookout_noleh1.png` | Beat 6 (frame animasi, sekilas) |
| `rimba_lookout_noleh2.png` | Beat 6 (frame animasi, sekilas) |
| `rimba_lookout_facing_mountain.png` | Beat 6 → 7 |

### Karakter — Abah (2 file)
| Nama File | Dipakai selama |
|---|---|
| `abah_idle_teras_talking.png` | Beat 1 → 2 |
| `abah_lookout_facing_mountain.png` | Beat 4 → 7 |

### UI (3 file)
| Nama File | Dipakai di |
|---|---|
| `s1_ui_dialoguebox.png` | Beat 2, Beat 7 |
| `s1_ui_actionbutton_lihat.png` | Beat 5 |
| `s1_ui_tapindicator.png` | Beat 5 |

### Audio (3 file, placeholder dulu kalau belum final)
| Nama File | Dipakai di |
|---|---|
| `s1_sfx_transition_blackscreen.wav` | Beat 3 |
| `s1_sfx_tap_confirm.wav` | Beat 5 |
| `s1_ambience_morning.wav` | Loop Beat 1 → 8, fade out saat handoff |

**PENTING:** "Dipakai selama Beat X → Y" artinya file YANG SAMA tetap ditampilkan tanpa diganti sepanjang rentang itu. INI BUKAN instruksi untuk membuat banyak file per beat. Total file final: **16 file cutscene + 3 file exploration = 19 file**, semua terpakai, jangan tambah/kurang.

## B.5 Import & Setup Unity
1. Semua PNG: Texture Type = **Sprite (2D and UI)**, Sprite Mode = **Single** (file sudah terpisah per pose, TIDAK perlu Sprite Editor slicing)
2. Buat 2 ScriptableObject:
   - `RimbaSpriteSet` — field `Sprite`: `idlePintu`, `lookoutFacingAbah`, `lookoutNoleh1`, `lookoutNoleh2`, `lookoutFacingMountain`
   - `AbahSpriteSet` — field `Sprite`: `idleTerasTalking`, `lookoutFacingMountain`
3. Assign manual tiap file ke field sesuai tabel B.4
4. Animasi Beat 6: swap sprite berurutan `lookoutFacingAbah → lookoutNoleh1 → lookoutNoleh2 → lookoutFacingMountain`, tiap frame ± 0.1-0.15 detik via Coroutine sederhana (TIDAK perlu Animator Controller penuh)
5. Background exploration (bagian dari B.4): dipasang sebagai parallax layers di area jalan menuju kebun, dengan `MovementBoundary2D` yang match panjang path

## B.6 Beat Breakdown Lengkap (DENGAN DIALOG)

### Beat 1 — Spawn
- Shot 1 (Wide, full-body): `s1_bg_wide_establishing.png`
- Rimba: `rimba_idle_pintu.png` (dekat pintu)
- Abah: `abah_idle_teras_talking.png` (dekat teras)
- `movementLocked = true`

### Beat 2 — Dialog Pertama
- Shot 1 tetap
- UI: `s1_ui_dialoguebox.png`
- Dialog:

| Speaker | Sunda | Terjemahan |
|---|---|---|
| Abah | "Baba tos gugah gening ? Sok gera wawasuh heula" | "Baba udah bangun ternyata? Sana cuci muka dulu" |

- Trigger lanjut: player tap dialogue box

### Beat 3 — Transisi (Blackscreen)
- Player tap dialog → layar hitam, `s1_sfx_transition_blackscreen.wav`
- Hard cut (bukan animasi pan), durasi ± 0.5-1 detik
- Merepresentasikan Rimba cuci muka (terjadi off-screen, tidak ditampilkan)

### Beat 4 — Reveal Shot 2
- Shot 2 (Sitting/Look-out): `s1_bg_lookout_window.png` + `s1_bg_lookout_mountain_sky.png`
- Rimba & Abah sudah duduk berdua di meja teras
- Abah: `abah_lookout_facing_mountain.png` — sudah menghadap gunung
- Rimba: `rimba_lookout_facing_abah.png` — masih menghadap Abah
- Posisi: berdampingan dari awal, TIDAK berubah X/Y sepanjang Shot 2, hanya sprite yang berubah

### Beat 5 — Action Button (STRICT — WAJIB TAP)
- UI: `s1_ui_actionbutton_lihat.png` + `s1_ui_tapindicator.png` muncul
- **WAJIB tap tombol ini eksplisit — TIDAK ADA auto-fallback/timeout.** Scene freeze total sampai player tap
- SFX saat ditekan: `s1_sfx_tap_confirm.wav`

### Beat 6 — Rimba Menoleh
- Sprite Rimba swap berurutan: `rimba_lookout_facing_abah` → `rimba_lookout_noleh1` → `rimba_lookout_noleh2` → `rimba_lookout_facing_mountain`
- Posisi TIDAK berubah, hanya sprite
- Setelah animasi selesai → auto-lanjut ke Beat 7

### Beat 7 — Dialog Lanjutan
- Shot 2 tetap — **TIDAK load aset baru**, reuse dari akhir Beat 6 (`rimba_lookout_facing_mountain.png` + `abah_lookout_facing_mountain.png` + background Shot 2)
- UI: floating bubble via `FloatingDialogueController.Show(speaker, sunda, indo, HeadAnchor)`, bubble follow `HeadAnchorShot2Rimba/Abah`, `BoxPointer` flip otomatis (bisa di-override manual)
- Dialog:

| # | Speaker | Sunda | Terjemahan |
|---|---|---|---|
| 1 | Abah | "Aya jalan nu kudu dilakonan ku nyalira, Ba. Abah keur ngora ge kitu" | "Ada jalan yang harus ditempuh sendiri, Ba. Ketika Abah muda juga begitu" |
| 2 | Rimba | "Maksudna kumaha bah ?" | "Maksudnya gimana bah?" |
| 3 | Abah | "Moal karasa ayeuna. Engke ge ngarti sorangan" | "Ga bakal kerasa sekarang. Nanti juga ngerti sendiri." |
| 4 | Rimba | *(kebingungan, tidak ada baris dialog — hanya reaksi visual/pose bingung)* | — |
| 5 | Abah | "Tos tong di pikiran teuing. Ayeuna mah urang ka kebon we hayu, bisi kaburu panas" | "Udah jangan terlalu dipikirin. Sekarang kita ke kebon aja, takutnya keburu panas" |
| 6 | Rimba | "Muhun bah, Hayu" | "Iya bah, Ayo" |

### Beat 8 — Ending Loop (testing)
- `Scene1CutsceneController` fade to black 0.5s, hold 1s, fade back, lalu `StartCutscene()` lagi (loop) kalau `loopForTesting=true`
- Kalau `loopForTesting=false`: freeze di black (tunggu exploration system)
- Player jalan ke kebun + transisi Scene 2 di luar scope task ini

## B.7 Behavior Rules (Ringkasan)
- `movementLocked = true` sepanjang Beat 1-7, jadi `false` mulai Beat 8
- Beat 5: strict tap wajib via tombol eksplisit, TIDAK ADA timeout
- Beat 6: Rimba tidak pindah posisi, hanya sprite berubah
- Beat 7: TIDAK ada aset baru, reuse dari Beat 6
- `s1_ambience_morning.wav` loop terus dari Beat 1, fade out di Beat 8

## B.8 Definisi Selesai
Task ini SELESAI ketika:
1. Scene `Ch1_TerasRumahKebun.unity` bisa dijalankan dari Beat 1 sampai Beat 8 tanpa error (0 error, 1 warning CS0414 yang harmless)
2. Beat 5 tap wajib diterima sebelum lanjut ke Beat 6-7
3. Semua 6 baris dialog Beat 7 tampil berurutan sebagai floating bubble (Abah↔Rimba pindah anchor + BoxPointer flip) sesuai tabel B.6
4. Setelah Beat 8, loop balik ke Beat 1 buat testing layout (sampai exploration siap)
5. Scene 2 dan seterusnya BELUM diimplementasikan
6. Layout bubble + BoxPointer bisa kamu atur sendiri: `HeadAnchor_*` di Scene View, offset di `FloatingDialogueController` Inspector