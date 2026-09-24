# Scene 1 Cutscene - Manual Setup Guide

## Status Implementasi

✅ **SELESAI:**
- [x] Struktur folder asset (16 file lengkap)
- [x] Rename file double extension (.png.png → .png)
- [x] Script C# (5 controller + 2 ScriptableObject class)
- [x] ScriptableObject assets (RimbaSpriteSet.asset, AbahSpriteSet.asset)
- [x] Scene hierarchy structure (Shot1, Shot2, UI, Audio, Blackscreen)
- [x] Component setup (Scene1CutsceneController, CutsceneSpriteSwapper, CutsceneAudioManager)

⚠️ **PERLU MANUAL SETUP DI UNITY INSPECTOR:**

---

## 1. Configure Texture Import Settings (PENTING!)

Semua PNG sprite belum di-set sebagai **Sprite (2D and UI)**. Lakukan manual:

### Cara:
1. Di Unity, pilih semua file PNG di folder:
   - `Assets/_Project/Art/Characters/Rimba/Scene1/` (5 files)
   - `Assets/_Project/Art/Characters/Abah/Scene1/` (2 files)
   - `Assets/_Project/Art/Environment/Chapter1/Scene1_TerasRumah/` (3 files)
   - `Assets/_Project/Art/UI/Dialogue/` (1 file)
   - `Assets/_Project/Art/UI/ActionButtons/` (2 files)

2. Di Inspector, set:
   - **Texture Type:** `Sprite (2D and UI)`
   - **Sprite Mode:** `Single`
   - **Pixels Per Unit:** `100`
   - **Filter Mode:** `Point (no filter)` ← untuk pixel art
   - **Compression:** `None`

3. Klik **Apply**

---

## 2. Assign Sprite References ke ScriptableObjects

### RimbaSpriteSet.asset
Path: `Assets/_Project/Data/Cutscenes/Chapter1/Scene1/RimbaSpriteSet.asset`

| Field | Assign Sprite |
|---|---|
| `idlePintu` | `rimba_idle_pintu` |
| `lookoutFacingAbah` | `rimba_lookout_facing_abah` |
| `lookoutNoleh1` | `rimba_lookout_noleh1` |
| `lookoutNoleh2` | `rimba_lookout_noleh2` |
| `lookoutFacingMountain` | `rimba_lookout_facing_mountain` |

### AbahSpriteSet.asset
Path: `Assets/_Project/Data/Cutscenes/Chapter1/Scene1/AbahSpriteSet.asset`

| Field | Assign Sprite |
|---|---|
| `idleTerasTalking` | `abah_idle_teras_talking` |
| `lookoutFacingMountain` | `abah_lookout_facing_mountain` |

---

## 3. Assign References di Scene1CutsceneController

Pilih GameObject **`Scene1_Cutscene`** di Hierarchy, lalu di Inspector component **Scene1CutsceneController**:

### Sprite Sets:
- **Rimba Sprite Set:** drag `RimbaSpriteSet.asset`
- **Abah Sprite Set:** drag `AbahSpriteSet.asset`

### Shot 1 - Wide Establishing:
- **Shot1 Root:** drag GameObject `Shot1_WideEstablishing`
- **Shot1 Rimba:** drag `Shot1_WideEstablishing/Char_Rimba` (SpriteRenderer)
- **Shot1 Abah:** drag `Shot1_WideEstablishing/Char_Abah` (SpriteRenderer)

### Shot 2 - Look-out:
- **Shot2 Root:** drag GameObject `Shot2_Lookout`
- **Shot2 Rimba:** drag `Shot2_Lookout/Char_Rimba` (SpriteRenderer)
- **Shot2 Abah:** drag `Shot2_Lookout/Char_Abah` (SpriteRenderer)

### UI Components:
- **Dialogue Display:** (belum dibuat - perlu setup Canvas + UI, lihat bagian 4)
- **Action Button:** (belum dibuat - perlu setup Canvas + UI, lihat bagian 4)
- **Blackscreen Overlay:** drag GameObject `Blackscreen` (akan ditambahkan UI Image)

### Animation:
- **Rimba Sprite Swapper:** drag `Shot2_Lookout/Char_Rimba` (sudah ada CutsceneSpriteSwapper component)

### Audio:
- **Audio Manager:** drag GameObject `Audio` (sudah ada CutsceneAudioManager component)
- **Ambient Morning:** drag `s1_ambience_morning.wav`
- **Sfx Transition Blackscreen:** drag `s1_sfx_transition_blackscreen.wav`

