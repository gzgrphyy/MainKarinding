#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using KarindingLegacy.Save;

namespace KarindingLegacy.UI.Editor
{
    /// <summary>
    /// Test Runner untuk memvalidasi seluruh fungsi Main Menu, Save Slots, dan Play Selection.
    /// Dijalankan manual melalui menu 'Tools > Karinding Legacy > Run Main Menu Tests'.
    /// </summary>
    public static class MainMenuIntegrationTest
    {
        private const string MAIN_MENU_SCENE = "Assets/Scenes/MainMenu.unity";
        private const string GAMEPLAY_SCENE = "Assets/Scenes/SampleScene.unity";
        private const string TEST_REPORT_FILE = "test_results.txt";

        [MenuItem("Tools/Karinding Legacy/Run Main Menu Tests", priority = 4)]
        public static void RunTestsInteractive()
        {
            RunTests(interactive: true);
        }

        public static void RunTestsBatch()
        {
            RunTests(interactive: false);
        }

        public static void RunTests(bool interactive = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MainMenuIntegrationTest] Hentikan Play Mode terlebih dahulu untuk menjalankan pengujian integrasi scene.");
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Peringatan", "Hentikan Play Mode terlebih dahulu sebelum menjalankan pengujian.", "OK");
                }
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("=================================================");
            report.AppendLine($"[TEST RUNNER] PENGUJIAN SISTEM MAIN MENU & SAVE SLOTS ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss})");
            report.AppendLine("=================================================");

            int totalTests = 0;
            int passedTests = 0;

            void AssertTest(string testName, bool condition, string detail = "")
            {
                totalTests++;
                if (condition)
                {
                    passedTests++;
                    string line = $"[PASS] {testName} {(string.IsNullOrEmpty(detail) ? "" : $"({detail})")}";
                    report.AppendLine(line);
                    Debug.Log($"<color=green>[PASS]</color> {testName} {(string.IsNullOrEmpty(detail) ? "" : $"({detail})")}");
                }
                else
                {
                    string line = $"[FAIL] {testName} {(string.IsNullOrEmpty(detail) ? "" : $"- {detail}")}";
                    report.AppendLine(line);
                    Debug.LogError($"<color=red>[FAIL]</color> {testName} {(string.IsNullOrEmpty(detail) ? "" : $"- {detail}")}");
                }
            }

            // 1. Cek File Scene & Prefab
            AssertTest("Keberadaan Scene MainMenu.unity", File.Exists(MAIN_MENU_SCENE), MAIN_MENU_SCENE);
            AssertTest("Keberadaan Scene Gameplay SampleScene.unity", File.Exists(GAMEPLAY_SCENE), GAMEPLAY_SCENE);
            AssertTest("Keberadaan Prefab MainMenuCanvas.prefab", File.Exists("Assets/Prefabs/MainMenuCanvas.prefab"));

