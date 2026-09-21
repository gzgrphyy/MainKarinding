# plan.md — Game Karinding (Unity)

> Working title: belum diputuskan (kandidat: "Gema Rimba"). Jangan hardcode judul game di kode.

## 0. Cara pakai dokumen ini 

- Kerjakan **satu fase per permintaan**. Jangan lompat ke fase berikutnya kecuali diminta.
- Sebelum menulis kode: baca dulu script yang sudah ada di `Assets/_Project/Scripts/` supaya tidak menulis ulang atau bentrok nama class.
- **Jangan edit manual** file `.unity`, `.prefab`, `.asset`, atau `.meta`. Kalau butuh membuat scene, prefab, atau asset lewat kode, tulis **Editor script** (di `Scripts/Editor/`) yang dijalankan lewat menu Unity.
- Hal yang hanya bisa dilakukan manual di Unity Editor ditulis sebagai bagian **"Langkah manual di Unity"** di akhir jawaban. Jangan dilewati.
- **Jangan mengklaim sesuatu "sudah jalan/selesai" tanpa bukti** (Console bersih, hasil Play mode). Kalau tidak bisa memverifikasi (mis. tidak bisa menjalankan Unity), tulis jelas: *"belum diverifikasi di Unity"*.
- Setiap fase diakhiri dengan: daftar file yang dibuat/diubah, langkah manual di Unity, dan cara menguji (acceptance checklist).
- Kalau ada informasi yang kurang, ajukan **maksimal satu pertanyaan** lalu lanjut dengan asumsi yang ditulis eksplisit.

## 1. Konteks proyek

Game mobile 2D (Android, Play Store) tentang penyebaran alat musik **Karinding** di Jawa Barat, tahun 1888. Protagonis **Rimba** mencari kakeknya, **Abah Harja**, yang pergi "mencari sesuatu yang tidak ada" dan meninggalkan kalimat terakhir "Moal Kataekan". Nilai inti: **Yakin, Sadar, Sabar**.

- Target: usia 10+, sesi ±30 menit, model bisnis edukasi.
- Perangkat: Android 9+ (API 28+), RAM min 3GB, target 30fps min / 60fps ideal.
- Referensi gameplay: The Tall Keeper. Referensi UI dialog: Citampi Stories (pola RPG mobile: panel bawah, name tag, portrait, efek ketik, tap lanjut, tombol pilihan).
- Sumber cerita: `Storyline.docx` (baru sampai **Chapter 1**). Dialog utama dalam bahasa Sunda dengan subtitle terjemahan Indonesia (opsional, bisa di-toggle).
- Tim: Geza & Akram (programmer), Andhika & Pryzha (art), Daus (narasi & audio).

## 2. Aturan teknis & konvensi

**Engine & setelan**
- Unity **6000.3.0f1**. Plugin: DOTween, Odin Inspector, FMOD. TextMeshPro sudah diimpor.
- **Active Input Handling = Input System Package (New) only.** Dilarang memakai `Input.GetKey`, `Input.mousePosition`, `OnMouseDown`, dan API input legacy lain.
  - Klik pada UI: `IPointerClickHandler` + EventSystem dengan **Input System UI Input Module**.
  - Klik pada objek dunia 2D: `IPointerClickHandler` + `Physics2DRaycaster` di Main Camera + Collider2D.
  - Input custom: `UnityEngine.InputSystem` (`Touchscreen`, `Pointer`, atau `InputAction`).
- DOTween: kalau muncul error `DG could not be found`, penyebabnya modul DOTween belum di-**Apply** di DOTween Utility Panel (bukan masalah asmdef).
- **Tidak ada `.asmdef`.** Semua script di satu assembly.