### Exploration Handoff:
- **Exploration Root:** (kosongkan dulu - akan dibuat nanti untuk exploration phase)

---

## 4. Setup UI Canvas (Manual Required)

Cutscene butuh **Canvas (Screen Space - Overlay)** untuk dialogue box dan action button.

### Langkah:
1. Klik kanan di Hierarchy → **UI → Canvas**
2. Rename jadi `Canvas_Cutscene`
3. Pindahkan ke dalam `Scene1_Cutscene/UI_Cutscene/`

### A. Dialogue Box Setup:
1. Klik kanan `Canvas_Cutscene` → **UI → Image**
2. Rename jadi `DialogueBox`
3. Add component: **CutsceneDialogueDisplay**
4. Setup Inspector:
   - **Dialogue Box Root:** drag `DialogueBox` GameObject itu sendiri
   - **Speaker Name Text:** buat child TextMeshProUGUI untuk nama speaker
   - **Dialogue Text:** buat child TextMeshProUGUI untuk text dialog
   - **Dialogue Box Image:** drag Image component dari `DialogueBox`
   - Assign sprite `s1_ui_dialoguebox.png` ke Image component
5. Position & Anchoring:
   - Anchor: **Bottom Center**
   - Position Y: `100` (sesuaikan nanti)

### B. Action Button Setup:
1. Klik kanan `Canvas_Cutscene` → **UI → Button**
2. Rename jadi `ActionButton_Lihat`
3. Add component: **CutsceneActionButton**
4. Setup Inspector:
   - **Button Root:** drag `ActionButton_Lihat` GameObject itu sendiri
   - **Action Button:** drag Button component
   - **Tap Indicator:** buat child Image untuk animasi tap (sprite: `s1_ui_tapindicator.png`)
   - **Sfx Source:** drag AudioSource dari `Audio` GameObject
   - **Tap Confirm Clip:** drag `s1_sfx_tap_confirm.wav`
5. Assign sprite `s1_ui_actionbutton_lihat.png` ke Button Image
6. Position: **Center** atau sesuai mockup

### C. Blackscreen Setup:
1. Pilih GameObject `Blackscreen`
2. Add component: **Canvas** (buat Canvas terpisah untuk fullscreen overlay)
3. Set Canvas:
   - **Render Mode:** `Screen Space - Overlay`
   - **Sort Order:** `100` (paling atas)
4. Add component: **Image**
5. Set Image:
   - **Color:** Hitam (RGBA: 0, 0, 0, 255)
   - **Raycast Target:** OFF (supaya tidak block input)
6. Stretch Anchor:
   - Anchor Presets: **Stretch All** (fill screen)
   - Left/Right/Top/Bottom: `0`

---

## 5. Configure CutsceneSpriteSwapper

Pilih GameObject **`Shot2_Lookout/Char_Rimba`**, di Inspector component **CutsceneSpriteSwapper**:

- **Target Renderer:** drag SpriteRenderer component dari GameObject ini sendiri
- **Frame Duration:** `0.12` (default sudah OK, bisa adjust kalau mau lebih cepat/lambat)

---

## 6. Configure CutsceneAudioManager

Pilih GameObject **`Audio`**, di Inspector component **CutsceneAudioManager**:

### Audio Sources (sudah ada 2 AudioSource, assign manual):
- **Ambient Source:** drag AudioSource pertama (untuk loop ambient)
- **Sfx Source:** drag AudioSource kedua (untuk one-shot SFX)

### Fade Settings:
- **Fade Out Duration:** `1.5` (default OK)

---

## 7. Assign Background Sprites Manually

Karena texture belum di-set sebagai Sprite, setelah step 1 selesai:

### Shot 1:
- `Shot1_WideEstablishing/BG_WideEstablishing` → assign sprite `s1_bg_wide_establishing.png`

### Shot 2:
- `Shot2_Lookout/BG_Mountain_Sky` → assign sprite `s1_bg_lookout_mountain_sky.png`
- `Shot2_Lookout/BG_Window` → assign sprite `s1_bg_lookout_window.png`

### Set Sorting Layer (opsional, untuk depth control):
Buat Sorting Layers baru via **Edit → Project Settings → Tags and Layers**:
- `Background Far` (Order: -100)
- `Background Near` (Order: -50)
- `Characters` (Order: 0)