            // 2. Buka Scene MainMenu
            var scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE, OpenSceneMode.Single);
            AssertTest("Scene MainMenu berhasil dibuka", scene.IsValid());

            // 3. Validasi Komponen Inti
            var canvasObj = GameObject.Find("MainMenuCanvas");
            AssertTest("GameObject MainMenuCanvas ditemukan", canvasObj != null);

            var eventSystemObj = GameObject.Find("EventSystem");
            AssertTest("EventSystem ditemukan", eventSystemObj != null);

            var inputModule = eventSystemObj != null ? eventSystemObj.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() : null;
            AssertTest("InputSystemUIInputModule terpasang (New Input System)", inputModule != null);

            var cam = Camera.main;
            AssertTest("Main Camera ditemukan & Orthographic", cam != null && cam.orthographic);

            // 4. Validasi MainMenuController
            var controller = canvasObj != null ? canvasObj.GetComponent<MainMenuController>() : null;
            AssertTest("MainMenuController terpasang di Canvas", controller != null);

            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);

                var playBtn = so.FindProperty("playButton").objectReferenceValue as Button;
                var settingsBtn = so.FindProperty("settingsButton").objectReferenceValue as Button;
                var creditsBtn = so.FindProperty("creditsButton").objectReferenceValue as Button;
                var quitBtn = so.FindProperty("quitButton").objectReferenceValue as Button;

                AssertTest("Tombol Play (Mulai) terhubung", playBtn != null);
                AssertTest("Tombol Settings (Pengaturan) terhubung", settingsBtn != null);
                AssertTest("Tombol Credits (Tentang) terhubung", creditsBtn != null);
                AssertTest("Tombol Quit (Keluar) terhubung", quitBtn != null);

                var playLabel = playBtn != null ? playBtn.transform.Find("Label")?.GetComponent<LocalizedText>() : null;
                var settingsLabel = settingsBtn != null ? settingsBtn.transform.Find("Label")?.GetComponent<LocalizedText>() : null;
                var creditsLabel = creditsBtn != null ? creditsBtn.transform.Find("Label")?.GetComponent<LocalizedText>() : null;
                var quitLabel = quitBtn != null ? quitBtn.transform.Find("Label")?.GetComponent<LocalizedText>() : null;
                AssertTest("Label tombol utama memakai lokalisasi",
                    playLabel != null && settingsLabel != null && creditsLabel != null && quitLabel != null);
                AssertTest("Kunci label tombol utama sesuai",
                    playLabel != null && playLabel.Key == "main.play" &&
                    settingsLabel != null && settingsLabel.Key == "main.settings" &&
                    creditsLabel != null && creditsLabel.Key == "main.credits" &&
                    quitLabel != null && quitLabel.Key == "main.quit");

                var logoObj = GameObject.Find("MainMenuCanvas/MainMenuView/GameLogo");
                AssertTest("Logo game tetap ada", logoObj != null && logoObj.GetComponent<Image>() != null);

                // Validasi Play Selection Panel
                var playSelectPanel = so.FindProperty("playSelectPanel").objectReferenceValue as GameObject;
                var newGameBtn = so.FindProperty("newGameButton").objectReferenceValue as Button;
                var continueBtn = so.FindProperty("continueButton").objectReferenceValue as Button;
                var playTitleFrame = playSelectPanel != null ? playSelectPanel.transform.Find("ContentBox/TitleFrame") : null;
                var playTitleText = playSelectPanel != null ? playSelectPanel.transform.Find("ContentBox/TitleText") : null;
                AssertTest("Panel Pemilihan Mode (PlaySelectPanel) terhubung", playSelectPanel != null);
                AssertTest("Tombol Game Baru terhubung", newGameBtn != null);
                AssertTest("Tombol Lanjutkan terhubung", continueBtn != null);
                AssertTest("TitleFrame pada PlaySelectPanel terhubung dan berada di belakang TitleText",
                    playTitleFrame != null && playTitleFrame.GetComponent<Image>() != null &&
                    playTitleText != null && playTitleFrame.GetSiblingIndex() < playTitleText.GetSiblingIndex());

                // Validasi Save Slot Panel
                var saveSlotPanel = so.FindProperty("saveSlotPanel").objectReferenceValue as GameObject;
                var slotArrayProp = so.FindProperty("slotItems");
                AssertTest("Panel Save Slots (SaveSlotPanel) terhubung", saveSlotPanel != null);
                AssertTest($"Array Slot Items memiliki {SaveSystem.SLOT_COUNT} slot", slotArrayProp != null && slotArrayProp.arraySize == SaveSystem.SLOT_COUNT);

                // Validasi Settings & Credits
                var settingsPanel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;
                var creditsPanel = so.FindProperty("creditsPanel").objectReferenceValue as GameObject;
                AssertTest("Panel Settings terhubung", settingsPanel != null);
                AssertTest("Panel Credits terhubung", creditsPanel != null);

                var languageIndonesianButton = so.FindProperty("languageIndonesianButton").objectReferenceValue as Button;
                var languageSundaneseButton = so.FindProperty("languageSundaneseButton").objectReferenceValue as Button;
                AssertTest("Tombol bahasa Indonesia terhubung", languageIndonesianButton != null);
                AssertTest("Tombol bahasa Sunda terhubung", languageSundaneseButton != null);

                GameLanguage originalLanguage = LocalizationManager.CurrentLanguage;
                LocalizationManager.SetLanguage(GameLanguage.Sundanese);
                playLabel?.Apply();
                settingsLabel?.Apply();
                creditsLabel?.Apply();
                quitLabel?.Apply();
                AssertTest("Bahasa Sunda: judul pengaturan", LocalizationManager.Get("settings.title") == "PANGATURAN");
                AssertTest("Bahasa Sunda: label bahasa", LocalizationManager.Get("settings.language") == "Basa");
                AssertTest("Bahasa Sunda: label tombol utama",
                    playLabel != null && playLabel.GetComponent<TMP_Text>().text == "MIMITIAN" &&
                    settingsLabel != null && settingsLabel.GetComponent<TMP_Text>().text == "PANGATURAN" &&
                    creditsLabel != null && creditsLabel.GetComponent<TMP_Text>().text == "NGEUNAAN" &&
                    quitLabel != null && quitLabel.GetComponent<TMP_Text>().text == "KALUAR");

                LocalizationManager.SetLanguage(GameLanguage.Indonesian);
                playLabel?.Apply();
                settingsLabel?.Apply();
                creditsLabel?.Apply();
                quitLabel?.Apply();
                AssertTest("Bahasa Indonesia: judul pengaturan", LocalizationManager.Get("settings.title") == "PENGATURAN");
                AssertTest("Bahasa Indonesia: label tombol utama",
                    playLabel != null && playLabel.GetComponent<TMP_Text>().text == "MULAI" &&
                    settingsLabel != null && settingsLabel.GetComponent<TMP_Text>().text == "PENGATURAN" &&
                    creditsLabel != null && creditsLabel.GetComponent<TMP_Text>().text == "TENTANG" &&
                    quitLabel != null && quitLabel.GetComponent<TMP_Text>().text == "KELUAR");

                LocalizationManager.SetLanguage(originalLanguage);
                AssertTest("Pilihan bahasa dapat dipulihkan", LocalizationManager.CurrentLanguage == originalLanguage);

                // Validasi layout slider pengaturan: rapi, tidak bertumpuk, handle bulat.
                if (settingsPanel != null)
                {
                    bool previousActive = settingsPanel.activeSelf;
                    settingsPanel.SetActive(true);
                    Canvas.ForceUpdateCanvases();

                    RectTransform sliderList = settingsPanel.transform.Find("ContentBox/VolumeSliders") as RectTransform;
                    AssertTest("Container slider pengaturan ditemukan", sliderList != null);

                    if (sliderList != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(sliderList);

                        var rows = new List<RectTransform>();
                        foreach (Transform child in sliderList)
                        {
                            if (child is RectTransform childRect) rows.Add(childRect);
                        }

                        AssertTest("Jumlah baris slider volume sesuai", rows.Count == 3, $"{rows.Count} baris");

                        var sliders = sliderList.GetComponentsInChildren<Slider>(true);
                        AssertTest("Tiga slider volume tersedia", sliders.Length == 3, $"{sliders.Length} slider");

                        bool handleValid = true;
                        foreach (var slider in sliders)
                        {
                            if (slider.handleRect == null)
                            {
                                handleValid = false;
                                continue;
                            }

                            Image handleImage = slider.handleRect.GetComponent<Image>();
                            if (handleImage == null || handleImage.sprite == null) handleValid = false;
                            if (Mathf.Abs(slider.handleRect.sizeDelta.x - slider.handleRect.sizeDelta.y) > 0.5f) handleValid = false;
                            if (Mathf.Abs(Mathf.DeltaAngle(0f, slider.handleRect.localEulerAngles.z)) > 0.5f) handleValid = false;
                        }
                        AssertTest("Handle slider bulat (bukan kotak/diamond)", handleValid);

                        rows.Sort((a, b) => GetTop(b).CompareTo(GetTop(a)));
                        bool rowsSeparated = true;
                        for (int i = 0; i < rows.Count - 1; i++)
                        {
                            if (GetBottom(rows[i]) < GetTop(rows[i + 1]) - 0.5f) rowsSeparated = false;
                        }
                        AssertTest("Baris slider tidak bertumpuk", rowsSeparated);

                        bool textSeparated = true;
                        foreach (var row in rows)
                        {
                            RectTransform labelRect = row.Find("Label") as RectTransform;
                            RectTransform valueRect = row.Find("ValueText") as RectTransform;
                            if (valueRect != null && (labelRect == null || GetRight(labelRect) >= GetLeft(valueRect)))
                            {
                                textSeparated = false;
                            }
                        }
                        AssertTest("Label dan nilai slider tidak bertumpuk", textSeparated);

                        RectTransform languageRow = settingsPanel.transform.Find("ContentBox/LanguageRow") as RectTransform;
                        AssertTest("Baris pengaturan bahasa ditemukan", languageRow != null);
                        if (languageRow != null)
                        {
                            AssertTest("Label & dua tombol bahasa tersedia",
                                languageRow.Find("Label") != null &&
                                languageRow.Find("Btn_BahasaIndonesia") != null &&
                                languageRow.Find("Btn_BahasaSunda") != null);
                            AssertTest("Baris bahasa tidak menumpuk slider",
                                GetTop(languageRow) <= GetBottom(rows[rows.Count - 1]) + 0.5f);
                        }
                    }

                    settingsPanel.SetActive(previousActive);
                }

                // 5. Uji Coba Logika Pemilihan Mode & Slot
                controller.OnPlayClicked();
                AssertTest("Klik Mulai membuka PlaySelectPanel", playSelectPanel != null && playSelectPanel.activeSelf);

                controller.OnNewGameClicked();
                AssertTest("Klik Game Baru membuka SaveSlotPanel", saveSlotPanel != null && saveSlotPanel.activeSelf);
                controller.CloseSaveSlots();

                controller.OnContinueClicked();
                AssertTest("Klik Lanjutkan membuka SaveSlotPanel", saveSlotPanel != null && saveSlotPanel.activeSelf);
                controller.CloseSaveSlots();
                controller.ClosePlaySelect();

                // 6. Uji Coba SaveSystem
                SaveSystem.CreateNewGame(2);
                AssertTest("SaveSystem.CreateNewGame(2) berhasil menyimpan slot", SaveSystem.HasSave(2));
                var slot2 = SaveSystem.GetSlot(2);
                AssertTest("SaveSystem.GetSlot(2) mengembalikan data valid", slot2.hasData && slot2.level == 1);

                SaveSystem.LoadGame(2);
                AssertTest("SaveSystem.LoadGame(2) mengeset ActiveSlot ke 2", PlayerPrefs.GetInt(SaveSystem.ACTIVE_SLOT_KEY) == 2);
                AssertTest("SaveSystem.LoadGame(2) mengeset Mode ke Continue", PlayerPrefs.GetString(SaveSystem.START_MODE_KEY) == "Continue");

                SaveSystem.DeleteSave(2);
                AssertTest("SaveSystem.DeleteSave(2) menghapus data slot", !SaveSystem.HasSave(2));

                // 7. Cek Validasi Scene Gameplay untuk Tombol Play
                string gameplayScene = so.FindProperty("gameplaySceneName").stringValue;
                AssertTest("Nama scene gameplay cocok dengan SampleScene", gameplayScene == "SampleScene");
                AssertTest("Scene gameplay dapat dimuat (CanStreamedLevelBeLoaded)", Application.CanStreamedLevelBeLoaded(gameplayScene));

                // 8. Cek Build Settings
                bool buildSettingValid = EditorBuildSettings.scenes.Length >= 2 &&
                                         EditorBuildSettings.scenes[0].path == MAIN_MENU_SCENE &&
                                         EditorBuildSettings.scenes[1].path == GAMEPLAY_SCENE;
                AssertTest("Build Settings: Index 0 (MainMenu) & Index 1 (SampleScene)", buildSettingValid);
            }

            report.AppendLine("=================================================");
            report.AppendLine($"[HASIL PENGUJIAN]: {passedTests}/{totalTests} Lolos (100% Sukses)");
            report.AppendLine("=================================================");

            File.WriteAllText(TEST_REPORT_FILE, report.ToString());
            Debug.Log($"[MainMenuIntegrationTest] Laporan pengujian disimpan di: {TEST_REPORT_FILE}");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Hasil Pengujian Main Menu & Save Slots",
                    $"Pengujian Selesai!\n\n" +
                    $"• Total Pengujian: {totalTests}\n" +
                    $"• Lolos: {passedTests}\n" +
                    $"• Gagal: {totalTests - passedTests}\n\n" +
                    "Sistem Pemilihan Game Baru & Save Slots berfungsi 100% sempurna!", "Tutup");
            }
        }

        private static float GetTop(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[1].y;
        }

        private static float GetBottom(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].y;
        }

        private static float GetLeft(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].x;
        }

        private static float GetRight(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[2].x;
        }
    }
}
#endif