**Kode**
- Namespace per modul: `Karinding.Dialogue`, `Karinding.Story`, `Karinding.Crafting`, `Karinding.Rhythm`, `Karinding.Codex`, `Karinding.Core`, `Karinding.UI`.
- Satu class per file; nama file = nama class. Folder script mengikuti namespace.
- Tidak boleh ada dua class dengan nama sama di modul berbeda. Awali dengan nama modul kalau perlu (`RhythmGameManager`).
- **Antar-modul hanya lewat event dan method publik**, tidak boleh memanggil class internal modul lain. Setiap modul menyediakan (1) method pemicu dan (2) event hasil (`OnCompleted`). Penyambung ada di `Chapter1Flow` lewat `UnityEvent` di Inspector.
- Script yang memakai `UnityEditor` **wajib** di `Assets/_Project/Scripts/Editor/`.
- Tidak ada scene "Test" untuk fitur baru; bangun di scene utama (`Scenes/Game/Chapter1`).
- Kode debug diapit `#if UNITY_EDITOR` atau `[Conditional]`, dan diberi komentar `// DEBUG: hapus nanti`.

**Struktur folder** (root: `Assets/_Project/`)
```
Art/{Characters,Environment,UI}
Audio/{Music,SFX}
Data/{Dialogue/Chapter1,Crafting,Rhythm}
Fonts
Prefabs/{UI,Crafting,Rhythm,Managers}
Scenes/Game            (Boot, MainMenu, Chapter1)
Scripts/{Core,Story,Dialogue,Crafting,Rhythm,Codex,UI,Debug,Editor}
```
Plugin (DOTween, Sirenix/Odin, TextMesh Pro, FMOD) tetap di root `Assets/`.

**Git**
- Branch per fitur: `feature/<nama>`; merge ke `main` kalau stabil. Jangan dua orang mengedit scene yang sama bersamaan.
- Asset Serialization = Force Text; Version Control = Visible Meta Files; Git LFS untuk png/psd/wav/mp3/fbx.
- Commit message: `<modul>: <ringkasan>` (contoh: `dialogue: add UI builder editor tool`).

## 3. Status saat ini

**Sudah ada di project** (dibuat di sesi sebelumnya; baca dulu sebelum mengubah):

| File | Isi ringkas |
|---|---|
| `Scripts/Dialogue/DialogueData.cs` | `LineData`, `DialogueLine : LineData` (punya `choices`), `DialogueChoice` (label, labelTranslation, npcId, affinityDelta, responseLines), `DialogueSequence : ScriptableObject` (`sequenceId`, `lines`) |
| `Scripts/Dialogue/DialogueView.cs` | UI: fade CanvasGroup, efek ketik (tap = tampil semua), `WaitForTap`, indikator lanjut, portrait kiri/kanan, subtitle terjemahan, `ShowChoices/HideChoices`. Menerima tap lewat `IPointerClickHandler` di root |
| `Scripts/Dialogue/DialogueChoiceButton.cs` | `Setup(label, translation, onClick)` |
| `Scripts/Dialogue/DialogueManager.cs` | Singleton; `StartDialogue(seq, onComplete)`, `StopDialogue()`, `SetTranslation(bool)`, `IsPlaying`; event `OnDialogueStarted/OnDialogueEnded(string id)`, `OnDialogueEvent(string eventId)`, `OnAffinityChanged(string npcId, int delta)`. Pilihan player ditampilkan dulu sebagai baris ucapan Rimba (`echoChosenChoice`) |
| `Scripts/Debug/DialogueDebugStarter.cs` | Pemicu tes; **tidak dipakai** lagi |
| `Scripts/Editor/Chapter1DialogueGenerator.cs` | Menu *Karinding → Dialogue → Generate Chapter 1*; membuat **13** asset `DialogueSequence` |
| `Scripts/Story/Chapter1Flow.cs` | Memutar S1 → S2 → `onStartRhythmTutorial`; `OnRhythmTutorialFinished()` memutar S3_Selesai → S4 → `onAfterS4` |

**13 asset dialog Chapter 1:** `Ch1_S1_Teras`, `Ch1_S2_Pengenalan` (pilihan A/B/C), `Ch1_S3_HintMaksa`, `Ch1_S3_Selesai`, `Ch1_S4_Filosofi`, `Ch1_S5_Intro`, `Ch1_S5_BambuSalah`, `Ch1_S5_BambuBenar`, `Ch1_S6_Intro`, `Ch1_S7_Coba`, `Ch1_S7_Selesai`, `Ch1_S8_Perpisahan`, `Ch1_S8_Akhir`.