Lalu assign:
- `BG_Mountain_Sky` → Sorting Layer: `Background Far`
- `BG_Window` → Sorting Layer: `Background Near`
- Semua `Char_*` → Sorting Layer: `Characters`

---

## 8. Testing Cutscene

1. Pastikan Main Camera position di `(0, 0, -10)` untuk melihat scene 2D
2. Tekan **Play** di Unity
3. Cutscene harus berjalan otomatis (Beat 1 → Beat 8)
4. **Beat 5** harus menunggu player tap tombol "Lihat" sebelum lanjut

### Expected Flow:
- Beat 1: Spawn Rimba & Abah di Shot1 (auto-advance 1 detik)
- Beat 2: Dialog Abah → tap untuk lanjut
- Beat 3: Blackscreen 0.7 detik
- Beat 4: Reveal Shot2 (auto-advance 0.5 detik)
- Beat 5: Tombol "Lihat" muncul → **WAJIB TAP** sebelum lanjut
- Beat 6: Rimba menoleh (4 frame sprite swap)
- Beat 7: 6 baris dialog berurutan → tap untuk advance tiap baris
- Beat 8: Fade out ambient, deactivate cutscene shots

---

## 9. Troubleshooting

### Sprite tidak muncul di Scene:
- Pastikan texture sudah di-set sebagai **Sprite (2D and UI)** (step 1)
- Pastikan Camera Projection = **Orthographic**
- Pastikan Z position background > character (Background: Z=10/20, Character: Z=0)

### Cutscene tidak jalan otomatis:
- Pastikan `Scene1CutsceneController` ada di GameObject `Scene1_Cutscene`
- Cek Console untuk error (red message)

### Action Button tidak muncul:
- Pastikan Canvas di-setup dengan benar (step 4B)
- Pastikan `CutsceneActionButton` component sudah assign semua reference

### Audio tidak bunyi:
- Pastikan AudioClip files sudah di-import
- Pastikan AudioSource tidak di-mute
- Check Audio Mixer settings (kalau ada)

---

## 10. Next Steps (Out of Scope Cutscene)

Setelah cutscene selesai (Beat 8), perlu dibuat:

1. **Exploration System:**
   - PlayerMovementController (side-scroll kiri/kanan)
   - Exploration Camera (follow player)
   - Parallax background system (gunakan `explore_path_terasketebon_*.png`)
   - MovementBoundary2D

2. **Integration:**
   - Implement `IPlayerMovementLock` interface di PlayerMovementController
   - Assign `explorationRoot` reference di Scene1CutsceneController Inspector

3. **Dialogue System (Citampi Story style):**
   - Integrate existing DialogueSystem (kalau sudah ada)
   - Atau ganti `CutsceneDialogueDisplay` dengan DialogueSystem yang lebih robust

---

## File Structure Summary

```
Assets/_Project/
├── Art/
│   ├── Characters/
│   │   ├── Rimba/Scene1/ (5 sprites)
│   │   └── Abah/Scene1/ (2 sprites)
│   ├── Environment/Chapter1/
│   │   ├── Scene1_TerasRumah/ (3 backgrounds)
│   │   └── Scene1_Exploration/ (3 parallax layers)
│   └── UI/
│       ├── Dialogue/ (1 dialogue box)
│       └── ActionButtons/ (2 UI sprites)
│
├── Audio/
│   ├── SFX/UI/ (1 tap SFX)
│   ├── SFX/Transitions/ (1 transition SFX)
│   └── Music/Ambience/ (1 ambient loop)
│
├── Data/Cutscenes/Chapter1/Scene1/
│   ├── RimbaSpriteSet.asset
│   └── AbahSpriteSet.asset
│
├── Scripts/Cutscenes/
│   ├── RimbaSpriteSet.cs
│   ├── AbahSpriteSet.cs
│   ├── Scene1CutsceneController.cs
│   ├── CutsceneDialogueDisplay.cs
│   ├── CutsceneActionButton.cs
│   ├── CutsceneSpriteSwapper.cs
│   └── CutsceneAudioManager.cs
│
└── Scenes/Game/
    └── Chapter1.unity
```

---

## Credits

**Implementation Date:** 2026-09-23  
**Style Reference:** A Space for the Unbound (cutscene structure)  
**Dialogue Style:** Citampi Story (akan di-integrate nanti)  
**Target Platform:** Android (API 28+)

---

Selamat setup! 🚀
