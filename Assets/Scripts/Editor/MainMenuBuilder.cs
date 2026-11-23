#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KarindingLegacy.UI.Editor
{
    /// <summary>
    /// Generator otomatis Unity Editor untuk membangun Scene Main Menu, Canvas, EventSystem,
    /// seluruh tombol, modal pemilihan mode (Game Baru/Lanjutkan), modal save slots, modal settings/credits,
    /// animasi UI, dan konfigurasi Build Settings yang disesuaikan untuk layar Mobile (Touch-friendly).
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuBuilder
    {
        private const string SCENE_DIR = "Assets/Scenes";
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
        private const string GAMEPLAY_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string OPENING_CUTSCENE_SCENE_PATH = "Assets/_Project/_projects_FirstScene/Opening_Scene/Opening_Cutscene.unity";
        private const string PREFAB_DIR = "Assets/Prefabs";
        private const string PREFAB_PATH = "Assets/Prefabs/MainMenuCanvas.prefab";
        private const string ASSET_DIR = "Assets/Asset";
        private const string MENU_FONT_PATH = "Assets/_Project/Art/UI/Font/PixelifySans-VariableFont_wght SDF.asset";

        static MainMenuBuilder()
        {
            EditorApplication.delayCall += SharpenExistingMenuText;
        }

        [MenuItem("Tools/Karinding Legacy/Sharpen Main Menu Font", priority = 5)]
        public static void SharpenMenuTextNow()
        {
            SharpenExistingMenuText();
        }

        [MenuItem("Tools/Karinding Legacy/Build Main Menu Scene", priority = 1)]
        public static void BuildMenuManual()
        {
            BuildMainMenuScene(silent: false);
        }

        public static void BuildMenuBatch()
        {
            BuildMainMenuScene(silent: true);
        }

        [MenuItem("Tools/Karinding Legacy/Open Main Menu Scene", priority = 2)]
        public static void OpenMainMenuScene()
        {
            if (File.Exists(MAIN_MENU_SCENE_PATH))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH);
                    Debug.Log($"[MainMenuBuilder] Membuka scene: {MAIN_MENU_SCENE_PATH}");
                }
            }
            else
            {
                Debug.LogWarning($"[MainMenuBuilder] Scene {MAIN_MENU_SCENE_PATH} belum ada. Membangun sekarang...");
                BuildMainMenuScene(silent: false);
            }
        }

        [MenuItem("Tools/Karinding Legacy/Open Gameplay Scene (SampleScene)", priority = 3)]
        public static void OpenGameplayScene()
        {
            if (File.Exists(GAMEPLAY_SCENE_PATH))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(GAMEPLAY_SCENE_PATH);
                    Debug.Log($"[MainMenuBuilder] Membuka scene gameplay: {GAMEPLAY_SCENE_PATH}");
                }
            }
            else
            {
                Debug.LogError($"[MainMenuBuilder] Gameplay scene {GAMEPLAY_SCENE_PATH} tidak ditemukan.");
            }
        }

        public static void BuildMainMenuScene(bool silent = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MainMenuBuilder] Tidak dapat membangun scene saat Play Mode sedang aktif.");
                return;
            }

            try
            {
                if (!Directory.Exists(SCENE_DIR)) Directory.CreateDirectory(SCENE_DIR);
                if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // 1. Setup Camera
                GameObject cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                cameraObj.transform.position = new Vector3(0, 0, -10);
                Camera cam = cameraObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 1f);
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cameraObj.AddComponent<AudioListener>();

                var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpCamType != null)
                {
                    cameraObj.AddComponent(urpCamType);
                }

                // 2. Setup EventSystem
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();

                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemObj.AddComponent(inputModuleType);
                }
                else
                {
                    eventSystemObj.AddComponent<StandaloneInputModule>();
                }

                // 3. Muat Sprites
                Sprite sprBg = LoadSprite("backround-new.png");
                Sprite sprLogo = LoadSprite("e57069e4-dc3b-4576-ae85-9176ab353eb7.png");
                if (sprLogo == null) sprLogo = LoadSprite("Name Game.png");
                Sprite sprTitleFrame = LoadSprite("title_frame.png");
                Sprite sprDivider = LoadSprite("20_decorative_divider.png");
                Sprite sprBlankNormal = LoadSprite("02_frame_normal_blank.png");
                Sprite sprBlankHover = LoadSprite("03_frame_hover_blank.png");
                Sprite sprBorder = LoadSprite("border.png");

                TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MENU_FONT_PATH);
                if (menuFont == null)
                {
                    Debug.LogError("[MainMenuBuilder] Font PixelifySans SDF tidak ditemukan: " + MENU_FONT_PATH);
                    menuFont = TMP_Settings.defaultFontAsset;
                }

                // 4. Setup Canvas Scaler untuk Mobile Landscape
                GameObject canvasObj = new GameObject("MainMenuCanvas");
                canvasObj.layer = LayerMask.NameToLayer("UI");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                // Match 0.5 menyeimbangkan skala lebar dan tinggi untuk PC/laptop landscape.
                // Desain asli tetap presisi pada 1920x1080, dan proporsi UI stabil di rasio lain.
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
                MainMenuController controller = canvasObj.AddComponent<MainMenuController>();

                // 5. Background Image (Full Stretch)
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.sprite = sprBg;
                bgImage.raycastTarget = false;
                bgImage.color = Color.white;

                // Background "cover": Isi seluruh layar tanpa distorsi, crop hanya di tepi.
                // EnvelopeParent membuat background selalu menutup area Canvas pada rasio apa pun.
                AspectRatioFitter bgFitter = bgObj.AddComponent<AspectRatioFitter>();
                bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                bgFitter.aspectRatio = sprBg.rect.width / sprBg.rect.height;

                // 6. Main View Container
                GameObject mainViewObj = new GameObject("MainMenuView");
                mainViewObj.transform.SetParent(canvasObj.transform, false);
                RectTransform mainViewRect = mainViewObj.AddComponent<RectTransform>();
                mainViewRect.anchorMin = Vector2.zero;
                mainViewRect.anchorMax = Vector2.one;
                mainViewRect.sizeDelta = Vector2.zero;
                mainViewObj.AddComponent<CanvasGroup>();
                UIPageTransition mainMenuTransition = mainViewObj.AddComponent<UIPageTransition>();

                // Logo Game Title
                GameObject logoObj = new GameObject("GameLogo");
                logoObj.transform.SetParent(mainViewObj.transform, false);
                RectTransform logoRect = logoObj.AddComponent<RectTransform>();
                logoRect.anchorMin = new Vector2(0.5f, 1f);
                logoRect.anchorMax = new Vector2(0.5f, 1f);
                logoRect.pivot = new Vector2(0.5f, 1f);
                logoRect.anchoredPosition = new Vector2(0f, -40f);
                logoRect.sizeDelta = new Vector2(725f, 250f);
                Image logoImage = logoObj.AddComponent<Image>();
                logoImage.sprite = sprLogo;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;

                // Header Divider
                GameObject divObj = new GameObject("TitleDivider");
                divObj.transform.SetParent(mainViewObj.transform, false);
                RectTransform divRect = divObj.AddComponent<RectTransform>();
                divRect.anchorMin = new Vector2(0.5f, 1f);
                divRect.anchorMax = new Vector2(0.5f, 1f);
                divRect.pivot = new Vector2(0.5f, 1f);
                divRect.anchoredPosition = new Vector2(0f, -300f);
                divRect.sizeDelta = new Vector2(600f, 18f);
                Image divImage = divObj.AddComponent<Image>();
                divImage.sprite = sprDivider;
                divImage.preserveAspect = true;
                divImage.raycastTarget = false;

                // Buttons Container (Ukuran Tombol Mobile: Tinggi 90px, Lebar 440px)
                GameObject buttonsContainerObj = new GameObject("ButtonsContainer");
                buttonsContainerObj.transform.SetParent(mainViewObj.transform, false);
                RectTransform btnContainerRect = buttonsContainerObj.AddComponent<RectTransform>();
                btnContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
                btnContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
                btnContainerRect.pivot = new Vector2(0.5f, 0.5f);
                btnContainerRect.anchoredPosition = new Vector2(0f, -120f);
                btnContainerRect.sizeDelta = new Vector2(480f, 440f);

                VerticalLayoutGroup vLayout = buttonsContainerObj.AddComponent<VerticalLayoutGroup>();
                vLayout.spacing = 18f;
                vLayout.childAlignment = TextAnchor.MiddleCenter;
                vLayout.childControlWidth = false;
                vLayout.childControlHeight = false;
                vLayout.childForceExpandWidth = false;
                vLayout.childForceExpandHeight = false;

                Button btnPlay = CreateButton(buttonsContainerObj, "Btn_Play", "main.play", sprBlankNormal, sprBlankHover, menuFont, new Vector2(440f, 92f));
                Button btnSettings = CreateButton(buttonsContainerObj, "Btn_Settings", "main.settings", sprBlankNormal, sprBlankHover, menuFont, new Vector2(460f, 90f));
                Button btnCredits = CreateButton(buttonsContainerObj, "Btn_Credits", "main.credits", sprBlankNormal, sprBlankHover, menuFont, new Vector2(440f, 90f));
                Button btnQuit = CreateButton(buttonsContainerObj, "Btn_Quit", "main.quit", sprBlankNormal, sprBlankHover, menuFont, new Vector2(440f, 90f));

                // Footer Version Text
                GameObject footerObj = new GameObject("VersionText");
                footerObj.transform.SetParent(mainViewObj.transform, false);
                RectTransform footerRect = footerObj.AddComponent<RectTransform>();
                footerRect.anchorMin = new Vector2(1f, 0f);
                footerRect.anchorMax = new Vector2(1f, 0f);
                footerRect.pivot = new Vector2(1f, 0f);
                footerRect.anchoredPosition = new Vector2(-35f, 25f);
                footerRect.sizeDelta = new Vector2(300f, 35f);
                TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
                footerText.text = "Karinding Legacy v1.2.0 (Mobile Ready)";
                footerText.font = menuFont;
                footerText.fontSize = 18;
                footerText.alignment = TextAlignmentOptions.Right;
                footerText.color = new Color(0.88f, 0.82f, 0.70f, 0.65f);
                footerText.raycastTarget = false;

                // 7. Modal Pemilihan Mode (Game Baru / Lanjutkan) - Ukuran Ergonomis Mobile
                var playSelectData = BuildPlaySelectModal(canvasObj, sprTitleFrame, sprBlankNormal, sprBlankHover, sprDivider, menuFont);

                // 8. Modal Save Slots - 6 slot dalam grid 2x3 (Touch-friendly)
                var saveSlotData = BuildSaveSlotModal(canvasObj, sprBlankNormal, sprBlankHover, sprDivider, menuFont);

                // 9. Settings Modal Panel - Touch-friendly Sliders dengan border asset
                var settingsData = BuildSettingsModal(canvasObj, sprBlankNormal, sprBlankHover, sprDivider, menuFont, sprBorder);

                // 10. New Game Confirmation Modal
                var confirmData = BuildNewGameConfirmModal(canvasObj, sprBlankNormal, sprBlankHover, sprDivider, menuFont);

                // 11. Credits Modal Panel
                var creditsData = BuildCreditsModal(canvasObj, sprBlankNormal, sprBlankHover, sprDivider, menuFont);

                // 11. Hubungkan Serialized Properties pada MainMenuController
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("gameplaySceneName").stringValue = "SampleScene";
                so.FindProperty("mainMenuView").objectReferenceValue = mainViewObj;
                so.FindProperty("mainMenuTransition").objectReferenceValue = mainMenuTransition;
                so.FindProperty("gameLogo").objectReferenceValue = logoObj;
                so.FindProperty("gameLogoFrame").objectReferenceValue = null;
                so.FindProperty("mainMenuButtonsContainer").objectReferenceValue = buttonsContainerObj;
                so.FindProperty("titleDivider").objectReferenceValue = divObj;
                so.FindProperty("playButton").objectReferenceValue = btnPlay;
                so.FindProperty("settingsButton").objectReferenceValue = btnSettings;
                so.FindProperty("creditsButton").objectReferenceValue = btnCredits;
                so.FindProperty("quitButton").objectReferenceValue = btnQuit;

                // Play Select Panel
                so.FindProperty("playSelectPanel").objectReferenceValue = playSelectData.panelObj;
                so.FindProperty("playSelectTransition").objectReferenceValue = playSelectData.transition;
                so.FindProperty("newGameButton").objectReferenceValue = playSelectData.newGameBtn;
                so.FindProperty("continueButton").objectReferenceValue = playSelectData.continueBtn;
                so.FindProperty("playSelectCloseButton").objectReferenceValue = playSelectData.closeBtn;

                // Save Slot Panel
                so.FindProperty("saveSlotPanel").objectReferenceValue = saveSlotData.panelObj;
                so.FindProperty("saveSlotTransition").objectReferenceValue = saveSlotData.transition;
                so.FindProperty("saveSlotTitleText").objectReferenceValue = saveSlotData.titleText;
                so.FindProperty("saveSlotSubtitleText").objectReferenceValue = saveSlotData.subtitleText;
                so.FindProperty("saveSlotCloseButton").objectReferenceValue = saveSlotData.closeBtn;

                var slotArrayProp = so.FindProperty("slotItems");
                slotArrayProp.arraySize = saveSlotData.slotItems.Length;
                for (int i = 0; i < saveSlotData.slotItems.Length; i++)
                {
                    slotArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = saveSlotData.slotItems[i];
                }

                // New Game Confirmation Panel
                so.FindProperty("newGameConfirmPanel").objectReferenceValue = confirmData.panelObj;
                so.FindProperty("newGameConfirmTransition").objectReferenceValue = confirmData.transition;
                so.FindProperty("newGameConfirmText").objectReferenceValue = confirmData.messageText;
                so.FindProperty("newGameConfirmYesButton").objectReferenceValue = confirmData.yesBtn;
                so.FindProperty("newGameConfirmCancelButton").objectReferenceValue = confirmData.cancelBtn;

                // Settings & Credits
                so.FindProperty("settingsPanel").objectReferenceValue = settingsData.panelObj;
                so.FindProperty("settingsTransition").objectReferenceValue = settingsData.transition;
                so.FindProperty("masterVolumeSlider").objectReferenceValue = settingsData.masterSlider;
                so.FindProperty("musicVolumeSlider").objectReferenceValue = settingsData.musicSlider;
                so.FindProperty("sfxVolumeSlider").objectReferenceValue = settingsData.sfxSlider;
                so.FindProperty("masterVolumeText").objectReferenceValue = settingsData.masterText;
                so.FindProperty("musicVolumeText").objectReferenceValue = settingsData.musicText;
                so.FindProperty("sfxVolumeText").objectReferenceValue = settingsData.sfxText;
                so.FindProperty("settingsCloseButton").objectReferenceValue = settingsData.closeBtn;
                so.FindProperty("languageIndonesianButton").objectReferenceValue = settingsData.languageIndonesianBtn;
                so.FindProperty("languageSundaneseButton").objectReferenceValue = settingsData.languageSundaneseBtn;
                so.FindProperty("languageIndonesianText").objectReferenceValue = settingsData.languageIndonesianText;
                so.FindProperty("languageSundaneseText").objectReferenceValue = settingsData.languageSundaneseText;

                so.FindProperty("creditsPanel").objectReferenceValue = creditsData.panelObj;
                so.FindProperty("creditsTransition").objectReferenceValue = creditsData.transition;
                so.FindProperty("creditsCloseButton").objectReferenceValue = creditsData.closeBtn;

                so.ApplyModifiedProperties();

                // 12. Simpan Prefab & Scene (scene disimpan terakhir agar tidak tertinggal dirty)
                PrefabUtility.SaveAsPrefabAsset(canvasObj, PREFAB_PATH);
                UpdateBuildSettings();
                EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Simpan sekali lagi setelah refresh aset agar scene benar-benar bersih.
                EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);

                Debug.Log($"[MainMenuBuilder] Main Menu diperbarui sukses: responsif PC landscape 1920x1080.");

                if (!silent)
                {
                    EditorUtility.DisplayDialog("Main Menu Siap!",
                        "Scene Main Menu berhasil diperbarui untuk PC/laptop landscape.\n\n" +
                        "Canvas Scaler: Scale With Screen Size, referensi 1920x1080, match 0.5.\n" +
                        "Background menggunakan mode cover agar tidak ada ruang kosong atau distorsi.\n" +
                        "Pilihan Game Baru, Lanjutkan, 6 Save Slot, dan konfirmasi tetap aktif.",
                        "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MainMenuBuilder] Kesalahan saat membangun Main Menu: {ex}");
            }
        }

        private static Button CreateButton(GameObject parent, string name, string localizationKey, Sprite normal, Sprite hover, TMP_FontAsset font, Vector2 size)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = normal;
            img.preserveAspect = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            if (hover != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState ss = new SpriteState
                {
                    highlightedSprite = hover,
                    pressedSprite = hover,
                    selectedSprite = hover
                };
                btn.spriteState = ss;
            }

            btnObj.AddComponent<UIHoverAnimation>();

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            txtRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = LocalizationManager.Get(localizationKey);
            txt.font = font;
            txt.fontSize = 32;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.96f, 0.82f, 0.44f);
            txt.raycastTarget = false;

            AttachLocalizedText(txt, localizationKey);

            return btn;
        }

        // ==================== MODAL PILIH MODE (GAME BARU / LANJUTKAN) ====================

        private struct PlaySelectModalData
        {
            public GameObject panelObj;
            public UIModalTransition transition;
            public Button newGameBtn;
            public Button continueBtn;
            public Button closeBtn;
        }

        private static PlaySelectModalData BuildPlaySelectModal(GameObject parent, Sprite titleFrame, Sprite blankNormal, Sprite blankHover, Sprite divider, TMP_FontAsset font)
        {
            var data = new PlaySelectModalData();

            GameObject panelObj = new GameObject("PlaySelectPanel");
            panelObj.transform.SetParent(parent.transform, false);
            data.panelObj = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<CanvasGroup>();
            data.transition = panelObj.AddComponent<UIModalTransition>();

            // Halaman pilihan mode tampil langsung di atas background tanpa panel/backdrop.
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelObj.transform, false);
            RectTransform boxRect = contentBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = new Vector2(0f, -80f);
            boxRect.sizeDelta = new Vector2(680f, 520f);

            // Frame Title di belakang TitleText
            if (titleFrame != null)
            {
                CreateImage(contentBox, "TitleFrame", titleFrame, new Vector2(0f, 195f), new Vector2(460f, 150f));
            }
            else
            {
                // Top Divider fallback jika frame title tidak tersedia
                CreateImage(contentBox, "TopDivider", divider, new Vector2(0f, 160f), new Vector2(520f, 16f));
            }

            // Title
            CreateLocalizedText(contentBox, "TitleText", "play.title", font, 34, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(0f, 195f), new Vector2(480f, 45f), TextAlignmentOptions.Center);

            // Tombol Game Baru (Touch Target: 440 x 88 px)
            data.newGameBtn = CreateActionButton(contentBox, "Btn_GameBaru", "GAME BARU", blankNormal, blankHover, font,
                new Vector2(0f, 85f), new Vector2(440f, 88f), "play.newGame");

            // Tombol Lanjutkan (Touch Target: 440 x 88 px)
            data.continueBtn = CreateActionButton(contentBox, "Btn_Lanjutkan", "LANJUTKAN", blankNormal, blankHover, font,
                new Vector2(0f, -15f), new Vector2(440f, 88f), "play.continue");

            // Bottom Divider
            CreateImage(contentBox, "BottomDivider", divider, new Vector2(0f, -100f), new Vector2(520f, 16f));

            // Tombol Kembali
            data.closeBtn = CreateActionButton(contentBox, "Btn_Kembali", "KEMBALI", blankNormal, blankHover, font,
                new Vector2(0f, -180f), new Vector2(260f, 58f), "common.back");

            panelObj.SetActive(false);
            return data;
        }

        // ==================== MODAL SAVE SLOTS (6 SLOT) ====================

        private struct SaveSlotModalData
        {
            public GameObject panelObj;
            public UIModalTransition transition;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI subtitleText;
            public SaveSlotUIItem[] slotItems;
            public Button closeBtn;
        }

        private static SaveSlotModalData BuildSaveSlotModal(GameObject parent, Sprite blankNormal, Sprite blankHover, Sprite divider, TMP_FontAsset font)
        {
            var data = new SaveSlotModalData();

            GameObject panelObj = new GameObject("SaveSlotPanel");
            panelObj.transform.SetParent(parent.transform, false);
            data.panelObj = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<CanvasGroup>();
            data.transition = panelObj.AddComponent<UIModalTransition>();

            CreateBackdrop(panelObj, 0.38f);

            // Content Box Mobile (Lebar 820, Tinggi 640) - transparan agar background tetap terlihat.
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelObj.transform, false);
            RectTransform boxRect = contentBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(820f, 640f);
            Image boxImg = contentBox.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.10f, 0.08f, 0.88f);

            // Title
            data.titleText = CreateText(contentBox, "TitleText", LocalizationManager.Get("save.title"), font, 32, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(0f, 272f), new Vector2(600f, 45f), TextAlignmentOptions.Center);

            // Top Divider
            CreateImage(contentBox, "TopDivider", divider, new Vector2(0f, 234f), new Vector2(640f, 16f));

            // Subtitle
            data.subtitleText = CreateText(contentBox, "SubtitleText", LocalizationManager.Get("save.subtitleContinue"), font, 18, FontStyles.Normal, new Color(0.88f, 0.84f, 0.76f),
                new Vector2(0f, 202f), new Vector2(740f, 30f), TextAlignmentOptions.Center);

            // 6 Save Slots dalam daftar vertikal bergaya modern:
            // teks saja + garis pemisah tipis, tanpa frame/gambar kartu (touch-friendly).
            data.slotItems = new SaveSlotUIItem[6];
            const float slotRowWidth = 720f;
            const float slotRowHeight = 60f;
            const float slotRowStep = 64f;
            const float firstRowY = 136f;
            Color slotLineColor = new Color(0.96f, 0.82f, 0.44f, 0.22f);
            Color slotHoverTint = new Color(0.96f, 0.82f, 0.44f, 0.10f);

            for (int i = 0; i < 6; i++)
            {
                int slotIndex = i + 1;
                float rowY = firstRowY - (i * slotRowStep);

                GameObject slotObj = new GameObject($"SlotItem_{slotIndex}");
                slotObj.transform.SetParent(contentBox.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.anchoredPosition = new Vector2(0f, rowY);
                slotRect.sizeDelta = new Vector2(slotRowWidth, slotRowHeight);

                // Latar transparan: hanya muncul samar saat hover/tekan (tanpa frame gambar).
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = slotHoverTint;

                Button slotBtn = slotObj.AddComponent<Button>();
                slotBtn.targetGraphic = slotBg;
                slotBtn.transition = Selectable.Transition.ColorTint;
                slotBtn.colors = new ColorBlock
                {
                    normalColor = new Color(1f, 1f, 1f, 0.2f),
                    highlightedColor = Color.white,
                    pressedColor = new Color(1f, 0.92f, 0.72f, 1f),
                    selectedColor = Color.white,
                    disabledColor = new Color(1f, 1f, 1f, 0.2f),
                    colorMultiplier = 1f,
                    fadeDuration = 0.08f
                };

                slotObj.AddComponent<UIHoverAnimation>();
                CanvasGroup slotCg = slotObj.AddComponent<CanvasGroup>();

                // Garis pemisah modern di bawah setiap baris slot.
                Image slotLine = CreateImage(contentBox, $"SlotLine_{slotIndex}", null,
                    new Vector2(0f, rowY - (slotRowHeight * 0.5f) - 2f), new Vector2(slotRowWidth, 1.5f));
                slotLine.color = slotLineColor;

                // Nomor slot (kiri)
                TextMeshProUGUI sTitle = CreateText(slotObj, "SlotTitle", LocalizationManager.Format("slot.title", slotIndex), font, 20, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                    new Vector2(-268f, 0f), new Vector2(110f, 34f), TextAlignmentOptions.Left);

                // Detail slot (tengah, satu baris teks)
                TextMeshProUGUI sDetails = CreateText(slotObj, "SlotDetails", LocalizationManager.Get("slot.loading"), font, 16, FontStyles.Normal, new Color(0.90f, 0.86f, 0.78f),
                    new Vector2(40f, 0f), new Vector2(400f, 34f), TextAlignmentOptions.Left);

                // Delete Button: teks saja tanpa kotak, touch size tetap 48 x 48 px.
                GameObject delObj = new GameObject("Btn_Delete");
                delObj.transform.SetParent(slotObj.transform, false);
                RectTransform delRect = delObj.AddComponent<RectTransform>();
                delRect.anchoredPosition = new Vector2(328f, 0f);
                delRect.sizeDelta = new Vector2(48f, 48f);
                Image delImg = delObj.AddComponent<Image>();
                delImg.color = new Color(1f, 1f, 1f, 0f);
                Button delBtn = delObj.AddComponent<Button>();
                delBtn.targetGraphic = delImg;
                delObj.AddComponent<UIHoverAnimation>();

                CreateText(delObj, "Text", "X", font, 22, FontStyles.Bold, new Color(0.95f, 0.55f, 0.45f), Vector2.zero, new Vector2(48f, 48f), TextAlignmentOptions.Center);

                SaveSlotUIItem item = slotObj.AddComponent<SaveSlotUIItem>();
                SerializedObject itemSo = new SerializedObject(item);
                itemSo.FindProperty("slotIndex").intValue = slotIndex;
                itemSo.FindProperty("slotTitleText").objectReferenceValue = sTitle;
                itemSo.FindProperty("slotDetailsText").objectReferenceValue = sDetails;
                itemSo.FindProperty("selectButton").objectReferenceValue = slotBtn;
                itemSo.FindProperty("deleteButton").objectReferenceValue = delBtn;
                itemSo.FindProperty("canvasGroup").objectReferenceValue = slotCg;
                itemSo.ApplyModifiedProperties();

                data.slotItems[i] = item;
            }

            // Bottom Divider
            CreateImage(contentBox, "BottomDivider", divider, new Vector2(0f, -240f), new Vector2(740f, 16f));

            // Back Button
            data.closeBtn = CreateActionButton(contentBox, "Btn_Kembali", "KEMBALI", blankNormal, blankHover, font,
                new Vector2(0f, -288f), new Vector2(270f, 58f), "common.back");

            panelObj.SetActive(false);
            return data;
        }

        // ==================== MODAL KONFIRMASI GAME BARU ====================

        private struct NewGameConfirmModalData
        {
            public GameObject panelObj;
            public UIModalTransition transition;
            public TextMeshProUGUI messageText;
            public Button yesBtn;
            public Button cancelBtn;
        }

        private static NewGameConfirmModalData BuildNewGameConfirmModal(GameObject parent, Sprite blankNormal, Sprite blankHover, Sprite divider, TMP_FontAsset font)
        {
            var data = new NewGameConfirmModalData();

            GameObject panelObj = new GameObject("NewGameConfirmPanel");
            panelObj.transform.SetParent(parent.transform, false);
            data.panelObj = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<CanvasGroup>();
            data.transition = panelObj.AddComponent<UIModalTransition>();

            CreateBackdrop(panelObj);

            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelObj.transform, false);
            RectTransform boxRect = contentBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(700f, 380f);
            Image boxImg = contentBox.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);

            CreateLocalizedText(contentBox, "TitleText", "confirm.title", font, 32, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(0f, 135f), new Vector2(560f, 45f), TextAlignmentOptions.Center);

            CreateImage(contentBox, "TopDivider", divider, new Vector2(0f, 100f), new Vector2(560f, 16f));

            data.messageText = CreateText(contentBox, "MessageText", "Mulai game baru pada slot ini?", font, 24, FontStyles.Normal, new Color(0.94f, 0.90f, 0.82f),
                new Vector2(0f, 25f), new Vector2(600f, 110f), TextAlignmentOptions.Center);

            data.yesBtn = CreateActionButton(contentBox, "Btn_YaMulai", "YA, MULAI", blankNormal, blankHover, font,
                new Vector2(-145f, -110f), new Vector2(250f, 62f), "confirm.yes");

            data.cancelBtn = CreateActionButton(contentBox, "Btn_Batal", "BATAL", blankNormal, blankHover, font,
                new Vector2(145f, -110f), new Vector2(250f, 62f), "confirm.cancel");

            panelObj.SetActive(false);
            return data;
        }

        // ==================== MODAL SETTINGS ====================

        private struct SettingsModalData
        {
            public GameObject panelObj;
            public UIModalTransition transition;
            public Slider masterSlider;
            public Slider musicSlider;
            public Slider sfxSlider;
            public TextMeshProUGUI masterText;
            public TextMeshProUGUI musicText;
            public TextMeshProUGUI sfxText;
            public Button closeBtn;
            public Button languageIndonesianBtn;
            public Button languageSundaneseBtn;
            public TextMeshProUGUI languageIndonesianText;
            public TextMeshProUGUI languageSundaneseText;
        }

        private const float SettingsRowHeight = 84f;
        private const float SettingsRowSpacing = 18f;
        private const float SettingsRowWidth = 680f;
        private const float SettingsLanguageGap = 24f;
        private const float SettingsLanguageRowHeight = 84f;

        private static SettingsModalData BuildSettingsModal(GameObject parent, Sprite blankNormal, Sprite blankHover, Sprite divider, TMP_FontAsset font, Sprite borderSprite)
        {
            var data = new SettingsModalData();

            GameObject panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(parent.transform, false);
            data.panelObj = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<CanvasGroup>();
            data.transition = panelObj.AddComponent<UIModalTransition>();

            CreateBackdrop(panelObj);

            // Tiga slider tetap memakai susunan lama. Baris bahasa ditambahkan di bawahnya
            // tanpa menggeser posisi slider yang sudah ada.
            const int sliderCount = 3;
            const float topPadding = 36f;
            const float titleHeight = 46f;
            const float headerGap = 12f;
            const float dividerHeight = 16f;
            const float listGap = 34f;
            const float footerGap = 34f;
            const float buttonHeight = 62f;
            const float bottomPadding = 36f;

            float listHeight = sliderCount * SettingsRowHeight + (sliderCount - 1) * SettingsRowSpacing;
            float languageBlockHeight = SettingsLanguageGap + SettingsLanguageRowHeight;
            float contentHeight = topPadding + titleHeight + headerGap + dividerHeight + listGap + listHeight
                + languageBlockHeight + footerGap + dividerHeight + 24f + buttonHeight + bottomPadding;

            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelObj.transform, false);
            RectTransform boxRect = contentBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(820f, contentHeight);
            boxRect.anchoredPosition = new Vector2(0f, -languageBlockHeight * 0.5f);
            Image boxImg = contentBox.AddComponent<Image>();
            if (borderSprite != null)
            {
                boxImg.sprite = borderSprite;
                boxImg.preserveAspect = false;
                boxImg.color = Color.white;
            }
            else
            {
                boxImg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);
            }

            float topEdge = contentHeight * 0.5f;
            float titleCenterY = topEdge - topPadding - titleHeight * 0.5f;
            float topDividerY = titleCenterY - titleHeight * 0.5f - headerGap - dividerHeight * 0.5f;
            float listTopY = topDividerY - dividerHeight * 0.5f - listGap;
            float listCenterY = listTopY - listHeight * 0.5f;
            float languageCenterY = listTopY - listHeight - SettingsLanguageGap - SettingsLanguageRowHeight * 0.5f;
            float bottomDividerY = languageCenterY - SettingsLanguageRowHeight * 0.5f - footerGap - dividerHeight * 0.5f;
            float buttonCenterY = bottomDividerY - dividerHeight * 0.5f - 24f - buttonHeight * 0.5f;

            CreateLocalizedText(contentBox, "TitleText", "settings.title", font, 34, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(0f, titleCenterY), new Vector2(520f, titleHeight), TextAlignmentOptions.Center);

            CreateImage(contentBox, "TopDivider", divider, new Vector2(0f, topDividerY), new Vector2(560f, dividerHeight));

            GameObject sliderList = new GameObject("VolumeSliders");
            sliderList.transform.SetParent(contentBox.transform, false);
            RectTransform listRect = sliderList.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, listCenterY);
            listRect.sizeDelta = new Vector2(SettingsRowWidth, listHeight);

            VerticalLayoutGroup listLayout = sliderList.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = SettingsRowSpacing;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = false;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = false;
            listLayout.childForceExpandHeight = false;

            Sprite knobSprite = LoadKnobSprite();

            var masterData = CreateSlider(sliderList, "MasterVolumeSlider", "settings.master", font, knobSprite);
            data.masterSlider = masterData.slider;
            data.masterText = masterData.valueText;

            var musicData = CreateSlider(sliderList, "MusicVolumeSlider", "settings.music", font, knobSprite);
            data.musicSlider = musicData.slider;
            data.musicText = musicData.valueText;

            var sfxData = CreateSlider(sliderList, "SFXVolumeSlider", "settings.sfx", font, knobSprite);
            data.sfxSlider = sfxData.slider;
            data.sfxText = sfxData.valueText;

            CreateLanguageRow(contentBox, new Vector2(0f, languageCenterY), blankNormal, blankHover, font,
                out data.languageIndonesianBtn, out data.languageSundaneseBtn,
                out data.languageIndonesianText, out data.languageSundaneseText);

            CreateImage(contentBox, "BottomDivider", divider, new Vector2(0f, bottomDividerY), new Vector2(560f, dividerHeight));

            data.closeBtn = CreateActionButton(contentBox, "Btn_Kembali", "KEMBALI", blankNormal, blankHover, font,
                new Vector2(0f, buttonCenterY), new Vector2(300f, buttonHeight), "common.back");

            panelObj.SetActive(false);
            return data;
        }

        // ==================== MODAL CREDITS ====================

        private struct CreditsModalData
        {
            public GameObject panelObj;
            public UIModalTransition transition;
            public Button closeBtn;
        }

        private static CreditsModalData BuildCreditsModal(GameObject parent, Sprite blankNormal, Sprite blankHover, Sprite divider, TMP_FontAsset font)
        {
            var data = new CreditsModalData();

            GameObject panelObj = new GameObject("CreditsPanel");
            panelObj.transform.SetParent(parent.transform, false);
            data.panelObj = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<CanvasGroup>();
            data.transition = panelObj.AddComponent<UIModalTransition>();

            CreateBackdrop(panelObj);

            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelObj.transform, false);
            RectTransform boxRect = contentBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(780f, 590f);
            Image boxImg = contentBox.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);

            CreateLocalizedText(contentBox, "TitleText", "credits.title", font, 34, FontStyles.Bold, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(0f, 230f), new Vector2(520f, 45f), TextAlignmentOptions.Center);

            CreateImage(contentBox, "TopDivider", divider, new Vector2(0f, 190f), new Vector2(560f, 16f));

            CreateLocalizedText(contentBox, "CreditsBody", "credits.body", font, 20, FontStyles.Normal, new Color(0.92f, 0.89f, 0.82f),
                new Vector2(0f, 25f), new Vector2(640f, 260f), TextAlignmentOptions.Center);

            CreateImage(contentBox, "BottomDivider", divider, new Vector2(0f, -135f), new Vector2(560f, 16f));

            data.closeBtn = CreateActionButton(contentBox, "Btn_Kembali", "KEMBALI", blankNormal, blankHover, font,
                new Vector2(0f, -210f), new Vector2(260f, 58f), "common.back");

            panelObj.SetActive(false);
            return data;
        }

        // ==================== HELPER CREATION METHODS ====================

        private static void CreateBackdrop(GameObject parent, float alpha = 0.82f)
        {
            GameObject overlay = new GameObject("Backdrop");
            overlay.transform.SetParent(parent.transform, false);
            RectTransform overRect = overlay.AddComponent<RectTransform>();
            overRect.anchorMin = Vector2.zero;
            overRect.anchorMax = Vector2.one;
            overRect.sizeDelta = Vector2.zero;
            Image overImg = overlay.AddComponent<Image>();
            overImg.color = new Color(0.04f, 0.03f, 0.02f, alpha);
        }

        private struct SliderElements
        {
            public Slider slider;
            public TextMeshProUGUI valueText;
        }

        private static Sprite LoadKnobSprite()
        {
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (knob == null)
            {
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
            return knob;
        }

        private static SliderElements CreateSlider(GameObject parent, string name, string labelKey, TMP_FontAsset font, Sprite knobSprite)
        {
            var result = new SliderElements();

            GameObject row = new GameObject(name + "_Row");
            row.transform.SetParent(parent.transform, false);
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(SettingsRowWidth, SettingsRowHeight);

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minWidth = SettingsRowWidth;
            rowLayout.preferredWidth = SettingsRowWidth;
            rowLayout.minHeight = SettingsRowHeight;
            rowLayout.preferredHeight = SettingsRowHeight;

            // Label dan nilai berada pada satu baris di atas track, jadi tidak saling menimpa.
            CreateLocalizedText(row, "Label", labelKey, font, 24, FontStyles.Normal, new Color(0.94f, 0.90f, 0.82f),
                new Vector2(-152f, 23f), new Vector2(360f, 30f), TextAlignmentOptions.Left);

            CreateText(row, "ValueText", "100%", font, 22, FontStyles.Normal, new Color(0.96f, 0.82f, 0.44f),
                new Vector2(257f, 23f), new Vector2(150f, 30f), TextAlignmentOptions.Right);

            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform sRect = sliderObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -20f);
            sRect.sizeDelta = new Vector2(660f, 30f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject track = new GameObject("Track");
            track.transform.SetParent(sliderObj.transform, false);
            RectTransform trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.sizeDelta = new Vector2(0f, 5f);
            Image trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(0.85f, 0.80f, 0.70f, 0.22f);
            trackImg.raycastTarget = true;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.5f);
            faRect.anchorMax = new Vector2(1f, 0.5f);
            faRect.offsetMin = new Vector2(12f, -2.5f);
            faRect.offsetMax = new Vector2(-12f, 2.5f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fill.AddComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.94f, 0.76f, 0.34f, 1f);
            fillImg.raycastTarget = false;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRect = handleArea.AddComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.offsetMin = new Vector2(12f, 0f);
            haRect.offsetMax = new Vector2(-12f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRect = handle.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(24f, 24f);
            Image hImg = handle.AddComponent<Image>();
            hImg.sprite = knobSprite;
            hImg.preserveAspect = true;
            hImg.color = new Color(0.98f, 0.85f, 0.48f, 1f);

            slider.targetGraphic = hImg;
            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.direction = Slider.Direction.LeftToRight;

            result.slider = slider;
            result.valueText = row.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();

            return result;
        }

        private static void CreateLanguageRow(GameObject parent, Vector2 anchoredPosition, Sprite blankNormal, Sprite blankHover, TMP_FontAsset font,
            out Button indonesianButton, out Button sundaneseButton, out TextMeshProUGUI indonesianText, out TextMeshProUGUI sundaneseText)
        {
            GameObject row = new GameObject("LanguageRow");
            row.transform.SetParent(parent.transform, false);
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(SettingsRowWidth, SettingsLanguageRowHeight);
            rowRect.anchoredPosition = anchoredPosition;

            CreateLocalizedText(row, "Label", "settings.language", font, 24, FontStyles.Normal, new Color(0.94f, 0.90f, 0.82f),
                new Vector2(-152f, 23f), new Vector2(360f, 30f), TextAlignmentOptions.Left);

            indonesianButton = CreateActionButton(row, "Btn_BahasaIndonesia", "INDONESIA", blankNormal, blankHover, font,
                new Vector2(-135f, -18f), new Vector2(250f, 44f));
            sundaneseButton = CreateActionButton(row, "Btn_BahasaSunda", "SUNDA", blankNormal, blankHover, font,
                new Vector2(135f, -18f), new Vector2(250f, 44f));

            indonesianText = indonesianButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            sundaneseText = sundaneseButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();

            if (indonesianText != null) indonesianText.fontSize = 22;
            if (sundaneseText != null) sundaneseText.fontSize = 22;
        }

        private static Button CreateActionButton(GameObject parent, string name, string text, Sprite normal, Sprite hover, TMP_FontAsset font, Vector2 pos, Vector2 size, string localizationKey = null)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = normal;
            img.preserveAspect = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            if (hover != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState ss = new SpriteState
                {
                    highlightedSprite = hover,
                    pressedSprite = hover,
                    selectedSprite = hover
                };
                btn.spriteState = ss;
            }

            btnObj.AddComponent<UIHoverAnimation>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = 26;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.96f, 0.82f, 0.44f);
            txt.raycastTarget = false;

            if (!string.IsNullOrEmpty(localizationKey))
            {
                AttachLocalizedText(txt, localizationKey);
            }

            return btn;
        }

        private static TextMeshProUGUI CreateLocalizedText(GameObject parent, string name, string key, TMP_FontAsset font, int fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateText(parent, name, LocalizationManager.Get(key), font, fontSize, style, color, pos, size, alignment);
            AttachLocalizedText(text, key);
            return text;
        }

        private static void AttachLocalizedText(TextMeshProUGUI text, string key)
        {
            if (text == null || string.IsNullOrEmpty(key)) return;

            LocalizedText localized = text.gameObject.AddComponent<LocalizedText>();
            localized.SetKey(key);
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string content, TMP_FontAsset font, int fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent.transform, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            return txt;
        }

        private static Image CreateImage(GameObject parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            GameObject imgObj = new GameObject(name);
            imgObj.transform.SetParent(parent.transform, false);
            RectTransform rect = imgObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = imgObj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static Sprite LoadSprite(string filename)
        {
            string path = $"{ASSET_DIR}/{filename}";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length > 0)
            {
                return sprites.OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SharpenExistingMenuText()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(MAIN_MENU_SCENE_PATH)) return;
            if (!File.ReadAllText(MAIN_MENU_SCENE_PATH).Contains("UnityEngine.UI::UnityEngine.UI.Text")) return;

            Scene scene = EditorSceneManager.GetActiveScene();
            bool needsReload = scene.path != MAIN_MENU_SCENE_PATH
                || UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0;
            if (needsReload)
            {
                scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            }

            Text[] legacyTexts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (legacyTexts.Length == 0) return;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MENU_FONT_PATH);
            if (font == null)
            {
                Debug.LogError("[MainMenuBuilder] Font SDF tidak ditemukan: " + MENU_FONT_PATH);
                return;
            }

            var converted = new Dictionary<Text, TextMeshProUGUI>();
            foreach (Text legacy in legacyTexts)
            {
                GameObject go = legacy.gameObject;
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.font = font;
                tmp.text = legacy.text.Replace("✕", "X");
                tmp.fontSize = legacy.fontSize;
                tmp.fontStyle = ToTmpFontStyle(legacy.fontStyle);
                tmp.alignment = ToTmpAlignment(legacy.alignment);
                tmp.color = legacy.color;
                tmp.raycastTarget = legacy.raycastTarget;
                tmp.maskable = legacy.maskable;
                tmp.richText = legacy.supportRichText;
                tmp.textWrappingMode = legacy.horizontalOverflow == HorizontalWrapMode.Wrap
                    ? TextWrappingModes.Normal
                    : TextWrappingModes.NoWrap;
                tmp.overflowMode = legacy.verticalOverflow == VerticalWrapMode.Truncate
                    ? TextOverflowModes.Truncate
                    : TextOverflowModes.Overflow;
                tmp.lineSpacing = legacy.lineSpacing - 1f;
                tmp.enableAutoSizing = false;
                converted.Add(legacy, tmp);
            }

            var components = new List<Component>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<Component>(true));
            }

            foreach (Component component in components)
            {
                if (component == null) continue;
                SerializedObject so = new SerializedObject(component);
                SerializedProperty property = so.GetIterator();
                bool changed = false;
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Text referenced = property.objectReferenceValue as Text;
                    if (referenced == null || !converted.TryGetValue(referenced, out TextMeshProUGUI replacement)) continue;
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (Text legacy in legacyTexts)
            {
                UnityEngine.Object.DestroyImmediate(legacy, true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);
            Debug.Log($"[MainMenuBuilder] {converted.Count} label main menu dikonversi ke TextMeshPro (SDF) agar font tidak blur.");
        }

        private static FontStyles ToTmpFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static void UpdateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            scenes.RemoveAll(s => s.path == MAIN_MENU_SCENE_PATH || s.path == OPENING_CUTSCENE_SCENE_PATH);
            scenes.Insert(0, new EditorBuildSettingsScene(OPENING_CUTSCENE_SCENE_PATH, true));
            scenes.Insert(0, new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true));

            bool hasGameplay = scenes.Any(s => s.path == GAMEPLAY_SCENE_PATH);
            if (!hasGameplay && File.Exists(GAMEPLAY_SCENE_PATH))
            {
                scenes.Add(new EditorBuildSettingsScene(GAMEPLAY_SCENE_PATH, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