**Event dialog yang sudah didefinisikan:** `CODEX_PANCEPENGAN`, `CODEX_PANABEUH`, `CODEX_CECET_UCING`, `CODEX_GETARAN_KEHIDUPAN`, `ABAH_IKAT_BEDOG`, `CHAPTER1_END`.

**Belum ada / belum diverifikasi:** UI dialog di scene, prefab `DialogueCanvas`, scene `Chapter1`, modul Crafting (masih di project terpisah, baru fase *search* yang jadi), Rhythm Minigame, StoryState/Codex/Save.

## 4. Roadmap

Urutan = prioritas. Kerjakan berurutan.

---

### FASE 1 — Verifikasi fondasi dialog
**Tujuan:** memastikan semua yang sudah dimasukkan compile bersih dan asset dialog ter-generate di path yang benar.

Tugas:
- [ ] 1.1 Review semua script di `Scripts/Dialogue`, `Scripts/Story`, `Scripts/Editor` untuk error compile yang terlihat (using, namespace, tipe, referensi TMP/DOTween). Perbaiki bila ada.
- [ ] 1.2 Pastikan konstanta `Folder` di `Chapter1DialogueGenerator` = `"Assets/_Project/Data/Dialogue/Chapter1"`.
- [ ] 1.3 Tambahkan validasi di generator: kalau `Assets/_Project` tidak ditemukan, tampilkan `EditorUtility.DisplayDialog` (mencegah path ganda seperti `Assets/Assets/_Project`).

Langkah manual di Unity: buka Console (harus 0 error) → menu *Karinding → Dialogue → Generate Chapter 1 (skip existing)* → cek ada 13 asset di `Data/Dialogue/Chapter1`.

Acceptance:
- Console 0 error dan 0 warning dari script kita.
- 13 asset ada dan `sequenceId` terisi (contoh: `ch1_s2_pengenalan`).
- `Ch1_S2_Pengenalan` punya 1 baris dengan 3 pilihan (A/B/C), tiap pilihan punya 1 response.

---

### FASE 2 — UI dialog jadi prefab (otomatis via Editor script)
**Tujuan:** UI dialog dibangun oleh tool, bukan diklik manual, supaya reproducible dan bisa dipakai tim.

Tugas:
- [ ] 2.1 Tulis `Scripts/Editor/DialogueUIBuilder.cs` dengan menu *Karinding → Dialogue → Build Dialogue UI* yang:
  - membuat prefab `Prefabs/UI/DialogueChoiceButton.prefab` (Button + Image + TMP label + TMP terjemahan opsional + komponen `DialogueChoiceButton`, referensi terisi);
  - membuat prefab `Prefabs/UI/DialogueCanvas.prefab` dengan hierarki di bawah, komponen `DialogueView` (semua `[SerializeField]` terisi via `SerializedObject`) dan `DialogueManager` (field `view` terisi);
  - Canvas: Screen Space - Overlay, Canvas Scaler = Scale With Screen Size, reference **1920×1080**, match 0.5, orientasi landscape; `GraphicRaycaster` ada;
  - kalau scene aktif belum punya EventSystem, membuatnya dengan **InputSystemUIInputModule** (bukan StandaloneInputModule);
  - idempotent: kalau prefab sudah ada, tanyakan overwrite atau skip (jangan menimpa diam-diam).
- [ ] 2.2 Placeholder visual: panel = Image warna solid semi-transparan, nama, teks, dan tombol memakai font TMP default. Art final menyusul dari tim art.
- [ ] 2.3 Toggle terjemahan: tombol kecil di pojok panel; memanggil `DialogueManager.SetTranslation(bool)` **dan** langsung me-refresh baris yang sedang tampil (tambahkan method publik seperlunya di `DialogueView`/`DialogueManager`).
- [ ] 2.4 Tambahkan event `OnLineShown(LineData line)` di `DialogueManager` sebagai titik sambung audio/animasi (FMOD nanti). Belum perlu memutar suara apa pun.

