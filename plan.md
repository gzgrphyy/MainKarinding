# PLAN: Scene 1 — Teras Rumah Pagi
## Chapter 1, "A Space for the Unbound" visual style overhaul

---

## 1. CONTEXT PROJECT

- Unity 6000.3.0f1, target mobile Android (API 28+)
- Project: "Main Karinding" (folder lokal `C:\Users\geza\Main Karinding`)
- Sistem yang SUDAH ADA — REUSE, JANGAN dibuat ulang:
  - DialogueSystem (gaya Citampi Story)
  - StoryState / QuestManager
  - PlayerMovementController + MovementBoundary2D (kontrol jalan kiri-kanan)
- Overhaul saat ini: VISUAL ONLY (pixel art + side-scrolling). Gameplay/minigame lama (Rhythm Minigame, Crafting Puzzle) TIDAK diubah.
- Folder kerja aset scene ini: `_Project/Scenes/Chapter1/Scene1_TerasRumah/`

## 2. SCOPE

Bikin 2 komponen baru:
1. **Scene1CutsceneController** — jalanin cutscene Beat 1-7 (lihat bagian 4)
2. **Handoff ke ExplorationController** yang sudah ada — Beat 8, transisi teras → kebun

Target akhir task ini: **Scene 1 bisa dimainkan dari awal (spawn) sampai player mulai jalan menuju kebun.**

---

## 3. DAFTAR ASET

### PENTING — cara baca tabel ini:
Kolom "Dipakai selama" = rentang beat di mana **file yang SAMA tetap ditampilkan tanpa diganti**. Ini BUKAN instruksi untuk bikin 1 file per beat. Total file di bawah ini sudah final, jangan ditambah.

### 3.1 Background — Cutscene (3 file)
| Nama File | Dipakai selama |
|---|---|
| `s1_bg_wide_establishing.png` | Beat 1 → 3 |
| `s1_bg_lookout_window.png` | Beat 4 → 7 |
| `s1_bg_lookout_mountain_sky.png` | Beat 4 → 7 |

### 3.2 Background — Exploration (3 file, ASET BARU, bukan reuse dari cutscene)
| Nama File | Fungsi |
|---|---|
| `explore_path_terasketebon_near.png` | Parallax layer dekat (tanah, setapak, rumput) |
| `explore_path_terasketebon_mid.png` | Parallax layer tengah (pohon, semak) |
| `explore_path_terasketebon_far.png` | Parallax layer jauh (gunung, langit) — palet warna konsisten sama `s1_bg_lookout_mountain_sky.png`, tapi file terpisah karena komposisi lebar/scrollable, beda sama background cutscene yang statis |

### 3.3 Karakter — Rimba (5 file)
| Nama File | Dipakai selama |
|---|---|
| `rimba_idle_pintu.png` | Beat 1 → 3 |
| `rimba_lookout_facing_abah.png` | Beat 4 → 5 |
| `rimba_lookout_noleh1.png` | Beat 6 (frame animasi, sekilas) |
| `rimba_lookout_noleh2.png` | Beat 6 (frame animasi, sekilas) |
| `rimba_lookout_facing_mountain.png` | Beat 6 → 7 |

### 3.4 Karakter — Abah (2 file)
| Nama File | Dipakai selama |
|---|---|
| `abah_idle_teras_talking.png` | Beat 1 → 2 |
| `abah_lookout_facing_mountain.png` | Beat 4 → 7 |

### 3.5 UI (3 file)
| Nama File | Dipakai di |
|---|---|
| `s1_ui_dialoguebox.png` | Beat 2, Beat 7 (semua baris dialog) |
| `s1_ui_actionbutton_lihat.png` | Beat 5 |
| `s1_ui_tapindicator.png` | Beat 5 |

### 3.6 Audio (3 file, placeholder dulu kalau belum final)
| Nama File | Dipakai di |
|---|---|
| `s1_sfx_transition_blackscreen.wav` | Beat 3 |
| `s1_sfx_tap_confirm.wav` | Beat 5 |
| `s1_ambience_morning.wav` | Loop Beat 1 → 8, fade out saat handoff |

**Total: 16 file cutscene + 3 file exploration = 19 file.**

---

## 4. IMPORT & SETUP (Unity)

1. Semua PNG: Texture Type = **Sprite (2D and UI)**, Sprite Mode = **Single**
2. Buat 2 ScriptableObject:
   - `RimbaSpriteSet` — field `Sprite`: `idlePintu`, `lookoutFacingAbah`, `lookoutNoleh1`, `lookoutNoleh2`, `lookoutFacingMountain`
   - `AbahSpriteSet` — field `Sprite`: `idleTerasTalking`, `lookoutFacingMountain`