Hierarki target `DialogueCanvas`:
```
DialogueCanvas (Canvas, CanvasScaler, GraphicRaycaster)
├─ DialogueRoot        [CanvasGroup, Image alpha 0 + Raycast Target ON, DialogueView]
│  ├─ PortraitLeft     (Image)
│  ├─ PortraitRight    (Image)
│  ├─ Panel            (Image, anchor bawah)
│  │   ├─ NameTag      → SpeakerText (TMP)
│  │   ├─ BodyText     (TMP)
│  │   ├─ TranslationText (TMP, italic, warna lebih pudar)
│  │   ├─ ContinueIndicator (Image/TMP "▼")
│  │   └─ TranslationToggle (Toggle/Button)
│  └─ ChoiceContainer  (Vertical Layout Group + ContentSizeFitter)   ← sibling TERAKHIR
DialogueManager        [DialogueManager, ref view → DialogueRoot]
```

Catatan teknis: `ChoiceContainer` harus sibling terakhir supaya tombol berada di atas tap-catcher. Font TMP harus mendukung karakter `é` (Néangan, haté).

Langkah manual di Unity: jalankan menu builder, seret `DialogueCanvas` ke scene bila belum ada, cek tidak ada *Missing reference* di Inspector.

Acceptance:
- Prefab `DialogueCanvas` dan `DialogueChoiceButton` ada; tidak ada field `None` yang seharusnya terisi.
- Tap di layar di Play mode tidak menghasilkan error Input System.
- Toggle terjemahan mengubah tampilan langsung.

---

### FASE 3 — Scene `Chapter1` + alur dialog awal
**Tujuan:** dialog Chapter 1 bisa dimainkan dari awal di scene utama.

Tugas:
- [ ] 3.1 Editor script `Scripts/Editor/Chapter1SceneSetup.cs` (menu *Karinding → Setup → Chapter1 Scene*) yang membuat `Scenes/Game/Chapter1.unity` berisi: Main Camera (+`Physics2DRaycaster`), `DialogueCanvas` prefab, GameObject `Chapter1Flow` dengan 4 sequence terisi otomatis dari `Data/Dialogue/Chapter1`. Kalau scene sudah ada, hanya tambahkan yang belum ada.
- [ ] 3.2 Tambahkan **hook debug** (hanya `#if UNITY_EDITOR`, dengan Input System, bukan Input legacy): tombol `F1` memanggil `OnRhythmTutorialFinished()` supaya S3 dan S4 bisa diuji sebelum minigame ada.
- [ ] 3.3 Tambahkan `Debug.Log` bertag `[Chapter1Flow]` di tiap transisi untuk diagnosis.

Acceptance:
- Play → S1 tampil dengan efek ketik → tap lanjut sampai selesai.
- S2: setelah baris "Ieu teh nami na Karinding…" muncul 3 pilihan; memilih salah satu menampilkan ucapan Rimba lalu jawaban Abah, lalu lanjut ke 3 penjelasan bagian (memicu event `CODEX_*`).
- Setelah S2 selesai, `onStartRhythmTutorial` terpanggil (log terlihat).
- F1 memutar S3_Selesai lalu S4 dengan benar; `IsPlaying` kembali `false` setelah selesai.

---

### FASE 4 — Backbone cerita: StoryState, Codex, Affinity, Save
**Tujuan:** state cerita terpusat supaya semua modul tidak saling hardcode.

Tugas:
- [ ] 4.1 `Karinding.Story.StoryState` (enum) untuk sub-beat Chapter 1, sesuai urutan:
  `Frame_RimbaTertidur → MasaKecil_KesehariaanAbah → MasaKecil_PengenalanKarinding → MasaKecil_BelajarMainKarinding → MasaKecil_MembuatKarinding → MasaKecil_MenyelesaikanKarinding → MasaKecil_MemainkanKarindingSendiri → MasaKecil_PesanNilaiKehidupan → KeberangkatanAbah`.
- [ ] 4.2 `StoryManager` (singleton): `CurrentState`, `SetState(StoryState)`, event `OnStateChanged(old, new)`.
- [ ] 4.3 `CodexManager` di `Karinding.Codex`: subscribe `DialogueManager.OnDialogueEvent`; 4 entri Chapter 1 = Pancepengan (keyakinan), Panabeuh (kesabaran), Cecet Ucing (resonansi), Getaran dan Awal Kehidupan. Data entri berupa ScriptableObject (`CodexEntry`: id, judul, isi, `unlocked`). Event `OnEntryUnlocked`. UI Codex **belum**; cukup data + log.
- [ ] 4.4 `AffinityManager`: subscribe `OnAffinityChanged`; simpan skor per `npcId`; tier (mis. Asing/Kenal/Dekat) sebagai konstanta yang mudah diubah (angka final belum ditentukan, tandai `TODO`).
- [ ] 4.5 `SaveSystem` (JSON di `Application.persistentDataPath`): simpan `storyState`, daftar codex terbuka, affinity, preferensi terjemahan. Method `Save()`/`Load()`; auto-save saat `StoryState` berubah.
- [ ] 4.6 Refactor `Chapter1Flow` agar berbasis `StoryState` (bisa lanjut dari save), tanpa mengubah API publik yang sudah ada.

Acceptance:
- Setelah S2, tiga entri Codex terbuka; setelah S4 entri keempat terbuka (terlihat di log).
- Stop lalu Play lagi: state dan codex termuat dari file save.
- Tidak ada modul lain yang memanggil `StoryManager` selain lewat event atau method publik yang terdokumentasi.

---

### FASE 5 — Integrasi Crafting Puzzle (Hari Kedua)
**Tujuan:** menyambung Crafting Puzzle (search + assembly) ke cerita.

Konteks: modul Crafting **dibangun di project terpisah**; baru fase *search* (Brain Out-style hidden object, tap-to-find) yang jadi. Fase assembly **belum ada**. Desain yang berlaku:
- Search: tidak ada item decoy (semua yang bisa ditap adalah bahan). Item yang ketemu **berpindah ke tray tengah-bawah layar**.
- Alur penuh: **Search → Dialog (`Ch1_S6_Intro`) → Cutscene placeholder → Assembly Cut (0–50%) → Assembly Tap (50–100%) → Preview 3D karinding → lanjut cerita**.
- Cutscene placeholder: layar hitam + SFX (mis. suara gergaji) + teks placeholder; mulai otomatis saat search selesai; auto-lanjut ke assembly saat selesai.
- Assembly: **satu progress bar 0–100%**. Fase Cut (0–50%): swipe garis lurus berkali-kali. Fase Tap (50–100%): tap cepat berulang di tombol, ada **decay** kalau terlalu lambat. Default tuning: `cutMinStraightness = 0.85`, `tapDecayGraceTime = 0.5s`, `tapDecayRate = 7%/detik` (semua `[SerializeField]`, mudah diubah). Input di luar fase yang benar **diabaikan diam-diam** (tanpa shake/penalti; tidak ada fail state).
- `OnAssemblyComplete` adalah **event terpisah** (tidak memanggil story system langsung).
- Preview 3D: kamera + RenderTexture terpisah (game tetap 2D); pemain bebas memutar model (depan/belakang/atas/bawah) dengan drag; tombol **"Lanjut"** eksplisit (bukan auto-advance).

Tugas:
- [ ] 5.1 Impor modul Crafting dari project lama lewat **Export/Import Package** (scripts, prefab hotspot, config). Jangan copy lewat File Explorer. Rapikan ke namespace `Karinding.Crafting` dan folder `_Project`. Cek kompatibilitas Input System (`CraftingHotspot` harus `IPointerClickHandler`; Main Camera punya `Physics2DRaycaster`).
- [ ] 5.2 Implementasi tray: item ketemu → DOTween bergerak ke slot tray tengah-bawah.
- [ ] 5.3 `CraftingCutsceneStub` (layar hitam, SFX placeholder, teks, auto-advance, event `OnCutsceneFinished`).
- [ ] 5.4 `AssemblyController` (state: Idle → Cut → Tap → Complete), `CutSwipeDetector` (validasi kelurusan swipe), `TapRapidController` (dengan decay), `AssemblyProgressBar`. Event: `OnAssemblyProgress(float)`, `OnAssemblyComplete`.
- [ ] 5.5 `KarindingPreview3D` (kamera + RenderTexture, rotasi drag, tombol Lanjut → event `OnPreviewFinished`). Model 3D belum ada: pakai placeholder (mis. Cylinder) dulu.
- [ ] 5.6 Rangkai di `Chapter1Flow`: `S4 → S5_Intro → Search → S6_Intro → Cutscene → Assembly → Preview → (Rhythm S7)`. Set `StoryState` di tiap transisi.