3. Assign manual tiap file ke field sesuai tabel 3.3 / 3.4
4. Animasi Beat 6: swap sprite berurutan `lookoutFacingAbah → lookoutNoleh1 → lookoutNoleh2 → lookoutFacingMountain`, tiap frame ± 0.1-0.15 detik via Coroutine (tidak perlu Animator Controller)
5. Background exploration (3.2): pasang di scene exploration sebagai parallax layers terpisah dari cutscene, dengan `MovementBoundary2D` yang match panjang path teras → kebun

---

## 5. BEAT BREAKDOWN

### Beat 1 — Spawn
- Shot 1 (Wide Establishing): `s1_bg_wide_establishing.png`
- Rimba: `rimba_idle_pintu.png` (dekat pintu)
- Abah: `abah_idle_teras_talking.png` (dekat teras)

### Beat 2 — Dialog Pertama
- UI: `s1_ui_dialoguebox.png`
- Dialog: Abah — "Baba tos gugah gening ? Sok gera wawasuh heula" *("Baba udah bangun ternyata? Sana cuci muka dulu")*
- Trigger lanjut: player tap dialogue box

### Beat 3 — Transisi
- Blackscreen (`s1_sfx_transition_blackscreen.wav`), hard cut, ± 0.5-1 detik

### Beat 4 — Reveal Shot 2
- Shot 2 (Look-out): `s1_bg_lookout_window.png` + `s1_bg_lookout_mountain_sky.png`
- Abah: `abah_lookout_facing_mountain.png` (sudah menghadap gunung)
- Rimba: `rimba_lookout_facing_abah.png` (masih menghadap Abah)
- Posisi: berdampingan dari awal, TIDAK berubah X/Y sepanjang Shot 2

### Beat 5 — Action Button (STRICT)
- UI: `s1_ui_actionbutton_lihat.png` + `s1_ui_tapindicator.png`
- **WAJIB tap — TIDAK ADA auto-fallback/timeout.** Scene freeze sampai ditap
- SFX: `s1_sfx_tap_confirm.wav`

### Beat 6 — Rimba Menoleh
- Sprite Rimba swap: `facing_abah → noleh1 → noleh2 → facing_mountain`
- Posisi tetap sama, hanya sprite berubah

### Beat 7 — Dialog Lanjutan
- Shot 2 tetap (TIDAK load aset baru, reuse dari akhir Beat 6)
- UI: `s1_ui_dialoguebox.png`, floating di samping kepala speaker

| # | Speaker | Sunda | Terjemahan |
|---|---|---|---|
| 1 | Abah | "Aya jalan nu kudu dilakonan ku nyalira, Ba. Abah keur ngora ge kitu" | "Ada jalan yang harus ditempuh sendiri, Ba. Ketika Abah muda juga begitu" |
| 2 | Rimba | "Maksudna kumaha bah ?" | "Maksudnya gimana bah?" |
| 3 | Abah | "Moal karasa ayeuna. Engke ge ngarti sorangan" | "Ga bakal kerasa sekarang. Nanti juga ngerti sendiri." |
| 4 | Rimba | *(kebingungan, tidak ada dialog — hanya reaksi visual)* | — |
| 5 | Abah | "Tos tong di pikiran teuing. Ayeuna mah urang ka kebon we hayu, bisi kaburu panas" | "Udah jangan terlalu dipikirin. Sekarang kita ke kebon aja, takutnya keburu panas" |
| 6 | Rimba | "Muhun bah, Hayu" | "Iya bah, Ayo" |

### Beat 8 — Handoff ke Exploration
- Cutscene selesai → `Scene1CutsceneController` set `PlayerMovementController.movementLocked = false`
- Kamera transisi ke Exploration Camera
- Background aktif berganti ke aset exploration (bagian 3.2)
- Player bisa mulai jalan menuju kebun (pakai kontrol kiri/kanan yang sudah ada di PlayerMovementController)

---

## 6. BEHAVIOR RULES

- `movementLocked = true` sepanjang Beat 1-7, `false` mulai Beat 8
- Beat 5: strict tap wajib, tidak ada timeout
- Beat 6: Rimba tidak pindah posisi, hanya sprite berubah
- Beat 7: tidak ada aset baru, reuse dari Beat 6
- `s1_ambience_morning.wav` loop terus sampai fade out di Beat 8

---

## 7. DEFINISI SELESAI

Task ini dianggap selesai ketika:
1. Scene bisa dijalankan dari Beat 1 sampai Beat 8 tanpa error
2. Beat 5 tap wajib diterima sebelum lanjut ke Beat 6-7
3. Semua 6 baris dialog Beat 7 tampil berurutan dengan benar
4. Setelah Beat 8, player bisa mengontrol Rimba jalan di area exploration teras→kebun dengan kontrol yang sudah ada