Asumsi yang perlu dikonfirmasi: posisi `Ch1_S5_Intro` dan `Ch1_S6_Intro` di alur di atas.

Acceptance (tiap poin harus dibuktikan di Play mode, bukan asumsi):
- Search selesai → item ada di tray → dialog S6 tampil → cutscene → assembly jalan otomatis.
- Progress bar naik di fase Cut hanya dengan swipe lurus; berhenti di 50% lalu berpindah ke fase Tap; turun kalau tap terlalu lambat; 100% memicu `OnAssemblyComplete` tepat sekali.
- Preview 3D bisa diputar; tombol Lanjut menutupnya dan cerita lanjut.

---

### FASE 6 — Rhythm Minigame ("mainin karinding")
**Tujuan:** minigame ritme bergaya Guitar Hero yang dipakai di dua tempat: tutorial (Scene 3) dan memainkan karinding buatan sendiri (Scene 7, kesulitan lebih tinggi).

Desain (final):
- Orientasi **landscape**. **Satu track horizontal di tengah layar.**
- Semua note **spawn dari tepi kanan layar dan bergerak ke kiri**; ada **satu garis hit vertikal** di tengah layar.
- **4 lane** direpresentasikan lewat **warna/bentuk note** (bukan posisi vertikal), dipetakan ke **4 tombol kecil**: 2 di sisi kiri, 2 di sisi kanan layar.
- MVP: **tap-note saja**, tanpa hold-note.
- Chart **ditulis manual per level** (bukan auto-generate dari audio/BPM): ScriptableObject `RhythmChart` berisi daftar `(waktuDetik, laneIndex)`.
- Tingkat kesulitan mengatur tempo dan jendela timing.
- Filosofi: ritme tenang dan konsisten = dengung yang benar; terlalu cepat atau asal = suara sumbang. Tidak perlu fail state keras; gagal berulang bisa memicu dialog hint `Ch1_S3_HintMaksa`.

Tugas:
- [ ] 6.1 Data: `RhythmChart` (SO), `RhythmDifficulty` (kecepatan note, hit window Perfect/Good), `RhythmResult` (skor, akurasi, jumlah Perfect/Good/Miss).
- [ ] 6.2 `IRhythmClock` (abstraksi waktu lagu) + implementasi awal berbasis `AudioSettings.dspTime`, supaya bisa diganti ke FMOD nanti tanpa mengubah logika.
- [ ] 6.3 `NoteSpawner`, `NoteView` (bergerak kanan → kiri, dipool), `HitLine`, `RhythmInputHandler` (4 tombol UI, Input System, multi-touch).
- [ ] 6.4 `RhythmGameController`: `StartGame(RhythmChart, RhythmDifficulty)`, event `OnCompleted(RhythmResult)`, umpan balik dasar (DOTween pulse, placeholder SFX).
- [ ] 6.5 Buat 2 chart tes: *Tutorial* (mudah, sedikit note) dan *Karinding Sendiri* (lebih rapat).
- [ ] 6.6 Sambungkan ke `Chapter1Flow`: `onStartRhythmTutorial` → Rhythm (tutorial) → `OnRhythmTutorialFinished()`; setelah Preview 3D → Rhythm (kesulitan lebih tinggi) → `S7_Selesai`.

Catatan konflik desain: `Storyline.docx` Scene 3 mendeskripsikan minigame "tempatkan tangan lalu tahan tombol dengan ritme". Desain Guitar Hero di atas yang berlaku. **Konfirmasi ke Daus** apakah narasi Scene 3 perlu disesuaikan.

Acceptance:
- Note bergerak mulus kanan → kiri pada 30fps dan 60fps; ketepatan judgement tidak bergantung frame rate.
- 4 tombol bisa ditekan bersamaan (multi-touch) tanpa input hilang.
- `OnCompleted` terpanggil tepat sekali dengan `RhythmResult` benar.

---

### FASE 7 — Penutup Chapter 1 (Scene 8)
- [ ] 7.1 Alur: `S8_Perpisahan` (event `ABAH_IKAT_BEDOG` memicu animasi Abah mengikat bedog; placeholder cukup log) → interaksi **"coba raih tangan Abah"** (satu tombol tap/hold singkat, **apa pun hasil inputnya Abah tetap menghilang**, tidak ada fail state) → `S8_Akhir` → event `CHAPTER1_END` → transisi "Rimba bangun".
- [ ] 7.2 `StoryState` menjadi `KeberangkatanAbah` lalu tandai Chapter 1 selesai di save.

### FASE 8 — Opening & Frame
- [ ] 8.1 `OpeningSequence`: teks narasi pembuka dari bagian OPENING di `Storyline.docx` ditampilkan berurutan dengan fade DOTween di layar hitam (placeholder untuk cinematic final).
- [ ] 8.2 Beat `Frame_RimbaTertidur` (Rimba tertidur di bawah pohon) sebagai placeholder yang mengalir ke Hari Pertama.

### FASE 9 — Build & QA Android
- [ ] 9.1 Player Settings: Android min API 28, IL2CPP, ARM64, orientasi landscape.
- [ ] 9.2 Canvas Scaler dan Safe Area untuk notch; uji di beberapa rasio layar.
- [ ] 9.3 Cek performa (target 30fps min): pooling note, tidak ada alokasi per frame di `Update`.
- [ ] 9.4 Uji karakter Sunda (`é`) di semua font, uji sentuhan multi-touch di perangkat asli.

---

## 5. Kontrak modul (untuk dibagikan ke tim)

Setiap modul yang diserahkan ke project utama wajib memenuhi ini:

| Aturan | Isi |
|---|---|
| Namespace | `Karinding.<Modul>` |
| Lokasi | `Scripts/<Modul>`, `Prefabs/<Modul>`, `Data/<Modul>`, `Art/<Modul>` |
| Pemicu | Satu method publik (contoh: `StartPuzzle(config)`, `StartGame(chart, difficulty)`) |
| Hasil | Satu event (contoh: `OnCompleted(Result)`); tidak memanggil story system langsung |
| Serah terima | Prefab + script + data. **Bukan scene.** |
| Input | Input System saja (tanpa API legacy) |
| Bukti | Tandai fitur "selesai" hanya jika sudah diuji di Play mode |

## 6. Pertanyaan terbuka / perlu konfirmasi

- [ ] terjemahan baris "Moal Kataekan" (di generator masih tebakan: "Tidak akan sampai / tidak akan tercapai").
- [ ] "sabat" di S2 opsi B sudah dikoreksi menjadi "sabar" — benar?
- [ ] tiga baris penjelasan Pancepengan/Panabeuh/Cecet Ucing adalah adaptasi dari deskripsi dokumen; rapikan bahasa Sundanya.
- [ ] narasi Scene 3 (minigame) menyesuaikan desain Guitar Hero?
- [ ] Posisi `S5_Intro` dan `S6_Intro` di alur crafting (lihat Fase 5).
- [ ] Nilai tier affinity, nama game final, dan nama repo GitHub.
- [ ] Detail UI Citampi Stories (posisi portrait, gaya tombol pilihan): kirim screenshot untuk penyesuaian layout.
- [ ] Portrait karakter dan art panel dialog dari tim art.

## 7. Definition of Done (per fase)

1. Console bersih (0 error) — atau ditulis eksplisit "belum diverifikasi di Unity".
2. Semua checklist Acceptance fase itu lolos di Play mode.
3. Tidak ada referensi `None`/Missing di Inspector untuk komponen baru.
4. Struktur folder dan namespace sesuai konvensi.
5. Perubahan di-commit di branch fitur dengan pesan sesuai konvensi.